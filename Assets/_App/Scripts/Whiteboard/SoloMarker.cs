using Mirror;
using UnityEngine;

/// <summary>
/// Marqueur utilisable HORS SEANCE, pour tester le tableau sans lancer de host.
///
/// Le vrai marqueur vit dans le prefab joueur, qui n'existe qu'en seance.
/// Ce composant en fait une copie au demarrage (sans ses composants reseau),
/// la colle a la main droite du rig local, et ne l'active que hors seance.
/// En seance, c'est le marqueur du joueur reseau qui prend le relais.
///
/// Ajoute automatiquement par LocalRigAnchors : rien a poser dans la scene.
/// </summary>
public class SoloMarker : MonoBehaviour
{
    VRMarker marker;

    void Start()
    {
        var prefab = NetworkManager.singleton != null ? NetworkManager.singleton.playerPrefab : null;
        var source = prefab != null ? prefab.GetComponentInChildren<VRMarker>(true) : null;
        if (source == null)
        {
            Debug.LogWarning("[SoloMarker] Aucun VRMarker dans le playerPrefab du NetworkManager : " +
                             "pas de marqueur hors seance.", this);
            enabled = false;
            return;
        }

        // Copie sous un parent inactif : Awake ne tourne pas avant qu'on ait
        // retire les NetworkBehaviour (un NetworkTransform sans NetworkIdentity
        // inonderait la console d'erreurs).
        var holder = new GameObject("SoloMarker (hors seance)");
        holder.SetActive(false);
        var copy = Instantiate(source.gameObject, holder.transform);
        foreach (var nb in copy.GetComponentsInChildren<NetworkBehaviour>(true))
            DestroyImmediate(nb);

        marker = copy.GetComponent<VRMarker>();
        marker.UseWithoutNetwork();
        holder.SetActive(true);
        marker.SetColor(marker.Color);   // teinte le crayon avec l'encre par defaut
    }

    void LateUpdate()
    {
        if (marker == null) return;

        bool offline = !NetworkClient.active && !NetworkServer.active;
        if (marker.gameObject.activeSelf != offline) marker.gameObject.SetActive(offline);
        if (!offline) return;

        NetworkWhiteboard.EnsureVisibleOffline();

        // Meme recopie de pose que VRPlayerRig pour le joueur reseau.
        var hand = LocalRigAnchors.Instance != null ? LocalRigAnchors.Instance.RightHand : null;
        if (hand != null)
            marker.transform.SetPositionAndRotation(hand.position, hand.rotation);
    }

    void OnDestroy()
    {
        if (marker != null) Destroy(marker.transform.parent.gameObject);
    }
}
