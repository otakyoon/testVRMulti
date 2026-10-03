using Mirror;
using UnityEngine;

/// <summary>
/// Le soleil de la maison. Le serveur choisit l'heure, tout le monde voit
/// les memes ombres.
///
/// A poser sur un objet "Soleil" qui porte aussi une NetworkIdentity.
/// Glisser la Directional Light de la scene dans le champ "sun".
/// </summary>
public class SharedSun : NetworkBehaviour
{
    [SerializeField] Light sun;
    [SerializeField] float maxElevation = 66f; // hauteur du soleil a midi (ete a Nantes : ~66 degres)
    [SerializeField] float northOffset = 0f;   // pour tourner la maison par rapport au nord

    // TODO 1 : partager cette valeur avec tous les joueurs.
    //          Ajouter au-dessus : [SyncVar(hook = nameof(OnHourChanged))]
    float hour = 12f;

    public float Hour => hour;

    // TODO 2 : un joueur qui arrive en retard doit appliquer l'heure actuelle.
    //          public override void OnStartClient() => Apply();

    // TODO 3 : ecrire le hook, appele quand la nouvelle heure arrive.
    //          void OnHourChanged(float oldHour, float newHour) => Apply();

    // Appele par le slider.
    public void SetHour(float newHour)
    {
        // TODO 4 : seul le serveur a le droit de changer une SyncVar.
        //          if (!isServer) { Debug.Log("Seul le serveur peut changer l'heure... pour l'instant !"); return; }

        hour = newHour;
        Apply();
    }

    // Fourni : transforme l'heure en position du soleil.
    // 6h : le soleil se leve a l'est. 13h : il est au sud. 20h : il se couche a l'ouest.
    void Apply()
    {
        float t = Mathf.InverseLerp(6f, 20f, hour);                // 0 le matin -> 1 le soir
        float elevation = Mathf.Sin(t * Mathf.PI) * maxElevation;  // monte puis redescend
        float azimuth = Mathf.Lerp(90f, 270f, t);                  // est -> sud -> ouest

        // La lumiere part du soleil : elle va dans la direction opposee (+180 degres).
        sun.transform.rotation = Quaternion.Euler(elevation, azimuth + 180f + northOffset, 0f);
    }
}
