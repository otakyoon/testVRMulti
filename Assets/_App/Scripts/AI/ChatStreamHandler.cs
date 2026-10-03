using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// FOURNI. Lit une reponse en streaming (lignes "data: {...}") et donne
/// chaque morceau de texte des qu'il arrive.
///
/// Les octets arrivent -> on les coupe en lignes -> on garde les lignes
/// "data:" -> on lit le JSON -> on donne le morceau a onPiece.
/// </summary>
public class ChatStreamHandler : DownloadHandlerScript
{
    readonly Action<string> onPiece;
    readonly Decoder decoder = Encoding.UTF8.GetDecoder(); // un "e" accentue peut etre coupe entre deux paquets
    readonly StringBuilder line = new StringBuilder();
    readonly StringBuilder raw = new StringBuilder();

    /// <summary>Tout ce qui est arrive, pour afficher les messages d'erreur.</summary>
    public string Raw => raw.ToString();

    public ChatStreamHandler(Action<string> onPiece) : base(new byte[4096])
    {
        this.onPiece = onPiece;
    }

    // Appele par Unity chaque fois que des octets arrivent du reseau.
    protected override bool ReceiveData(byte[] data, int length)
    {
        if (data == null || length == 0) return true;

        var chars = new char[decoder.GetCharCount(data, 0, length)];
        decoder.GetChars(data, 0, length, chars, 0);

        foreach (char c in chars)
        {
            raw.Append(c);
            if (c != '\n') { line.Append(c); continue; }
            ReadLine(line.ToString().Trim()); // une ligne complete est arrivee
            line.Clear();
        }
        return true;
    }

    void ReadLine(string text)
    {
        if (!text.StartsWith("data:")) return; // lignes vides, commentaires...
        string json = text.Substring(5).Trim();
        if (json == "[DONE]") return;          // fin du flux

        var chunk = JsonUtility.FromJson<StreamChunk>(json);
        if (chunk == null || chunk.choices == null || chunk.choices.Length == 0) return;

        var delta = chunk.choices[0].delta;
        if (delta != null && !string.IsNullOrEmpty(delta.content)) onPiece(delta.content);
    }

    [Serializable] class StreamDelta { public string content; }
    [Serializable] class StreamChoice { public StreamDelta delta; }
    [Serializable] class StreamChunk { public StreamChoice[] choices; }
}
