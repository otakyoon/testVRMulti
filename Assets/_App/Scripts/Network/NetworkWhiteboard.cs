using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Un paquet de points appartenant a un trait. On n'envoie JAMAIS la texture
/// sur le reseau, uniquement la geometrie du trait.
/// </summary>
public struct StrokeChunk
{
    public int strokeId;      // identifie le trait, pour raccorder les paquets et gerer l'undo
    public Color32 color;
    public float width;       // en fraction d'UV
    public Vector2[] points;  // UV 0..1
}

/// <summary>
/// A poser sur le meme GameObject que le WhiteboardSurface.
/// Le serveur (= le host) detient l'historique et le rejoue aux arrivants tardifs.
///
/// Fonctionne aussi HORS SEANCE (test en solo, sans host) : SubmitChunk,
/// RequestUndo et RequestClear passent par le reseau pendant une seance, et
/// s'appliquent en local sinon. Le VRMarker n'appelle jamais les Cmd directement.
/// </summary>
[RequireComponent(typeof(WhiteboardSurface))]
public class NetworkWhiteboard : NetworkBehaviour
{
    // Un lot de 20 chunks ~ 1 ko : loin des limites de message de KCP.
    const int HistoryChunkSize = 20;

    WhiteboardSurface surface;

    // Historique de ce qui est affiche. Fait foi sur le serveur ; les clients
    // distants en gardent une copie pour que l'undo marche encore apres la seance.
    readonly List<StrokeChunk> history = new List<StrokeChunk>();
    // Dernier point connu par trait, pour raccorder deux paquets consecutifs.
    readonly Dictionary<int, Vector2> lastPoint = new Dictionary<int, Vector2>();
    // Traits deja dessines localement par prediction : leur echo RPC est ignore.
    readonly HashSet<int> predictedStrokes = new HashSet<int>();

    // Tous les tableaux de la scene, meme desactives (cf. EnsureVisibleOffline).
    static readonly List<NetworkWhiteboard> all = new List<NetworkWhiteboard>();

    void Awake()
    {
        surface = GetComponent<WhiteboardSurface>();
        all.Add(this);
    }

    void OnDestroy() => all.Remove(this);

    public Color Background => surface.Background;

    /// <summary>Vrai pendant une seance : les actions passent par le serveur.</summary>
    bool Online => isClient;

    /// <summary>
    /// En fin de seance, Mirror DESACTIVE les objets de scene reseau : le
    /// tableau disparaitrait. Appele chaque frame hors seance par SoloMarker.
    /// </summary>
    public static void EnsureVisibleOffline()
    {
        foreach (var b in all)
            if (b != null && !b.gameObject.activeSelf) b.gameObject.SetActive(true);
    }

    // ---------------- API utilisee par le VRMarker ----------------

    public void SubmitChunk(StrokeChunk chunk)
    {
        if (Online) CmdAddChunk(chunk);
        // Hors seance, les points sont deja traces par PredictPoint : on archive seulement.
        else if (chunk.points != null && chunk.points.Length > 0) history.Add(chunk);
    }

    public void RequestUndo(int strokeId)
    {
        if (Online) { CmdUndo(strokeId); return; }
        history.RemoveAll(s => s.strokeId == strokeId);
        ClearLocal();
        foreach (var c in history) Apply(c);
    }

    public void RequestClear()
    {
        if (Online) { CmdClear(); return; }
        history.Clear();
        ClearLocal();
    }

    // ---------------- Client -> Serveur ----------------

    [Command(requiresAuthority = false)]
    void CmdAddChunk(StrokeChunk chunk)
    {
        if (chunk.points == null || chunk.points.Length == 0) return;
        history.Add(chunk);
        RpcApplyChunk(chunk);
    }

    [Command(requiresAuthority = false)]
    void CmdClear()
    {
        history.Clear();
        RpcClear();
    }

