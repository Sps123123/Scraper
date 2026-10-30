using System.Text.RegularExpressions;

namespace Data_Enrichment_Helper;

public static class CountryDetectorService
{
    public static string Detect(string phone)
    {
        string clean = Regex.Replace(phone, @"[^\d+]", "");
        if (!clean.StartsWith("+"))
        {
            if (clean.StartsWith("00")) clean = "+" + clean.Substring(2);
            else return "Unknown";
        }

        if (clean.StartsWith("+91")) return "India";
        if (clean.StartsWith("+92")) return "Pakistan";
        if (clean.StartsWith("+880")) return "Bangladesh";
        if (clean.StartsWith("+971")) return "UAE";
        if (clean.StartsWith("+966")) return "Saudi Arabia";
        if (clean.StartsWith("+974")) return "Qatar";
        if (clean.StartsWith("+965")) return "Kuwait";
        if (clean.StartsWith("+973")) return "Bahrain";
        if (clean.StartsWith("+968")) return "Oman";
        if (clean.StartsWith("+44")) return "UK";
        if (clean.StartsWith("+61")) return "Australia";
        if (clean.StartsWith("+64")) return "New Zealand";
        if (clean.StartsWith("+27")) return "South Africa";
        if (clean.StartsWith("+33")) return "France";
        if (clean.StartsWith("+49")) return "Germany";
        if (clean.StartsWith("+39")) return "Italy";
        if (clean.StartsWith("+34")) return "Spain";
        if (clean.StartsWith("+81")) return "Japan";
        if (clean.StartsWith("+86")) return "China";
        if (clean.StartsWith("+65")) return "Singapore";
        if (clean.StartsWith("+60")) return "Malaysia";
        if (clean.StartsWith("+62")) return "Indonesia";
        if (clean.StartsWith("+63")) return "Philippines";
        if (clean.StartsWith("+66")) return "Thailand";
        if (clean.StartsWith("+84")) return "Vietnam";
        if (clean.StartsWith("+1")) return "US/Canada";
        return "Other";
    }
}