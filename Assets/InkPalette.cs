using UnityEngine;

/// <summary>
/// Couleurs et epaisseurs proposees par le menu, et couleurs attribuees aux
/// joueurs. Un seul endroit a modifier pour changer la palette.
/// </summary>
public static class InkPalette
{
    // Toutes lisibles sur fond blanc.
    public static readonly Color[] Inks =
    {
        new Color(0.08f, 0.08f, 0.08f),  // noir
        new Color(0.10f, 0.30f, 0.85f),  // bleu
        new Color(0.85f, 0.15f, 0.15f),  // rouge
        new Color(0.10f, 0.60f, 0.25f),  // vert
        new Color(0.95f, 0.55f, 0.10f),  // orange
        new Color(0.55f, 0.20f, 0.75f),  // violet
    };

    // Le noir est exclu : chaque joueur recoit une couleur qui l'identifie.
    public const int FirstPlayerInk = 1;

    // Fractions d'UV : fin, moyen, gros.
    public static readonly float[] Widths = { 0.002f, 0.004f, 0.008f };

    public const float EraserWidth = 0.03f;

    public static Color PlayerInk(int index)
    {
        int count = Inks.Length - FirstPlayerInk;
        return Inks[FirstPlayerInk + ((index % count) + count) % count];
    }
}
