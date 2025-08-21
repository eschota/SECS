using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private MeshRenderer[] meshRenderers;
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRenderers;
    [SerializeField] private Color playerColor;

    private Material[] originalMaterials;
    private Material[] coloredMaterials;

    public override void Spawned()
    {
        ValidateAndSerializeMeshRenderers();
        GenerateRandomBrightColor();
        ApplyEmissiveColor();
    }

    // ДВИЖЕНИЕ ТОЛЬКО У ВЛАДЕЛЬЦА!
    private void FixedUpdate()
    {
        if (!Object || !Object.HasInputAuthority) return;

        if (Camera.main != null)
        {
            transform.SetPositionAndRotation(
                Camera.main.transform.position,
                Camera.main.transform.rotation
            );
        }
    }

    [ContextMenu("Validate Mesh Renderers")]
    void ValidateAndSerializeMeshRenderers()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        var origMats = new List<Material>();
        foreach (var mr in meshRenderers) origMats.AddRange(mr.materials);
        foreach (var smr in skinnedMeshRenderers) origMats.AddRange(smr.materials);
        originalMaterials = origMats.ToArray();
    }

    void GenerateRandomBrightColor()
    {
        float hue = Random.Range(0f, 1f);
        float saturation = Random.Range(0.7f, 1f);
        float value = Random.Range(0.8f, 1f);
        playerColor = Color.HSVToRGB(hue, saturation, value);
    }

    void ApplyEmissiveColor()
    {
        foreach (var r in meshRenderers) ApplyColorToRenderer(r);
        foreach (var r in skinnedMeshRenderers) ApplyColorToSkinnedRenderer(r);
    }

    void ApplyColorToRenderer(MeshRenderer renderer)
    {
        var mats = renderer.materials;
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i]; if (m == null) continue;
            var nm = new Material(m);
            if (nm.HasProperty("_BaseColor")) nm.SetColor("_BaseColor", playerColor);
            else if (nm.HasProperty("_Color")) nm.SetColor("_Color", playerColor);
            if (nm.HasProperty("_EmissionColor"))
            {
                nm.SetColor("_EmissionColor", playerColor * 0.5f);
                nm.EnableKeyword("_EMISSION");
            }
            mats[i] = nm;
        }
        renderer.materials = mats;
    }

    void ApplyColorToSkinnedRenderer(SkinnedMeshRenderer renderer)
    {
        var mats = renderer.materials;
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i]; if (m == null) continue;
            var nm = new Material(m);
            if (nm.HasProperty("_BaseColor")) nm.SetColor("_BaseColor", playerColor);
            else if (nm.HasProperty("_Color")) nm.SetColor("_Color", playerColor);
            if (nm.HasProperty("_EmissionColor"))
            {
                nm.SetColor("_EmissionColor", playerColor * 0.5f);
                nm.EnableKeyword("_EMISSION");
            }
            mats[i] = nm;
        }
        renderer.materials = mats;
    }

    public void SetPlayerColor(Color c) { playerColor = c; ApplyEmissiveColor(); }
    public Color GetPlayerColor() => playerColor;
}
