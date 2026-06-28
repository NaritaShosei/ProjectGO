using System;
using System.Collections.Generic;
using System.Text;

public class CSVDateLoader
{
    /// <summary> CSVDataをstring[,]形式に変換 </summary>
    /// <param name="csvData"> CSVデータの文字列 </param>
    /// <returns> 2次元配列に変換されたCSVデータ </returns>
    public static string[,] ParseCsv(string csvData)
    {
        if (string.IsNullOrEmpty(csvData)) return new string[0, 0];

        // CSVデータを行ごとに分割
        string[] rows = csvData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        List<string[]> parsedRows = new List<string[]>(rows.Length);
        int rowCount = rows.Length;
        int colCount = 0;

        for (int i = 0; i < rowCount; i++)
        {
            string[] cols = SplitCsvLine(rows[i]);
            parsedRows.Add(cols);
            colCount = Math.Max(colCount, cols.Length);
        }

        // CSVデータを2次元配列に変換
        string[,] result = new string[rowCount, colCount];

        for (int i = 0; i < rowCount; i++)
        {
            string[] cols = parsedRows[i];

            for (int j = 0; j < colCount; j++)
            {
                // 
                result[i, j] = j < cols.Length ? cols[j] : "";
            }
        }

        return result;
    }

    /// <summary> CSVの1行を解析してフィールドに分割 </summary>
    /// <param name="line"> CSVの1行の文字列 </param>
    /// <returns> 分割されたフィールドの配列 </returns>
    private static string[] SplitCsvLine(string line)
    {
        List<string> fields = new List<string>();
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
