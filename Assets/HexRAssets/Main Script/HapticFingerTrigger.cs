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
