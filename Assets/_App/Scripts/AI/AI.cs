using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using UnityEngine;

/// <summary>
/// FOURNI. Le systeme d'IA, global : on s'en sert de partout, sans rien cabler.
///
///   Turn turn = AI.Ask(Agent.Architect, "Est-ce un bon emplacement ?");
///   AI.SetSceneContext("famille", "Un couple, deux enfants, un chien.");
///   AI.SetPlayerContext("role", "Je suis le pere.");
///
/// Contexte de la SCENE : envoye avec toutes les questions (lu sur le serveur).
/// Contexte du JOUEUR  : envoye seulement quand ce joueur pose une question.
///
/// Chaque question donne un Turn (entree + sortie), garde dans AI.History.
///
/// Reseau : le joueur demande au serveur (Command) ; seul le serveur appelle
/// l'IA ; chaque tour est partage avec tous les joueurs (SyncDictionary),
/// meme ceux qui arrivent en retard.
///
/// A poser une seule fois dans la scene, sur un objet "AI" avec une NetworkIdentity.
/// </summary>
[RequireComponent(typeof(AIText))]
public class AI : NetworkBehaviour
{
    // ---------------- Les agents : un par valeur de l'enum Agent ----------------

    [SerializeField] List<AgentConfig> agents = new List<AgentConfig>();

    // ---------------- Etat partage : tous les tours ----------------

    /// <summary>Ce qui voyage sur le reseau pour un tour (entree + sortie).</summary>
    public struct TurnData
    {
        // Entree
        public Agent agent;
        public string text;
        public string prompt;
        public string sceneContext;
        public string playerContext;
        public string elementContext;
        public string author;
        public Color32 authorColor;
        public uint authorNetId;
        public string time;
        // Sortie
        public string output;
        public bool done;
        public float duration;
        // Ordre d'arrivee sur le serveur
        public int order;
    }

    readonly SyncDictionary<int, TurnData> turns = new SyncDictionary<int, TurnData>();
    int nextOrder; // serveur seulement

    // ---------------- Etat local, sur chaque machine ----------------

    static AI instance;
    static readonly Dictionary<int, Turn> local = new Dictionary<int, Turn>();
    static readonly Dictionary<string, string> sceneFacts = new Dictionary<string, string>();
    static readonly Dictionary<string, string> playerFacts = new Dictionary<string, string>();
    static readonly List<ContextSource> sources = new List<ContextSource>();
    static readonly Dictionary<uint, int> playerNumbers = new Dictionary<uint, int>(); // serveur

    /// <summary>Un nouveau tour a commence (sur CHAQUE joueur, quel que soit l'auteur).</summary>
    public static event Action<Turn> Asked;

    AIText llm;

