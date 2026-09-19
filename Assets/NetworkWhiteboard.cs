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
/// </summary>
[RequireComponent(typeof(WhiteboardSurface))]
public class NetworkWhiteboard : NetworkBehaviour
{
    // Un lot de 20 chunks ~ 1 ko : loin des limites de message de KCP.
    const int HistoryChunkSize = 20;

    WhiteboardSurface surface;

    // Historique cote serveur uniquement.
    readonly List<StrokeChunk> history = new List<StrokeChunk>();
    // Dernier point connu par trait, pour raccorder deux paquets consecutifs.
    readonly Dictionary<int, Vector2> lastPoint = new Dictionary<int, Vector2>();

    void Awake() => surface = GetComponent<WhiteboardSurface>();

    // ---------------- Client -> Serveur ----------------

    [Command(requiresAuthority = false)]
    public void CmdAddChunk(StrokeChunk chunk)
    {
        if (chunk.points == null || chunk.points.Length == 0) return;
        history.Add(chunk);
        RpcApplyChunk(chunk);
    }

    [Command(requiresAuthority = false)]
    public void CmdClear()
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
    public void CmdUndo(int strokeId)
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
    void RpcApplyChunk(StrokeChunk chunk) => Apply(chunk);

    /// <summary>Rejeu par lots vers tous les clients (undo).</summary>
    [ClientRpc]
    void RpcReplay(StrokeChunk[] chunks)
    {
        foreach (var c in chunks) Apply(c);
    }

    [ClientRpc]
    void RpcClear()
    {
        lastPoint.Clear();
        surface.Clear();
    }

    /// <summary>Rattrapage d'un arrivant tardif, par lots.</summary>
    [TargetRpc]
    void TargetHistory(NetworkConnectionToClient target, StrokeChunk[] chunks)
    {
        foreach (var c in chunks) Apply(c);
    }

    // ---------------- Commun ----------------

    void Apply(StrokeChunk chunk)
    {
        if (chunk.points == null || chunk.points.Length == 0) return;

        Vector2 prev;
        int i = 0;

        if (lastPoint.TryGetValue(chunk.strokeId, out var p))
        {
            prev = p;
            // Le VRMarker reinjecte la queue du paquet precedent en tete du
            // suivant : ce point est deja trace, on le saute pour eviter un
            // tampon redondant a chaque jointure.
            if (chunk.points[0] == prev) i = 1;
        }
        else
        {
            // Premier paquet du trait : un segment de longueur nulle depose un
            // point, ce qui est bien le rendu attendu pour une simple touche.
            prev = chunk.points[0];
        }

        for (; i < chunk.points.Length; i++)
        {
            surface.DrawSegment(prev, chunk.points[i], chunk.color, chunk.width);
            prev = chunk.points[i];
        }

        lastPoint[chunk.strokeId] = prev;
    }

    public override void OnStartClient()
    {
        // Le host a deja tout : seuls les clients distants demandent le rattrapage.
        if (!isServer) CmdRequestHistory();
    }
}
