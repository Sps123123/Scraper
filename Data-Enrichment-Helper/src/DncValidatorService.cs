using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Data_Enrichment_Helper;

public class DncValidatorService
{
    private const string MailTmApi = "https://api.mail.tm";
    private const string DncUrl = "https://thedncproject.org/#";
    private const int BatchSize = 10;
    private const int EmailWaitTimeoutSec = 6 * 60;

    // ✅ NEW: central constants
    private const int BatchDelayMs = 5000;   // 5s delay between batches / retries
    private const int RetryAttempts = 3;     // 3 attempts max, then skip

    // ============================================================
    // ✅ NEW: phone-cell classification
    // ============================================================
    private enum PhoneKind { Skip, Normal, ExtractOnly, Split }

    private (PhoneKind Kind, List<(string raw, string norm)> Numbers) ParsePhoneCell(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (PhoneKind.Skip, new List<(string, string)>());

        string trimmed = raw.Trim();

        // 1. Scientific notation like 1.13E+11 — skip completely
        if (Regex.IsMatch(trimmed, @"^\d+(\.\d+)?[Ee][+-]?\d+$"))
            return (PhoneKind.Skip, new List<(string, string)>());

        // 2. Extension — keep the cell text, extract only the main number
        //    e.g. "+12063290499 ext 702", "+16044694936 / Ext 1", "+13173967300 ext 136"
        if (Regex.IsMatch(trimmed, @"(?i)\bext\b"))
        {
            string beforeExt = Regex.Split(trimmed, @"(?i)\bext\b")[0].Trim().TrimEnd('/').Trim();
            string norm = Normalize(beforeExt);
            if (!string.IsNullOrEmpty(norm))
                return (PhoneKind.ExtractOnly, new List<(string, string)> { (trimmed, norm) });
            return (PhoneKind.Skip, new List<(string, string)>());
        }

        // 3. Slash-separated numbers (no "ext") — split into 2 rows
        //    e.g. "231-629-0402/231-629 0156", "(123) 555-4567/(210) 607-8995"
        if (trimmed.Contains('/'))
        {
            var parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries)
                               .Select(p => p.Trim())
                               .Where(p => !string.IsNullOrEmpty(p))
                               .ToList();

            var validParts = new List<(string raw, string norm)>();
            foreach (var p in parts)
            {
                string n = Normalize(p);
                if (!string.IsNullOrEmpty(n)) validParts.Add((p, n));
            }

            if (validParts.Count >= 2)
                return (PhoneKind.Split, validParts.Take(2).ToList());
            if (validParts.Count == 1)
                return (PhoneKind.Normal, validParts);
        }

        // 4. Normal case
        string normalized = Normalize(trimmed);
        if (!string.IsNullOrEmpty(normalized))
            return (PhoneKind.Normal, new List<(string, string)> { (trimmed, normalized) });

