using Mirror;
using UnityEngine;

/// <summary>
/// Un emplacement du jardin : poulailler, bassin, abri... Un seul element a la fois.
///
/// A poser sur un objet vide "Slot_Jardin" qui porte aussi une NetworkIdentity.
/// Chaque bouton appelle Build avec le numero du choix (0, 1, 2...).
/// </summary>
public class BuildSlot : NetworkBehaviour
{
    // Prefabs avec NetworkIdentity, ajoutes dans "Spawnable Prefabs" du NetworkManager.
    [SerializeField] GameObject[] choices;

    GameObject current; // connu du serveur seulement

    // Appele par un bouton. On envoie un numero : un prefab ne voyage pas sur le reseau.
    public void Build(int index) => CmdBuild(index);

    [Command(requiresAuthority = false)]
    void CmdBuild(int index)
    {
        if (index < 0 || index >= choices.Length) return; // le serveur verifie

        if (current != null) NetworkServer.Destroy(current); // on retire l'ancien

        current = Instantiate(choices[index], transform.position, transform.rotation);
        NetworkServer.Spawn(current); // apparait chez tout le monde, et chez les retardataires
    }
}
