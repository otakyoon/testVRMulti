using UnityEngine;

/// <summary>
/// FOURNI. A poser sur un element (poulailler, chambre...). Le bouton « ? »
/// de l'element appelle Ask().
///
/// Les ContextSource de portee "Element" de l'objet (ex : une NoteContext
/// qui decrit le poulailler) partent avec la question.
/// </summary>
public class AskButton : MonoBehaviour
{
    [SerializeField] Agent agent = Agent.Architect;
    [TextArea]
    [SerializeField] string question = "Est-ce un bon emplacement ? Que faut-il prevoir ?";

    public void Ask()
    {
        string about = ContextSource.DescribeAll(GetComponentsInParent<ContextSource>(), ContextScope.Element);
        AI.Ask(agent, question, about);
    }
}
