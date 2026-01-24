using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Camera MainCamera => _mainCamera;

    private Camera _mainCamera;
    private void Awake()
    {
        _mainCamera = Camera.main;

        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<CameraManager>())
        {
            ServiceLocator.Unregister<CameraManager>();
        }
    }
}
