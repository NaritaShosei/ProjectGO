using System.Collections.Generic;
using UnityEngine;

public class EnemyEffectReceiver : MonoBehaviour
{
    private Enemy _enemy;

    private EffectManager _effectManager;

    [SerializeField]
    private List<HitEffectRule> _rules;

    [Header("Effect Ids")]
    [SerializeField] private string _hitId = "hit";
    [SerializeField] private string _weakId = "weak";
    [SerializeField] private string _armorHitId = "armor_hit";
    [SerializeField] private string _armorBreakId = "armor_break";


    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _effectManager = ServiceLocator.Get<EffectManager>();

        if (_enemy == null)
        {
            Debug.LogError($"{nameof(Enemy)} が見つかりません", this);
            enabled = false;
            return;
        }

        _enemy.OnHitEffect += HandleHitEffect;
    }   

    private void OnDestroy()
    {
        if (_enemy != null)
        {
            _enemy.OnHitEffect -= HandleHitEffect;
        }
    }

    /// <summary>
    /// Enemyの被弾結果を受け取り、対応するエフェクトを再生する
    /// </summary>
    private void HandleHitEffect(HitEffectContext context)
    {
        Debug.Log("HitEffect受信");
        if (_effectManager == null) return;

        string key = GetEffectKey(context);

        if (string.IsNullOrEmpty(key)) return;

        Debug.Log($"再生EffectKey : {key}");

        _effectManager.PlayEffect(
            key,
            context.Position);
    }

    /// <summary>
    /// ヒット内容に応じて再生するエフェクトキーを決定する
    /// </summary>
    private string GetEffectKey(HitEffectContext context)
    {
        string id = GetEffectId(context);

        HitEffectRule rule =
            _rules.Find(x => x.Id == id);

        if (rule == null)
        {
            Debug.LogWarning($"Effect Rule が見つかりません : {id}");
            return string.Empty;
        }

        return rule.EffectKey;
    }

    private string GetEffectId(HitEffectContext context)
    {
        if (context.IsArmorBreak)
        {
            return _armorBreakId;
        }

        if (context.IsArmorHit)
        {
            return _armorHitId  ;
        }

        if (context.IsWeakPoint)
        {
            return _weakId;
        }

        return _hitId;
    }
}
