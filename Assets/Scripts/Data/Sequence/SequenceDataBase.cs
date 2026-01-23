using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SequenceDataBase", menuName = "GameData/Sequence/SequenceDataBase")]

public class SequenceDataBase : ScriptableObject
{
    public IReadOnlyList<SequenceBase> Sequences => _sequences;

    [SerializeField] private SequenceBase[] _sequences;
}
