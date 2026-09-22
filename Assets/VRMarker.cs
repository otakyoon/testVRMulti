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
    [SerializeField] Transform tip;                 // position de la pointe (son orientation est ignoree)
    [SerializeField] LayerMask boardMask = ~0;

    [Header("Tolerance de contact")]
    [Tooltip("Distance devant le tableau a partir de laquelle le trait commence (m).")]
    [SerializeField] float touchDistance = 0.04f;
    [Tooltip("Distance devant le tableau au-dela de laquelle le trait s'arrete (m). " +
             "Plus grande que touchDistance : un tremblement de la main ne coupe pas le trait.")]
    [SerializeField] float releaseDistance = 0.07f;
    [Tooltip("Profondeur tolérée DERRIERE la surface (m). Rien n'arrete la main en VR : " +
             "la pointe traverse souvent le tableau, on continue d'ecrire.")]
    [SerializeField] float penetrationDepth = 0.15f;

    [Header("Lissage (filtre 1 euro)")]
    [Tooltip("Frequence de coupure au repos (Hz). Plus bas = moins de tremblement, " +
             "mais plus de retard sur les gestes lents.")]
    [SerializeField] float minCutoff = 2f;
    [Tooltip("Gain de vitesse. Plus haut = moins de retard sur les gestes rapides.")]
    [SerializeField] float beta = 20f;
    [SerializeField] float derivativeCutoff = 1f;

    [Header("Trait")]
    [SerializeField] Color color = Color.black;
    [SerializeField] float width = 0.004f;          // fraction d'UV
    [SerializeField] float eraserWidth = InkPalette.EraserWidth;
    [Tooltip("Visuel du crayon, teinte avec l'encre courante chez le joueur local. " +
             "Vide : premier Renderer enfant.")]
    [SerializeField] Renderer penVisual;
    [SerializeField] Color eraserTint = new Color(0.85f, 0.85f, 0.85f);

    [Header("Annulation")]
    [SerializeField] int maxUndo = 50;

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

    // Style fige au debut du trait : changer d'outil en plein trait ne
    // melange pas deux couleurs dans le meme strokeId.
    Color32 strokeColor;
    float strokeWidth;

    bool erasing;
    NetworkWhiteboard lastBoard;
    // Traits de ce joueur, du plus ancien au plus recent.
    readonly List<(NetworkWhiteboard board, int id)> ownStrokes = new List<(NetworkWhiteboard, int)>();

    public Color Color => color;
    public float Width => width;
    public bool IsErasing => erasing;
    public bool CanUndo => ownStrokes.Count > 0;

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

        // Filtre en continu (pas seulement pendant le trait) : pas de saut au
        // premier contact, le filtre est deja cale sur la main.
        Vector3 tipPos = FilterTip(tip.position, Time.deltaTime);

        // Seuil plus large une fois le trait commence (hysteresis).
        float front = strokeId < 0 ? touchDistance : releaseDistance;

        if (TryProjectOnBoard(tipPos, front, out RaycastHit hit, out NetworkWhiteboard b))
        {
            // Passage d'un tableau a un autre : un trait ne chevauche pas deux surfaces.
            if (strokeId >= 0 && b != board) EndStroke();

            if (strokeId < 0)
            {
                board = b;
                lastBoard = b;
                strokeId = Random.Range(int.MinValue, int.MaxValue);
                strokeColor = erasing ? b.Background : color;
                strokeWidth = erasing ? eraserWidth : width;
                ownStrokes.Add((b, strokeId));
                if (ownStrokes.Count > maxUndo) ownStrokes.RemoveAt(0);
                sentAny = false;
                lastUv = hit.textureCoord;
                AddPoint(lastUv);
                nextSend = Time.time + sendInterval;
            }
            else if (Vector2.Distance(hit.textureCoord, lastUv) >= minUvStep)
            {
                lastUv = hit.textureCoord;
                AddPoint(lastUv);
            }

            if (Time.time >= nextSend) Flush();
        }
        else
        {
            EndStroke();
        }
    }

    // Etat du filtre 1 euro (Casiez et al., 2012) sur la position de la pointe.
    bool filterInit;
    Vector3 filteredPos;
    Vector3 filteredVel;

    /// <summary>
    /// Passe-bas dont la coupure monte avec la vitesse : lisse le tremblement
    /// de la main a l'arret ou au ralenti, sans trainee sur les gestes vifs.
    /// </summary>
    Vector3 FilterTip(Vector3 raw, float dt)
    {
        if (!filterInit || dt <= 0f)
        {
            filterInit = true;
            filteredPos = raw;
            filteredVel = Vector3.zero;
            return raw;
        }

        Vector3 vel = (raw - filteredPos) / dt;
        filteredVel = Vector3.Lerp(filteredVel, vel, Alpha(derivativeCutoff, dt));

        float cutoff = minCutoff + beta * filteredVel.magnitude;
        filteredPos = Vector3.Lerp(filteredPos, raw, Alpha(cutoff, dt));
        return filteredPos;
    }

    static float Alpha(float cutoff, float dt)
    {
        float tau = 1f / (2f * Mathf.PI * cutoff);
        return 1f / (1f + tau / dt);
    }

    readonly Collider[] overlaps = new Collider[8];

    /// <summary>
    /// Projette la pointe perpendiculairement sur le tableau le plus proche.
    /// Independant de l'inclinaison du marqueur, et tolerant a une pointe
    /// passee derriere la surface. Suppose le Quad Unity standard : face
    /// visible orientee vers -forward local.
    /// </summary>
    bool TryProjectOnBoard(Vector3 p, float frontDistance, out RaycastHit best, out NetworkWhiteboard bestBoard)
    {
        best = default;
        bestBoard = null;
        float bestAbs = float.MaxValue;

        int n = Physics.OverlapSphereNonAlloc(p, Mathf.Max(frontDistance, penetrationDepth),
                                              overlaps, boardMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Collider col = overlaps[i];
            // GetComponentInParent : le MeshCollider peut etre sur un quad enfant.
            var b = col.GetComponentInParent<NetworkWhiteboard>();
            if (b == null) continue;

            Vector3 faceDir = -col.transform.forward;
            float d = Vector3.Dot(p - col.transform.position, faceDir);   // > 0 : devant
            if (d > frontDistance || d < -penetrationDepth) continue;
            if (Mathf.Abs(d) >= bestAbs) continue;

            // Rayon court, tire 1 cm devant la surface au droit de la pointe :
            // fournit textureCoord et verifie qu'on est dans les bords du tableau.
            // (MeshCollider obligatoire, cf. WhiteboardSurface.)
            var ray = new Ray(p - faceDir * (d - 0.01f), -faceDir);
            if (col.Raycast(ray, out RaycastHit hit, 0.02f))
            {
                best = hit;
                bestBoard = b;
                bestAbs = Mathf.Abs(d);
            }
        }
        return bestBoard != null;
    }

    /// <summary>Trace tout de suite en local, et met en file pour le reseau.</summary>
    void AddPoint(Vector2 uv)
    {
        buffer.Add(uv);
        board.PredictPoint(strokeId, uv, strokeColor, strokeWidth);
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
            color = strokeColor,
            width = strokeWidth,
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

    // ---------------- Outils (appeles par le menu de main) ----------------

    /// <summary>Choisir une couleur repasse en mode encre.</summary>
    public void SetColor(Color c)
    {
        color = c;
        erasing = false;
        TintPen();
    }

    public void SetWidth(float w)
    {
        width = w;
        erasing = false;
        TintPen();
    }

    /// <summary>
    /// Gomme par zone : un trait large dans la couleur du fond. Passe par le
    /// meme chemin reseau qu'un trait normal, donc s'annule comme lui.
    /// </summary>
    public void SetEraser(bool on)
    {
        erasing = on;
        TintPen();
    }

    /// <summary>Annule le dernier trait de CE joueur, pour tout le monde.</summary>
    public void Undo()
    {
        EndStroke();
        // On saute les traits dont le tableau a disparu entre-temps.
        while (ownStrokes.Count > 0)
        {
            var last = ownStrokes[ownStrokes.Count - 1];
            ownStrokes.RemoveAt(ownStrokes.Count - 1);
            if (last.board != null)
            {
                last.board.CmdUndo(last.id);
                return;
            }
        }
    }

    /// <summary>Vide le dernier tableau utilise (ou le premier trouve) pour tout le monde.</summary>
    public void ClearBoard()
    {
        EndStroke();
        var target = lastBoard != null ? lastBoard : FindAnyObjectByType<NetworkWhiteboard>();
        if (target == null) return;
        target.CmdClear();
        ownStrokes.RemoveAll(s => s.board == target);
    }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>Retour visuel local : le crayon prend la couleur de l'outil actif.</summary>
    void TintPen()
    {
        if (!IsMine()) return;
        if (penVisual == null) penVisual = GetComponentInChildren<Renderer>();
        if (penVisual == null) return;

        Color c = erasing ? eraserTint : color;
        var block = new MaterialPropertyBlock();
        penVisual.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        block.SetColor(ColorId, c);
        penVisual.SetPropertyBlock(block);
    }
}
