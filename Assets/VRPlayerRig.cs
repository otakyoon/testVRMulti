using Mirror;
using UnityEngine;

/// <summary>
/// Racine du prefab joueur. Sur le joueur local uniquement, recopie chaque
/// frame la pose du rig XR de la scene vers les transforms repliques.
/// Les NetworkTransform poses sur head et rightHand se chargent de diffuser
/// ces poses aux autres clients (syncDirection = ClientToServer).
///
/// Sur les instances distantes ce script ne fait rien : leurs transforms sont
/// pilotes par le reseau.
/// </summary>
public class VRPlayerRig : NetworkBehaviour
{
    [SerializeField] Transform head;
    [SerializeField] Transform rightHand;

    bool warned;

    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        var anchors = LocalRigAnchors.Instance;
        if (anchors == null)
        {
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("[VRPlayerRig] Aucun LocalRigAnchors dans la scene : " +
                                 "le joueur local ne suivra pas le casque.", this);
            }
            return;
        }

        // LateUpdate : on passe apres le TrackedPoseDriver, donc on recopie
        // une pose deja a jour pour cette frame.
        if (head != null && anchors.Head != null)
            head.SetPositionAndRotation(anchors.Head.position, anchors.Head.rotation);

        if (rightHand != null && anchors.RightHand != null)
            rightHand.SetPositionAndRotation(anchors.RightHand.position, anchors.RightHand.rotation);
    }
}
