using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HitEffectRule
{
    [SerializeField] private PlayerMode _playerMode;
    [SerializeField] private string _id;
    [SerializeField] private List<string> _effectKeys;

    public PlayerMode PlayerMode => _playerMode;
    public string Id => _id;
    public IReadOnlyList<string> EffectKeys => _effectKeys;
}
