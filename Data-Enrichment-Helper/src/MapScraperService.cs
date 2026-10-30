// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Text.Json;
// using System.Text.RegularExpressions;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Playwright;

// namespace Data_Enrichment_Helper;

// public class MapScraperService
// {
//     public async Task ScrapeMultipleAsync(List<string> queries, string outputPath, 
//         Action<string, System.Drawing.Color> log, 
//         Action<int, int, int, int> progress, 
//         CancellationToken token)
//     {
//         log("Initializing Playwright driver...", System.Drawing.Color.Cyan);
        
//         using var playwright = await Playwright.CreateAsync();

//         // ✅ OPTIMIZATION: Open the writer ONCE for the entire batch, not per row.
//         using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(true));
//         writer.WriteLine("Website URL,Company Name,Phone Number,Category,Keyword");
//         writer.Flush(); // Save header immediately

//         int totalDone = 0;
//         int totalExtracted = 0;
//         int totalFailed = 0;
//         progress(0, queries.Count, 0, 0);

//         foreach (var query in queries)
//         {
//             if (token.IsCancellationRequested) break;
            
//             int extracted = 0;
//             try
//             {
//                 // Launch a fresh headed browser per query with Python-equivalent stealth args
//                 await using (var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
//                 { 
//                     Headless = false,
//                     Args = new[] { 
//                         "--disable-blink-features=AutomationControlled", 
//                         "--disable-features=IsolateOrigins,site-per-process",
//                         "--no-sandbox", 
//                         "--disable-dev-shm-usage", 
//                         "--disable-gpu",
//                         "--memory-pressure-off",
//                         "--max_old_space_size=512",
//                         "--window-size=1920,1080",
//                         "--lang=en-US",
//                         "--no-zygote",
//                         "--disable-background-networking",
//                         "--disable-sync",
//                         "--disable-default-apps",
//                         "--safebrowsing-disable-auto-update",
//                         "--disable-software-rasterizer"
//                     }
//                 }))
//                 {
//                     extracted = await ProcessSingleQueryAsync(browser, query, writer, log, token);
//                 } // Browser is disposed here
//             }
//             catch (Exception ex)
//             {
//                 log($"  ❌ Error processing query '{query}': {ex.Message}", System.Drawing.Color.Red);
//             }

//             totalExtracted += extracted;
//             if (extracted == 0) totalFailed++;
            
//             totalDone++;
//             progress(totalDone, queries.Count, totalExtracted, totalFailed);

//             // ✅ GARBAGE COLLECTION: Force cleanup of Chromium/Node.js unmanaged resources
//             GC.Collect();
//             GC.WaitForPendingFinalizers();
//             GC.Collect();
//         }

//         log($"All queries complete! Total extracted: {totalExtracted}", System.Drawing.Color.SpringGreen);
//     }

//     private async Task<int> ProcessSingleQueryAsync(IBrowser browser, string query, StreamWriter writer, Action<string, System.Drawing.Color> log, CancellationToken token)
//     {
//         log($"Navigating: {query}", System.Drawing.Color.LightGray);

//         await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
//         {
//             UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
//             ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
//             Locale = "en-US",
//             TimezoneId = "America/New_York",
//             Geolocation = new Geolocation { Latitude = 40.7128f, Longitude = -74.0060f },
//             Permissions = new[] { "geolocation" }
//         });
        
//         var page = await context.NewPageAsync();

//         try
//         {
//             // 1. Navigate to Maps Home
//             try
//             {
//                 await page.GotoAsync("https://www.google.com/maps", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });
//             }
//             catch (Exception ex)
//             {
//                 log($"  ⚠ Navigation failed: {ex.Message}", System.Drawing.Color.Orange);
//                 return 0;
//             }

//             // 2. Check for Captcha
//             if (await CheckCaptchaAsync(page))
//             {
//                 log($"  ⚠ CAPTCHA detected on Maps homepage!", System.Drawing.Color.Red);
//                 return 0;
//             }

//             await Task.Delay(1000, token);

//             // 3. Type Search Query
//             try
//             {
//                 await page.WaitForSelectorAsync("input[name='q']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });
//                 await page.FillAsync("input[name='q']", query, new PageFillOptions { Timeout = 30000 });
//                 await page.Keyboard.PressAsync("Enter");
//             }
//             catch (Exception ex)
//             {
//                 log($"  ❌ Search box interaction failed: {ex.Message}", System.Drawing.Color.Red);
//                 return 0;
//             }

