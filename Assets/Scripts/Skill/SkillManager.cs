using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    /// <summary>
    /// スキルのIDを登録する 登録済みであればfalseが返ってくる
    /// </summary>
    public bool TryRegisterSkillId(int id)
    {
        if (_ownedSkillIDs.Contains(id))
        {
            return false;
        }

        _ownedSkillIDs.Add(id);
        return true;
    }

    /// <summary>
    /// 開放済みのIDを取得する
    /// </summary>
    public List<int> GetOwnedSkillIDs() => _ownedSkillIDs;

    /// <summary>
    /// スキルの列挙を返す
    /// </summary>
    public IEnumerable<SkillBase> GetOwnedSkills()
    {
        foreach (var id in _ownedSkillIDs)
        {
            var skill = _skillDataBase.GetSkill(id);

            if (skill != null)
            {
                yield return skill;
            }
        }
    }

    [SerializeField] private SkillDataBase _skillDataBase;

    private List<int> _ownedSkillIDs = new List<int>();
}
