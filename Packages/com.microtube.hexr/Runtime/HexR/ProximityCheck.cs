using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HexR
{
    /// <summary>
    /// A trigger volume that reports "a hand is near me" into <see cref="PressureTrackerMain"/>,
    /// which gates every haptic call that doesn't pass ByPassHandCheck.
    ///
    /// This is the only hand-near source the OpenXR path has. Meta OVR gets two more for free
    /// (HandGrabInteractor/PokeInteractor, read by MetaOVRHandNearSource), which is why the OVR
    /// rig works without one of these and an OpenXR rig does not -- without a ProximityCheck in
    /// the scene, an OpenXR rig's IsHandNear is false forever and haptics simply never fire.
    ///
    /// Put it on the object the hand should be able to feel, size its trigger collider to the
    /// area that counts, and point it at both hands' Pressure Controllers.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ProximityCheck : MonoBehaviour
    {
        public PressureTrackerMain rightpressureTrackerMain, leftpressureTrackerMain;

        [Tooltip("How long after the last contact frame the hand still counts as near. Covers OnTriggerStay going quiet when a rigidbody sleeps -- which is what the original 0.5s coroutine watchdog was for.")]
        public float clearDelay = 0.5f;

        // Per-hand, not shared. The original kept a single `restart` flag for both hands, so one
        // hand entering the volume reset the other hand's watchdog.
        private float lastLeftContact = float.NegativeInfinity;
        private float lastRightContact = float.NegativeInfinity;
        private bool leftNear, rightNear;

        private void OnTriggerEnter(Collider other)
        {
            RegisterContact(other);
        }

        private void OnTriggerStay(Collider other)
        {
            RegisterContact(other);
        }

        private void OnTriggerExit(Collider other)
        {
            HapticFingerTrigger.HandType hand;
            if (!TryResolveHand(other, out hand))
            {
                return;
            }

            if (hand == HapticFingerTrigger.HandType.Left)
            {
                SetLeft(false);
            }
            else
            {
                SetRight(false);
            }
        }

        private void RegisterContact(Collider other)
        {
            HapticFingerTrigger.HandType hand;
            if (!TryResolveHand(other, out hand))
            {
                return;
            }

            if (hand == HapticFingerTrigger.HandType.Left)
            {
                lastLeftContact = Time.time;
                SetLeft(true);
            }
            else
            {
                lastRightContact = Time.time;
                SetRight(true);
            }
        }

        // Stand-in for the original's `other.name.Contains("L_Palm") || other.name.Contains("LeftGhostPalm")`.
        // Those were ghost-rig object names, and the ghost rig is gone (HexR > Migration > Remove
        // Ghost Hand Rig) -- the haptic colliders now sit on the raw tracked hand, whose joints
        // are named differently on every skeleton generation (L_Palm, XRHand_Palm,
        // l_palm_center_marker). Asking the component which hand it belongs to is stable across
        // all of them, and across both backends.
        private static bool TryResolveHand(Collider other, out HapticFingerTrigger.HandType hand)
        {
            hand = HapticFingerTrigger.HandType.Left;
            if (other == null)
            {
                return false;
            }

            HapticFingerTrigger trigger = other.GetComponent<HapticFingerTrigger>();
            if (trigger == null)
            {
                return false;
            }

            hand = trigger.handType;
            return true;
        }

        // OnTriggerStay stops being raised once a rigidbody sleeps, which would otherwise leave
        // the hand flagged as near indefinitely. Expiring on a timestamp does what the original
        // coroutine intended -- and actually runs, which it did not: OnTriggerEnter called
        // removeCollisiontrue(...) without StartCoroutine, so the returned IEnumerator was
        // discarded and the watchdog never started.
        private void Update()
        {
            if (leftNear && Time.time - lastLeftContact > clearDelay)
            {
                SetLeft(false);
            }
            if (rightNear && Time.time - lastRightContact > clearDelay)
            {
                SetRight(false);
            }
        }

        private void SetLeft(bool near)
        {
            if (leftNear == near)
            {
                return;
            }
            leftNear = near;
            if (leftpressureTrackerMain != null)
            {
                leftpressureTrackerMain.IsPhysicsCollisionNear(near);
            }
        }

        private void SetRight(bool near)
        {
            if (rightNear == near)
            {
                return;
            }
            rightNear = near;
            if (rightpressureTrackerMain != null)
            {
                rightpressureTrackerMain.IsPhysicsCollisionNear(near);
            }
        }

        // A hand left inside the volume when this is disabled or destroyed would otherwise stay
        // flagged as near on the tracker, which outlives this object.
        private void OnDisable()
        {
            SetLeft(false);
            SetRight(false);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ProximityCheck))]
    public class ProximityCheckEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProximityCheck controller = (ProximityCheck)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Auto Set Up"))
            {
                AutoSetUp(controller);
            }

            EditorGUILayout.HelpBox(
                "Auto Set Up finds both Pressure Controllers in the open scene and makes sure this object has a "
                + "trigger collider. Size the collider yourself -- it decides how close a hand has to be.",
                MessageType.None);
        }

        private static void AutoSetUp(ProximityCheck controller)
        {
            Undo.RecordObject(controller, "Auto Set Up Proximity Check");

            // Matches how HexRManager.AutoSetup finds the rest of the rig.
            GameObject right = GameObject.Find("Right Pressure Controller");
            GameObject left = GameObject.Find("Left Pressure Controller");

            if (right != null)
            {
                controller.rightpressureTrackerMain = right.GetComponent<PressureTrackerMain>();
            }
            if (left != null)
            {
                controller.leftpressureTrackerMain = left.GetComponent<PressureTrackerMain>();
            }

            if (controller.rightpressureTrackerMain == null || controller.leftpressureTrackerMain == null)
            {
                Debug.LogWarning("[HexR] Proximity Check: couldn't find both Pressure Controllers in this scene -- "
                    + "add a HexR rig first (HexR > Create HexR Rig), or assign them by hand.");
            }

            Collider existing = controller.GetComponent<Collider>();
            if (existing == null)
            {
                BoxCollider added = Undo.AddComponent<BoxCollider>(controller.gameObject);
                added.isTrigger = true;
                Debug.Log("[HexR] Proximity Check: added a BoxCollider set to trigger -- adjust its size to the area "
                    + "a hand should be able to feel.");
            }
            else if (!existing.isTrigger)
            {
                Undo.RecordObject(existing, "Auto Set Up Proximity Check");
                existing.isTrigger = true;
                Debug.Log("[HexR] Proximity Check: set the existing " + existing.GetType().Name + " to be a trigger.");
            }

            EditorUtility.SetDirty(controller);
        }
    }
#endif
}
