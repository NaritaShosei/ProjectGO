using Cysharp.Threading.Tasks;
using UnityEngine;

public class BlinkEffect
{
    private readonly Renderer[] _renderers;

    private int _blinkSpeed;
    
    private bool _isBlink;

    public BlinkEffect(Renderer[] renderers ,int blinkSpeed)
    {
        _renderers = renderers;
        _blinkSpeed = blinkSpeed;
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

            await UniTask.Delay(_blinkSpeed);

            if (!_isBlink) break;

            SetColor(Color.white);

            await UniTask.Delay(_blinkSpeed);
        }
    }

    private void SetColor(Color color)
    {
        foreach (Renderer renderer in _renderers)
        {
            renderer.material.SetColor("_BaseColor", color);
        }
    }
}
