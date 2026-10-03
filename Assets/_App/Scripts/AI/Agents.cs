/// <summary>
/// La liste des agents. Pour en ajouter un :
///   1. une ligne dans l'enum Agent,
///   2. ses valeurs de depart dans Agents.CreateDefault.
/// Ensuite, tout se regle dans l'Inspector de l'objet AI.
/// </summary>
public enum Agent
{
    Default,    // appel direct au LLM : pas de role, pas de contexte
    Architect,
    Gardener,   // EXERCICE 2
}

public static class Agents
{
    /// <summary>Valeurs de depart d'un agent, copiees dans l'Inspector de l'objet AI.</summary>
    public static AgentConfig CreateDefault(Agent agent) => agent switch
    {
        Agent.Default => new AgentConfig(agent, "IA", role: "", useContext: false),

        Agent.Architect => new AgentConfig(agent, "L'architecte",
            "Tu es l'architecte d'une famille qui visite sa future maison. " +
            "Reponds en francais, en 3 phrases maximum, avec des conseils concrets. " +
            "Si tu ne sais pas, dis-le."),

        // ---- EXERCICE 2 : le jardinier repond par UN mot, pour que le code puisse l'utiliser ----
        Agent.Gardener => new AgentConfig(agent, "Le jardinier",
            "Tu es le jardinier de la famille. " +
            "Reponds par UN SEUL mot, en minuscules, sans ponctuation."),

        _ => new AgentConfig(agent, agent.ToString(), role: ""),
    };
}
