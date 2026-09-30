using Mirror;
using UnityEngine;

/// <summary>
/// A poser sur la racine du prefab joueur. Le serveur attribue une couleur a
/// chaque arrivant ; elle teinte la tete et le crayon de ce joueur chez tout
/// le monde, et sert d'encre par defaut a son marqueur.
/// </summary>
public class PlayerColor : NetworkBehaviour
{
    // Compteur cote serveur : les couleurs tournent dans l'ordre d'arrivee.
    static int nextIndex;

    [SyncVar(hook = nameof(OnColorChanged))]
    Color32 color = Color.white;

    public Color Color => color;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public override void OnStartServer()
    {
        color = InkPalette.PlayerInk(nextIndex++);
    }

    // Les hooks ne sont pas garantis pour l'etat initial : on applique aussi ici.
    public override void OnStartClient() => Apply();
    public override void OnStartLocalPlayer() => Apply();

    void OnColorChanged(Color32 _, Color32 __) => Apply();

    void Apply()
    {
        var block = new MaterialPropertyBlock();
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.SetPropertyBlock(block);

        // Le joueur local ecrit par defaut dans sa couleur. Le marqueur
        // reteinte ensuite son crayon avec l'encre courante.
        if (isLocalPlayer)
        {
            var marker = GetComponentInChildren<VRMarker>();
            if (marker != null) marker.SetColor(color);
        }
    }
}
