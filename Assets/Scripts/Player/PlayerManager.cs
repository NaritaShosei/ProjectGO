using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private PlayerMove _move;
    [SerializeField] private PlayerAttacker _attacker;
    [SerializeField] private InputHandler _input;

    private void Awake()
    {
        _move.Init(this,_input);   
        _attacker.Init(this,_input);   
    }
}
