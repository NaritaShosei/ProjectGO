using System;
using UnityEditor;
using UnityEngine;

public class SkillExecutor : MonoBehaviour
{
    private SkillManager _skillManager;

    public void Iint(SkillManager skillManager)
    {
        _skillManager = skillManager;
    }

    void Update()
    {
        if (_skillManager != null) return;

        float deltatime = Time.deltaTime;

        foreach(var skill in _skillManager.GetOwnedSkills())
        {
            //skill.OnUpdate(deltatime);
        }
        
    }
}
