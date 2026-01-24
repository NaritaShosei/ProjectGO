using System.Diagnostics;

public class ResultPanelModel
{
    /// <summary>
    /// 未実装: ゲームロジックからリザルトデータを取得する
    /// </summary>
    /// <returns></returns>
    public ResultData GetResultData()
    {
        // 未実装のためテスト用データを返す
        return GetTestResultData();
    }

    /// <summary>
    /// テスト用のリザルトデータを生成する
    /// </summary>
    /// <returns></returns>
    public ResultData GetTestResultData()
    {
        Debug.Assert(true, "未実装: ゲームロジックからリザルトデータを取得する");
        ResultData resultData = new ResultData(
            true
            , "クリアウェーブ数: 10/10"
            , "キル数: 150"
            , "コンボ数: 25"
            , "与ダメージ: 12000"
            , "被ダメージ: 3000"
            , "回復量: 5000"
            , "ビルドバランス: 雷神"
            , "スキルリスト:\n- ファイアボール\n- アイススパイク\n- ライトニングストーム"
            , "最終ステータス:\n- 攻撃力: 1\n- 防御力: 2\n- 速度: 3"
            );

        return resultData;
    }
}
