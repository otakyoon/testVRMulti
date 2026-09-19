using UnityEngine;

/// <summary>
/// Rendu LOCAL du tableau : une RenderTexture sur laquelle on "tamponne" un brush.
/// Aucune logique reseau ici — chaque client dessine la meme chose a partir
/// des memes donnees de traits.
/// Prerequis : le quad doit avoir un MeshCollider (un BoxCollider ne renvoie
/// pas de RaycastHit.textureCoord).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class WhiteboardSurface : MonoBehaviour
{
    [SerializeField] int resolution = 2048;
    [SerializeField] Texture2D brush;          // tache ronde, blanche, alpha degrade sur les bords
    [SerializeField] Material blitMaterial;    // materiau utilisant Unlit/WhiteboardBrush
    [SerializeField] Color background = Color.white;

    RenderTexture rt;

    void Awake()
    {
        rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);
        rt.Create();
        GetComponent<Renderer>().material.mainTexture = rt;
        Clear();
    }

    void OnDestroy()
    {
        if (rt != null) rt.Release();
    }

    public void Clear()
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, background);
        RenderTexture.active = prev;
    }

    /// <summary>
    /// Trace un segment entre deux coordonnees UV (0..1) en tamponnant le brush.
    /// width est exprime en fraction d'UV (ex : 0.004 = 0.4 % de la largeur du tableau).
    /// </summary>
    public void DrawSegment(Vector2 uvA, Vector2 uvB, Color color, float width)
    {
        if (blitMaterial == null || brush == null) return;

        float sizePx = Mathf.Max(1f, width * resolution);
        float distPx = Vector2.Distance(uvA, uvB) * resolution;
        // un tampon tous les quarts de brush : trait continu sans sur-dessiner
        int steps = Mathf.Clamp(Mathf.CeilToInt(distPx / (sizePx * 0.25f)), 1, 256);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, resolution, resolution, 0);

        blitMaterial.SetColor("_Color", color);

        for (int i = 0; i <= steps; i++)
        {
            Vector2 uv = Vector2.Lerp(uvA, uvB, i / (float)steps);
            var r = new Rect(uv.x * resolution - sizePx * 0.5f,
                             (1f - uv.y) * resolution - sizePx * 0.5f,
                             sizePx, sizePx);
            Graphics.DrawTexture(r, brush, blitMaterial);
        }

        GL.PopMatrix();
        RenderTexture.active = prev;
    }
}
