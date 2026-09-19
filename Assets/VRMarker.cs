using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Marqueur tenu en main (ou pointe du XRRayInteractor).
/// Raycast court depuis la pointe, echantillonne des UV, et les envoie par
/// paquets — surtout pas un RPC par frame.
///
/// IMPORTANT : ce script tourne sur toutes les instances repliquees. Sans le
/// garde de propriete, chaque client piloterait aussi les marqueurs des autres
/// joueurs et chaque trait serait envoye N fois.
/// </summary>
public class VRMarker : MonoBehaviour
{
    [Header("Pointe")]
    [SerializeField] Transform tip;                 // origine du raycast, forward = axe du marqueur
    [SerializeField] float rayLength = 0.03f;       // 3 cm : il faut "toucher" le tableau
    [SerializeField] LayerMask boardMask = ~0;

    [Header("Trait")]
    [SerializeField] Color color = Color.black;
    [SerializeField] float width = 0.004f;          // fraction d'UV

    [Header("Reseau")]
    [SerializeField] float sendInterval = 0.08f;    // ~12 paquets/s
    [SerializeField] float minUvStep = 0.002f;      // filtre les points redondants

    [Tooltip("Ne dessine que si ce marqueur appartient au joueur local. " +
             "A decocher uniquement pour un marqueur hors reseau (test en solo).")]
    [SerializeField] bool requireOwnership = true;

    readonly List<Vector2> buffer = new List<Vector2>();
    NetworkWhiteboard board;
    int strokeId = -1;
    bool sentAny;
    float nextSend;
    Vector2 lastUv;

    NetworkIdentity identity;
    bool identityResolved;

    /// <summary>
    /// Vrai uniquement sur l'instance que le joueur local controle.
    /// Cherche le NetworkIdentity du prefab joueur en remontant la hierarchie.
    /// </summary>
    bool IsMine()
    {
        if (!requireOwnership) return true;

        if (!identityResolved)
        {
            identity = GetComponentInParent<NetworkIdentity>();
            identityResolved = true;
            if (identity == null)
                Debug.LogWarning(
                    $"[VRMarker] '{name}' n'a aucun NetworkIdentity parent : impossible de savoir " +
                    "a qui il appartient, il ne dessinera pas. Pose-le sous le prefab joueur, " +
                    "ou decoche requireOwnership pour un test en solo.", this);
        }

        // Faute de proprietaire identifiable, on ne dessine pas : mieux vaut un
        // marqueur inerte qu'un trait duplique sur tous les clients.
        return identity != null && identity.isOwned;
    }

    void Update()
    {
        if (tip == null) return;

        if (!IsMine())
        {
            EndStroke();
            return;
        }

        if (Physics.Raycast(tip.position, tip.forward, out RaycastHit hit, rayLength, boardMask))
        {
            // GetComponentInParent : le MeshCollider peut etre sur un quad enfant.
            var b = hit.collider.GetComponentInParent<NetworkWhiteboard>();
            if (b == null) { EndStroke(); return; }

            if (strokeId < 0)
            {
                board = b;
                strokeId = Random.Range(int.MinValue, int.MaxValue);
                sentAny = false;
                lastUv = hit.textureCoord;
                buffer.Add(lastUv);
                nextSend = Time.time + sendInterval;
            }
            else if (Vector2.Distance(hit.textureCoord, lastUv) >= minUvStep)
            {
                lastUv = hit.textureCoord;
                buffer.Add(lastUv);
            }

            if (Time.time >= nextSend) Flush();
        }
        else
        {
            EndStroke();
        }
    }

    void Flush()
    {
        if (board == null) return;

        // apres le premier envoi, buffer[0] est la queue du paquet precedent :
        // inutile de renvoyer un point seul.
        if (buffer.Count == 0 || (sentAny && buffer.Count < 2))
        {
            // on reprogramme quand meme, sinon Flush() est retente a chaque frame
            nextSend = Time.time + sendInterval;
            return;
        }

        board.CmdAddChunk(new StrokeChunk
        {
            strokeId = strokeId,
            color = color,
            width = width,
            points = buffer.ToArray()
        });

        Vector2 tail = buffer[buffer.Count - 1];
        buffer.Clear();
        buffer.Add(tail);          // raccord avec le paquet suivant
        sentAny = true;
        nextSend = Time.time + sendInterval;
    }

    void EndStroke()
    {
        if (strokeId < 0) return;
        Flush();
        buffer.Clear();
        strokeId = -1;
        sentAny = false;
        board = null;
    }

    public void SetColor(Color c) => color = c;
    public void SetWidth(float w) => width = w;
}
