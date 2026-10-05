namespace Verve
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>默认 CSV 工具实现；支持 RFC 4180 风格的引号字段。</para>
    /// </summary>
    internal sealed class CsvTool : ICsv
    {
        private static readonly char[] CsvValueCharacters = { ',', '"', '\r', '\n' };
        private static readonly char[] LineBreakCharacters = { '\r', '\n' };

        /// <inheritdoc />
        public void AppendRow(StringBuilder builder, IReadOnlyList<string> values)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (values == null) throw new ArgumentNullException(nameof(values));
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) builder.Append(',');
                AppendCsvValue(builder, values[i]);
            }
            builder.AppendLine();
        }

        private static void AppendCsvValue(StringBuilder builder, string value)
        {
            value ??= string.Empty;
            if (value.IndexOfAny(CsvValueCharacters) < 0)
            {
                builder.Append(value);
                return;
            }

            builder.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
        }

        private static string RemoveExcelSeparatorHeader(string text)
        {
            var lineEnd = text.IndexOfAny(LineBreakCharacters);
            if (lineEnd < 0 || !string.Equals(text[..lineEnd].Trim(), "sep=,", StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }

            int contentStart = lineEnd + 1;
            if (text[lineEnd] == '\r' && contentStart < text.Length && text[contentStart] == '\n')
            {
                contentStart++;
            }
            return text[contentStart..];
        }

        /// <inheritdoc />
        public IReadOnlyList<string[]> Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            text = RemoveExcelSeparatorHeader(text.TrimStart('\uFEFF'));
            var rows = new List<string[]>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;
            bool afterClosingQuote = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuotes)
                {
                    if (c != '"') cell.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                        afterClosingQuote = true;
                    }
                    continue;
                }

                if (c is ',' or '\r' or '\n')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    afterClosingQuote = false;
                    if (c == ',') continue;
                    rows.Add(row.ToArray());
                    row.Clear();
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    continue;
                }
                if (afterClosingQuote)
                    throw new InvalidDataException($"偏移量 {i + 1}：引号闭合后出现意外字符。");
                if (c == '"')
                {
                    if (cell.Length != 0) throw new InvalidDataException($"偏移量 {i + 1}：引号必须位于字段开头。");
                    inQuotes = true;
                }
                else cell.Append(c);
            }
            if (inQuotes) throw new InvalidDataException("CSV 中存在未闭合的引号字段。");

            if (cell.Length > 0 || row.Count > 0 || afterClosingQuote)
            {
                row.Add(cell.ToString());
                rows.Add(row.ToArray());
            }

            return rows;
        }
    }
}