using Cysharp.Threading.Tasks;
using UnityEngine;

public class BlinkEffect
{
    private readonly Renderer[] _renderers;

    float _blinkSpeed = 1f;
    
    private bool _isBlink;

    public BlinkEffect(Renderer[] renderers)
    {
        _renderers = renderers;
    }

    public void StartBlink()
    {
        if (_isBlink) return;

        _isBlink = true;

        BlinkLoop().Forget();
    }

    public void StopBlink()
    {
        _isBlink = false;

        foreach (var renderer in _renderers)
        {
            renderer.material.color = Color.white;
        }
    }

    private async UniTaskVoid BlinkLoop()
    {
        while(_isBlink)
        {
            SetColor(Color.red);

            await UniTask.Delay(100);

            if (!_isBlink) break;

            SetColor(Color.white);

            await UniTask.Delay(100);
        }
    }

    private void SetColor(Color color)
    {
        foreach (Renderer renderer in _renderers)
        {
            Debug.Log(renderer.name);
            //renderer.material.color = color;
            renderer.material.SetColor("_BaseColor", color);
        }
    }
}