//             // 4. Wait for Results
//             try
//             {
//                 await page.WaitForSelectorAsync("a[href*='https://www.google.com/maps/place']", new PageWaitForSelectorOptions { Timeout = 60000 });
//             }
//             catch (Exception)
//             {
//                 log($"  ❌ No results feed found for: {query}", System.Drawing.Color.Red);
//                 return 0;
//             }

//             // 5. Captcha check after search
//             if (await CheckCaptchaAsync(page))
//             {
//                 log($"  ⚠ CAPTCHA detected on search results!", System.Drawing.Color.Red);
//                 return 0;
//             }

//             await Task.Delay(500, token);

//             // 6. Scroll and Extract
//             return await ProcessListingsAsync(page, context, query, writer, log, token);
//         }
//         catch (Exception ex)
//         {
//             log($"  ❌ Fatal error: {ex.Message}", System.Drawing.Color.Red);
//             return 0;
//         }
//         finally 
//         { 
//             await page.CloseAsync(); 
//         }
//     }

//     private async Task<int> ProcessListingsAsync(IPage page, IBrowserContext context, string query, StreamWriter writer, Action<string, System.Drawing.Color> log, CancellationToken token)
//     {
//         int extracted = 0;
//         var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//         int noChangeCount = 0;
//         long lastHeight = 0;

//         string jsScript = @"
//             () => {
//                 try {
//                     const cards = document.querySelectorAll('div[role=""article""]');
//                     const results = [];
//                     const seenNames = new Set();

//                     for (const card of cards) {
//                         let name = null;
//                         const nameEl = card.querySelector('div.qBF1Pd.fontHeadlineSmall');
//                         if (nameEl) name = nameEl.innerText.trim();
//                         if (!name) {
//                             const alt = card.querySelector('div.fontHeadlineSmall');
//                             if (alt) name = alt.innerText.trim();
//                         }
//                         if (!name || seenNames.has(name)) continue;
//                         seenNames.add(name);

//                         let phone = null;
//                         const phoneSelectors = [
//                             'span.UsdlK',
//                             'span[aria-label*=""phone"" i]',
//                             'span[data-item-id*=""phone""]',
//                             'button[data-item-id*=""phone""] span',
//                         ];
//                         for (const sel of phoneSelectors) {
//                             const el = card.querySelector(sel);
//                             if (el && el.innerText.trim()) { phone = el.innerText.trim(); break; }
//                         }

//                         let category = null;
//                         const catEl = card.querySelector('div.W4Efsd div.W4Efsd > span > span');
//                         if (catEl) category = catEl.innerText.trim();

//                         let website = null;
//                         const webEl =
//                             card.querySelector('a[aria-label*=""Visit website""]') ||
//                             card.querySelector('a[aria-label*=""website"" i]')     ||
//                             card.querySelector('a[aria-label=""Website""]')        ||
//                             card.querySelector('a[data-value=""Website""]')        ||
//                             card.querySelector('a[title*=""website"" i]');

//                         if (webEl) {
//                             const rawHref = webEl.getAttribute('href') || webEl.href;
//                             if (rawHref) {
//                                 try {
//                                     const url = new URL(rawHref, window.location.origin);
//                                     if (!url.hostname.includes('google.')) {
//                                         website = url.toString();
//                                     } else {
//                                         const target =
//                                             url.searchParams.get('q')    ||
//                                             url.searchParams.get('url')  ||
//                                             url.searchParams.get('adurl');
//                                         if (target) {
//                                             try {
//                                                 const t = new URL(target);
//                                                 if (!t.hostname.includes('google.')) website = t.toString();
//                                             } catch(e) {}
//                                         }
//                                     }
//                                 } catch(e) {}
//                             }
//                         }

//                         // Extract the detail URL for fallback
//                         let detailUrl = null;
//                         const linkEl = card.querySelector('a.hfpxzc');
//                         if (linkEl) detailUrl = linkEl.getAttribute('href');

//                         results.push({ name, phone, category, website, detailUrl });
//                     }
//                     return results;
//                 } catch (e) {
//                     return [];
//                 }
//             }
//         ";

