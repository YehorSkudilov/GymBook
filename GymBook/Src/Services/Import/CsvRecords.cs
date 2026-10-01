using System.Text;

namespace GymBook.Services.Import;

/// <summary>
/// Splits CSV text into records of fields: a delimiter (';' by default), "quoted" fields that may hold the delimiter or
/// line breaks, and "" for a quote inside one. A blank line is an empty record, which some exports use as a separator.
/// </summary>
public static class CsvRecords
{
    public static List<List<string>> Parse(string text, char delimiter = ';')
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var fieldStarted = false;
        // A byte order mark at the start of the file isn't data.
        var i = text.Length > 0 && text[0] == '﻿' ? 1 : 0;
        for (; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                        quoted = false;
                }
                else
                    field.Append(c);
                continue;
            }
            switch (c)
            {
                case '"' when field.Length == 0:
                    quoted = true;
                    fieldStarted = true;
                    break;
                case var d when d == delimiter:
                    record.Add(field.ToString());
                    field.Clear();
                    fieldStarted = true;
                    break;
                case '\r':
                    break;
                case '\n':
                    EndRecord();
                    break;
                default:
                    field.Append(c);
                    fieldStarted = true;
                    break;
            }
        }
        if (fieldStarted || field.Length > 0 || record.Count > 0)
            EndRecord();
        return records;

        void EndRecord()
        {
            if (fieldStarted || field.Length > 0 || record.Count > 0)
                record.Add(field.ToString());
            records.Add(record);
            record = [];
            field.Clear();
            fieldStarted = false;
        }
    }
}
