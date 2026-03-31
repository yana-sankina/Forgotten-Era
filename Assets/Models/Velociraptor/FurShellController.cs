using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FurShellController : MonoBehaviour
{
    public static readonly List<FurShellController> ActiveControllers = new();

    [Header("Original Skin")]
    [Tooltip("ПОЛОЖИ СЮДА ОРИГИНАЛЬНЫЙ МАТЕРИАЛ ДИНОЗАВРА!")]
    public Material originalMaterial; // <-- НОВАЯ ПЕРЕМЕННАЯ ДЛЯ ИСХОДНИКА

    [Header("Shell Settings")]
    [Range(4, 128)]
    public int shellCount = 32;

    [Header("Fur Appearance")]
    [Range(0.0001f, 1f)]
    public float shellLength = 0.05f;

    [Range(5, 200)]
    public float density = 30f;

    [Range(0.01f, 1f)]
    public float thickness = 0.7f;

    [Range(1f, 6f)]
    public float elongation = 3f;

    [Range(0f, 5f)]
    public float occlusionAttenuation = 1.5f;

    [Header("Colors")]
    public Color baseColor = new Color(0.45f, 0.35f, 0.25f, 1f);
    public Color tipColor = new Color(0.65f, 0.55f, 0.4f, 1f);

    [Header("Comb Direction")]
    [Tooltip("Local-space direction fur is combed toward (e.g. 0,0,-1 for tail)")]
    public Vector3 combDirection = new Vector3(0f, 0f, -1f);

    [Range(0f, 2f)]
    public float combStrength = 0.3f;

    Material baseMaterial;         // shell 0, assigned to the renderer
    Material[] shellMaterials;     // shells 1..N, one material each with _ShellIndex baked in
    Renderer targetRenderer;

    public Material[] ShellMaterials => shellMaterials;
    public Renderer TargetRenderer => targetRenderer;
    public int ShellCount => shellCount;

    void OnEnable()
    {
        ActiveControllers.Add(this);
        targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<MeshRenderer>();
        SetupMaterials();
    }

    void OnDisable()
    {
        ActiveControllers.Remove(this);
        CleanupMaterials();
    }

    void OnValidate()
    {
        if (baseMaterial != null)
        {
            if (shellMaterials == null || shellMaterials.Length != shellCount - 1)
                SetupMaterials();
            else
                UpdateAllMaterials();
        }
    }

    void Update()
    {
        if (baseMaterial != null)
            UpdateAllMaterials();
    }

    void SetupMaterials()
    {
        if (targetRenderer == null) return;

        CleanupMaterials();

        var shader = Shader.Find("Custom/DinoFurShell");
        if (shader == null)
        {
            Debug.LogError("FurShellController: Shader 'Custom/DinoFurShell' not found.");
            return;
        }

        // БЕРЕМ ТЕКСТУРЫ ТОЛЬКО ИЗ СОХРАНЕННОГО ИСТОЧНИКА!
        var orig = originalMaterial; 

        baseMaterial = new Material(shader);
        baseMaterial.SetFloat("_ShellIndex", 0);
        baseMaterial.SetFloat("_ShellCount", shellCount);
        if (orig != null) CopyTextures(orig, baseMaterial);
        targetRenderer.material = baseMaterial;

        shellMaterials = new Material[shellCount - 1];
        for (int i = 0; i < shellMaterials.Length; i++)
        {
            shellMaterials[i] = new Material(shader);
            shellMaterials[i].SetFloat("_ShellIndex", i + 1);
            shellMaterials[i].SetFloat("_ShellCount", shellCount);
            if (orig != null) CopyTextures(orig, shellMaterials[i]);
        }

        UpdateAllMaterials();
    }

    void CleanupMaterials()
    {
        // Добавлено удаление baseMaterial (иначе память утекает)
        if (baseMaterial != null)
        {
            if (Application.isPlaying) Destroy(baseMaterial);
            else DestroyImmediate(baseMaterial);
            baseMaterial = null;
        }

        if (shellMaterials != null)
        {
            foreach (var m in shellMaterials)
            {
                if (m != null)
                {
                    if (Application.isPlaying) Destroy(m);
                    else DestroyImmediate(m);
                }
            }
            shellMaterials = null;
        }
    }

    static void CopyTextures(Material src, Material dst)
    {
        // ВАЖНО: Убедись, что твой оригинальный материал использует именно эти названия текстур!
        // В стандартном URP основная текстура называется "_BaseMap", а не "baseColorTexture".
        CopyTex(src, "baseColorTexture", dst);
        CopyTex(src, "_BaseMap", dst); // Я добавил подхват URP текстуры на всякий случай
        CopyTex(src, "_MainTex", dst); // И стандартной текстуры тоже
        CopyTex(src, "normalTexture", dst);
        CopyTex(src, "occlusionTexture", dst);
        CopyTex(src, "metallicRoughnessTexture", dst);
    }

    static void CopyTex(Material src, string name, Material dst)
    {
        if (src.HasProperty(name) && src.HasTexture(name))
        {
            var tex = src.GetTexture(name);
            if (tex != null) dst.SetTexture(name, tex);
        }
    }

    void UpdateAllMaterials()
    {
        UpdateMat(baseMaterial);
        if (shellMaterials != null)
            foreach (var m in shellMaterials)
                if (m != null) UpdateMat(m);
    }

    void UpdateMat(Material m)
    {
        m.SetFloat("_ShellLength", shellLength);
        m.SetFloat("_ShellCount", shellCount);
        m.SetFloat("_Density", density);
        m.SetFloat("_Thickness", thickness);
        m.SetFloat("_Elongation", elongation);
        m.SetFloat("_OcclusionAttenuation", occlusionAttenuation);
        m.SetColor("_BaseColor", baseColor);
        m.SetColor("_TipColor", tipColor);
        m.SetVector("_CombDir", combDirection.normalized);
        m.SetFloat("_CombStrength", combStrength);
    }
}