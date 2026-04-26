using UnityEngine;

public class WarriorWeaponEffect : MonoBehaviour, IWeaponEffect
{
    //開始時の処理
    public void Play()
    {
        gameObject.SetActive(true);
    }

    // エフェクト停止時の処理
    public void Stop()
    {
        gameObject.SetActive(false);
    }
}
