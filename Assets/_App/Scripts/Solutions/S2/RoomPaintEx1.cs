using Mirror;
using UnityEngine;

/// <summary>
/// SOLUTION S2 - EXERCICE 1 : peindre une piece.
///
/// Meme modele que SharedSun : une SyncVar + un hook + une Command.
/// On partage le NUMERO de la couleur, pas la couleur : un bouton ne peut
/// envoyer qu'un nombre, et chaque joueur a la meme palette.
///
/// Pour l'essayer : un objet vide par piece (Chambre, Salle de bain) avec une
/// NetworkIdentity et ce script. Remplir la palette et la liste des murs.
/// Chaque bouton : On Click -> RoomPaintEx1.Paint (int) avec 0, 1, 2, 3.
/// </summary>
public class RoomPaintEx1 : NetworkBehaviour
{
    [SerializeField] Color32[] palette;   // les couleurs proposees
    [SerializeField] Renderer[] walls;    // les murs de la piece

    // ---- EXERCICE 1 : la valeur partagee = le numero de la couleur ----
    [SyncVar(hook = nameof(OnColorChanged))]
    int colorIndex = 0;

    // Un joueur qui arrive en retard voit la bonne couleur.
    public override void OnStartClient() => Apply();

    // ---- EXERCICE 1 : le hook ----
    void OnColorChanged(int oldIndex, int newIndex) => Apply();

    // ---- EXERCICE 1 : appele par un bouton, sur n'importe quel joueur ----
    public void Paint(int index) => CmdPaint(index);

    // ---- EXERCICE 1 : la Command, sur le serveur ----
    [Command(requiresAuthority = false)]
    void CmdPaint(int index)
    {
        if (index < 0 || index >= palette.Length) return; // le serveur verifie
        colorIndex = index;
    }

    // ---- EXERCICE 1 : peindre tous les murs ----
    void Apply()
    {
        if (palette.Length == 0) return;
        foreach (var wall in walls) wall.material.color = palette[colorIndex];
    }
}
