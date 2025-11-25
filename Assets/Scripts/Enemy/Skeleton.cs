using UnityEngine;

public class Skeleton : EnemyBase
{
    [SerializeField] private EnemyBullet _enemyBulletPrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _bulletSpeed = 10f;
    private void Awake()
    {
        // 必要なコンポーネントがアサインされていなければ試しに取得
        if (_firePoint == null)
        {
            // 自分の子に firePoint があれば拾う
            var fp = transform.Find("FirePoint");
            if (fp != null) _firePoint = fp;
            else _firePoint = transform; // 最終手段で自分自身を使う
        }
    }
    protected override void AttackAction()
    {
        // 実際の弾の生成・発射
        FireBullet();
    }
    private void FireBullet()
    {
        if (_enemyBulletPrefab == null)
        {
            Debug.LogWarning($"{nameof(Skeleton)}: EnemyBullet prefab is not assigned.");
            return;
        }
        if (_firePoint == null)
        {
            Debug.LogWarning($"{nameof(Skeleton)}: FirePoint is not assigned.");
            return;
        }
        if (PlayerTransform == null)
        {
            Debug.LogWarning($"{nameof(Skeleton)}: Player transform is null.");
            return;
        }

        // 向き計算
        Vector3 dir = (PlayerTransform.position - _firePoint.position);
        dir.y = 0f;// 高さ差を無視
        dir.Normalize();

        // Instantiate（将来的にはオブジェクトプールを使うかも）
        var bullet = PoolManager.Instance.Spawn(_enemyBulletPrefab);
        if (bullet == null)
        {
            Debug.LogError($"[Skeleton] 弾の生成に失敗しました: プレハブ '{_enemyBulletPrefab.name}' の型不一致");
            return; // 弾の発射をスキップ
        }
        bullet.transform.position = _firePoint.position;
        bullet.transform.rotation = Quaternion.LookRotation(dir);
        bullet.Init(dir,AttackData);

    }

    // デバッグ用: 発射位置を Gizmo で可視化
    private void OnDrawGizmos()
    {
        if (_firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_firePoint.position, 0.05f);
            if (PlayerTransform != null)
            {
                Gizmos.DrawLine(_firePoint.position, PlayerTransform.position);
            }
        }
    }
}