//         while (noChangeCount < 4 && !token.IsCancellationRequested)
//         {
//             try
//             {
//                 await page.EvaluateAsync("document.querySelector(\"div[role='feed']\")?.focus()");
//             }
//             catch { }

//             await Task.Delay(200, token);

//             List<BusinessListing>? listings = null;
//             try
//             {
//                 string jsonResult = await page.EvaluateAsync<string>($"() => JSON.stringify(({jsScript})())");
                
//                 if (!string.IsNullOrEmpty(jsonResult) && jsonResult != "null")
//                 {
//                     listings = JsonSerializer.Deserialize<List<BusinessListing>>(jsonResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
//                 }
//             }
//             catch (Exception ex)
//             {
//                 log($"  ⚠ Extraction interrupted: {ex.Message}", System.Drawing.Color.Orange);
//             }

//             if (listings != null)
//             {
//                 foreach (var item in listings)
//                 {
//                     if (item == null || string.IsNullOrEmpty(item.name)) continue;
//                     if (seenNames.Contains(item.name)) continue;

//                     // ✅ FALLBACK TRIGGER: If phone or website is missing, scrape the detail page
//                     bool needsFallback = !IsValidWebsite(item.website) || string.IsNullOrEmpty(item.phone) || item.phone == "N/A";
                    
//                     if (needsFallback && !string.IsNullOrEmpty(item.detailUrl))
//                     {
//                         log($"  ⚠ Missing info for '{item.name}'. Opening card URL...", System.Drawing.Color.Yellow);
                        
//                         var detail = await FetchDetailPageAsync(context, item.detailUrl, log, token);
//                         if (detail != null)
//                         {
//                             if (string.IsNullOrEmpty(item.phone) || item.phone == "N/A") item.phone = detail.phone;
//                             if (!IsValidWebsite(item.website)) item.website = detail.website;
//                             if (string.IsNullOrEmpty(item.category) || item.category == "N/A") item.category = detail.category;
//                         }
//                     }

//                     string normWebsite = NormalizeWebsite(item.website) ?? "N/A";
                    
//                     // ✅ Write to the shared stream buffer
//                     writer.WriteLine($"{EscapeCsv(normWebsite)},{EscapeCsv(item.name)},{EscapeCsv(item.phone ?? "N/A")},{EscapeCsv(item.category ?? "N/A")},{EscapeCsv(query)}");
                    
//                     seenNames.Add(item.name);
//                     extracted++;
//                     log($"  ✅ Extracted: {item.name} | {item.phone} | {normWebsite}", System.Drawing.Color.LightGreen);
//                 }
                
//                 // ✅ INCREMENTALLY SAVE: Flush the buffer to disk after every extraction wave
//                 writer.Flush();
//             }

//             try
//             {
//                 for (int i = 0; i < 5; i++)
//                 {
//                     await page.Keyboard.PressAsync("PageDown");
//                     await Task.Delay(150, token);
//                 }
//                 await Task.Delay(1000, token);
//             }
//             catch (Exception)
//             {
//                 break;
//             }

//             if (await page.QuerySelectorAsync("span.HlvSq") != null)
//             {
//                 break;
//             }

//             try
//             {
//                 var newHeight = await page.EvaluateAsync<long>("document.querySelector('div[role=\"feed\"]').scrollHeight");
//                 if (newHeight == lastHeight)
//                 {
//                     noChangeCount++;
//                 }
//                 else
//                 {
//                     noChangeCount = 0;
//                     lastHeight = newHeight;
//                 }
//             }
//             catch
//             {
//                 break;
//             }
//         }

//         if (extracted == 0) log($"  No results extracted.", System.Drawing.Color.Orange);
//         return extracted;
//     }

//     private async Task<BusinessListing?> FetchDetailPageAsync(IBrowserContext context, string url, Action<string, System.Drawing.Color> log, CancellationToken token)
//     {
//         IPage? detailPage = null;
//         try
//         {
//             detailPage = await context.NewPageAsync();
//             await detailPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
            
//             // Wait for the main details panel to load
//             await detailPage.WaitForTimeoutAsync(2500);

//             string detailJsScript = @"
//                 () => {
//                     try {
//                         let name = null;
//                         const nameEl = document.querySelector('h1.DuwDvf');
//                         if (nameEl) name = nameEl.innerText.trim();

//                         let category = null;
//                         const catEl = document.querySelector('button.DkEaL');
//                         if (catEl) category = catEl.innerText.trim();

