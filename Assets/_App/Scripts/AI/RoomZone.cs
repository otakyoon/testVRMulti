using UnityEngine;

/// <summary>
/// FOURNI. Une piece de la maison, pour savoir ou se trouve le joueur.
/// A poser sur un objet avec un BoxCollider (Is Trigger coche) qui couvre la piece.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomZone : MonoBehaviour
{
    public string roomName = "Chambre 1";

    public bool Contains(Vector3 position) => GetComponent<Collider>().bounds.Contains(position);
}
