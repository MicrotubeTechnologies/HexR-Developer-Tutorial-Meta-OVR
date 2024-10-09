using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using HaptGlove;
using UnityEngine.UI;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using TMPro;
using System;
using Unity.VisualScripting;
namespace HexR
{
    public class PressureTrackerMain : MonoBehaviour
    {
        [Tooltip("Located in OVRHands ")]
        // The interactor is use to check if you are near a grabbable or pokeable gameobject and allows the haptic to be trigger
        public HandGrabInteractor handGrabInteractor;
        public PokeInteractor pokeInteractor;

        [HideInInspector]
        public int ThumbPressure, IndexPressure, MiddlePressure, RingPressure, LittlePressure, PalmPressure, TankPressure;
        [HideInInspector]
        public HaptGloveHandler gloveHandler;

        [HideInInspector]
        public bool HandGrabbing, PokeHovering, CollisionNearHand;
        private bool Hovering = false;
        //This is the central control for the pressure on each finger
        //As we want them to be within 60 KPA

        // Start is called before the first frame update
        void Start()
        {
            gloveHandler = gameObject.GetComponent<HaptGloveHandler>();
            ThumbPressure = 0;
            IndexPressure = 0;
            MiddlePressure = 0;
            RingPressure = 0;
            LittlePressure = 0;
            PalmPressure = 0;
            TankPressure = 0;
            CollisionNearHand = false;
        }

        // Update is called once per frame
        void Update()
        {
            HandGrabbing = IsHandGrabbing();
            PokeHovering = IsPokeHover();
            int[] AirPressure = gloveHandler?.GetAirPressure();
            if(AirPressure!= null)
            {
                ThumbPressure = ((int)Math.Round(AirPressure[0] / 100000.0) * 100000) - 100000;
                IndexPressure = ((int)Math.Round(AirPressure[1] / 100000.0) * 100000) - 100000;
                MiddlePressure = ((int)Math.Round(AirPressure[2] / 100000.0) * 100000) - 100000;
                RingPressure = ((int)Math.Round(AirPressure[3] / 100000.0) * 100000) - 100000;
                LittlePressure = ((int)Math.Round(AirPressure[4] / 100000.0) * 100000) - 100000;
                PalmPressure = ((int)Math.Round(AirPressure[5] / 100000.0) * 100000) - 100000;
                TankPressure = ((int)Math.Round(AirPressure[6] / 100000.0) * 100000) - 100000;
            }
        }

        #region Meta Hand Proximity Test
        private bool IsHandGrabbing()
        {
            if (handGrabInteractor != null)
            {
                // Check if the interactor is grabbing something
                return handGrabInteractor.HasInteractable;
            }
            else
            {
                return false;
            }
        }
        private bool IsPokeHover()
        {
            if (pokeInteractor != null)
            {
                // Check if the interactor is poking something
                return pokeInteractor.HasInteractable;
            }
            else
            {
                return false;
            }
        }
        public bool IsPhysicsCollisionNear(bool CollisionNearHand)
        {
            return CollisionNearHand;
        }
        #endregion

        #region Basic Haptics Functions For Single Trigger
        public void TriggerSingleHapticsIncrease(byte[] FingerTypeByte, int TargetPressure, bool ByPassHandInteractionCheck)
        {
            if (HandGrabbing == true || PokeHovering == true|| ByPassHandInteractionCheck == true)
            {
                TargetPressure = PressureChecker(TargetPressure);
                // btData contains the instruction for which haptics to be triggered and the incremented pressure
                byte[] btData = gloveHandler.haptics.ApplyHaptics(FingerTypeByte, (byte)TargetPressure, false);
                gloveHandler.BTSend(btData);

                //Update Pressure status
                UpdateSinglePressure(FingerTypeByte, TargetPressure);
            }
        }
        public void RemoveSingleHaptics(byte[] FingerTypeByte, string FingerTypeString, bool ByPassHandInteractionCheck)
        {
            if (HandGrabbing == true || PokeHovering == true || ByPassHandInteractionCheck == true)
            {
                byte[] btData = gloveHandler.haptics.ApplyHaptics(FingerTypeByte, (byte)60, false);
                gloveHandler.BTSend(btData);

                //Update Pressure status
                ResetSinglePressure(FingerTypeString);
            }
        }
        public void TriggerSingleVibrations(byte[] FingerTypeByte, byte VibrationStrength, bool ByPassHandInteractionCheck)
        {
            if (HandGrabbing == true || PokeHovering == true || ByPassHandInteractionCheck == true)
            {

                byte[] btData = gloveHandler.haptics.ApplyHaptics(VibrationStrength, FingerTypeByte, (byte)(30), false);
                gloveHandler.BTSend(btData);

            }
        }
        public void RemoveSingleVibration(byte[] FingerTypeByte, byte VibrationStrength)
        {

            byte[] btData = gloveHandler.haptics.ApplyHaptics(VibrationStrength, FingerTypeByte, 60, false);
            gloveHandler.BTSend(btData);
        }

