using Cysharp.Threading.Tasks;
using UnityEngine;


/// <summary> ボスの装備するアーマー </summary>
public class BossArmorView : MonoBehaviour
{
    public ArmorAttachmentPoint AttachmentPoints => _armorAttachmentPointsType;

    public bool IsBreak => _isBreak;

    public void Init()
    {
        RepairArmor().Forget();
    }

    /// <summary> アーマー修復時の処理 </summary>
    public async UniTask RepairArmor()
    {
        if (_isBreak == false) return;

        this.gameObject.SetActive(true);
        _isBreak = false;
    }

    /// <summary> アーマー破壊時の処理 </summary>
    public async UniTask BreakArmer()
    {
        if (_isBreak == true) return;

        this.gameObject.SetActive(false);
        _isBreak = true;
    }

    [Header("Armorの装着ヶ所を示すEnum")]
    [SerializeField, Tooltip("Armorの装着ヶ所を示すEnum")]
    private ArmorAttachmentPoint _armorAttachmentPointsType;

    private bool _isBreak = false;
}
