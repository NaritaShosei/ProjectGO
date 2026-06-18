using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDataBase", menuName = "GameData/Skill/DataBase")]
public class SkillDataBase : ScriptableObject
{

    public SkillBase GetSkill(int skillId)
    {
        CreateDictionary();

        _skillMap.TryGetValue(skillId, out var skill);
        return skill;
    }

    public SkillBase[] GetAllSkills() => _skills;

    [SerializeField] private SkillBase[] _skills;

    private Dictionary<int, SkillBase> _skillMap;

    private void OnEnable()
    {
        CreateDictionary();
    }

    private void CreateDictionary()
    {
        if (_skillMap != null) { return; }

        _skillMap = new Dictionary<int, SkillBase>();

        foreach (var skill in _skills)
        {
            if (!skill)
            {
                Debug.LogWarning("Skillがnullです");
                continue;
            }

            if (_skillMap.TryGetValue(skill.ID, out SkillBase existingSkill))
            {
                Debug.LogWarning(
                    $"Skill ID重複検出\n" +
                    $"ID: {skill.ID}\n" +
                    $"先に登録済み: {existingSkill.Name}\n" +
                    $"重複したSkill: {skill.Name}"
                );
                continue;
            }

            _skillMap.Add(skill.ID, skill);
        }
    }
}
