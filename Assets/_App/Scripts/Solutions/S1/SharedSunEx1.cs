using Mirror;
using TMPro;
using UnityEngine;

/// <summary>
/// SOLUTION S1 - EXERCICE 1 : lever, coucher et nuit.
///
/// C'est SharedSun, plus trois ajouts marques "EXERCICE 1" :
///   1. l'heure affichee sur le panneau ("14 h 30")
///   2. la couleur du soleil (orange le matin et le soir, blanc a midi)
///   3. la nuit : intensite 0 avant 6h et apres 20h
///
/// Pour l'essayer : remplacer SharedSun par SharedSunEx1 sur l'objet Soleil.
/// </summary>
public class SharedSunEx1 : NetworkBehaviour
{
    [SerializeField] Light sun;
    [SerializeField] float maxElevation = 66f;
    [SerializeField] float northOffset = 0f;

    // ---- EXERCICE 1 : nouveaux champs ----
    [SerializeField] TMP_Text timeLabel;      // un texte sur le panneau
    [SerializeField] Gradient sunColor;       // a regler dans l'Inspector : orange -> blanc -> orange
    [SerializeField] float dayIntensity = 1f; // intensite du soleil le jour

    [SyncVar(hook = nameof(OnHourChanged))]
    float hour = 12f;

    public float Hour => hour;

    // Un joueur qui arrive en retard applique l'heure actuelle : il voit aussi le bon texte.
    public override void OnStartClient() => Apply();

    void OnHourChanged(float oldHour, float newHour) => Apply();

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

    void Apply()
    {
        float t = Mathf.InverseLerp(6f, 20f, hour);
        float elevation = Mathf.Sin(t * Mathf.PI) * maxElevation;
        float azimuth = Mathf.Lerp(90f, 270f, t);
        sun.transform.rotation = Quaternion.Euler(elevation, azimuth + 180f + northOffset, 0f);

        // ---- EXERCICE 1.1 : l'heure en texte ----
        int totalMinutes = Mathf.RoundToInt(hour * 60f);  // 14.5 h -> 870 minutes
        int h = totalMinutes / 60;                        // 870 / 60 = 14
        int m = totalMinutes % 60;                        // reste : 30
        if (timeLabel != null) timeLabel.text = $"{h} h {m:00}";

        // ---- EXERCICE 1.2 : la couleur du soleil ----
        sun.color = sunColor.Evaluate(t);

        // ---- EXERCICE 1.3 : la nuit ----
        bool isNight = hour < 6f || hour > 20f;
        sun.intensity = isNight ? 0f : dayIntensity;
    }
}
