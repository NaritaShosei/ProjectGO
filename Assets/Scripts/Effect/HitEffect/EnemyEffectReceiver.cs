using System.Collections.Generic;
using UnityEngine;

public class EnemyEffectReceiver : MonoBehaviour
{
    private Enemy _enemy;
    private EffectManager _effectManager;

    [Header("Effect Ids"), Tooltip("ヒット時のエフェクトID")]
    [SerializeField] private string _hitId = "hit";
    [SerializeField] private string _armorHitId = "armor_hit";
    [SerializeField] private string _armorBreakId = "armor_break";

    [SerializeField, Tooltip("ヒットエフェクトのルール")]
    private List<HitEffectRule> _rules;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();

        if (_enemy == null)
        {
            Debug.LogError($"{nameof(Enemy)} が見つかりません", this);
            enabled = false;
            return;
        }

        if (!ServiceLocator.TryGet(out _effectManager))
        {
            Debug.LogError($"{nameof(EffectManager)} が見つかりません", this);
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
        if (_effectManager == null) return;

        IReadOnlyList<string> keys = GetEffectKeys(context);

        if (keys == null || keys.Count == 0) return;

        //登録されたエフェクトを再生
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
        if (_rules == null || _rules.Count == 0)
        {
            Debug.LogWarning("HitEffectRule が未設定です", this);
            return null;
        }

        //ヒットの種類を判定
        string id = GetHitType(context);

        //ヒットタイプ＋プレイヤーモードに対応するルールを検索
        HitEffectRule rule =
            _rules.Find(x => x.Id == id && x.PlayerMode == context.PlayerMode);

        if (rule == null)
        {
            Debug.LogWarning($"Effect Rule が見つかりません : {id}");
            return null;
        }

        return rule.EffectKeys;
    }

    /// <summary>
    /// 被弾内容からヒットタイプIDを判定する
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private string GetHitType(HitEffectContext context)
    {
        if (context.IsArmorBreak)
        {
            return _armorBreakId;
        }

        if (context.IsArmorHit)
        {
            return _armorHitId;
        }

        return _hitId;
    }
}
