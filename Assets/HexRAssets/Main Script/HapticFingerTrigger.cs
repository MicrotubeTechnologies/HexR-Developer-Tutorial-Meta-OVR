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
        private byte[] clutchStateIn, clutchStateOut;
        private int Pressure;
        private string Fingertype;
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
            //Debug = GameObject.Find("Panel Debug").GetComponent<Text>();
            Pressure = 0;
            gloveHandler = HexrLeftOrRight.GetComponent<HaptGloveHandler>();
            pressureTrackerMain = HexrLeftOrRight.GetComponent<PressureTrackerMain>();
            if (fingertype == FingerType.Thumb)
            {
                clutchStateIn = new byte[] { 0, 0 };
                clutchStateOut = new byte[] { 0, 2 };
                Fingertype = "thumb";
            }
            else if (fingertype == FingerType.Index)
            {
                clutchStateIn = new byte[] { 1, 0 };
                clutchStateOut = new byte[] { 1, 2 };
                Fingertype = "index";
            }
            else if (fingertype == FingerType.Middle)
            {
                clutchStateIn = new byte[] { 2, 0 };
                clutchStateOut = new byte[] { 2, 2 };
                Fingertype = "middle";
            }
            else if (fingertype == FingerType.Ring)
            {
                clutchStateIn = new byte[] { 3, 0 };
                clutchStateOut = new byte[] { 3, 2 };
                Fingertype = "ring";
            }
            else if (fingertype == FingerType.Little)
            {
                clutchStateIn = new byte[] { 4, 0 };
                clutchStateOut = new byte[] { 4, 2 };
                Fingertype = "little";
            }
            else if (fingertype == FingerType.Palm)
            {
                clutchStateIn = new byte[] { 5, 0 };
                clutchStateOut = new byte[] { 5, 2 };
                Fingertype = "palm";
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (fingertype == FingerType.Thumb)
            {
                Pressure = pressureTrackerMain.ThumbPressure;
            }
            else if (fingertype == FingerType.Index)
            {
                Pressure = pressureTrackerMain.IndexPressure;
            }
            else if (fingertype == FingerType.Middle)
            {
                Pressure = pressureTrackerMain.MiddlePressure;
            }
            else if (fingertype == FingerType.Ring)
            {
                Pressure = pressureTrackerMain.RingPressure;
            }
            else if (fingertype == FingerType.Little)
            {
                Pressure = pressureTrackerMain.LittlePressure;
            }
            else if (fingertype == FingerType.Palm)
            {
                Pressure = pressureTrackerMain.PalmPressure;
            }

        }
        public void TriggerFixPressure(byte TargetPressure)
        {
            if (TargetPressure > Pressure)
            {

                int IncreasePressure = (int)TargetPressure - Pressure;

                pressureTrackerMain.TriggerSingleHapticsIncrease(clutchStateIn, IncreasePressure, true);
            }

        }
        public void TriggerVibrationPressure(byte VibrationStrength)
        {
            pressureTrackerMain.TriggerSingleVibrations(clutchStateIn, VibrationStrength,true);
        }
        public void RemoveVibration(byte VibrationStrength)
        {
            pressureTrackerMain.RemoveSingleVibration(clutchStateOut, VibrationStrength);
        }
        public void RemoveHaptics()
        {
            if (Pressure != 0)
            {
                pressureTrackerMain.RemoveSingleHaptics(clutchStateOut, Fingertype,true);
            }

        }

    }
}
