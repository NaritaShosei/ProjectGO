using UnityEngine;

public interface ICharacter
{
    /// <summary>
    /// ロックオンなどの中心のTransformを取得する
    /// </summary>
    public Transform GetTargetCenter();
}
