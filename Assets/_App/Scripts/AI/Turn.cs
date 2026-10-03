using System;
using UnityEngine;

/// <summary>
/// FOURNI. Un tour complet avec un agent : ce qu'on lui a envoye (Input) et
/// ce qu'il a repondu (Output). Il en existe une copie sur chaque joueur.
///
///   Turn turn = AI.Ask(Agent.Architect, "Ma question");
///   turn.Changed  += t => label.text = t.Output.Text;   // a chaque morceau
///   turn.Finished += t => Debug.Log(t.Output.Text);     // une fois, a la fin
///
/// Tout est garde (AI.History) : on peut revoir une reponse, et comprendre
/// pourquoi l'IA a repondu ainsi (Input.Prompt = ce qu'elle a vraiment recu).
/// </summary>
public class Turn
{
    public int Id { get; }
    public TurnInput Input { get; } = new TurnInput();
    public TurnOutput Output { get; } = new TurnOutput();

    /// <summary>Raccourci : la reponse est complete.</summary>
    public bool Done => Output.Done;

    /// <summary>Ordre d'arrivee sur le serveur (pour l'historique).</summary>
    public int Order { get; internal set; } = int.MaxValue;

    /// <summary>A chaque nouveau morceau de la reponse.</summary>
    public event Action<Turn> Changed;

    /// <summary>Une seule fois, quand la reponse est complete. Si Done est deja vrai, il ne viendra plus.</summary>
    public event Action<Turn> Finished;

    internal Turn(int id, Agent agent, string text)
    {
        Id = id;
        Input.Agent = agent;
        Input.Text = text;
    }

    /// <summary>Recopie l'etat partage, puis previent les abonnes.</summary>
    internal void Apply(AI.TurnData data)
    {
        Order = data.order;

        Input.Agent = data.agent;
        Input.Text = data.text;
        Input.Prompt = data.prompt;
        Input.SceneContext = data.sceneContext;
        Input.PlayerContext = data.playerContext;
        Input.ElementContext = data.elementContext;
        Input.Author = data.author;
        Input.AuthorColor = data.authorColor;
        Input.Time = data.time;

        bool finishing = data.done && !Output.Done;
        Output.Text = data.output;
        Output.Done = data.done;
        Output.Duration = data.duration;

        Changed?.Invoke(this);
        if (finishing) Finished?.Invoke(this);
    }
}

/// <summary>Ce qui a ete envoye a l'agent.</summary>
public class TurnInput
{
    public Agent Agent { get; internal set; }

    /// <summary>La question, telle que le joueur l'a posee.</summary>
    public string Text { get; internal set; } = "";

    /// <summary>Le prompt complet vraiment envoye : contexte + question (sans le role).</summary>
    public string Prompt { get; internal set; } = "";

    public string SceneContext { get; internal set; } = "";
    public string PlayerContext { get; internal set; } = "";
    public string ElementContext { get; internal set; } = "";

    /// <summary>Qui a pose la question ("Joueur 2"), et sa couleur.</summary>
    public string Author { get; internal set; } = "Moi";
    public Color AuthorColor { get; internal set; } = Color.white;

    /// <summary>Vrai si c'est CE joueur qui a pose la question.</summary>
    public bool IsMine { get; internal set; }

    /// <summary>L'heure de la question ("14:32").</summary>
    public string Time { get; internal set; } = "";
}

/// <summary>Ce que l'agent a repondu.</summary>
public class TurnOutput
{
    /// <summary>Le texte, qui grandit morceau par morceau.</summary>
    public string Text { get; internal set; } = "";

    public bool Done { get; internal set; }

    /// <summary>Le temps de la reponse, en secondes.</summary>
    public float Duration { get; internal set; }
}
