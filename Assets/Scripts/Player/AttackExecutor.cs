using UnityEngine;

public class AttackExecutor : MonoBehaviour
{
    public bool Execute(AttackData_main attackData, AttackInput input)
    {
        Debug.Log($"{attackData.AttackName}で攻撃");
        return true;
    }
}
