using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Data_Enrichment_Helper;

public static class CsvService
{
    public static (List<string> Headers, List<List<string>> Rows) Read(string path)
    {
        var all = new List<List<string>>();
        var current = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        using var reader = new StreamReader(path, Encoding.UTF8);
        int ch;
        while ((ch = reader.Read()) != -1)
        {
            char c = (char)ch;
            if (inQuotes)
            {
                if (c == '"')
                {
                    int next = reader.Peek();
                    if (next == '"') { field.Append('"'); reader.Read(); }
                    else inQuotes = false;
                }
                else field.Append(c);
            }
            else
            {
                switch (c)
                {
                    case '"': inQuotes = true; break;
                    case ',': current.Add(field.ToString()); field.Clear(); break;
                    case '\r': break;
                    case '\n':
                        current.Add(field.ToString()); field.Clear();
                        all.Add(current); current = new List<string>();
                        break;
                    default: field.Append(c); break;
                }
            }
        }
        if (field.Length > 0 || current.Count > 0) { current.Add(field.ToString()); all.Add(current); }

        var headers = all.Count > 0 ? all[0].Select(h => h.Trim().TrimStart('\uFEFF')).ToList() : new List<string>();
        var rows = all.Skip(1).Where(r => r.Count > 0 && !(r.Count == 1 && string.IsNullOrWhiteSpace(r[0]))).ToList();
        return (headers, rows);
    }

    public static string Escape(string value)
    {
        if (value == null) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    public static StreamWriter CreateWriter(string outputPath)
    {
        return new StreamWriter(outputPath, false, new UTF8Encoding(true));
    }
}