using UnityEngine;

/// <summary>
/// SOLUTION S3 - EXERCICE 1 : ecrire sa propre source de contexte.
///
/// Dit a l'IA ce qui est construit a un emplacement du jardin.
/// Comme toutes les sources : heriter de ContextSource et ecrire Describe().
/// Portee "Scene" : seul le serveur sait ce qui est construit, et tous les
/// agents doivent le savoir.
///
/// Pour l'essayer : poser ce script sur Slot_Jardin et glisser le BuildSlot
/// dans "slot". Rien d'autre a cabler : la source s'inscrit toute seule.
/// </summary>
public class SlotContextEx1 : ContextSource
{
    // ---- EXERCICE 1 : ce qu'on decrit ----
    [SerializeField] BuildSlot slot;
    [SerializeField] string place = "Au fond du jardin, cote nord";

    // ---- EXERCICE 1 : une phrase pour l'IA ----
    public override string Describe()
    {
        if (slot.Current == null) return $"{place} : rien n'est construit.";

        // "Poulailler(Clone)" -> "Poulailler"
        string built = slot.Current.name.Replace("(Clone)", "").Trim();
        return $"{place} : {built}.";
    }
}