        #endregion

        #region Basic Haptics Function For Multiple Trigger
        public void TriggerAllHapticsIncrease(int TargetPressure)
        {
            if (HandGrabbing == true || PokeHovering == true || CollisionNearHand ==true)
            {
                TargetPressure = PressureChecker(TargetPressure);
                // ClutchState affecting all indenters
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 0 }
                            , new byte[] { 5, 0 }};
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, (byte)TargetPressure, false);
                gloveHandler.BTSend(btData);

                //Update Pressure status
                ThumbPressure += TargetPressure;
                IndexPressure += TargetPressure;
                MiddlePressure += TargetPressure;
                RingPressure += TargetPressure;
                LittlePressure += TargetPressure;
                PalmPressure += TargetPressure;
            }
        }
        public void TriggerCustomHapticsIncrease(byte[][] FingerTypeByte, int TargetPressure)
        {
            if (HandGrabbing == true || PokeHovering == true || CollisionNearHand == true)
            {
                TargetPressure = PressureChecker(TargetPressure);
                // ClutchState affecting all indenters
                byte[] btData = gloveHandler.haptics.ApplyHaptics(FingerTypeByte, (byte)TargetPressure, false);
                gloveHandler.BTSend(btData);
            }
        }
        public void TriggerPinchPressure(int TargetPressure)
        {
            //Index and Thumb
            if (HandGrabbing == true || PokeHovering == true || CollisionNearHand == true)
            {
                TargetPressure = PressureChecker(TargetPressure);
                // ClutchState affecting all indenters
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, (byte)TargetPressure, false);
                gloveHandler.BTSend(btData);

                //Update Pressure status
                ThumbPressure = ThumbPressure + TargetPressure;
                IndexPressure = IndexPressure + TargetPressure;
            }
        }
        public void TriggerAllVibrations()
        {
            if (HandGrabbing == true || PokeHovering == true || CollisionNearHand == true)
            {
                byte VibrationStrength = 30; // Between 10 to 60
                // ClutchState affecting all indenters
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 0 }
                            , new byte[] { 5, 0 }};
                byte[] btData = gloveHandler.haptics.ApplyHaptics(VibrationStrength, ClutchState, (byte)(30), false);
                gloveHandler.BTSend(btData);

            }
        }
        public void RemoveAllHaptics()
        {
            // ClutchState 1st Number: 0 = Thumb, 1 = Index, 2 = Middle, 3 = Ring, 4 = Little, 5 = Palm
            // ClutchState 2nd Number: 0 = Pressure In , 2 = Pressure Out

            byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }, new byte[] { 4, 2 }
                            , new byte[] { 5, 2 }};

            byte[] btData = gloveHandler.haptics.ApplyHaptics((byte)0, ClutchState, (byte)60, false);
            gloveHandler.BTSend(btData);
            ThumbPressure = 0; IndexPressure = 0; MiddlePressure = 0; RingPressure = 0; LittlePressure = 0; PalmPressure = 0;
            Hovering = false;
        }
        public void RemovePinchPressure()
        {
            if (ThumbPressure != 0 || IndexPressure != 0)
            {
                // ClutchState affecting all indenters
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, (byte)60, false);
                gloveHandler.BTSend(btData);

                //Update Pressure status
                ThumbPressure = 0;
                IndexPressure = 0;
            }
        }

        public async void TriggerPulseIt()
        {
            // pressure is first increase by 10 and after a delay it is reduce by 10
            // Currently used in drum scene to simulate pulse hitting a drum
            await PulseAllFinger();
        }
        public async Task PulseAllFinger()
        {
            // add pressure
            if (ThumbPressure + 10 < 60)
            {
                ThumbPressure = ThumbPressure + 10;
            }
            if (IndexPressure + 10 < 60)
            {
                IndexPressure = IndexPressure + 10;
            }
            if (MiddlePressure + 10 < 60)
            {
                MiddlePressure = MiddlePressure + 10;
            }
            if (RingPressure + 10 < 60)
            {
                RingPressure = RingPressure + 10;
            }
            if (LittlePressure + 10 < 60)
            {
                LittlePressure = LittlePressure + 10;
            }
            if (PalmPressure + 10 < 60)
            {
                PalmPressure = PalmPressure + 10;
            }

            byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 0 }
                            , new byte[] { 5, 0 }};

            byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, (byte)10, false);
            gloveHandler.BTSend(btData);
            // Wait for 0.3 seconds
            await Task.Delay(100);

            // Remove pressure
            if (10 <= ThumbPressure && ThumbPressure <= 60)
            {
                ThumbPressure = ThumbPressure - 10;
            }
            if (10 <= IndexPressure && IndexPressure <= 60)
            {
                IndexPressure = IndexPressure - 10;
            }
            if (10 <= MiddlePressure && MiddlePressure <= 60)
            {
                MiddlePressure = MiddlePressure - 10;
            }
            if (10 <= RingPressure && RingPressure <= 60)
            {
                RingPressure = RingPressure - 10;
            }
            if (10 <= LittlePressure && LittlePressure <= 60)
            {
                LittlePressure = LittlePressure - 10;
            }
            if (10 <= PalmPressure && PalmPressure <= 60)
            {
                PalmPressure = PalmPressure - 10;
            }

            byte[][] RemoveClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }
                                , new byte[] { 4, 2 } , new byte[] { 5, 2 }};

            byte[] RbtData = gloveHandler.haptics.ApplyHaptics(RemoveClutchState, (byte)10, false);
            gloveHandler.BTSend(RbtData);
        }
        public void HoverAllHapticsIncrease()
        {
            if (HandGrabbing == true || PokeHovering == true)
            {
                if (Hovering == false)
                {
                    Hovering = true;
                    // ClutchState affecting all indenters
                    byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 0 }
                            , new byte[] { 5, 0 }};
                    byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, (byte)20, false);
                    gloveHandler.BTSend(btData);
                    //Update Pressure status
                    ThumbPressure += 20;
                    IndexPressure += 20;
                    MiddlePressure += 20;
                    RingPressure += 20;
                    LittlePressure += 20;
                    PalmPressure += 20;
                }
            }
        }
        #endregion

        #region Helpers
        public void UpdateSinglePressure(byte[] WhichPressure, int ValueToChange)
        {
            // Update the pressure status when the different indenters pressure is different.
            if (WhichPressure[0] == 0)
            {
                ThumbPressure = ThumbPressure + ValueToChange;
            }
            else if (WhichPressure[0] == 1)
            {
                IndexPressure = IndexPressure + ValueToChange;
            }
            else if (WhichPressure[0] == 2)
            {
                MiddlePressure = MiddlePressure + ValueToChange;
            }
            else if (WhichPressure[0] == 3)
            {
                RingPressure = RingPressure + ValueToChange;
            }
            else if (WhichPressure[0] == 4)
            {
                LittlePressure = LittlePressure + ValueToChange;
            }
            else if (WhichPressure[0] == 5)
            {
                PalmPressure = PalmPressure + ValueToChange;
            }

        }
        public void ResetSinglePressure(string WhichPressure)
        {
            // Update the pressure status when the different indenters pressure is different.
            if (WhichPressure == "thumb")
            {
                ThumbPressure = 0;
            }
            else if (WhichPressure == "index")
            {
                IndexPressure = 0;
            }
            else if (WhichPressure == "middle")
            {
                MiddlePressure = 0;
            }
            else if (WhichPressure == "ring")
            {
                RingPressure = 0;
            }
            else if (WhichPressure == "little")
            {
                LittlePressure = 0;
            }
            else if (WhichPressure == "palm")
            {
                PalmPressure = 0;
            }

        }
        private int PressureChecker(int Input)
        {
            // to ensure pressure is within 60
            if(Input > 60)
            {
                Input = 60;
            }
            return Input;
        }
        #endregion

    }
}

