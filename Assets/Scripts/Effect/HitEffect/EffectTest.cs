using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class EffectTest : MonoBehaviour
{
    [SerializeField] private EffectManager _effectManager;
    void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            Debug.Log("osareta");
            _effectManager.PlayEffect("HitEffect_T", transform.position);
        }
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            Debug.Log("osareta");
            _effectManager.PlayEffect("HitEffect_W", transform.position);
        }
    }
}


//public async UniTask PlayAttachedAsync(Transform parent)
//{
//    _view.transform.SetParent(parent, false);
//    _view.transform.localPosition = Vector3.zero;
//    _view.transform.localRotation = Quaternion.identity;

//    _view.Play();

//    await UniTask.WaitUntil(() => !_view.IsAlive());

//    Dispose();
//}

//public async UniTask PlayAtAsync(Vector3 position)
//{
//    _view.transform.SetParent(null, false);
//    _view.transform.position = position;
//    _view.transform.rotation = Quaternion.identity;

//    _view.Play();

//    await UniTask.WaitUntil(() => !_view.IsAlive());

//    Dispose();
//}
