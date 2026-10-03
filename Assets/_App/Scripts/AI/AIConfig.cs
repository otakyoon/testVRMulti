using UnityEngine;

/// <summary>
/// FOURNI. Les reglages des IA : adresses, cles et modeles.
///
/// Creer le sien : clic droit dans Assets/_App/Secrets/Resources
/// -> Create -> App -> AI Config, et le nommer exactement "AIConfig".
/// Il est charge tout seul (Resources.Load) : aucune scene ne le reference.
/// Le dossier Secrets est ignore par Git : la cle ne part jamais sur le depot.
/// Passer d'une IA en ligne a une IA locale = changer une adresse, pas le code.
/// </summary>
[CreateAssetMenu(menuName = "App/AI Config")]
public class AIConfig : ScriptableObject
{
    [Header("Texte (en ligne)")]
    public string textUrl;      // adresse de l'IA texte, ex : https://.../v1 (sans "/" a la fin)
    public string textKey;      // la cle d'API : JAMAIS sur Git
    public string textModel;    // le nom du modele

    [Header("Voix (locale, seance suivante)")]
    public string speechUrl = "http://localhost:8880/v1";
    public string speechKey = "not-needed";
    public string speechModel = "kokoro";
}
