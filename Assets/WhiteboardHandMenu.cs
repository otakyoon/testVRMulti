using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Menu d'outils accroche a la main gauche : couleurs, epaisseurs, gomme,
/// annuler, tout effacer. Apparait quand on tourne la paume vers soi, se
/// clique au rayon de la main droite (comme le panneau reseau).
///
/// Objet de SCENE, local, jamais replique. A poser sur un GameObject portant
/// un Canvas World Space et un TrackedDeviceGraphicRaycaster. L'interface est
/// construite au demarrage : rien a cabler dans l'inspecteur.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class WhiteboardHandMenu : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Decalage du menu au-dessus de la main (m, repere monde).")]
    [SerializeField] Vector3 offset = new Vector3(0f, 0.12f, 0f);
    [Tooltip("Axe local de la main gauche qui sort de la paume. " +
             "+X pour la pose 'grip' OpenXR d'un controleur gauche.")]
    [SerializeField] Vector3 palmAxis = Vector3.right;
    [Tooltip("Affiche le menu quand la paume est tournee vers la tete au-dela de ce seuil (cos).")]
    [SerializeField, Range(0f, 1f)] float showThreshold = 0.5f;
    [Tooltip("Le masque sous ce seuil : l'ecart evite le clignotement.")]
    [SerializeField, Range(0f, 1f)] float hideThreshold = 0.25f;
    [Tooltip("Pour le debogage ou si le geste gene : menu toujours visible.")]
    [SerializeField] bool alwaysVisible = false;

    [Header("Apparence")]
    [SerializeField] Color background = new Color(0.10f, 0.11f, 0.13f, 0.95f);
    [SerializeField] Color neutral = new Color(0.22f, 0.24f, 0.28f, 1f);
    [SerializeField] Color selected = new Color(0.20f, 0.45f, 0.80f, 1f);
    [SerializeField] Color danger = new Color(0.60f, 0.20f, 0.20f, 1f);

    const float ConfirmDelay = 3f;

    Canvas canvas;
    CanvasGroup group;
    bool visible;

    readonly List<(Outline frame, Color ink)> swatches = new List<(Outline, Color)>();
    readonly List<(Image bg, float width)> widthButtons = new List<(Image, float)>();
    Image eraserBg;
    Button undoButton;
    TextMeshProUGUI clearLabel;
    Image clearBg;
    float clearArmedUntil = -1f;

    VRMarker cachedMarker;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        BuildUI();
        SetVisible(alwaysVisible);
    }

    void LateUpdate()
    {
        var anchors = LocalRigAnchors.Instance;
        Transform hand = anchors != null ? anchors.LeftHand : null;
        Transform head = anchors != null ? anchors.Head : null;
        if (hand == null || head == null)
        {
            SetVisible(false);
            return;
        }

        // Placement : au-dessus de la main, tourne vers les yeux.
        Vector3 pos = hand.position + offset;
        transform.position = pos;
        Vector3 look = pos - head.position;
        if (look.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.LookRotation(look, Vector3.up);

        // Visibilite : paume tournee vers la tete, avec hysteresis.
        Vector3 palm = hand.TransformDirection(palmAxis).normalized;
        float facing = Vector3.Dot(palm, (head.position - hand.position).normalized);
        bool want = alwaysVisible || (visible ? facing > hideThreshold : facing > showThreshold);
        SetVisible(want);

        if (visible) RefreshState();
    }

    void SetVisible(bool on)
    {
        visible = on;
        canvas.enabled = on;
        group.interactable = on;
        group.blocksRaycasts = on;
        if (!on) Disarm();
    }

    /// <summary>
    /// Le marqueur du joueur local. En solo (hors reseau), on se rabat sur
    /// le premier marqueur de la scene.
    /// </summary>
    VRMarker Marker
    {
        get
        {
            if (cachedMarker != null) return cachedMarker;
            if (NetworkClient.localPlayer != null)
                cachedMarker = NetworkClient.localPlayer.GetComponentInChildren<VRMarker>();
            else if (!NetworkClient.active)
                cachedMarker = FindAnyObjectByType<VRMarker>();
            return cachedMarker;
        }
    }

    // ---------------- Actions ----------------

    // Pas de "?." : il contourne le test de destruction des objets Unity.
    void WithMarker(System.Action<VRMarker> action)
    {
        var m = Marker;
        if (m != null) action(m);
        Disarm();
    }

    void PickColor(Color c) => WithMarker(m => m.SetColor(c));
    void PickWidth(float w) => WithMarker(m => m.SetWidth(w));
    void ToggleEraser() => WithMarker(m => m.SetEraser(!m.IsErasing));
    void Undo() => WithMarker(m => m.Undo());

    /// <summary>Deux clics : le premier arme, le second (sous 3 s) efface.</summary>
    void Clear()
    {
        if (Time.time < clearArmedUntil)
        {
            WithMarker(m => m.ClearBoard());
        }
        else
        {
            clearArmedUntil = Time.time + ConfirmDelay;
        }
    }

    void Disarm() => clearArmedUntil = -1f;

    // ---------------- Etat ----------------

    void RefreshState()
    {
        var m = Marker;
        bool has = m != null;

        foreach (var (frame, ink) in swatches)
            frame.enabled = has && !m.IsErasing && SameColor(m.Color, ink);

        foreach (var (bg, w) in widthButtons)
            bg.color = has && !m.IsErasing && Mathf.Approximately(m.Width, w) ? selected : neutral;

        eraserBg.color = has && m.IsErasing ? selected : neutral;
        undoButton.interactable = has && m.CanUndo;

        bool armed = Time.time < clearArmedUntil;
        clearLabel.text = armed ? "Confirmer ?" : "Tout effacer";
        clearBg.color = armed ? new Color(0.85f, 0.25f, 0.25f) : danger;
    }

    // La couleur joueur transite en Color32 : l'arrondi a 1/255 casse l'egalite stricte.
    static bool SameColor(Color a, Color b) => ((Vector4)(a - b)).sqrMagnitude < 1e-4f;

    // ---------------- Construction de l'UI ----------------

    void BuildUI()
    {
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(420, 400);
        // ~17 x 16 cm : lisible a bout de bras, sans masquer le tableau.
        rt.localScale = Vector3.one * 0.0004f;

        var bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = background;

        var column = NewRect("Column", transform);
        Stretch(column);
        var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 12;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        // Couleurs
        var colors = Row(column, 80);
        foreach (var ink in InkPalette.Inks)
        {
            var c = ink;
            MakeButton(colors, c, () => PickColor(c), out var img);
            // Cadre de selection : un contour blanc autour de la pastille.
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(5, -5);
            outline.enabled = false;
            swatches.Add((outline, c));
        }

        // Epaisseurs : un apercu du trait dans chaque bouton.
        var widths = Row(column, 70);
        foreach (var w in InkPalette.Widths)
        {
            float width = w;
            MakeButton(widths, neutral, () => PickWidth(width), out var img);
            var line = NewRect("Line", img.transform);
            line.anchorMin = new Vector2(0.2f, 0.5f);
            line.anchorMax = new Vector2(0.8f, 0.5f);
            // 2048 px de texture : meme rapport que sur le tableau, en plus epais pour la lisibilite.
            line.sizeDelta = new Vector2(0, Mathf.Max(3f, width * 2048f * 1.5f));
            var lineImg = line.gameObject.AddComponent<Image>();
            lineImg.color = Color.white;
            lineImg.raycastTarget = false;
            widthButtons.Add((img, width));
        }

        // Gomme / Annuler
        var tools = Row(column, 80);
        MakeTextButton(tools, "Gomme", neutral, ToggleEraser, out eraserBg);
        undoButton = MakeTextButton(tools, "Annuler", neutral, Undo, out _);

        // Tout effacer
        var clearRow = Row(column, 80);
        MakeTextButton(clearRow, "Tout effacer", danger, Clear, out clearBg);
        clearLabel = clearBg.GetComponentInChildren<TextMeshProUGUI>();
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false);
        return r;
    }

    static void Stretch(RectTransform r, float inset = 0f)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }

    static RectTransform Row(Transform parent, float height)
    {
        var row = NewRect("Row", parent);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;
        row.gameObject.AddComponent<LayoutElement>().minHeight = height;
        return row;
    }

    static Button MakeButton(Transform parent, Color color, UnityAction onClick, out Image img)
    {
        var go = NewRect("Button", parent).gameObject;
        img = go.AddComponent<Image>();
        img.color = color;
        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);
        return button;
    }

    static Button MakeTextButton(Transform parent, string text, Color color, UnityAction onClick, out Image img)
    {
        var button = MakeButton(parent, color, onClick, out img);
        var label = NewRect("Label", button.transform);
        Stretch(label);
        var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 30;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return button;
    }
}
