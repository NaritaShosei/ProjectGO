using UnityEngine;

public class ResultUIManager : MonoBehaviour
{
    public ResultPanelView View => _view;

    [SerializeField] private ResultPanelView _view;
}
