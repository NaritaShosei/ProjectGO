using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HitEffectRule
{
    public PlayerMode PlayerMode => _playerMode;
    public string Id => _id;
    public IReadOnlyList<string> EffectKeys => _effectKeys;

    [SerializeField] private PlayerMode _playerMode;
    [SerializeField] private string _id　= string.Empty;
    [SerializeField] private List<string> _effectKeys = new();
}
