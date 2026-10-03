using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// L'appel a l'IA texte : une question, une reponse morceau par morceau.
/// Pas de reseau Mirror ici, pas d'agent : juste l'API.
///
/// Ajoute tout seul sur l'objet AI. Les reglages (adresse, cle, modele) sont
/// lus dans Assets/_App/Secrets/Resources/AIConfig.
/// </summary>
public class AIText : MonoBehaviour
{
    AIConfig config;

    void Awake()
    {
        config = Resources.Load<AIConfig>("AIConfig");
        if (config == null)
            Debug.LogError("[AIText] AIConfig introuvable. Le creer dans Assets/_App/Secrets/Resources " +
                           "(clic droit -> Create -> App -> AI Config), nomme exactement \"AIConfig\".");
    }

    // systemPrompt : le role (vide = pas de role). userPrompt : le contexte + la question.
    // onPiece : chaque morceau, des qu'il arrive. onDone : la reponse complete.
    public void Ask(string systemPrompt, string userPrompt, Action<string> onPiece, Action<string> onDone)
    {
        StartCoroutine(AskRoutine(systemPrompt, userPrompt, onPiece, onDone));
    }

    IEnumerator AskRoutine(string systemPrompt, string userPrompt, Action<string> onPiece, Action<string> onDone)
    {
        // TODO 1 : la lettre. Une liste de ChatMessage :
        //          un message "system" (systemPrompt) SEULEMENT s'il n'est pas vide,
        //          puis un message "user" (userPrompt).
        //          Puis un ChatRequest : model = config.textModel, stream = true, messages = la liste.ToArray()

        // TODO 2 : l'enveloppe.
        //          using var request = new UnityWebRequest(config.textUrl + "/chat/completions", "POST");
        //          request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
        //          + en-tetes "Content-Type" (application/json) et "Authorization" ("Bearer " + config.textKey)

        // TODO 3 : lire les morceaux. Un ChatStreamHandler qui ajoute chaque morceau a "full"
        //          et appelle onPiece, puis : yield return request.SendWebRequest();

        // TODO 4 : en cas d'erreur, Debug.LogError + onDone avec un message ; sinon onDone(full).

        onDone("(AIText n'est pas encore ecrit : voir les TODO)");
        yield break;
    }

    // --- Forme du JSON envoye (fourni) ---
    [Serializable] class ChatMessage { public string role; public string content; }
    [Serializable] class ChatRequest { public string model; public bool stream; public ChatMessage[] messages; }
}
