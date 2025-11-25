using UnityEngine;

public class Goblin : EnemyBase
{
    protected override void AttackAction()
    {
        var col = Physics.OverlapSphere(transform.position + transform.forward, AttackData.Range);
        foreach (var hit in col)
        {
            if (hit.TryGetComponent<IPlayer>(out var player))
            {
                player.AddDamage(AttackData.Power);
                Debug.Log($"{nameof(gameObject)}がプレイヤーに{AttackData.Power}ダメージ！");
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, AttackData.Range);
    }
}
