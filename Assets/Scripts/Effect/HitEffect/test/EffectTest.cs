using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EffectTest : MonoBehaviour
{
    [SerializeField] private GameObject _thunderPrefab;
    [SerializeField] private GameObject _warriorPrefab;

    [SerializeField] private Transform _spawnPoint; 
    private void Update()
    {
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            Vector3 pos = new Vector3(0, 0, 0);

            _manager.PlayEffect("TestHitEffect_Thunder", pos);
        }
    }
    /// <summary>
    /// マウス位置をワールド座標に変換
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit))
        {
            return hit.point;
        }

        return ray.origin + ray.direction * 5f;
    }


    [SerializeField] private EffectManager _manager;

}
