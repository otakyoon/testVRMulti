using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>A qui s'adresse une phrase de contexte.</summary>
public enum ContextScope
{
    Scene,    // envoyee avec TOUTES les questions (decrite sur le serveur)
    Player,   // envoyee seulement quand CE joueur pose une question (decrite sur son casque)
    Element,  // envoyee seulement par le bouton « ? » de cet element
}

/// <summary>
/// FOURNI. Une source de contexte : un composant qui decrit UNE chose en une
/// phrase, pour l'IA. Ex : "Il est 8h dans la visite."
///
/// Elle s'inscrit toute seule aupres du systeme AI : rien a cabler.
/// Decrite au moment de chaque question, donc toujours a jour.
/// Decocher le composant = l'IA ne recoit plus cette phrase.
///
/// Pour en creer une : heriter de ContextSource et ecrire Describe().
/// </summary>
public abstract class ContextSource : MonoBehaviour
{
    [SerializeField] protected ContextScope scope = ContextScope.Scene;

    public ContextScope Scope => scope;

    /// <summary>Une phrase qui decrit ce que ce composant sait.</summary>
    public abstract string Describe();

    protected virtual void OnEnable() => AI.Register(this);
    protected virtual void OnDisable() => AI.Unregister(this);

    /// <summary>Les phrases des sources actives d'une portee, une par ligne ("" si aucune).</summary>
    public static string DescribeAll(IEnumerable<ContextSource> sources, ContextScope scope)
    {
        var text = new StringBuilder();
        foreach (var source in sources)
            if (source != null && source.isActiveAndEnabled && source.scope == scope)
                text.AppendLine("- " + source.Describe());
        return text.ToString();
    }
}