//                         let phone = null;
//                         let website = null;

//                         // Look for Phone and Website in the exact classes you provided
//                         const items = document.querySelectorAll('div.Io6YTe');
//                         for (const item of items) {
//                             const text = item.innerText.trim();
//                             // Regex check for phone number format
//                             if (!phone && /^\+?[0-9\s\-()]{7,20}$/.test(text)) {
//                                 phone = text;
//                             } 
//                             // Regex check for domain format (e.g. nyccd.com)
//                             else if (!website && text.includes('.') && !text.includes(' ')) {
//                                 website = text;
//                             }
//                         }
                        
//                         // Standard Google Maps fallback attributes
//                         if (!phone) {
//                             const pEl = document.querySelector('button[data-item-id^=""phone:tel:""] div.Io6YTe');
//                             if (pEl) phone = pEl.innerText.trim();
//                         }
//                         if (!website) {
//                             const wEl = document.querySelector('a[data-item-id=""authority""] div.Io6YTe');
//                             if (wEl) website = wEl.innerText.trim();
//                         }

//                         return { name, phone, website, category };
//                     } catch (e) {
//                         return null;
//                     }
//                 }
//             ";

//             string jsonResult = await detailPage.EvaluateAsync<string>($"() => JSON.stringify(({detailJsScript})())");
            
//             if (!string.IsNullOrEmpty(jsonResult) && jsonResult != "null")
//             {
//                 return JsonSerializer.Deserialize<BusinessListing>(jsonResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
//             }
//         }
//         catch (Exception ex)
//         {
//             log($"    ⚠ Detail fallback failed: {ex.Message}", System.Drawing.Color.Orange);
//         }
//         finally
//         {
//             if (detailPage != null) await detailPage.CloseAsync();
//         }
//         return null;
//     }

//     private async Task<bool> CheckCaptchaAsync(IPage page)
//     {
//         string[] captchaSelectors = {
//             "div[aria-label='Captcha']",
//             "iframe[src*='recaptcha']",
//             "div#captcha",
//             "form#captcha-form",
//             "div.g-recaptcha"
//         };

//         foreach (var sel in captchaSelectors)
//         {
//             try
//             {
//                 if (await page.QuerySelectorAsync(sel) != null) return true;
//             }
//             catch { }
//         }

//         var url = page.Url;
//         if (url.Contains("sorry/index") || url.Contains("sorry?continue"))
//             return true;

//         return false;
//     }

//     private bool IsValidWebsite(string? url)
//     {
//         if (string.IsNullOrEmpty(url)) return false;
//         if (url.Contains("/aclk?") || url.StartsWith("/aclk")) return false;
//         if (url.Contains("googleadservices")) return false;
//         return true;
//     }

//     private string? NormalizeWebsite(string? url)
//     {
//         if (string.IsNullOrEmpty(url)) return null;
//         if (url.Contains("/aclk?") || url.Contains("googleadservices")) return null;
        
//         url = Regex.Replace(url, @"https?://(www\.)?", "");
//         url = Regex.Replace(url, @"[/?#].*$", "");
//         url = url.ToLower().Trim();
        
//         if (string.IsNullOrEmpty(url) || !url.Contains(".")) return null;
//         return url;
//     }

//     private string EscapeCsv(string value)
//     {
//         if (string.IsNullOrEmpty(value)) return "";
//         if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
//             return "\"" + value.Replace("\"", "\"\"") + "\"";
//         return value;
//     }

//     public class BusinessListing
//     {
//         public string? name { get; set; }
//         public string? phone { get; set; }
//         public string? website { get; set; }
//         public string? category { get; set; }
//         public string? detailUrl { get; set; }
//     }
// }


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Data_Enrichment_Helper;

public class MapScraperService
{
    public async Task ScrapeMultipleAsync(List<string> queries, string outputPath, 
        Action<string, System.Drawing.Color> log, 
        Action<int, int, int, int> progress, 
        CancellationToken token)
    {
        log("Initializing Playwright driver...", System.Drawing.Color.Cyan);
        
        using var playwright = await Playwright.CreateAsync();

        // ✅ OPTIMIZATION: Single headless browser for all queries
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
        { 
            Headless = true,
            Args = new[] { 
                "--disable-blink-features=AutomationControlled", 
                "--disable-features=IsolateOrigins,site-per-process",
                "--no-sandbox", 
                "--disable-dev-shm-usage", 
                "--disable-gpu",
                "--memory-pressure-off",
                "--max_old_space_size=512",
                "--window-size=1920,1080",
                "--lang=en-US"
            }
        });

