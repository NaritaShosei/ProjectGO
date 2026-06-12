using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveSequenceData", menuName = "GameData/Wave Sequence Data")]
public class WaveSequenceData : ScriptableObject
{
    public List<WaveData> Waves = new();
}
