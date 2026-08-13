using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

namespace HexR.MetaOVR
{
    /// <summary>
    /// Feeds Meta OVR's grab/poke state into a <see cref="PressureTrackerMain"/>'s hand-near
    /// gating.
    ///
    /// PressureTrackerMain used to poll the interactors itself, which meant the core runtime
    /// assembly referenced Oculus.Interaction and so could not be compiled -- or even installed
    /// -- in a project without the Meta SDK. Only the polling moved here; the two interactor
    /// fields stay on PressureTrackerMain (widened to MonoBehaviour) because they hold live
    /// scene references that relocating the field would silently drop.
    ///
    /// This whole assembly is gated on HEXR_META_OVR, so in an OpenXR project it doesn't exist
    /// and nothing here runs. The OpenXR path leaves HandGrabbing/PokeHovering false and relies
    /// on ProximityCheck for CollisionNearHand instead.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PressureTrackerMain))]
    public class MetaOVRHandNearSource : MonoBehaviour
    {
        [Tooltip("Where to search when auto-finding the interactors. Defaults to this hand's PhysicsHandTracking.handRoot.")]
        public Transform searchRoot;

        private PressureTrackerMain tracker;

        // Cast once and cached: the fields are typed MonoBehaviour on the tracker, and casting
        // both of them every frame on both hands is pure waste. Re-resolved whenever the
        // underlying reference changes, so wiring them in the Inspector during Play mode works.
        private MonoBehaviour cachedGrabSource;
        private MonoBehaviour cachedPokeSource;
        private HandGrabInteractor grabInteractor;
        private PokeInteractor pokeInteractor;

        private void Awake()
        {
            tracker = GetComponent<PressureTrackerMain>();
        }

        private void Start()
        {
            // Self-heal for rigs assembled by hand rather than through AutoSetup, and for scenes
            // saved before this component existed. Only searches for whichever is missing.
            if (tracker == null || (tracker.handGrabInteractor != null && tracker.pokeInteractor != null))
            {
                return;
            }

            Transform root = ResolveSearchRoot();
            if (root == null)
            {
                Debug.LogWarning("[HexR] " + name + ": no hand root to search for Meta interactors -- grab/poke "
                    + "haptics gating will stay off for this hand. Assign handGrabInteractor/pokeInteractor on the "
                    + "Pressure Controller, set searchRoot, or re-run HexR > Auto Setup Scene.");
                return;
            }

            AutoFind(root, name);
        }

        private Transform ResolveSearchRoot()
        {
            if (searchRoot != null)
            {
                return searchRoot;
            }

            // The Pressure Controller sits under the rig alongside the hand it belongs to, so the
            // hand's own PhysicsHandTracking is the reliable way to reach the tracked hand root --
            // the same one AutoSetup hands to the backend hook.
            PhysicsHandTracking tracking = GetComponentInParent<PhysicsHandTracking>();
            return tracking != null ? tracking.handRoot : null;
        }

        /// <summary>
        /// Finds and assigns whichever interactors aren't already set on the tracker, searching
        /// inactive children too (the OVR rig keeps interactors disabled until the hand is
        /// tracked). Returns true only if both ended up assigned.
        /// </summary>
        public bool AutoFind(Transform handRoot, string label)
        {
            if (tracker == null)
            {
                tracker = GetComponent<PressureTrackerMain>();
            }
            if (tracker == null || handRoot == null)
            {
                return false;
            }

            if (tracker.handGrabInteractor == null)
            {
                tracker.handGrabInteractor = handRoot.GetComponentInChildren<HandGrabInteractor>(true);
            }
            if (tracker.pokeInteractor == null)
            {
                tracker.pokeInteractor = handRoot.GetComponentInChildren<PokeInteractor>(true);
            }

            if (tracker.handGrabInteractor == null || tracker.pokeInteractor == null)
            {
                Debug.LogWarning("[HexR] AutoSetup: couldn't auto-find " + label + " Pressure Controller's "
                    + "HandGrabInteractor/PokeInteractor under \"" + handRoot.name + "\" -- manual wiring may be "
                    + "needed for Meta OVR grab/poke-based haptics gating.");
                return false;
            }

            return true;
        }

        private void Update()
        {
            if (tracker == null)
            {
                return;
            }

            Resolve();

            // Same semantics PressureTrackerMain.IsHandGrabbing/IsPokeHover had: an unassigned
            // (or wrong-typed) interactor reads as "not grabbing/poking" rather than throwing.
            tracker.HandGrabbingCheck(grabInteractor != null && grabInteractor.HasInteractable);
            tracker.PokeHoveringCheck(pokeInteractor != null && pokeInteractor.HasInteractable);
        }

        private void Resolve()
        {
            if (!ReferenceEquals(cachedGrabSource, tracker.handGrabInteractor))
            {
                cachedGrabSource = tracker.handGrabInteractor;
                grabInteractor = cachedGrabSource as HandGrabInteractor;
                WarnOnMismatch(cachedGrabSource, grabInteractor, "handGrabInteractor", "HandGrabInteractor");
            }

            if (!ReferenceEquals(cachedPokeSource, tracker.pokeInteractor))
            {
                cachedPokeSource = tracker.pokeInteractor;
                pokeInteractor = cachedPokeSource as PokeInteractor;
                WarnOnMismatch(cachedPokeSource, pokeInteractor, "pokeInteractor", "PokeInteractor");
            }
        }

        // The fields accept any MonoBehaviour now, so a wrong drag in the Inspector would
        // otherwise fail completely silently -- the cast just yields null and gating stays off.
        private void WarnOnMismatch(MonoBehaviour assigned, Object resolved, string fieldName, string expectedType)
        {
            if (assigned != null && resolved == null)
            {
                Debug.LogWarning("[HexR] " + name + ": " + fieldName + " is assigned a "
                    + assigned.GetType().Name + ", which is not a " + expectedType
                    + " -- grab/poke gating will stay off for this hand.");
            }
        }
    }
}
