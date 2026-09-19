using System.Collections.Generic;
using Mirror;
using Mirror.Discovery;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Panneau de controle reseau en world-space, utilisable au rayon depuis une
/// manette VR. Remplace les HUD IMGUI de Mirror, qui ne sont ni visibles ni
/// cliquables dans un casque (IMGUI dessine en coordonnees ecran et attend un
/// clic souris).
///
/// A poser sur un GameObject portant un Canvas en World Space et un
/// TrackedDeviceGraphicRaycaster. Le panneau construit toute son interface
/// lui-meme au demarrage : rien a cabler dans l'inspecteur.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class NetworkPanel : MonoBehaviour
{
    [Header("Apparence")]
    [SerializeField] Color background = new Color(0.10f, 0.11f, 0.13f, 0.95f);
    [SerializeField] Color accent = new Color(0.20f, 0.45f, 0.80f, 1f);
    [SerializeField] Color neutral = new Color(0.22f, 0.24f, 0.28f, 1f);
    [SerializeField] Color danger = new Color(0.60f, 0.20f, 0.20f, 1f);

    NetworkDiscovery discovery;

    TextMeshProUGUI statusLabel;
    Transform actionsRoot;
    Transform serversRoot;
    TextMeshProUGUI serversLabel;

    Button hostButton, findButton, serverOnlyButton, stopButton;

    readonly Dictionary<long, ServerResponse> found = new Dictionary<long, ServerResponse>();

    // Etat affiche la frame precedente, pour ne reconstruire que sur changement.
    string lastState = null;

    void Awake()
    {
        // Resolu au runtime : le panneau reste autonome meme si on le deplace
        // ou si l'objet Network est renomme.
        discovery = FindFirstObjectByType<NetworkDiscovery>();
        if (discovery == null)
            Debug.LogWarning("[NetworkPanel] Aucun NetworkDiscovery dans la scene : " +
                             "la recherche de serveurs sera indisponible.", this);

        BuildUI();
    }

    void OnEnable()
    {
        if (discovery != null)
            discovery.OnServerFound.AddListener(OnServerFound);
    }

    void OnDisable()
    {
        if (discovery != null)
            discovery.OnServerFound.RemoveListener(OnServerFound);
    }

    void Update() => RefreshState();

    // ---------------- Actions ----------------

    void Host()
    {
        found.Clear();
        NetworkManager.singleton.StartHost();
        if (discovery != null) discovery.AdvertiseServer();
    }

    void ServerOnly()
    {
        found.Clear();
        NetworkManager.singleton.StartServer();
        if (discovery != null) discovery.AdvertiseServer();
    }

    void Find()
    {
        found.Clear();
        ClearServerList();
        if (discovery != null) discovery.StartDiscovery();
        SetStatus("Recherche d'une seance...");
    }

    void Stop()
    {
        if (NetworkServer.active && NetworkClient.isConnected) NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected) NetworkManager.singleton.StopClient();
        else if (NetworkServer.active) NetworkManager.singleton.StopServer();

        if (discovery != null) discovery.StopDiscovery();
        found.Clear();
        ClearServerList();
    }

    void OnServerFound(ServerResponse info)
    {
        if (found.ContainsKey(info.serverId)) return;
        found[info.serverId] = info;

        string label = info.EndPoint != null ? info.EndPoint.Address.ToString() : info.uri.ToString();
        MakeButton(serversRoot, $"Rejoindre  {label}", accent, () =>
        {
            if (discovery != null) discovery.StopDiscovery();
            NetworkManager.singleton.StartClient(info.uri);
        });

        serversLabel.text = $"Seances trouvees : {found.Count}";
    }

    // ---------------- Etat ----------------

    void RefreshState()
    {
        string state =
            NetworkServer.active && NetworkClient.active ? "host" :
            NetworkServer.active ? "server" :
            NetworkClient.isConnected ? "client" :
            NetworkClient.active ? "connecting" : "idle";

        if (state == lastState) return;
        lastState = state;

        bool running = state != "idle";

        hostButton.gameObject.SetActive(!running);
        findButton.gameObject.SetActive(!running);
        serverOnlyButton.gameObject.SetActive(!running);
        stopButton.gameObject.SetActive(running);
        serversRoot.gameObject.SetActive(!running);
        serversLabel.gameObject.SetActive(!running);

        switch (state)
        {
            case "host":       SetStatus("Vous hebergez la seance."); break;
            case "server":     SetStatus("Serveur seul : vous ne dessinez pas."); break;
            case "client":     SetStatus("Connecte a la seance."); break;
            case "connecting": SetStatus("Connexion..."); break;
            default:           SetStatus("Hors ligne."); break;
        }
    }

    void SetStatus(string text)
    {
        if (statusLabel != null) statusLabel.text = text;
    }

    void ClearServerList()
    {
        if (serversRoot == null) return;
        for (int i = serversRoot.childCount - 1; i >= 0; i--)
            Destroy(serversRoot.GetChild(i).gameObject);
        if (serversLabel != null) serversLabel.text = "Seances trouvees : 0";
    }

    // ---------------- Construction de l'UI ----------------

    void BuildUI()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(600, 760);

        // Fond
        var bg = gameObject.GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = background;

        // Colonne principale
        var column = new GameObject("Column", typeof(RectTransform)).GetComponent<RectTransform>();
        column.SetParent(transform, false);
        Stretch(column);
        var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 28, 28);
        layout.spacing = 14;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        MakeLabel(column, "Seance partagee", 44, FontStyles.Bold, Color.white, 64);
        statusLabel = MakeLabel(column, "Hors ligne.", 28, FontStyles.Normal,
                                new Color(0.75f, 0.78f, 0.82f), 44);

        actionsRoot = column;

        hostButton = MakeButton(column, "Heberger la seance", accent, Host);
        findButton = MakeButton(column, "Chercher une seance", neutral, Find);
        serverOnlyButton = MakeButton(column, "Serveur seul (sans dessiner)", neutral, ServerOnly);
        stopButton = MakeButton(column, "Quitter la seance", danger, Stop);

        serversLabel = MakeLabel(column, "Seances trouvees : 0", 26, FontStyles.Normal,
                                 new Color(0.75f, 0.78f, 0.82f), 40);

        var list = new GameObject("Servers", typeof(RectTransform)).GetComponent<RectTransform>();
        list.SetParent(column, false);
        var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 10;
        listLayout.childControlHeight = false;
        listLayout.childControlWidth = true;
        listLayout.childForceExpandWidth = true;
        serversRoot = list;

        stopButton.gameObject.SetActive(false);
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    TextMeshProUGUI MakeLabel(Transform parent, string text, float size,
                              FontStyles style, Color color, float height)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;

        go.AddComponent<LayoutElement>().minHeight = height;
        return tmp;
    }

    Button MakeButton(Transform parent, string text, Color color, UnityAction onClick)
    {
        var go = new GameObject(text, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        // Cible confortable au rayon : viser petit en VR est penible.
        go.AddComponent<LayoutElement>().minHeight = 86;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        Stretch((RectTransform)labelGo.transform);

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 30;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return button;
    }
}
