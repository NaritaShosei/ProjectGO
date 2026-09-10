/// <summary>BossCameraControllerへ渡すチューニング値の組。CameraManagerのInspector値から詰め替える。</summary>
public readonly struct BossCameraSettings
{
    /// <summary>プレイヤー↔ボスがこの距離(m)以下で足元(Under)を注視する。</summary>
    public readonly float FramingNearDistance;

    /// <summary>プレイヤー↔ボスがこの距離(m)以上で頭(Top)を注視する。</summary>
    public readonly float FramingFarDistance;

    /// <summary>足元⇔頭の注視補間を滑らかにする追従時間(秒)。0で即時。</summary>
    public readonly float FramingSmoothTime;

    /// <summary>カメラの定位置（プレイヤーから見てボスの反対側）を追従させる速度(度/秒)。</summary>
    public readonly float OrbitTrackSpeed;

    /// <summary>入力で振れる注視点の左右オフセットの最大値(m)。</summary>
    public readonly float SwivelRange;

    /// <summary>入力に対する注視点オフセットの変化速度(m/秒)。</summary>
    public readonly float SwivelSpeed;

    /// <summary>入力が無いとき注視点オフセットを中央へ戻す速度(m/秒)。</summary>
    public readonly float SwivelReturnSpeed;

    /// <summary>姿勢ごとのFOVズーム設定。姿勢が変わると一致するエントリの倍率へ寄せる。</summary>
    public readonly BossPostureZoom[] PostureZooms;

    /// <summary>ボスカメラ終了時に通常視野へ戻す時間(秒)。</summary>
    public readonly float ZoomResetDuration;

    public BossCameraSettings(
        float framingNearDistance,
        float framingFarDistance,
        float framingSmoothTime,
        float orbitTrackSpeed,
        float swivelRange,
        float swivelSpeed,
        float swivelReturnSpeed,
        BossPostureZoom[] postureZooms,
        float zoomResetDuration)
    {
        FramingNearDistance = framingNearDistance;
        FramingFarDistance = framingFarDistance;
        FramingSmoothTime = framingSmoothTime;
        OrbitTrackSpeed = orbitTrackSpeed;
        SwivelRange = swivelRange;
        SwivelSpeed = swivelSpeed;
        SwivelReturnSpeed = swivelReturnSpeed;
        PostureZooms = postureZooms;
        ZoomResetDuration = zoomResetDuration;
    }
}
