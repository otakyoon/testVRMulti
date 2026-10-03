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
        // 1. La lettre : le role (s'il y en a un), la question, et on demande le streaming
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new ChatMessage { role = "system", content = systemPrompt });
        messages.Add(new ChatMessage { role = "user", content = userPrompt });

        var body = new ChatRequest { model = config.textModel, stream = true, messages = messages.ToArray() };

        // 2. L'enveloppe : l'adresse + qui je suis
        using var request = new UnityWebRequest(config.textUrl + "/chat/completions", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + config.textKey);

        // 3. On lit la reponse morceau par morceau (ChatStreamHandler est fourni)
        string full = "";
        var stream = new ChatStreamHandler(piece => { full += piece; onPiece(piece); });
        request.downloadHandler = stream;

        yield return request.SendWebRequest(); // les morceaux arrivent PENDANT l'attente

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[AIText] " + request.error + "\n" + stream.Raw);
            onDone("(L'IA ne repond pas : " + request.error + ")");
            yield break;
        }

        // 4. Fini : la reponse complete
        onDone(full);
    }

    // --- Forme du JSON envoye (fourni) ---
    [Serializable] class ChatMessage { public string role; public string content; }
    [Serializable] class ChatRequest { public string model; public bool stream; public ChatMessage[] messages; }
}
