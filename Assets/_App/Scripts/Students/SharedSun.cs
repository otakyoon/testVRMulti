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

    // Valeur partagee : le serveur la change, Mirror l'envoie a tous les joueurs.
    [SyncVar(hook = nameof(OnHourChanged))]
    float hour = 12f;

    public float Hour => hour;

    // Un joueur qui arrive en retard recoit l'heure actuelle : on l'applique.
    public override void OnStartClient() => Apply();

    // Le hook : appele quand la nouvelle heure arrive.
    void OnHourChanged(float oldHour, float newHour) => Apply();

    // Appele par le slider, sur n'importe quel joueur.
    public void SetHour(float newHour) => CmdSetHour(newHour);

    // S'execute sur le SERVEUR. Le soleil n'appartient a personne : requiresAuthority = false.
    // Le hook se charge ensuite d'appeler Apply() chez chaque joueur.
    [Command(requiresAuthority = false)]
    void CmdSetHour(float newHour)
    {
        hour = Mathf.Clamp(newHour, 5f, 22f); // le serveur verifie toujours ce qu'on lui envoie
    }

    // Transforme l'heure en position du soleil.
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
