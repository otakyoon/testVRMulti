using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

/// <summary>
/// A poser sur le XR Origin de la SCENE (un seul par scene, non reseau).
/// Sert de point de rendez-vous : le prefab joueur, instancie par Mirror,
/// ne peut pas referencer d'objets de scene et vient donc lire ici les
/// transforms a suivre.
///
/// Le rig XR reste local et n'est jamais replique : c'est ce qui evite le
/// piege classique du prefab joueur qui embarque sa propre camera et son
/// AudioListener, et qui en fait apparaitre un par joueur distant.
///
/// Les deux ancres se resolvent toutes seules a partir des composants du rig.
/// Les champs serialises ne servent qu'a forcer autre chose si besoin.
/// </summary>
[RequireComponent(typeof(XROrigin))]
public class LocalRigAnchors : MonoBehaviour
{
    public static LocalRigAnchors Instance { get; private set; }

    [Tooltip("Laisser vide : la camera du XR Origin est utilisee.")]
    [SerializeField] Transform headOverride;

    [Tooltip("Laisser vide : la main droite active (controleur ou main suivie) est utilisee.")]
    [SerializeField] Transform rightHandOverride;

    [Tooltip("Laisser vide : la main gauche active (controleur ou main suivie) est utilisee.")]
    [SerializeField] Transform leftHandOverride;

    XROrigin origin;
    XRInputModalityManager modality;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[LocalRigAnchors] Plusieurs instances dans la scene, " +
                             "la derniere chargee gagne.", this);
        Instance = this;

        origin = GetComponent<XROrigin>();
        modality = GetComponent<XRInputModalityManager>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Transform Head
    {
        get
        {
            if (headOverride != null) return headOverride;
            if (origin != null && origin.Camera != null) return origin.Camera.transform;
            return null;
        }
    }

    /// <summary>
    /// Resolu a chaque appel : le XRInputModalityManager bascule entre
    /// controleur et main suivie en cours de session, l'ancre doit suivre.
    /// </summary>
    public Transform RightHand
    {
        get
        {
            if (rightHandOverride != null) return rightHandOverride;
            if (modality == null) return null;

            var controller = modality.rightController;
            if (controller != null && controller.activeInHierarchy) return controller.transform;

            var hand = modality.rightHand;
            if (hand != null && hand.activeInHierarchy) return hand.transform;

            return null;
        }
    }

    /// <summary>Main libre, qui porte le menu d'outils. Meme resolution que RightHand.</summary>
    public Transform LeftHand
    {
        get
        {
            if (leftHandOverride != null) return leftHandOverride;
            if (modality == null) return null;

            var controller = modality.leftController;
            if (controller != null && controller.activeInHierarchy) return controller.transform;

            var hand = modality.leftHand;
            if (hand != null && hand.activeInHierarchy) return hand.transform;

            return null;
        }
    }
}
