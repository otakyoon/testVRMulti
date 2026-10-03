using TMPro;
using UnityEngine;

/// <summary>
/// FOURNI. Affiche la reponse d'un agent dans un texte, pendant qu'elle s'ecrit.
/// Choisir l'agent dans la liste, glisser un Text (TMP).
/// </summary>
public class AgentAnswerLabel : MonoBehaviour
{
    [SerializeField] Agent agent = Agent.Architect;
    [SerializeField] TMP_Text label;
    [SerializeField] string idleText = "Cliquez sur un « ? » pour poser une question.";

    Turn current;

    void Reset() => label = GetComponent<TMP_Text>();

    void OnEnable()
    {
        AI.Asked += OnAsked;
        label.text = idleText;
        Follow(AI.Latest(agent)); // un joueur en retard voit la derniere reponse
    }

    void OnDisable()
    {
        AI.Asked -= OnAsked;
        Follow(null);
    }

    void OnAsked(Turn turn)
    {
        if (turn.Input.Agent != agent) return;
        // Un joueur en retard recoit les anciens tours dans le desordre : on garde le plus recent.
        if (current != null && turn.Order < current.Order) return;
        Follow(turn);
    }

    void Follow(Turn turn)
    {
        if (current != null) current.Changed -= Show;
        current = turn;
        if (current == null) return;
        current.Changed += Show;
        Show(current);
    }

    void Show(Turn turn)
    {
        label.text = turn.Output.Text == ""
            ? AI.GetAgent(turn.Input.Agent).displayName + " reflechit..."
            : turn.Output.Text;
    }
}
