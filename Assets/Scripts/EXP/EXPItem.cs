using System;
using UnityEngine;

public class EXPItem : MonoBehaviour, ISpeedChange
{
    private const float INTERACT_RANGE = 0.1f;

    /// <summary>
    /// アイテムがリリースされたときに発火するイベント。引数にはリリースされたアイテム自身が渡される。
    /// </summary>
    public event Action<EXPItem> OnReleased;

    public float TimeScale { get; set; } = 1f;

    /// <summary>
    /// アイテムの状態を更新するメソッド。毎フレーム呼び出される。プレイヤーとの距離を計算し、距離がマグネット範囲内であれば、プレイヤーに向かって移動する処理を行う。
    /// </summary>
    public void Tick(IPlayer player, float magnetRange)
    {
        Vector3 playerCenterPos = player.GetTargetCenter().position;

        // プレイヤーとの距離を計算
        float distanceToPlayer = Vector3.Distance(transform.position, playerCenterPos);
        // 距離がマグネット範囲内であれば、プレイヤーに向かって移動する
        if (distanceToPlayer <= magnetRange)
        {
            Vector3 direction = (playerCenterPos - transform.position).normalized;

            float t = Mathf.Clamp01(1f - (distanceToPlayer / magnetRange));
            float speed = Mathf.Lerp(0f, 10f, t); // 距離が近いほど速くなる
            transform.position += direction * speed * Time.deltaTime * TimeScale;

            // プレイヤーとの距離がインタラクト範囲内であれば、Interactを呼び出す
            if (distanceToPlayer <= INTERACT_RANGE)
            {
                Interact();
            }
        }
    }

    /// <summary>
    /// アイテムとインタラクトしたときの処理を行うメソッド。
    /// </summary>
    public void Interact()
    {
        // 経験値を加算する処理などをここに実装
        if (ServiceLocator.TryGet(out EXPManager expManager))
        {
            expManager.AddEXP(_expValue);
        }

        // アイテムをリリース
        OnReleased?.Invoke(this);
    }

    /// <summary>
    /// ゲームのスピードが変化したときに呼び出されるメソッド。引数には新しいスピードの倍率が渡される。
    /// </summary>
    /// <param name="scale"></param>
    public void OnSpeedChange(float scale)
    {
        TimeScale = scale;
    }

    [SerializeField] private float _expValue;
}
