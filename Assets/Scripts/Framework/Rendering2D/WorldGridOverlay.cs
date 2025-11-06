using UnityEngine;

/// <summary>
/// Scales a world-space quad to match the camera view and drives grid LOD.
/// Put on the same GameObject as the MeshRenderer with the grid material.
/// </summary>
[ExecuteAlways]
public class WorldGridOverlay : MonoBehaviour
{
    public Camera targetCamera;
    public Material gridMaterial;

    [Header("LOD")]
    public float targetCellsTall = 2f; // Number of lines on screen before the cells show a 10x10 bigger area
    public float baseStep = 10f; // Smallest step
    public int majorEvery = 10; // Thick line step
    public float pixelThickness = 1.0f; // Grid line thickness (screen pixels)
    public float blendSpan = 0.15f; // fraction of a decade to blend (e.g. 0.15 = 15%)


    private void Awake()
    {
        var r = GetComponent<Renderer>();
        r.sortingLayerName = "MapOverlay";
        r.sortingOrder = 0; // any
    }

    void Reset()
    {
        targetCamera = Camera.main;
        var mr = GetComponent<MeshRenderer>();
        if (mr) gridMaterial = mr.sharedMaterial;
    }

    void LateUpdate()
    {
        if (!targetCamera || !gridMaterial) return;

        float h = targetCamera.orthographicSize * 2f;
        float w = h * targetCamera.aspect;

        transform.position = new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, 0f);
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(w, h, 1f);

        // --- LOD calc ---
        float ideal = h / targetCellsTall;
        float k = Mathf.Log10(Mathf.Max(ideal / baseStep, 1e-6f));
        int eLo = Mathf.FloorToInt(k);
        int eHi = eLo + 1;

        float stepLo = baseStep * Mathf.Pow(10f, eLo);
        float stepHi = baseStep * Mathf.Pow(10f, eHi);

        // Only blend in the top 'blendSpan' slice of the decade
        float startBlend = eHi - blendSpan;
        float t = Mathf.Clamp01((k - startBlend) / blendSpan);

        gridMaterial.SetFloat("_StepA", stepLo);
        gridMaterial.SetFloat("_StepB", stepHi);
        gridMaterial.SetFloat("_Blend", t);

        // --- Push params ---
        gridMaterial.SetFloat("_StepA", stepLo);
        gridMaterial.SetFloat("_StepB", stepHi);
        gridMaterial.SetFloat("_Blend", t);
        gridMaterial.SetFloat("_MajorEvery", majorEvery);
        gridMaterial.SetFloat("_PxThickness", pixelThickness);

        // constant screen-space thickness: convert px -> world units (Y axis)
        float worldPerPixel = h / Screen.height; // = (2*orthoSize)/Screen.height
        gridMaterial.SetFloat("_WorldPerPixel", worldPerPixel);
    }
}
