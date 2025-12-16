using Cysharp.Threading.Tasks;
using UnityEngine;

public class AttackHandler : MonoBehaviour
{
    [Header("Playerの中心のTransform")]
    [SerializeField] private Transform _playerTransform;

    private AttackData _currentData;

    [SerializeField]
    private AttackAreaView _view;
    [System.Serializable]
    private class AttackAreaView
    {
        private GameObject _sphere;
        [SerializeField] private Material _material;
        [SerializeField] private int _time = 1;
        public void Init(AttackData data, Vector3 position)
        {
            _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _sphere.transform.position = position;
            _sphere.transform.localScale = Vector3.one * data.Radius * 2f; // 半径 × 2
            var renderer = _sphere.GetComponent<Renderer>();
            renderer.material = new Material(_material);

            Destroy(_sphere.GetComponent<Collider>()); // 判定用じゃないなら消す

            _ = Run();
        }

        private async UniTask Run()
        {
            await UniTask.Delay(_time);

            Destroy(_sphere);
        }
    }

    private void Awake()
    {
        _view = new AttackAreaView();
    }

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

        _view.Init(data, _playerTransform.position + transform.forward * _currentData.Range);

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
