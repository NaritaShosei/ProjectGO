using UnityEngine;

public class AttackHandler : MonoBehaviour
{
    [Header("Playerの中心のTransform")]
    [SerializeField] private Transform _playerTransform;

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
        var colls = Physics.OverlapSphere(_playerTransform.position + transform.forward * _currentData.Range, data.Radius);

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
        Gizmos.DrawWireSphere(_playerTransform.position + transform.forward * _currentData.Range, _currentData.Radius);

        // 攻撃方向の表示
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(_playerTransform.position, _playerTransform.forward * _currentData.Range);
    }
#endif
}
