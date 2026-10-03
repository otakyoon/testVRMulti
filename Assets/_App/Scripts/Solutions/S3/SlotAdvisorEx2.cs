using System;
using UnityEngine;

/// <summary>
/// SOLUTION S3 - EXERCICE 2 (bonus) : un deuxieme agent, le jardinier, qui choisit
/// quoi construire.
///
/// 1. Agents.cs : Gardener ajoute a l'enum, avec un role qui IMPOSE le format
///    (un seul mot de la liste).
/// 2. Ce script lui pose la question, attend la reponse COMPLETE (Finished),
///    transforme le mot en numero et appelle Build(numero).
///
/// Pour l'essayer : poser ce script sur Slot_Jardin, glisser le BuildSlot,
/// et un bouton « Que mettre ici ? » -> Suggest().
/// </summary>
public class SlotAdvisorEx2 : MonoBehaviour
{
    [SerializeField] BuildSlot slot;

    // ---- EXERCICE 2 : les mots acceptes, dans le MEME ordre que les choix du BuildSlot ----
    [SerializeField] string[] words = { "poulailler", "bassin", "abri" };

    public void Suggest()
    {
        // ---- EXERCICE 2 : on demande au jardinier, et on attend la fin de la reponse ----
        Turn turn = AI.Ask(Agent.Gardener,
            "Que faut-il construire a cet emplacement du jardin ? Choisis parmi : " + string.Join(", ", words) + ".");
        if (turn != null) turn.Finished += BuildFromWord;
    }

    // ---- EXERCICE 2 : le mot devient un numero ----
    void BuildFromWord(Turn turn)
    {
        string word = turn.Output.Text.Trim().Trim('.', '!', '"', '\'').ToLowerInvariant();
        int index = Array.IndexOf(words, word);
        if (index < 0)
        {
            Debug.Log($"[SlotAdvisor] Mot inconnu : '{turn.Output.Text}'. On ne construit rien.");
            return;
        }

        slot.Build(index); // une Command : marche depuis n'importe quel joueur
    }
}
