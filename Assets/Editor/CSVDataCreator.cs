using Cysharp.Threading.Tasks;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEditor.LightingExplorerTableColumn;

#if UNITY_EDITOR
public class CSVDataCreator : EditorWindow
{
    private string _gasUrl = "";
    private string _scriptableObjectName = "";
    private string _outPutCsvFilePath = "";

    private DataType _dataType;

    private int _spaceSize = 10;
    private const string _dataSaveFilePath = "Assets/Data/CSV";

    [MenuItem("Tools/GenerateCSVData")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(CSVDataCreator));
    }

    void OnGUI()
    {
        //生成するDataの名前
        EditorGUILayout.Space(_spaceSize);
        EditorGUILayout.LabelField("生成するCSVDataの名前");
        _scriptableObjectName = EditorGUILayout.TextField("CSVName", _scriptableObjectName);

        //生成するDataを選択
        EditorGUILayout.Space(_spaceSize);
        EditorGUILayout.LabelField("生成するDataのGASURL");
        _gasUrl = EditorGUILayout.TextField("GASURL", _gasUrl);

        // GASから取得したCSVデータを保存するファイル名のパスを設定
        _outPutCsvFilePath = _dataSaveFilePath + "/" + _scriptableObjectName + "CSV" + ".csv";

        EditorGUILayout.Space(_spaceSize);

        //CSVDataのみを生成する処理
        if (GUILayout.Button("Generate CSVData"))
        {
            DataGenerate();
        }
    }

    private async void DataGenerate()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(_gasUrl))
        {
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var csvData = request.downloadHandler.text;

                // 先頭の\uFEFFを削除
                if (csvData[0] == '\uFEFF')
                {
                    csvData = csvData.Substring(1);
                }

                // CSVファイルとして保存
                SaveCsvFile(csvData);
            }
        }
    }

    // CSVファイルに保存する
    private void SaveCsvFile(string data)
    {
        File.WriteAllText(_outPutCsvFilePath, data, Encoding.UTF8);
        AssetDatabase.Refresh();
    }
}
#endif
