using System;
using UnityEngine;

public class EXPItem : MonoBehaviour, ISpeedChange
{
    /// <summary>
    /// アイテムがリリースされたときに発火するイベント。引数にはリリースされたアイテム自身が渡される。
    /// </summary>
    public event Action<EXPItem> OnReleased;

    public float TimeScale { get => _timeScale; set => _timeScale = value; }

    /// <summary>
    /// アイテムを初期化するメソッド。引数にはアイテムをインタラクトするプレイヤーの情報が渡される。
    /// </summary>
    public void Init(IPlayer player, float magnetRange)
    {
        _player = player;
        _magnetRange = magnetRange;
    }

    /// <summary>
    /// アイテムの状態を更新するメソッド。毎フレーム呼び出される。プレイヤーとの距離を計算し、距離がマグネット範囲内であれば、プレイヤーに向かって移動する処理を行う。
    /// </summary>
    public void Tick()
    {
        // プレイヤーとの距離を計算
        float distanceToPlayer = Vector3.Distance(transform.position, _player.GetTargetCenter().position);
        // 距離がマグネット範囲内であれば、プレイヤーに向かって移動する
        if (distanceToPlayer <= _magnetRange)
        {
            Vector3 direction = (_player.GetTargetCenter().position - transform.position).normalized;
            float speed = Mathf.Lerp(0, 10f, 1 - (distanceToPlayer / _magnetRange)); // 距離が近いほど速くなる
            transform.position += direction * speed * Time.deltaTime * TimeScale;
        }
    }

    /// <summary>
    /// アイテムとインタラクトしたときの処理を行うメソッド。引数にはインタラクトしたオブジェクトが渡される。
    /// </summary>
    public void Interact(GameObject interactor)
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
        _timeScale = scale;
    }

    [SerializeField] private float _expValue;
    private IPlayer _player;
    private float _magnetRange;
    private float _timeScale;
}
