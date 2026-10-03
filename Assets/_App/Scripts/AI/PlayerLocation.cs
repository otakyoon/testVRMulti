using UnityEngine;

/// <summary>
/// FOURNI. Dit a l'IA dans quelle piece se trouve le joueur qui pose la question.
///
/// Seul le casque du joueur le sait : cette source a la portee
/// "Player" : elle part avec les questions de ce joueur.
/// </summary>
public class PlayerLocation : ContextSource
{
    // Ajoute dans l'editeur : cette source est du contexte du JOUEUR.
    void Reset() => scope = ContextScope.Player;

    public override string Describe()
    {
        var head = Camera.main; // la camera du casque local
        if (head == null) return "Position du visiteur inconnue.";

        foreach (var zone in FindObjectsByType<RoomZone>(FindObjectsSortMode.None))
            if (zone.Contains(head.transform.position))
                return $"Le visiteur est dans : {zone.roomName}.";

        return "Le visiteur est dehors.";
    }
}
