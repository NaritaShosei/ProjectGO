/// <summary>BossCameraControllerへ渡すチューニング値の組。CameraManagerのInspector値から詰め替える。</summary>
public readonly struct BossCameraSettings
{
    /// <summary>この距離(m)以下でボスの足元(CameraAngleUnder)を注視する。Far との間は距離の比率で足元⇔頭を補間。</summary>
    public readonly float FramingNearDistance;

    /// <summary>この距離(m)以上でボスの頭(CameraAngleTop)を注視する。</summary>
    public readonly float FramingFarDistance;

    /// <summary>カメラの定位置（プレイヤーから見てボスの反対側）へ向け直す最大回転速度(度/秒)。</summary>
    public readonly float OrbitTrackSpeed;

    /// <summary>カメラの定位置追従にかける滑らかさ(秒)。大きいほど急な方位変化にも慣性を持って緩やかに追従する。通常ロックオンとは独立したボス専用の値。</summary>
    public readonly float OrbitSmoothTime;

    /// <summary>入力で注視点を左右へずらせる最大量(m)。オービット位置は動かさず視線だけ振る。</summary>
    public readonly float SwivelRange;

    /// <summary>入力を入れている間に注視点オフセットが伸びる速さ(m/秒)。</summary>
    public readonly float SwivelSpeed;

    /// <summary>入力を離したとき注視点オフセットが中央（ボス正面）へ戻る速さ(m/秒)。</summary>
    public readonly float SwivelReturnSpeed;

    /// <summary>姿勢ごとのFOVズーム設定。姿勢が変わると一致するエントリの倍率へ寄せる。</summary>
    public readonly BossPostureZoom[] PostureZooms;

    /// <summary>ボスカメラ終了時に通常視野へ戻す時間(秒)。</summary>
    public readonly float ZoomResetDuration;

    public BossCameraSettings(
        float framingNearDistance,
        float framingFarDistance,
        float orbitTrackSpeed,
        float orbitSmoothTime,
        float swivelRange,
        float swivelSpeed,
        float swivelReturnSpeed,
        BossPostureZoom[] postureZooms,
        float zoomResetDuration)
    {
        FramingNearDistance = framingNearDistance;
        FramingFarDistance = framingFarDistance;
        OrbitTrackSpeed = orbitTrackSpeed;
        OrbitSmoothTime = orbitSmoothTime;
        SwivelRange = swivelRange;
        SwivelSpeed = swivelSpeed;
        SwivelReturnSpeed = swivelReturnSpeed;
        PostureZooms = postureZooms;
        ZoomResetDuration = zoomResetDuration;
    }
}
