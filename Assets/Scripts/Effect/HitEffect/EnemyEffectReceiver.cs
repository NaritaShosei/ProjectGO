using System.Collections.Generic;
using UnityEngine;

public class EnemyEffectReceiver : MonoBehaviour
{
    private Enemy _enemy;
    private EffectManager _effectManager;

    [Header("Effect Ids"), Tooltip("ヒット時のエフェクトID")]
    [SerializeField] private string _hitId = "hit";
    [SerializeField] private string _weakId = "weak";
    [SerializeField] private string _armorHitId = "armor_hit";
    [SerializeField] private string _armorBreakId = "armor_break";

    [SerializeField,Tooltip("ヒットエフェクトのルール")]
    private List<HitEffectRule> _rules;

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

        IReadOnlyList<string> keys = GetEffectKeys(context);

        if (keys == null || keys.Count == 0) return;

        Debug.Log($"再生EffectKey : {string.Join(", ", keys)}");

        foreach (string key in keys)
        {
            _effectManager.PlayEffect(
                key,
                context.Position);
        }
    }

    /// <summary>
    /// ヒット内容に応じて再生するエフェクトキーを決定する
    /// </summary>
    private IReadOnlyList<string> GetEffectKeys(HitEffectContext context)
    {
        string id = GetHitType(context);

        HitEffectRule rule =
            _rules.Find(x => x.Id == id && x.PlayerMode == context.PlayerMode);

        if (rule == null)
        {
            Debug.LogWarning($"Effect Rule が見つかりません : {id}");
            return null;
        }

        return rule.EffectKeys;
    }

    private string GetHitType(HitEffectContext context)
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
