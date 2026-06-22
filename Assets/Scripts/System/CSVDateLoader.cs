using System;
using System.Text.RegularExpressions;

public class CSVDateLoader
{
    /// <summary> CSVDataをstring[,]形式に変換 </summary>
    /// <param name="csvData"> CSVデータの文字列 </param>
    /// <returns> 2次元配列に変換されたCSVデータ </returns>
    public static string[,] ParseCsv(string csvData)
    {
        // CSVデータを行ごとに分割
        string[] rows = csvData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // 最初の行を解析して列数を取得
        string[] firstRow = SplitCsvLine(rows[0]);
        int rowCount = rows.Length;
        int colCount = firstRow.Length;

        // CSVデータを2次元配列に変換
        string[,] result = new string[rowCount, colCount];

        for (int i = 0; i < rowCount; i++)
        {
            string[] cols = SplitCsvLine(rows[i]);

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
        // 正規表現を使用して、ダブルクォーテーションで囲まれたフィールドとカンマで区切られたフィールドを分割
        MatchCollection matches = Regex.Matches(line, "\"([^\"]*)\"|([^,]+)");

        string[] fields = new string[matches.Count];

        for (int i = 0; i < matches.Count; i++)
        {
            fields[i] = matches[i].Value.Trim('"'); // ダブルクォーテーションを削除
        }

        return fields;
    }
}