        using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(true));
        writer.WriteLine("Website URL,Company Name,Phone Number,Category,Keyword");
        writer.Flush();

        int totalDone = 0;
        int totalExtracted = 0;
        int totalFailed = 0;
        progress(0, queries.Count, 0, 0);

        foreach (var query in queries)
        {
            if (token.IsCancellationRequested) break;
            
            int extracted = 0;
            try
            {
                extracted = await ProcessSingleQueryAsync(browser, query, writer, log, token);
            }
            catch (Exception ex)
            {
                log($"  ❌ Error processing query '{query}': {ex.Message}", System.Drawing.Color.Red);
            }

            totalExtracted += extracted;
            if (extracted == 0) totalFailed++;
            
            totalDone++;
            progress(totalDone, queries.Count, totalExtracted, totalFailed);
        }

        log($"All queries complete! Total extracted: {totalExtracted}", System.Drawing.Color.SpringGreen);
    }

    private async Task<int> ProcessSingleQueryAsync(IBrowser browser, string query, StreamWriter writer, Action<string, System.Drawing.Color> log, CancellationToken token)
    {
        log($"Navigating: {query}", System.Drawing.Color.LightGray);

        // New context per query to prevent state leakage, but sharing the browser process
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "en-US",
            TimezoneId = "America/New_York",
            Geolocation = new Geolocation { Latitude = 40.7128f, Longitude = -74.0060f },
            Permissions = new[] { "geolocation" }
        });
        
        var page = await context.NewPageAsync();

        try
        {
            try
            {
                await page.GotoAsync("https://www.google.com/maps", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });
            }
            catch (Exception ex)
            {
                log($"  ⚠ Navigation failed: {ex.Message}", System.Drawing.Color.Orange);
                return 0;
            }

            if (await CheckCaptchaAsync(page))
            {
                log($"  ⚠ CAPTCHA detected on Maps homepage!", System.Drawing.Color.Red);
                return 0;
            }

            await Task.Delay(500, token); // Reduced delay

            try
            {
                await page.WaitForSelectorAsync("input[name='q']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });
                await page.FillAsync("input[name='q']", query, new PageFillOptions { Timeout = 30000 });
                await page.Keyboard.PressAsync("Enter");
            }
            catch (Exception ex)
            {
                log($"  ❌ Search box interaction failed: {ex.Message}", System.Drawing.Color.Red);
                return 0;
            }

            try
            {
                await page.WaitForSelectorAsync("a[href*='https://www.google.com/maps/place']", new PageWaitForSelectorOptions { Timeout = 60000 });
            }
            catch (Exception)
            {
                log($"  ❌ No results feed found for: {query}", System.Drawing.Color.Red);
                return 0;
            }

            if (await CheckCaptchaAsync(page))
            {
                log($"  ⚠ CAPTCHA detected on search results!", System.Drawing.Color.Red);
                return 0;
            }

            await Task.Delay(300, token); // Reduced delay

            return await ProcessListingsAsync(page, context, query, writer, log, token);
        }
        catch (Exception ex)
        {
            log($"  ❌ Fatal error: {ex.Message}", System.Drawing.Color.Red);
            return 0;
        }
        finally 
        { 
            await page.CloseAsync(); 
        }
    }

    private async Task<int> ProcessListingsAsync(IPage page, IBrowserContext context, string query, StreamWriter writer, Action<string, System.Drawing.Color> log, CancellationToken token)
    {
        int extracted = 0;
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int noChangeCount = 0;
        long lastHeight = 0;

        string jsScript = @"
            () => {
                try {
                    const cards = document.querySelectorAll('div[role=""article""]');
                    const results = [];
                    const seenNames = new Set();

                    for (const card of cards) {
                        let name = null;
                        const nameEl = card.querySelector('div.qBF1Pd.fontHeadlineSmall');
                        if (nameEl) name = nameEl.innerText.trim();
                        if (!name) {
                            const alt = card.querySelector('div.fontHeadlineSmall');
                            if (alt) name = alt.innerText.trim();
                        }
                        if (!name || seenNames.has(name)) continue;
                        seenNames.add(name);

                        let phone = null;
                        const phoneSelectors = [
                            'span.UsdlK',
                            'span[aria-label*=""phone"" i]',
                            'span[data-item-id*=""phone""]',
                            'button[data-item-id*=""phone""] span',
                        ];
                        for (const sel of phoneSelectors) {
                            const el = card.querySelector(sel);
                            if (el && el.innerText.trim()) { phone = el.innerText.trim(); break; }
                        }

                        let category = null;
                        const catEl = card.querySelector('div.W4Efsd div.W4Efsd > span > span');
                        if (catEl) category = catEl.innerText.trim();

                        let website = null;
                        const webEl =
                            card.querySelector('a[aria-label*=""Visit website""]') ||
                            card.querySelector('a[aria-label*=""website"" i]')     ||
                            card.querySelector('a[aria-label=""Website""]')        ||
                            card.querySelector('a[data-value=""Website""]')        ||
                            card.querySelector('a[title*=""website"" i]');

                        if (webEl) {
                            const rawHref = webEl.getAttribute('href') || webEl.href;
                            if (rawHref) {
                                try {
                                    const url = new URL(rawHref, window.location.origin);
                                    if (!url.hostname.includes('google.')) {
                                        website = url.toString();
                                    } else {
                                        const target =
                                            url.searchParams.get('q')    ||
                                            url.searchParams.get('url')  ||
                                            url.searchParams.get('adurl');
                                        if (target) {
                                            try {
                                                const t = new URL(target);
                                                if (!t.hostname.includes('google.')) website = t.toString();
                                            } catch(e) {}
                                        }
                                    }
                                } catch(e) {}
                            }
                        }

                        let detailUrl = null;
                        const linkEl = card.querySelector('a.hfpxzc');
                        if (linkEl) detailUrl = linkEl.getAttribute('href');

                        results.push({ name, phone, category, website, detailUrl });
                    }
                    return results;
                } catch (e) {
                    return [];
                }
            }
        ";

        while (noChangeCount < 4 && !token.IsCancellationRequested)
        {
            try
            {
                await page.EvaluateAsync("document.querySelector(\"div[role='feed']\")?.focus()");
            }
            catch { }

            await Task.Delay(100, token);

            List<BusinessListing>? listings = null;
            try
            {
                string jsonResult = await page.EvaluateAsync<string>($"() => JSON.stringify(({jsScript})())");
                
                if (!string.IsNullOrEmpty(jsonResult) && jsonResult != "null")
                {
                    listings = JsonSerializer.Deserialize<List<BusinessListing>>(jsonResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                log($"  ⚠ Extraction interrupted: {ex.Message}", System.Drawing.Color.Orange);
            }

            if (listings != null)
            {
                foreach (var item in listings)
                {
                    if (item == null || string.IsNullOrEmpty(item.name)) continue;
                    if (seenNames.Contains(item.name)) continue;

                    bool needsFallback = !IsValidWebsite(item.website) || string.IsNullOrEmpty(item.phone) || item.phone == "N/A";
                    
                    if (needsFallback && !string.IsNullOrEmpty(item.detailUrl))
                    {
                        log($"  ⚠ Missing info for '{item.name}'. Opening card URL...", System.Drawing.Color.Yellow);
                        
                        var detail = await FetchDetailPageAsync(context, item.detailUrl, log, token);
                        if (detail != null)
                        {
                            if (string.IsNullOrEmpty(item.phone) || item.phone == "N/A") item.phone = detail.phone;
                            if (!IsValidWebsite(item.website)) item.website = detail.website;
                            if (string.IsNullOrEmpty(item.category) || item.category == "N/A") item.category = detail.category;
                        }
                    }

                    string normWebsite = NormalizeWebsite(item.website) ?? "N/A";
                    
                    writer.WriteLine($"{EscapeCsv(normWebsite)},{EscapeCsv(item.name)},{EscapeCsv(item.phone ?? "N/A")},{EscapeCsv(item.category ?? "N/A")},{EscapeCsv(query)}");
                    
                    seenNames.Add(item.name);
                    extracted++;
                    log($"  ✅ Extracted: {item.name} | {item.phone} | {normWebsite}", System.Drawing.Color.LightGreen);
                }
                
                writer.Flush();
            }

            try
            {
                // Reduced scroll delay for speed
                for (int i = 0; i < 3; i++)
                {
                    await page.Keyboard.PressAsync("PageDown");
                    await Task.Delay(80, token);
                }
                await Task.Delay(500, token);
            }
            catch (Exception)
            {
                break;
            }

            if (await page.QuerySelectorAsync("span.HlvSq") != null)
            {
                break;
            }

            try
            {
                var newHeight = await page.EvaluateAsync<long>("document.querySelector('div[role=\"feed\"]').scrollHeight");
                if (newHeight == lastHeight)
                {
                    noChangeCount++;
                }
                else
                {
                    noChangeCount = 0;
                    lastHeight = newHeight;
                }
            }
            catch
            {
                break;
            }
        }

        if (extracted == 0) log($"  No results extracted.", System.Drawing.Color.Orange);
        return extracted;
    }

    private async Task<BusinessListing?> FetchDetailPageAsync(IBrowserContext context, string url, Action<string, System.Drawing.Color> log, CancellationToken token)
    {
        IPage? detailPage = null;
        try
        {
            detailPage = await context.NewPageAsync();
            await detailPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
            
            await detailPage.WaitForTimeoutAsync(1200); // Reduced wait time

            string detailJsScript = @"
                () => {
                    try {
                        let name = null;
                        const nameEl = document.querySelector('h1.DuwDvf');
                        if (nameEl) name = nameEl.innerText.trim();

                        let category = null;
                        const catEl = document.querySelector('button.DkEaL');
                        if (catEl) category = catEl.innerText.trim();

                        let phone = null;
                        let website = null;

                        const items = document.querySelectorAll('div.Io6YTe');
                        for (const item of items) {
                            const text = item.innerText.trim();
                            if (!phone && /^\+?[0-9\s\-()]{7,20}$/.test(text)) {
                                phone = text;
                            } 
                            else if (!website && text.includes('.') && !text.includes(' ')) {
                                website = text;
                            }
                        }
                        
                        if (!phone) {
                            const pEl = document.querySelector('button[data-item-id^=""phone:tel:""] div.Io6YTe');
                            if (pEl) phone = pEl.innerText.trim();
                        }
                        if (!website) {
                            const wEl = document.querySelector('a[data-item-id=""authority""] div.Io6YTe');
                            if (wEl) website = wEl.innerText.trim();
                        }

                        return { name, phone, website, category };
                    } catch (e) {
                        return null;
                    }
                }
            ";

            string jsonResult = await detailPage.EvaluateAsync<string>($"() => JSON.stringify(({detailJsScript})())");
            
            if (!string.IsNullOrEmpty(jsonResult) && jsonResult != "null")
            {
                return JsonSerializer.Deserialize<BusinessListing>(jsonResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (Exception ex)
        {
            log($"    ⚠ Detail fallback failed: {ex.Message}", System.Drawing.Color.Orange);
        }
        finally
        {
            if (detailPage != null) await detailPage.CloseAsync();
        }
        return null;
    }

    private async Task<bool> CheckCaptchaAsync(IPage page)
    {
        string[] captchaSelectors = {
            "div[aria-label='Captcha']",
            "iframe[src*='recaptcha']",
            "div#captcha",
            "form#captcha-form",
            "div.g-recaptcha"
        };

        foreach (var sel in captchaSelectors)
        {
            try
            {
                if (await page.QuerySelectorAsync(sel) != null) return true;
            }
            catch { }
        }

        var url = page.Url;
        if (url.Contains("sorry/index") || url.Contains("sorry?continue"))
            return true;

        return false;
    }

    private bool IsValidWebsite(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (url.Contains("/aclk?") || url.StartsWith("/aclk")) return false;
        if (url.Contains("googleadservices")) return false;
        return true;
    }

    private string? NormalizeWebsite(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (url.Contains("/aclk?") || url.Contains("googleadservices")) return null;
        
        url = Regex.Replace(url, @"https?://(www\.)?", "");
        url = Regex.Replace(url, @"[/?#].*$", "");
        url = url.ToLower().Trim();
        
        if (string.IsNullOrEmpty(url) || !url.Contains(".")) return null;
        return url;
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    public class BusinessListing
    {
        public string? name { get; set; }
        public string? phone { get; set; }
        public string? website { get; set; }
        public string? category { get; set; }
        public string? detailUrl { get; set; }
    }
}