using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillDataBase))]
public class SkillDataBaseEditor : Editor
{
    private SerializedProperty _skillsProperty;

    private void OnEnable()
    {
        _skillsProperty = serializedObject.FindProperty("_skills");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var duplicatedIds = GetDuplicatedIds();

        if (duplicatedIds.Count > 0)
        {
            string message =
                "重複しているSkill IDがあります\n" +
                string.Join(", ", duplicatedIds);

            EditorGUILayout.HelpBox(
                message,
                MessageType.Error);
        }

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Skill一覧", EditorStyles.boldLabel);

        Dictionary<int, int> idCount = new();

        for (int i = 0; i < _skillsProperty.arraySize; i++)
        {
            var element = _skillsProperty.GetArrayElementAtIndex(i);

            SkillBase skill = element.objectReferenceValue as SkillBase;

            if (skill == null)
            {
                EditorGUILayout.HelpBox(
                    $"Element {i} : NULL",
                    MessageType.Warning);
                continue;
            }

            if (!idCount.TryAdd(skill.ID, 1))
            {
                idCount[skill.ID]++;
            }
        }

        for (int i = 0; i < _skillsProperty.arraySize; i++)
        {
            var element = _skillsProperty.GetArrayElementAtIndex(i);

            SkillBase skill = element.objectReferenceValue as SkillBase;

            if (skill == null)
            {
                continue;
            }

            bool duplicated =
                idCount.TryGetValue(skill.ID, out int count) &&
                count > 1;

            Color originalColor = GUI.backgroundColor;

            if (duplicated)
            {
                GUI.backgroundColor = Color.red;
            }

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.ObjectField(
                skill,
                typeof(SkillBase),
                false);

            EditorGUILayout.LabelField(
                $"ID : {skill.ID}",
                $"Name : {skill.Name}");

            if (duplicated)
            {
                EditorGUILayout.HelpBox(
                    $"ID {skill.ID} が重複しています",
                    MessageType.Error);
            }

            EditorGUILayout.EndVertical();

            GUI.backgroundColor = originalColor;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private List<int> GetDuplicatedIds()
    {
        Dictionary<int, int> counts = new();
        List<int> duplicated = new();

        for (int i = 0; i < _skillsProperty.arraySize; i++)
        {
            var element = _skillsProperty.GetArrayElementAtIndex(i);
            SkillBase skill = element.objectReferenceValue as SkillBase;

            if (skill == null)
            {
                continue;
            }

            counts.TryGetValue(skill.ID, out int count);
            counts[skill.ID] = count + 1;
        }

        foreach (var pair in counts)
        {
            if (pair.Value > 1)
            {
                duplicated.Add(pair.Key);
            }
        }

        return duplicated;
    }
}