        return (PhoneKind.Skip, new List<(string, string)>());
    }

    // ✅ NEW: a row is "already done" if any DNC cell is non-empty
    private static bool HasDncResults(List<string> row, int[] dncIndices)
    {
        foreach (int idx in dncIndices)
            if (idx >= 0 && idx < row.Count && !string.IsNullOrWhiteSpace(row[idx]))
                return true;
        return false;
    }

    public async Task RunAsync(string inputCsvPath, string outputCsvPath,
        Action<string, System.Drawing.Color> log,
        Action<int, int> progress,
        CancellationToken token)
    {
        log("Starting DNC Validation on CSV...", System.Drawing.Color.Cyan);

        var (headers, rows) = CsvService.Read(inputCsvPath);
        if (rows.Count == 0) { log("CSV is empty.", System.Drawing.Color.Red); return; }

        int phoneIndex = headers.FindIndex(h => h.Contains("Phone", StringComparison.OrdinalIgnoreCase) || h.Contains("Number", StringComparison.OrdinalIgnoreCase));
        if (phoneIndex < 0) { log("No 'Phone' column. Using column 0.", System.Drawing.Color.Orange); phoneIndex = 0; }

        string[] dncCols = { "ON DNC", "IS MOBILE", "ON STATE", "IS LITIGATOR" };
        int[] dncIndices = new int[4];
        for (int i = 0; i < 4; i++)
        {
            int idx = headers.FindIndex(h => h.Trim().Equals(dncCols[i], StringComparison.OrdinalIgnoreCase));
            if (idx < 0) { headers.Add(dncCols[i]); idx = headers.Count - 1; }
            dncIndices[i] = idx;
        }

        foreach (var row in rows) while (row.Count < headers.Count) row.Add("");

        // ============================================================
        // ✅ NEW: Expand slash-separated phones into extra rows + pre-parse
        // ============================================================
        var expandedRows = new List<List<string>>();
        var normDigits   = new List<string>();   // per-row DNC digits ("" = skip)
        var skippedRow   = new List<bool>();
        var alreadyDone  = new List<bool>();     // ✅ NEW: row already has DNC results

        foreach (var original in rows)
        {
            var parsed  = ParsePhoneCell(original[phoneIndex]);
            bool hasDnc = HasDncResults(original, dncIndices);   // ✅ NEW

            if (parsed.Kind == PhoneKind.Skip)
            {
                expandedRows.Add(original);
                normDigits.Add("");
                skippedRow.Add(true);
                alreadyDone.Add(hasDnc);                          // ✅ NEW
            }
            else if (parsed.Kind == PhoneKind.Split)
            {
                // Original row keeps the first number
                var row1 = new List<string>(original);
                row1[phoneIndex] = parsed.Numbers[0].raw;
                expandedRows.Add(row1);
                normDigits.Add(parsed.Numbers[0].norm);
                skippedRow.Add(false);
                alreadyDone.Add(hasDnc);                          // ✅ NEW

                // Brand-new row inserted right after, keeps second number
                var row2 = new List<string>(original);
                row2[phoneIndex] = parsed.Numbers[1].raw;
                expandedRows.Add(row2);
                normDigits.Add(parsed.Numbers[1].norm);
                skippedRow.Add(false);
                alreadyDone.Add(hasDnc);                          // ✅ NEW
            }
            else // Normal or ExtractOnly
            {
                expandedRows.Add(original);
                normDigits.Add(parsed.Numbers[0].norm);
                skippedRow.Add(false);
                alreadyDone.Add(hasDnc);                          // ✅ NEW
            }
        }

        rows = expandedRows;

        int alreadyDoneCount = alreadyDone.Count(x => x);         // ✅ NEW
        if (alreadyDoneCount > 0)
            log($"Skipping {alreadyDoneCount} rows that already have DNC results.", System.Drawing.Color.LightGray);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-blink-features=AutomationControlled", "--no-sandbox" }
        });

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        httpClient.Timeout = TimeSpan.FromSeconds(60);

        int totalToProcess = rows.Count;
        int processedCount = 0;
        progress(processedCount, totalToProcess);

        // ✅ Tracks every row that got a real DNC result during the main pass
        var successfulIndices = new HashSet<int>();

        // ============================================================
        // MAIN PASS
        // ============================================================
        using (var writer = CsvService.CreateWriter(outputCsvPath))
        {
            writer.WriteLine(string.Join(",", headers.Select(CsvService.Escape)));
            writer.Flush();

            int nextRowToWrite = 0;
            var batchNums = new List<string>();
            var batchIndices = new List<int>();

            for (int i = 0; i < rows.Count; i++)
            {
                if (token.IsCancellationRequested) break;

                // ✅ NEW: skip rows that already have DNC results — leave them untouched
                if (alreadyDone[i])
                {
                    processedCount++;
                    continue;
                }

                string norm = normDigits[i];   // ✅ CHANGED: per-row pre-parsed digits

                if (!string.IsNullOrEmpty(norm)) { batchNums.Add(norm); batchIndices.Add(i); }
                else { foreach (int idx in dncIndices) rows[i][idx] = ""; }

                if (batchNums.Count == BatchSize)
                {
                    var result = await ProcessBatchWithRetryAsync(browser, httpClient, batchNums, batchIndices, rows, dncIndices, log, token);
                    if (result.success)
                        foreach (var idx in result.succeededIndices) successfulIndices.Add(idx);

                    int maxIdx = batchIndices.Last();
                    for (int j = nextRowToWrite; j <= maxIdx; j++)
                    {
                        writer.WriteLine(string.Join(",", rows[j].Select(CsvService.Escape)));
                        writer.Flush();
                    }
                    nextRowToWrite = maxIdx + 1;

                    processedCount += batchNums.Count;
                    progress(processedCount, totalToProcess);

                    batchNums.Clear();
                    batchIndices.Clear();

                    // ✅ 5-second pause between batches
                    await Task.Delay(BatchDelayMs, token);
                }
            }

            if (batchNums.Count > 0 && !token.IsCancellationRequested)
            {
                var result = await ProcessBatchWithRetryAsync(browser, httpClient, batchNums, batchIndices, rows, dncIndices, log, token);
                if (result.success)
                    foreach (var idx in result.succeededIndices) successfulIndices.Add(idx);
            }

            for (int j = nextRowToWrite; j < rows.Count; j++)
            {
                writer.WriteLine(string.Join(",", rows[j].Select(CsvService.Escape)));
                writer.Flush();
            }
        }

        processedCount = totalToProcess;
        progress(processedCount, totalToProcess);

        // ============================================================
        // ✅ RETRY PASS: scan for missing → rerun in batches of 10
        // ============================================================
        if (!token.IsCancellationRequested)
        {
            var missingIndices = new List<int>();
            for (int i = 0; i < rows.Count; i++)
            {
                if (skippedRow[i] || alreadyDone[i]) continue;   // ✅ NEW: also skip alreadyDone
                string norm = normDigits[i];   // ✅ CHANGED: use pre-parsed digits
                if (!string.IsNullOrEmpty(norm) && !successfulIndices.Contains(i))
                    missingIndices.Add(i);
            }

            if (missingIndices.Count > 0)
            {
                log($"\n═══ RETRY PASS: {missingIndices.Count} rows missing DNC results ═══", System.Drawing.Color.Orange);

                var retryNums = new List<string>();
                var retryIndices = new List<int>();

                foreach (var idx in missingIndices)
                {
                    if (token.IsCancellationRequested) break;

                    retryNums.Add(normDigits[idx]);   // ✅ CHANGED
                    retryIndices.Add(idx);

                    if (retryNums.Count == BatchSize)
                    {
                        await ProcessBatchWithRetryAsync(browser, httpClient, retryNums, retryIndices, rows, dncIndices, log, token);
                        retryNums.Clear();
                        retryIndices.Clear();

                        // ✅ 5-second pause between retry batches
                        await Task.Delay(BatchDelayMs, token);
                    }
                }

                if (retryNums.Count > 0 && !token.IsCancellationRequested)
                    await ProcessBatchWithRetryAsync(browser, httpClient, retryNums, retryIndices, rows, dncIndices, log, token);

                // ✅ Rewrite the entire CSV so retry results land in the SAME positions
                //    and no other column/row is disturbed.
                log("Rewriting final CSV with retry results...", System.Drawing.Color.LightGray);
                var tempPath = outputCsvPath + ".tmp";
                using (var finalWriter = CsvService.CreateWriter(tempPath))
                {
                    finalWriter.WriteLine(string.Join(",", headers.Select(CsvService.Escape)));
                    foreach (var row in rows)
                        finalWriter.WriteLine(string.Join(",", row.Select(CsvService.Escape)));
                    finalWriter.Flush();
                }
                if (File.Exists(outputCsvPath)) File.Delete(outputCsvPath);
                File.Move(tempPath, outputCsvPath);
            }
            else
            {
                log("No missing rows — every valid phone received DNC results.", System.Drawing.Color.Green);
            }
        }

        log("DNC Validation Complete!", System.Drawing.Color.Green);
    }

    // ============================================================
    // Retry wrapper — up to 3 attempts, then skip
    // ============================================================
    private async Task<(bool success, List<int> succeededIndices)> ProcessBatchWithRetryAsync(
        IBrowser browser, HttpClient httpClient,
        List<string> batchNums, List<int> batchIndices,
        List<List<string>> rows, int[] dncIndices,
        Action<string, System.Drawing.Color> log, CancellationToken token)
    {
        var batchSucceeded = new List<int>();

        for (int attempt = 1; attempt <= RetryAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();

            var (success, succeeded) = await ProcessBatchCoreAsync(
                browser, httpClient, batchNums, batchIndices, rows, dncIndices, log, token,
                existingSucceeded: batchSucceeded);

            if (succeeded != null)
            {
                foreach (var idx in succeeded)
                    if (!batchSucceeded.Contains(idx))
                        batchSucceeded.Add(idx);
            }

            if (success)
                return (true, batchSucceeded);

            if (attempt < RetryAttempts)
            {
                log($"  ⚠ Batch attempt {attempt}/{RetryAttempts} failed. Retrying in {BatchDelayMs / 1000}s...", System.Drawing.Color.Orange);
                await Task.Delay(BatchDelayMs, token); // ✅ 5s between retries
            }
        }

        log($"  ⏭ Batch skipped after {RetryAttempts} attempts.", System.Drawing.Color.Red);
        return (batchSucceeded.Count > 0, batchSucceeded);
    }

    private async Task<(bool success, List<int> succeededIndices)> ProcessBatchCoreAsync(
        IBrowser browser, HttpClient httpClient,
        List<string> batchNums, List<int> batchIndices,
        List<List<string>> rows, int[] dncIndices,
        Action<string, System.Drawing.Color> log, CancellationToken token,
        List<int> existingSucceeded)
    {
        var succeededIndices = new List<int>();
        log($"--- Processing Batch of {batchNums.Count} valid numbers ---", System.Drawing.Color.LightGray);

        var inbox = await CreateMailTmInboxAsync(httpClient, log, token);
        if (inbox == null)
        {
            log("  ⏭ Skipping batch — inbox could not be created.", System.Drawing.Color.Orange);
            foreach (int rowIdx in batchIndices)
                if (!existingSucceeded.Contains(rowIdx))
                    foreach (int idx in dncIndices) rows[rowIdx][idx] = "";
            return (false, succeededIndices);
        }

        var (email, tokenAuth) = inbox.Value;
        log($"Using email: {email}", System.Drawing.Color.LightGray);

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
            Locale = "en-US"
        });
        context.SetDefaultTimeout(60000);

        try
        {
            await SubmitDncFormAsync(context, email, batchNums, log);
            var (mailId, mailBody) = await WaitForMailTmEmailAsync(httpClient, tokenAuth, token);

            if (string.IsNullOrEmpty(mailBody))
            {
                log("Email timeout. Leaving DNC columns empty.", System.Drawing.Color.Red);
                foreach (int rowIdx in batchIndices)
                    if (!existingSucceeded.Contains(rowIdx))
                        foreach (int idx in dncIndices) rows[rowIdx][idx] = "";
                return (false, succeededIndices);
            }

            var resultsDict = ParseEmailHtml(mailBody);

            for (int i = 0; i < batchIndices.Count; i++)
            {
                int rowIndex = batchIndices[i];
                string num = batchNums[i];

                if (resultsDict.TryGetValue(num, out var cols))
                {
                    rows[rowIndex][dncIndices[0]] = cols[0];
                    rows[rowIndex][dncIndices[1]] = cols[1];
                    rows[rowIndex][dncIndices[2]] = cols[2];
                    rows[rowIndex][dncIndices[3]] = cols[3];
                    succeededIndices.Add(rowIndex);
                }
                else if (!existingSucceeded.Contains(rowIndex))
                {
                    foreach (int idx in dncIndices) rows[rowIndex][idx] = "";
                }
            }

            if (!string.IsNullOrEmpty(mailId))
                await DeleteMailTmEmailAsync(httpClient, tokenAuth, mailId);

            log($"Batch complete. Updated {succeededIndices.Count}/{batchIndices.Count} rows.", System.Drawing.Color.Green);

            return (succeededIndices.Count > 0, succeededIndices);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log($"Batch Error: {ex.Message}", System.Drawing.Color.Red);
            foreach (int rowIdx in batchIndices)
                if (!existingSucceeded.Contains(rowIdx))
                    foreach (int idx in dncIndices) rows[rowIdx][idx] = "";
            return (false, succeededIndices);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    private string Normalize(string val)
    {
        var digits = new string(val.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && digits.StartsWith("1")) digits = digits.Substring(1);
        return digits.Length == 10 ? digits : "";
    }

    // ============================================================
    // MAIL.TM — ✅ 3 attempts, 5s delay between them
    // ============================================================
    private async Task<(string email, string token)?> CreateMailTmInboxAsync(
        HttpClient http, Action<string, System.Drawing.Color> log, CancellationToken token)
    {
        for (int attempt = 1; attempt <= RetryAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                var domainsResp = await http.GetAsync($"{MailTmApi}/domains", token);
                if (!domainsResp.IsSuccessStatusCode)
                {
                    log($"  ⚠ Attempt {attempt}/{RetryAttempts}: domains HTTP {domainsResp.StatusCode}", System.Drawing.Color.Orange);
                    if (attempt < RetryAttempts) await Task.Delay(BatchDelayMs, token);
                    continue;
                }

                string domainsJson = await domainsResp.Content.ReadAsStringAsync(token);
                var domainsData = JsonSerializer.Deserialize<MailTmDomainsResponse>(domainsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var domain = domainsData?.HydraMember?.FirstOrDefault(d => d.IsActive && !d.IsPrivate)
                          ?? domainsData?.HydraMember?.FirstOrDefault();

                if (domain == null || string.IsNullOrEmpty(domain.Domain))
                {
                    log($"  ⚠ Attempt {attempt}/{RetryAttempts}: no usable domain returned", System.Drawing.Color.Orange);
                    if (attempt < RetryAttempts) await Task.Delay(BatchDelayMs, token);
                    continue;
                }

                string username = RandomString(10);
                string password = RandomString(12);
                string email = $"{username}@{domain.Domain}";

                var createResp = await http.PostAsync($"{MailTmApi}/accounts",
                    new StringContent(JsonSerializer.Serialize(new { address = email, password }), Encoding.UTF8, "application/json"), token);

                if (!createResp.IsSuccessStatusCode)
                {
                    log($"  ⚠ Attempt {attempt}/{RetryAttempts}: account create HTTP {createResp.StatusCode}", System.Drawing.Color.Orange);
                    if (attempt < RetryAttempts) await Task.Delay(BatchDelayMs, token);
                    continue;
                }

                var tokenResp = await http.PostAsync($"{MailTmApi}/token",
                    new StringContent(JsonSerializer.Serialize(new { address = email, password }), Encoding.UTF8, "application/json"), token);

                if (!tokenResp.IsSuccessStatusCode)
                {
                    log($"  ⚠ Attempt {attempt}/{RetryAttempts}: token HTTP {tokenResp.StatusCode}", System.Drawing.Color.Orange);
                    if (attempt < RetryAttempts) await Task.Delay(BatchDelayMs, token);
                    continue;
                }

                string tokenJson = await tokenResp.Content.ReadAsStringAsync(token);
                var tokenData = JsonSerializer.Deserialize<MailTmTokenResponse>(tokenJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!string.IsNullOrEmpty(tokenData?.Token))
                    return (email, tokenData.Token);

                log($"  ⚠ Attempt {attempt}/{RetryAttempts}: no token in response", System.Drawing.Color.Orange);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                log($"  ⚠ Attempt {attempt}/{RetryAttempts} exception: {ex.Message}", System.Drawing.Color.Orange);
            }

            if (attempt < RetryAttempts) await Task.Delay(BatchDelayMs, token);
        }

        return null;
    }

    private string RandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var rnd = new Random();
        return new string(Enumerable.Repeat(chars, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
    }

    private async Task<(string id, string body)> WaitForMailTmEmailAsync(HttpClient http, string token, CancellationToken cts)
    {
        var start = DateTime.Now;
        while ((DateTime.Now - start).TotalSeconds < EmailWaitTimeoutSec)
        {
            cts.ThrowIfCancellationRequested();

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"{MailTmApi}/messages");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var resp = await http.SendAsync(req, cts);

                if (resp.IsSuccessStatusCode)
                {
                    string msgsJson = await resp.Content.ReadAsStringAsync(cts);
                    var data = JsonSerializer.Deserialize<MailTmMessagesResponse>(msgsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var msg = data?.HydraMember?.FirstOrDefault();
                    if (msg != null)
                    {
                        var bodyReq = new HttpRequestMessage(HttpMethod.Get, $"{MailTmApi}/messages/{msg.Id}");
                        bodyReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                        var bodyResp = await http.SendAsync(bodyReq, cts);
                        string bodyJson = await bodyResp.Content.ReadAsStringAsync(cts);

                        var bodyData = JsonSerializer.Deserialize<MailTmMessageBody>(bodyJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        string html = bodyData?.Html?.FirstOrDefault() ?? bodyData?.Text ?? "";
                        return (msg.Id, html);
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }

            await Task.Delay(5000, cts);
        }
        return ("", "");
    }

    private async Task DeleteMailTmEmailAsync(HttpClient http, string token, string id)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, $"{MailTmApi}/messages/{id}");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            await http.SendAsync(req);
        }
        catch { }
    }

    private async Task SubmitDncFormAsync(IBrowserContext context, string email, List<string> numbers, Action<string, System.Drawing.Color> log)
    {
        var page = await context.NewPageAsync();
        await page.GotoAsync(DncUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.WaitForSelectorAsync("input[name='email']", new PageWaitForSelectorOptions { Timeout = 30000 });
        await page.Locator("input[name='email']").First.FillAsync(email);

        var numberInputSelector = "#test-numbers-form input[placeholder*='555'], #test-numbers-form input[type='tel'], #test-numbers-form input.form-control:not([name='email']):not([type='checkbox'])";
        var plusBtn = page.Locator("#test-numbers-form button:has-text('+')").First;

        for (int i = 0; i < numbers.Count; i++)
        {
            var inputs = page.Locator(numberInputSelector);
            while (await inputs.CountAsync() <= i)
            {
                await plusBtn.ClickAsync();
                await Task.Delay(500);
                inputs = page.Locator(numberInputSelector);
            }

            await inputs.Nth(i).FillAsync(numbers[i]);
            await Task.Delay(250);
        }

        var checkbox = page.Locator("#test-numbers-form input[type='checkbox']").First;
        if (!await checkbox.IsCheckedAsync()) await checkbox.CheckAsync();

        await page.Locator("#test-numbers-form button:has-text('SUBMIT')").First.ClickAsync();

        try
        {
            await page.WaitForFunctionAsync("() => document.body.innerText.toLowerCase().includes('your results have been sent')", null, new PageWaitForFunctionOptions { Timeout = 180000 });
            log("DNC submit success!", System.Drawing.Color.Green);
        }
        catch { log("Submit success text not detected, checking email anyway.", System.Drawing.Color.Orange); }
    }

    private Dictionary<string, string[]> ParseEmailHtml(string html)
    {
        var dict = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(html)) return dict;

        var trRegex = new Regex(@"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var tdRegex = new Regex(@"<td[^>]*>(.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var tagRegex = new Regex(@"<[^>]+>");

        foreach (Match tr in trRegex.Matches(html))
        {
            var tds = tdRegex.Matches(tr.Groups[1].Value);
            if (tds.Count == 5)
            {
                var vals = tds.Cast<Match>().Select(td => tagRegex.Replace(td.Groups[1].Value, "").Trim()).ToArray();
                var num = new string(vals[0].Where(char.IsDigit).ToArray());
                if (num.Length == 11 && num.StartsWith("1")) num = num.Substring(1);
                if (num.Length == 10) dict[num] = new[] { vals[1], vals[2], vals[3], vals[4] };
            }
        }
        return dict;
    }

    // ============================================================
    // JSON MODELS
    // ============================================================
    private class MailTmDomainsResponse
    {
        [JsonPropertyName("hydra:member")]
        public List<MailTmDomain>? HydraMember { get; set; }
    }

    private class MailTmDomain
    {
        [JsonPropertyName("domain")]
        public string Domain { get; set; } = "";
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        [JsonPropertyName("isPrivate")]
        public bool IsPrivate { get; set; }
    }

    private class MailTmTokenResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = "";
    }

    private class MailTmMessagesResponse
    {
        [JsonPropertyName("hydra:member")]
        public List<MailTmMessage>? HydraMember { get; set; }
    }

    private class MailTmMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
    }

    private class MailTmMessageBody
    {
        [JsonPropertyName("html")]
        public List<string>? Html { get; set; }
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}