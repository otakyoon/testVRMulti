using Mirror;
using UnityEngine;

/// <summary>
/// SOLUTION S1 - EXERCICE 2 (bonus) : ete ou hiver.
///
/// C'est SharedSun, plus une deuxieme SyncVar "summer" marquee "EXERCICE 2".
/// La saison change la hauteur du soleil a midi et la duree du jour
/// (valeurs pour Nantes, latitude 47 degres nord).
///
/// Pour l'essayer : remplacer SharedSun par SharedSunEx2 sur l'objet Soleil,
/// ajouter un Toggle sur le panneau : On Value Changed -> SetSummer (bool dynamique).
/// </summary>
public class SharedSunEx2 : NetworkBehaviour
{
    [SerializeField] Light sun;
    [SerializeField] float northOffset = 0f;

    [SyncVar(hook = nameof(OnHourChanged))]
    float hour = 12f;

    // ---- EXERCICE 2 : une deuxieme valeur partagee ----
    [SyncVar(hook = nameof(OnSummerChanged))]
    bool summer = true;

    public float Hour => hour;

    public override void OnStartClient() => Apply();

    void OnHourChanged(float oldHour, float newHour) => Apply();

    // ---- EXERCICE 2 : son propre hook ----
    void OnSummerChanged(bool oldSummer, bool newSummer) => Apply();

    public void SetHour(float newHour)
    {
        if (!isServer)
        {
            Debug.Log("Seul le serveur peut changer l'heure... pour l'instant !");
            return;
        }

        hour = newHour;
        Apply();
    }

    // ---- EXERCICE 2 : appele par le Toggle, meme regle que SetHour ----
    public void SetSummer(bool isSummer)
    {
        if (!isServer)
        {
            Debug.Log("Seul le serveur peut changer la saison... pour l'instant !");
            return;
        }

        summer = isSummer;
        Apply();
    }

    void Apply()
    {
        // ---- EXERCICE 2 : les valeurs dependent de la saison ----
        float sunrise = summer ? 6f : 8.5f;          // lever du soleil
        float sunset = summer ? 22f : 17f;           // coucher du soleil
        float maxElevation = summer ? 66f : 19f;     // hauteur a midi

        float t = Mathf.InverseLerp(sunrise, sunset, hour);
        float elevation = Mathf.Sin(t * Mathf.PI) * maxElevation;
        float azimuth = Mathf.Lerp(90f, 270f, t);
        sun.transform.rotation = Quaternion.Euler(elevation, azimuth + 180f + northOffset, 0f);
    }
}
