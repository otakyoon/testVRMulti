using System;
using UnityEngine;

/// <summary>
/// FOURNI. Les reglages d'un agent : son nom, son role, et s'il recoit le contexte.
/// Visibles et modifiables dans l'Inspector de l'objet AI, et depuis le code
/// avec AI.GetAgent(Agent.X).
/// </summary>
[Serializable]
public class AgentConfig
{
    public Agent agent;
    public string displayName;

    [Tooltip("Le role de l'agent (system prompt). Vide = pas de role.")]
    [TextArea(3, 8)]
    public string role;

    [Tooltip("Envoyer le contexte (scene, joueur, element) avec chaque question.")]
    public bool useContext = true;

    public AgentConfig(Agent agent, string displayName, string role, bool useContext = true)
    {
        this.agent = agent;
        this.displayName = displayName;
        this.role = role;
        this.useContext = useContext;
    }
}
