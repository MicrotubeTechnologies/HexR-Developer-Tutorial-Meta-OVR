using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HaptGlove;
using UnityEngine.UI;
using TMPro;

namespace HexR
{
    public class HapticFingerTrigger : MonoBehaviour
    {
        private HaptGloveHandler gloveHandler;
        public GameObject HexrLeftOrRight;
        private PressureTrackerMain pressureTrackerMain;
        public HandType handType;
        public FingerType fingertype;
        private Haptics.Finger HapticsFingertype;

        // The raw tracked hand can end up with a different lossyScale on-device than it had
        // in the Editor when Auto Setup baked in the collider's size/radius (e.g. hand-size
        // calibration, or a differently-scaled runtime hand rig vs. whatever was in the
        // scene at edit time) -- cache the Editor-authored size once, then continuously
        // counteract the live scale so the collider stays the physical size it was tuned to,
        // regardless of what's actually driving the scale mismatch.
        private SphereCollider sphereCollider;
        private BoxCollider boxCollider;
        private float originalSphereRadius;
        private Vector3 originalBoxSize;
        private bool loggedScale;
        public enum FingerType
        {
            Index,
            Middle,
            Ring,
            Thumb,
            Little,
            Palm
        };
        public enum HandType
        {
            Left,
            Right
        };

        //This controls the haptic being send to the individual fingers after it has been triggered by a collider that is
        //on the gameobject that the hand is touching

        // Guarantees the Rigidbody every haptic collider needs, whatever put the collider there
        // -- Auto Setup, the runtime scene-change rebind, or someone adding one by hand.
        //
        // Unity raises no trigger events between two colliders that both lack a Rigidbody, and
        // neither Meta's tracked hand nor the haptic zones (SpecialHaptics, HexRGrabbable) have
        // one. Without this the colliders sit on the fingertips looking perfectly correct and
        // never fire. Living on the component itself rather than only in Auto Setup means the
        // guarantee cannot be lost by a code path that forgets to call it.
        public static Rigidbody EnsureHapticBody(GameObject target)
        {
            Rigidbody body = target.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = target.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;

            // Interpolation smooths a simulated body between physics steps; these are driven
            // directly by hand tracking, so it would only add a frame of lag.
            body.interpolation = RigidbodyInterpolation.None;

            // ContinuousSpeculative rather than ContinuousDynamic: a kinematic Rigidbody
            // supports only the speculative flavour of continuous detection -- Unity warns and
            // falls back if given any other continuous mode. Speculative is the one that matters
            // here regardless, since it is what stops a fast-moving fingertip tunnelling through
            // a thin haptic zone between fixed steps.
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            return body;
        }

        private void Awake()
        {
            // Before any physics step runs, so the very first touch registers.
            EnsureHapticBody(gameObject);
        }

        // Start is called before the first frame update
        void Start()
        {
            sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null) { originalSphereRadius = sphereCollider.radius; }

            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null) { originalBoxSize = boxCollider.size; }

            // All three lookups below used to be unguarded, and that failure mode is invisible
            // from the outside: an exception here aborts Start() partway, so the collider is
            // still sitting on the joint and still fires trigger callbacks, but this component
            // never finishes wiring itself and every haptic call after it throws. The hand looks
            // correctly set up and simply does nothing. Report it instead.
            if (HexrLeftOrRight == null)
            {
                Debug.LogError("[HexR] " + name + " (" + handType + " " + fingertype + "): HexrLeftOrRight is not "
                    + "assigned, so this trigger has no glove to send to. Re-run HexR > Auto Setup Scene.");
            }
            else
            {
                gloveHandler = HexrLeftOrRight.GetComponent<HaptGloveHandler>();
            }

            string controllerName = (handType == HandType.Left ? "Left" : "Right") + " Pressure Controller";
            GameObject controllerObj = GameObject.Find(controllerName);
            if (controllerObj == null)
            {
                // GameObject.Find only sees ACTIVE objects, and the Pressure Controllers are
                // per-scene objects rather than part of the DontDestroyOnLoad rig -- so a scene
                // without one (or with it starting inactive) lands here, not on a missing collider.
                Debug.LogError("[HexR] " + name + " (" + handType + " " + fingertype + "): no active \""
                    + controllerName + "\" in scene \"" + gameObject.scene.name + "\" -- this finger will "
                    + "not produce haptics. Every scene needs its own active Pressure Controller.");
            }
            else
            {
                pressureTrackerMain = controllerObj.GetComponent<PressureTrackerMain>();
                if (pressureTrackerMain == null)
                {
                    Debug.LogError("[HexR] " + name + " (" + handType + " " + fingertype + "): \"" + controllerName
                        + "\" has no PressureTrackerMain component -- this finger will not produce haptics.");
                }
            }

            if (fingertype == FingerType.Thumb)
            {
                HapticsFingertype = Haptics.Finger.Thumb;
            }
            else if (fingertype == FingerType.Index)
            {
                HapticsFingertype = Haptics.Finger.Index;
            }
            else if (fingertype == FingerType.Middle)
            {
                HapticsFingertype = Haptics.Finger.Middle;
            }
            else if (fingertype == FingerType.Ring)
            {
                HapticsFingertype = Haptics.Finger.Ring;
            }
            else if (fingertype == FingerType.Little)
            {
                HapticsFingertype = Haptics.Finger.Pinky;
            }
            else if (fingertype == FingerType.Palm)
            {
                HapticsFingertype = Haptics.Finger.Palm;
            }
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 scale = transform.lossyScale;

            if (!loggedScale && (Mathf.Abs(scale.x - 1f) > 0.01f || Mathf.Abs(scale.y - 1f) > 0.01f || Mathf.Abs(scale.z - 1f) > 0.01f))
            {
                loggedScale = true;
                Debug.Log("[HexR] " + gameObject.name + " (" + handType + " " + fingertype + ") has lossyScale " + scale + " at runtime -- compensating collider size to counteract it.");
            }

            if (sphereCollider != null)
            {
                float scaleFactor = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                if (scaleFactor > 0.0001f)
                {
                    sphereCollider.radius = originalSphereRadius / scaleFactor;
                }
            }

            if (boxCollider != null)
            {
                boxCollider.size = new Vector3(
                    Mathf.Abs(scale.x) > 0.0001f ? originalBoxSize.x / scale.x : originalBoxSize.x,
                    Mathf.Abs(scale.y) > 0.0001f ? originalBoxSize.y / scale.y : originalBoxSize.y,
                    Mathf.Abs(scale.z) > 0.0001f ? originalBoxSize.z / scale.z : originalBoxSize.z);
            }
        }
        public void TriggerFixPressure(float TargetPressure)
        {
            pressureTrackerMain.CustomSingleHaptics(HapticsFingertype, true, TargetPressure, 1f, true);
        }
        public void TriggerVibrationPressure(float Frequency, float Intensity)
        {
            byte[] btData = gloveHandler.haptics.HEXRVibration(HapticsFingertype, true, Frequency, Intensity);
            gloveHandler.BTSend(btData);
        }
        public void RemoveVibration()
        {
            byte[] btData = gloveHandler.haptics.HEXRVibration(HapticsFingertype, false, 0, 0);
            gloveHandler.BTSend(btData);
        }
        public void RemoveHaptics()
        {
            pressureTrackerMain.CustomSingleHaptics(HapticsFingertype, false, 0, 1f, true);

        }

    }
}
