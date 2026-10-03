using UnityEngine;

/// <summary>
/// FOURNI. Une source de contexte ecrite a la main dans l'Inspector.
/// Ex : la famille, ou la description d'un element (poulailler, chambre...).
/// </summary>
public class NoteContext : ContextSource
{
    [TextArea(2, 6)]
    [SerializeField] string text = "Une famille : un couple, deux enfants de 6 et 10 ans, un chien.";

    public override string Describe() => text;
}
