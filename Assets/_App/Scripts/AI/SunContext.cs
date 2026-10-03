using UnityEngine;

/// <summary>
/// FOURNI. Donne l'heure de la visite a l'IA, a partir du soleil partage.
/// </summary>
public class SunContext : ContextSource
{
    [SerializeField] SharedSun sun;

    public override string Describe()
    {
        int minutes = Mathf.RoundToInt(sun.Hour * 60f);
        return $"Il est {minutes / 60}h{minutes % 60:00} dans la visite.";
    }
}
