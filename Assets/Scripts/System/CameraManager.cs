using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Camera MainCamera => _mainCamera;

    private Camera _mainCamera;
    private void Awake()
    {
        _mainCamera = Camera.main;
    }
}
