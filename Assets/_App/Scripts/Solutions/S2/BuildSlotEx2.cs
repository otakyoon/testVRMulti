using Mirror;
using UnityEngine;

/// <summary>
/// SOLUTION S2 - EXERCICE 2 (bonus) : un "ding" et un bouton Demolir.
///
/// C'est BuildSlot, plus deux ajouts marques "EXERCICE 2" :
///   2.1 un son et un effet chez tout le monde quand on construit (ClientRpc)
///   2.2 Build(-1) demolit sans reconstruire
///
/// Pour l'essayer : remplacer BuildSlot par BuildSlotEx2 sur Slot_Jardin,
/// ajouter une AudioSource et un ParticleSystem, et un bouton
/// "Demolir" : On Click -> BuildSlotEx2.Build (int) avec -1.
///
/// A observer : un joueur qui arrive en retard voit le poulailler (etat),
/// mais n'entend pas le ding (evenement).
/// </summary>
public class BuildSlotEx2 : NetworkBehaviour
{
    [SerializeField] GameObject[] choices;

    // ---- EXERCICE 2.1 : le son et l'effet ----
    [SerializeField] AudioSource ding;
    [SerializeField] ParticleSystem puff;

    GameObject current;

    public void Build(int index) => CmdBuild(index);

    [Command(requiresAuthority = false)]
    void CmdBuild(int index)
    {
        // ---- EXERCICE 2.2 : -1 est maintenant autorise (= demolir) ----
        if (index < -1 || index >= choices.Length) return;

        if (current != null) NetworkServer.Destroy(current);

        // ---- EXERCICE 2.2 : demolir = retirer sans reconstruire ----
        if (index == -1) return;

        current = Instantiate(choices[index], transform.position, transform.rotation);
        NetworkServer.Spawn(current);

        // ---- EXERCICE 2.1 : prevenir tout le monde ----
        RpcBuilt();
    }

    // ---- EXERCICE 2.1 : appele par le serveur, s'execute chez CHAQUE joueur ----
    [ClientRpc]
    void RpcBuilt()
    {
        if (ding != null) ding.Play();
        if (puff != null) puff.Play();
    }
}
