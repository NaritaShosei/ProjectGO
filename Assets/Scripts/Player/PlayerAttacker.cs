using UnityEngine;

public class PlayerAttacker : MonoBehaviour
{
    private PlayerManager _manager;
    private InputHandler _input;

    public void Init(PlayerManager manager, InputHandler input)
    {
        _manager = manager;
        _input = input;
    }

}