    // Remet a zero entre deux Play, meme sans rechargement du domaine.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        local.Clear();
        sceneFacts.Clear();
        playerFacts.Clear();
        sources.Clear();
        playerNumbers.Clear();
        Asked = null;
    }

    void Awake()
    {
        instance = this;
        llm = GetComponent<AIText>();
        FillAgents();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Dans l'editeur : une entree par agent de l'enum, ajoutee automatiquement.
    void Reset() => FillAgents();
    void OnValidate() => FillAgents();

    void FillAgents()
    {
        foreach (Agent agent in Enum.GetValues(typeof(Agent)))
            if (!agents.Exists(c => c.agent == agent))
                agents.Add(Agents.CreateDefault(agent));
    }

    // ======================= API pour tout le monde =======================

    /// <summary>
    /// Pose une question a un agent. "about" = le contexte de l'element concerne (optionnel).
    /// Rend le tour tout de suite : la reponse arrive ensuite, morceau par morceau.
    /// </summary>
    public static Turn Ask(Agent agent, string question, string about = "")
    {
        if (instance == null)
        {
            Debug.LogError("[AI] Il faut un objet AI dans la scene.");
            return null;
        }

        int id = UnityEngine.Random.Range(1, int.MaxValue);
        var turn = new Turn(id, agent, question);
        turn.Input.IsMine = true;
        local[id] = turn;
        Asked?.Invoke(turn);

        string player = DescribeFacts(playerFacts, ContextScope.Player); // seul ce casque le sait

        if (NetworkClient.active)
        {
            instance.CmdAsk(id, agent, question, about, player);
        }
        else
        {
            // Hors seance (test en solo) : on appelle l'IA directement.
            // Mirror desactive les objets de scene a NetworkIdentity tant qu'ils ne sont
            // pas spawnes : on reactive l'objet AI pour pouvoir lancer la coroutine.
            if (!instance.gameObject.activeSelf) instance.gameObject.SetActive(true);
            var data = NewTurnData(agent, question, about, player, "Moi", Color.white, 0);
            instance.Run(data, turn.Apply);
        }
        return turn;
    }

    /// <summary>Les reglages d'un agent (nom, role...). Modifiables aussi depuis le code.</summary>
    public static AgentConfig GetAgent(Agent agent)
    {
        if (instance != null)
        {
            var config = instance.agents.Find(c => c.agent == agent);
            if (config != null) return config;
        }
        return Agents.CreateDefault(agent);
    }

    /// <summary>Tous les tours connus, du plus ancien au plus recent.</summary>
    public static IReadOnlyList<Turn> History => local.Values.OrderBy(t => t.Order).ThenBy(t => t.Id).ToList();

    /// <summary>Le dernier tour connu d'un agent, ou null.</summary>
    public static Turn Latest(Agent agent) => History.LastOrDefault(t => t.Input.Agent == agent);

    /// <summary>Contexte de la SCENE (texte vide = retirer). A appeler sur tous les joueurs : le serveur le lit.</summary>
    public static void SetSceneContext(string key, string text) => SetFact(sceneFacts, key, text);

    /// <summary>Contexte de CE joueur (texte vide = retirer). Envoye avec ses questions.</summary>
    public static void SetPlayerContext(string key, string text) => SetFact(playerFacts, key, text);

    static void SetFact(Dictionary<string, string> facts, string key, string text)
    {
        if (string.IsNullOrEmpty(text)) facts.Remove(key);
        else facts[key] = text;
    }

    // Les ContextSource s'inscrivent toutes seules (OnEnable / OnDisable).
    internal static void Register(ContextSource source)
    {
        if (!sources.Contains(source)) sources.Add(source);
    }

    internal static void Unregister(ContextSource source) => sources.Remove(source);

    // ======================= Serveur =======================

    [Command(requiresAuthority = false)]
    void CmdAsk(int id, Agent agent, string question, string about, string player,
                NetworkConnectionToClient sender = null)
    {
        var identity = sender != null ? sender.identity : null;
        uint netId = identity != null ? identity.netId : 0;
        var playerColor = identity != null ? identity.GetComponent<PlayerColor>() : null;
        Color color = playerColor != null ? playerColor.Color : Color.white;

        var data = NewTurnData(agent, question, about, player, PlayerName(netId), color, netId);
        data.order = nextOrder++;

        Run(data, updated => turns[id] = updated); // -> envoye a tous les joueurs
    }

    static TurnData NewTurnData(Agent agent, string question, string about, string player,
                                string author, Color color, uint netId) => new TurnData
    {
        agent = agent,
        text = question,
        elementContext = about,
        playerContext = player,
        author = author,
        authorColor = color,
        authorNetId = netId,
        time = DateTime.Now.ToString("HH:mm"),
        output = "",
    };

    // "Joueur 1", "Joueur 2"... dans l'ordre de leur premiere question.
    static string PlayerName(uint netId)
    {
        if (!playerNumbers.TryGetValue(netId, out int number))
            playerNumbers[netId] = number = playerNumbers.Count + 1;
        return "Joueur " + number;
    }

    // Construit le prompt, appelle l'IA, et publie le tour a chaque morceau.
    void Run(TurnData data, Action<TurnData> publish)
    {
        AgentConfig config = GetAgent(data.agent);

        data.sceneContext = DescribeFacts(sceneFacts, ContextScope.Scene);
        data.prompt = config.useContext ? BuildPrompt(data) : data.text;
        Debug.Log($"[AI] {config.displayName} recoit :\n{data.prompt}");
        publish(data);

        float start = Time.time;
        llm.Ask(config.role, data.prompt,
            piece => { data.output += piece; publish(data); },
            full =>
            {
                data.output = full;
                data.done = true;
                data.duration = Time.time - start;
                publish(data);
            });
    }

    // L'ordre est toujours le meme : scene, joueur, element, question.
    static string BuildPrompt(TurnData data)
    {
        var prompt = new StringBuilder();
        prompt.Append("Contexte de la scene :\n").Append(data.sceneContext);
        prompt.Append("Contexte du visiteur :\n").Append(data.playerContext);
        if (!string.IsNullOrEmpty(data.elementContext))
            prompt.Append("Element concerne :\n").Append(data.elementContext);
        prompt.Append("Question : ").Append(data.text);
        return prompt.ToString();
    }

    static string DescribeFacts(Dictionary<string, string> facts, ContextScope scope)
    {
        var text = new StringBuilder();
        foreach (var fact in facts.Values) text.AppendLine("- " + fact);
        text.Append(ContextSource.DescribeAll(sources, scope));
        return text.Length > 0 ? text.ToString() : "- (rien)\n";
    }

    // ======================= Chaque joueur =======================

    public override void OnStartClient()
    {
        turns.OnAdd += OnTurnChanged;
        turns.OnSet += OnTurnSet;

        // Un joueur en retard recoit les tours existants sans OnAdd : on les lit ici.
        foreach (var pair in turns) OnTurnChanged(pair.Key);
    }

    public override void OnStopClient()
    {
        turns.OnAdd -= OnTurnChanged;
        turns.OnSet -= OnTurnSet;
    }

    void OnTurnSet(int id, TurnData oldData) => OnTurnChanged(id);

    void OnTurnChanged(int id)
    {
        TurnData data = turns[id];

        if (local.TryGetValue(id, out Turn turn))
        {
            turn.Apply(data);
            return;
        }

        // Nouveau tour : on le remplit AVANT de prevenir les autres scripts.
        // Un joueur en retard recoit donc les anciens tours deja finis
        // (Done = vrai) : ils ne declenchent pas Finished, rien n'est relu a voix haute.
        turn = new Turn(id, data.agent, data.text);
        turn.Input.IsMine = NetworkClient.localPlayer != null && NetworkClient.localPlayer.netId == data.authorNetId;
        turn.Apply(data);
        local[id] = turn;
        Asked?.Invoke(turn);
    }
}
