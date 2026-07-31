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

        // Start is called before the first frame update
        void Start()
        {
            sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null) { originalSphereRadius = sphereCollider.radius; }

            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null) { originalBoxSize = boxCollider.size; }

            gloveHandler = HexrLeftOrRight.GetComponent<HaptGloveHandler>();
            if(handType == HandType.Left)
            {
                pressureTrackerMain = GameObject.Find("Left Pressure Controller").GetComponent<PressureTrackerMain>();

            }
            else
            {
                pressureTrackerMain = GameObject.Find("Right Pressure Controller").GetComponent<PressureTrackerMain>();
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