    /// <summary>
    /// Annule un trait complet. On efface et on rejoue : simple et suffisant.
    /// Le rejeu part en lots — un RPC par chunk saturerait le buffer d'envoi
    /// KCP des que l'historique depasse quelques milliers d'entrees.
    /// </summary>
    [Command(requiresAuthority = false)]
    void CmdUndo(int strokeId)
    {
        history.RemoveAll(s => s.strokeId == strokeId);
        RpcClear();
        for (int i = 0; i < history.Count; i += HistoryChunkSize)
        {
            int count = Mathf.Min(HistoryChunkSize, history.Count - i);
            RpcReplay(history.GetRange(i, count).ToArray());
        }
    }

    [Command(requiresAuthority = false)]
    void CmdRequestHistory(NetworkConnectionToClient sender = null)
    {
        if (sender == null) return;
        for (int i = 0; i < history.Count; i += HistoryChunkSize)
        {
            int count = Mathf.Min(HistoryChunkSize, history.Count - i);
            TargetHistory(sender, history.GetRange(i, count).ToArray());
        }
    }

    // ---------------- Serveur -> Clients ----------------

    [ClientRpc]
    void RpcApplyChunk(StrokeChunk chunk)
    {
        KeepCopy(chunk);
        // L'auteur a deja trace ce trait en direct : pas de double rendu.
        // (Le rejeu undo / late joiner, lui, passe par RpcReplay / TargetHistory.)
        if (predictedStrokes.Contains(chunk.strokeId)) return;
        Apply(chunk);
    }

    /// <summary>Rejeu par lots vers tous les clients (undo).</summary>
    [ClientRpc]
    void RpcReplay(StrokeChunk[] chunks)
    {
        foreach (var c in chunks) { KeepCopy(c); Apply(c); }
    }

    [ClientRpc]
    void RpcClear()
    {
        if (!isServer) history.Clear();
        ClearLocal();
    }

    /// <summary>Rattrapage d'un arrivant tardif, par lots.</summary>
    [TargetRpc]
    void TargetHistory(NetworkConnectionToClient target, StrokeChunk[] chunks)
    {
        foreach (var c in chunks) { KeepCopy(c); Apply(c); }
    }

    // ---------------- Commun ----------------

    /// <summary>Copie locale de l'historique sur un client distant (le host a deja l'original).</summary>
    void KeepCopy(StrokeChunk chunk)
    {
        if (!isServer) history.Add(chunk);
    }

    void ClearLocal()
    {
        lastPoint.Clear();
        surface.Clear();
    }

    /// <summary>
    /// Prediction locale : l'auteur voit son trait a la frame meme, sans
    /// attendre le paquet de 80 ms ni l'aller-retour serveur.
    /// </summary>
    public void PredictPoint(int strokeId, Vector2 uv, Color32 color, float width)
    {
        predictedStrokes.Add(strokeId);
        ApplyPoint(strokeId, uv, color, width);
    }

    void Apply(StrokeChunk chunk)
    {
        if (chunk.points == null) return;
        foreach (var pt in chunk.points)
            ApplyPoint(chunk.strokeId, pt, chunk.color, chunk.width);
    }

    void ApplyPoint(int strokeId, Vector2 pt, Color32 color, float width)
    {
        Vector2 prev;
        if (lastPoint.TryGetValue(strokeId, out var p))
        {
            // Le VRMarker reinjecte la queue du paquet precedent en tete du
            // suivant : ce point est deja trace, on le saute pour eviter un
            // tampon redondant a chaque jointure.
            if (pt == p) return;
            prev = p;
        }
        else
        {
            // Premier point du trait : un segment de longueur nulle depose un
            // point, ce qui est bien le rendu attendu pour une simple touche.
            prev = pt;
        }

        surface.DrawSegment(prev, pt, color, width);
        lastPoint[strokeId] = pt;
    }

    public override void OnStartClient()
    {
        // Le host a deja tout, y compris ce qu'il a dessine avant d'ouvrir la
        // seance. Un client distant, lui, remplace son tableau solo par celui
        // de la seance.
        if (isServer) return;
        history.Clear();
        ClearLocal();
        CmdRequestHistory();
    }
}
