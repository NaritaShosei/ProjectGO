using UnityEngine;

public class RotateY : MonoBehaviour
{
    [SerializeField] private float _rotateSpeed = 90f;

    private void Update()
    {
        transform.Rotate(0f, _rotateSpeed * Time.deltaTime, 0f);
    }
}
