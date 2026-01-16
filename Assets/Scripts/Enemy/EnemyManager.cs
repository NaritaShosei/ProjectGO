using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public void Spawn(GameObject original, Vector3 pos)
    {
        var obj = Instantiate(original, pos, Quaternion.identity, parent: transform);

        if (obj.TryGetComponent(out IEnemy enemy))
        {
            enemy.OnDead += HandleEnemyDead;
            _enemies.Add(enemy);
        }

        else { Destroy(obj); Debug.LogWarning("IEnemyを継承していないオブジェクトを生成したため、破壊しました"); }
    }

    private List<IEnemy> _enemies = new();

    private void HandleEnemyDead(IEnemy enemy)
    {
        if (enemy != null)
        {
            enemy.OnDead -= HandleEnemyDead;
            _enemies.Remove(enemy);
        }
    }


    // デバッグ用
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 30), $"残り敵数：{_enemies.Count}");
    }
}
