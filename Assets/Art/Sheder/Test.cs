using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField]
    private Renderer targetRenderer;

    private Material material;

    private void Start()
    {
        material = targetRenderer.materials[3];
    }

    [ContextMenu("Flash")]
    public void Flash()
    {
        material.SetFloat("_FlashStartTime", Time.time);
    }
}
