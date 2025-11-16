using UnityEngine;

public class AttackHandler : MonoBehaviour
{
    [Header("PlayerのTransform")]
    [SerializeField] private Transform _playerTransform;
    [Header("攻撃の判定位置")]
    [SerializeField] private Transform _attackPoint;

    private AttackData _currentData;

    public void SetupData(AttackData data)
    {
        _currentData = data;
    }

    /// <summary>
    /// AnimationEventで呼び出す想定のメソッド
    /// </summary>
    public void ApplyAttack()
    {
        var data = _currentData;

        // 攻撃範囲からIEnemyを継承したオブジェクトを取得し攻撃する
        var colls = Physics.OverlapSphere(_attackPoint.position, data.Range / 2);

        foreach (var coll in colls)
        {
            if (!coll.TryGetComponent(out IEnemy enemy)) continue;

            // data のノックバック方向（ローカル）をプレイヤー向きに変換
            Vector3 worldDir = _playerTransform.TransformDirection(data.KnockbackDirection);

            Vector3 knockbackForce = worldDir.normalized * data.KnockbackForce;

            enemy.AddDamage(data.Power);
            enemy.AddKnockBackForce(knockbackForce);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !_currentData) { return; }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_attackPoint.position, _currentData.Range / 2);

        // 攻撃方向の表示
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(_attackPoint.position, _attackPoint.forward * _currentData.Range / 2);
    }
#endif
}
