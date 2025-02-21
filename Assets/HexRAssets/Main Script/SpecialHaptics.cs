using HaptGlove;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using static UnityEngine.GraphicsBuffer;

namespace HexR
{
    public class SpecialHaptics : MonoBehaviour
    {
        public PressureTrackerMain RightHandPhysics, LeftHandPhysics;
        private HaptGloveHandler RightHaptGloveHandler, LeftHaptGloveHandler;
        public enum Options { CustomVibrations, CustomHaptics, FountainEffect, RainDropEffect, HeartBeatEffect, HandSqueezeEffect }
        public Options TypeOfHaptics;
        private bool RemoveIt = false, IsTriggered = false, ReadyToDrop = true;
        private float timer = 0.2f;
        [Range(0.1f, 1f)]
        public float HapticStrenngthValue = 0.5f;
        private bool ThumbHB = false, IndexHB = false, MiddleHB = false, RingHB = false, LittleHB = false, PalmHB = false, RightHB = false, LeftHB = false;
        #region Custom Vibrations Fields

        [Range(0.1f, 40f)]
        public float VibrationsFrequencyValue = 2f;
        private bool RemoveCustomVibrationCheck = false;
        #endregion

        #region Custom Haptic Fields
        private HapticFingerTrigger hapticFingerTrigger2;
        [Range(10f, 60f)]
        public float HapticPressure = 10f;

        private bool RemoveHap = false;
        #endregion

        #region Heart Beat Fields
        public float InTimer = 0.4f, OutTimer = 0.3f;
        public float HeartBeatPressure = 0.5f;
        [Range(10f, 60f)]
        public bool IncludePalm = false;
        public HeartBeat heartbeat;
        private bool PressureIn = true, HapticsIsActivated = false;
        public enum HeartBeat { Regular, Irregular };
        #endregion

        #region Hand Squeeze Fields
        public UnityEvent OnSqueezeEventTrigger, OnReleaseEventTrigger;
        private FingerUseTracking RfingerUseTracking, LfingeruseTracking;

        [Range(0.1f, 1f)]
        public float SqueezeTightness = 0.2f;
        #endregion


        private void OnEnable()
        {
            if (RightHandPhysics != null) { RfingerUseTracking = RightHandPhysics.gameObject.GetComponent<FingerUseTracking>(); }
            else { Debug.Log("Right hand is not found"); }
            if (LeftHandPhysics != null) { LfingeruseTracking = LeftHandPhysics.gameObject.GetComponent<FingerUseTracking>(); }
            else { Debug.Log("Left hand is not found"); }

            RightHaptGloveHandler = RightHandPhysics.GetComponent<HaptGloveHandler>();
            LeftHaptGloveHandler = LeftHandPhysics.GetComponent<HaptGloveHandler>();

            if (TypeOfHaptics == Options.HeartBeatEffect)
            {
                StartCoroutine(HeartBeatIn());

            }

            if (TypeOfHaptics == Options.CustomVibrations)
            {
                StartCoroutine(VibrationHaptic());

            }
        }
        private void Update()
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
            }

        }
        private void OnTriggerEnter(Collider other)
        {
            if (TypeOfHaptics == Options.FountainEffect)
            {
                FountainHapticTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.CustomHaptics)
            {
                CustomHapticTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.RainDropEffect)
            {
                RaindropHapticTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.HeartBeatEffect)
            {
                HeartBeatTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.CustomVibrations)
            {
                CustomVibrationsTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.HandSqueezeEffect)
            {
                if (other.name.Contains("R_"))
                {
                    IsHandSqueezing(RfingerUseTracking);
                }
                if (other.name.Contains("L_"))
                {
                    IsHandSqueezing(LfingeruseTracking);
                }
            }
        }
        private void OnTriggerStay(Collider other)
        {
            if (TypeOfHaptics == Options.FountainEffect)
            {
                FountainHapticTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.RainDropEffect)
            {
                RaindropHapticTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.HeartBeatEffect)
            {
                HeartBeatTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.CustomVibrations)
            {
                CustomVibrationsTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.CustomHaptics)
            {
                CustomHapticTriggerEnter(other);
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (TypeOfHaptics == Options.FountainEffect)
            {
                FountainHapticTriggerExit(other);
            }
            else if (TypeOfHaptics == Options.RainDropEffect)
            {
                return;
            }
            else if (TypeOfHaptics == Options.HeartBeatEffect)
            {
                HeartBeatTriggerExit(other);
            }
            else if (TypeOfHaptics == Options.CustomVibrations)
            {
                CustomVibrationsExit(other);
            }
            else if (TypeOfHaptics == Options.CustomHaptics)
            {
                CustomHapticTriggerExit(other);
            }
        }


        #region Custom Vibrations
        public void CustomVibrationsTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger))
            {
                if (other.name.Contains("Index"))
                {
                    ThumbHB = true;
                }
                if (other.name.Contains("Index"))
                {
                    IndexHB = true;
                }
                if (other.name.Contains("Middle"))
                {
                    MiddleHB = true;
                }
                if (other.name.Contains("Ring"))
                {
                    RingHB = true;
                }
                if (other.name.Contains("Pinky") || other.name.Contains("Little"))
                {
                    LittleHB = true;
                }
                if (other.name.Contains("L_"))
                {
                    LeftHB = true;
                }
                if (other.name.Contains("R_"))
                {
                    RightHB = true;
                }
                if (other.name.Contains("Palm"))
                {
                    PalmHB = true;
                }
            }
        }
        private void CustomVibrationsExit(Collider other)
        {
            if (other.name.Contains("Index"))
            {
                ThumbHB = false;
            }
            if (other.name.Contains("Index"))
            {
                IndexHB = false;
            }
            if (other.name.Contains("Middle"))
            {
                MiddleHB = false;
            }
            if (other.name.Contains("Ring"))
            {
                RingHB = false;
            }
            if (other.name.Contains("Pinky") || other.name.Contains("Little"))
            {
                LittleHB = false;
            }
            if (other.name.Contains("Palm"))
            {
                PalmHB = false;
            }
        }

        IEnumerator VibrationHaptic()
        {
            if (RightHB)
            {
                TriggerHapticForVibrations(RightHaptGloveHandler);
            }
            else if (LeftHB)
            {
                TriggerHapticForVibrations(LeftHaptGloveHandler);
            }

            yield return new WaitForSeconds(0.2f);


            StartCoroutine(VibrationHaptic());
        }

        private void TriggerHapticForVibrations(HaptGloveHandler gloveHandler)
        {

            if (ThumbHB || IndexHB || MiddleHB || RingHB || LittleHB || PalmHB)
            {
                Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

                float[] ThePressure = new float[] { HeartBeatPressure, HeartBeatPressure, HeartBeatPressure, HeartBeatPressure, HeartBeatPressure, HeartBeatPressure };
                float[] TheFrequency = new float[] { VibrationsFrequencyValue, VibrationsFrequencyValue, VibrationsFrequencyValue, VibrationsFrequencyValue, VibrationsFrequencyValue, VibrationsFrequencyValue };
                bool[] TheBool = new bool[] { ThumbHB, IndexHB, MiddleHB, RingHB, LittleHB, PalmHB };

                byte[] btData = gloveHandler.haptics.HEXRVibration(AllFingers, TheBool, TheFrequency, ThePressure);
                gloveHandler.BTSend(btData);

                ThumbHB = IndexHB = MiddleHB = RingHB = LittleHB = PalmHB = false;
            }
            else
            {
                RightHB = LeftHB = false;
                Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

                float[] ThePressure = new float[] { 0, 0, 0, 0, 0, 0 };
                float[] TheFrequency = new float[] { 0, 0, 0, 0, 0, 0 };
                bool[] TheBool = new bool[] { false, false, false, false, false, false };

                byte[] btData = gloveHandler.haptics.HEXRVibration(AllFingers, TheBool, TheFrequency, ThePressure);
                gloveHandler.BTSend(btData);

                ThumbHB = IndexHB = MiddleHB = RingHB = LittleHB = PalmHB = false;
            }
        }

        #endregion

        #region Custom Haptics Trigger Based

        private void CustomHapticTriggerEnter(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger) && timer <= 0)
            {

                try
                {
                    hapticFingerTrigger2 = hapticFingerTrigger;
                    RemoveHap = false;
                    hapticFingerTrigger.TriggerFixPressure(HapticPressure);
                    timer = 0.1f;

                }
                catch { }
            }
        }
        private void CustomHapticTriggerExit(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger))
            {
                RemoveHap = true;
                StartCoroutine(RemoveHaptic(hapticFingerTrigger));
            }
        }
        IEnumerator RemoveHaptic(HapticFingerTrigger hapticFingerTrigger1)
        {
            // Wait for the specified delay time
            yield return new WaitForSeconds(0.1f);

            if (RemoveHap == true)
            {
                hapticFingerTrigger1?.RemoveHaptics();
            }
            else
            {
                RemoveHap = true;
                RemoveHaptic(hapticFingerTrigger1);
            }
        }
        #endregion

        #region Fountain Haptics
        private void FountainHapticTriggerEnter(Collider other)
        {
            if (IsTriggered == false)
            {
                if (other.gameObject.name == "R_Palm" || other.gameObject.name == "R_GhostPalm")
                {
                    IsTriggered = true;
                    HaptGloveHandler gloveHandler = RightHandPhysics.GetComponent<HaptGloveHandler>();
                    FountainEffect(gloveHandler);
                    StartCoroutine(RemoveFountainHaptic(RightHandPhysics));
                }
                if (other.gameObject.name == "L_Palm" || other.gameObject.name == "L_GhostPalm")
                {
                    IsTriggered = true;
                    HaptGloveHandler gloveHandler = LeftHandPhysics.GetComponent<HaptGloveHandler>();
                    FountainEffect(gloveHandler);
                    StartCoroutine(RemoveFountainHaptic(LeftHandPhysics));
                }
            }

            RemoveIt = false;
        }
        private void FountainHapticTriggerExit(Collider other)
        {
            if (other.gameObject.name == "R_Palm" || other.gameObject.name == "R_GhostPalm")
            {
                RightHandPhysics.RemoveAllVibrations();
                RemoveIt = false;
                IsTriggered = false;
            }
            if (other.gameObject.name == "L_Palm" || other.gameObject.name == "L_GhostPalm")
            {
                LeftHandPhysics.RemoveAllVibrations();
                RemoveIt = false;
                IsTriggered = false;
            }
        }
        IEnumerator RemoveFountainHaptic(PressureTrackerMain PressureTracker)
        {
            RemoveIt = true;
            // Wait for the specified delay time
            yield return new WaitForSeconds(0.3f);
            if (RemoveIt == true)
            {
                PressureTracker?.RemoveAllVibrations();
                RemoveIt = false;
                IsTriggered = false;
            }
            else
            {
                StartCoroutine(RemoveFountainHaptic(PressureTracker));
            }
            // Wait for the specified delay time
        }
        public void FountainEffect(HaptGloveHandler gloveHandler)
        {
            Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

            float[] TheFrequency = new float[] { 10f, 10f, 10f, 10f, 10f, 10f };
            float[] ThePressure = new float[] { 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f };
            bool[] TheBool = new bool[] { true, true, true, true, true, true };

            byte[] btData = gloveHandler.haptics.HEXRVibration(AllFingers, TheBool, TheFrequency, ThePressure);
            gloveHandler.BTSend(btData);
        }

        #endregion

        #region RainDrop Haptics

        private void RaindropHapticTriggerEnter(Collider other)
        {
            if (other.gameObject.name == "R_Palm" || other.gameObject.name == "R_GhostPalm")
            {
                if (ReadyToDrop)
                {
                    ReadyToDrop = false;
                    RemoveIt = false;
                    HaptGloveHandler gloveHandler = RightHandPhysics.GetComponent<HaptGloveHandler>();
                    RaindropEffect(Random.Range(1, 9), gloveHandler);
                    StartCoroutine(RestartRaindropHaptic());
                    StartCoroutine(RemoveRaindropHaptic(RightHandPhysics));
                }

            }
            if (other.gameObject.name == "L_Palm" || other.gameObject.name == "L_GhostPalm")
            {
                if (ReadyToDrop)
                {
                    ReadyToDrop = false;
                    RemoveIt = false;
                    HaptGloveHandler gloveHandler = LeftHandPhysics.GetComponent<HaptGloveHandler>();
                    RaindropEffect(Random.Range(1, 9), gloveHandler);
                    StartCoroutine(RestartRaindropHaptic());
                    StartCoroutine(RemoveRaindropHaptic(LeftHandPhysics));
                }
            }
        }
        IEnumerator RestartRaindropHaptic()
        {
            yield return new WaitForSeconds(0.2f);
            ReadyToDrop = true;
        }
        IEnumerator RemoveRaindropHaptic(PressureTrackerMain PressureTracker)
        {
            RemoveIt = true;
            // Wait for the specified delay time
            yield return new WaitForSeconds(0.4f);
            if (RemoveIt == true)
            {
                PressureTracker?.RemoveAllHaptics();
                RemoveIt = false;
            }
            else
            {
                RemoveRaindropHaptic(PressureTracker);
            }
            // Wait for the specified delay time
        }
        public void RaindropEffect(int Pattern, HaptGloveHandler gloveHandler)
        {
            Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

            float[] ThePressure = new float[] { HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue };
            float[] TheSpeed = new float[] { 1, 1, 1, 1, 1, 1 };

            // ClutchState affecting all indenters
            if (Pattern == 1)
            {
                // thumb Pinky
                bool[] TheBool = new bool[] { true, false, false, false, true, false };


                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
                gloveHandler.BTSend(btData);

            }
            else if (Pattern == 2)
            {
                // Index middle ring
                bool[] TheBool = new bool[] { false, true, true, true, false, false };

                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 3)
            {
                // Palm Middle
                bool[] TheBool = new bool[] { true, false, true, false, false, true };
                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 4)
            {
                // Index Thumb
                bool[] TheBool = new bool[] { true, true, false, false, false, false };

                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 5)
            {
                // ring middle
                bool[] TheBool = new bool[] { false, false, true, true, false, false };

                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 6)
            {
                // Palm
                bool[] TheBool = new bool[] { false, false, false, false, false, true };

                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 7)
            {
                //middle little
                bool[] TheBool = new bool[] { false, false, false, true, true, false };

                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 8)
            {
                //Index little
                bool[] TheBool = new bool[] { false, true, false, false, true, false };

                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
                gloveHandler.BTSend(btData);
            }
        }

        #endregion

        #region HeartBeat Pulse Haptics
        IEnumerator HeartBeatIn()
        {
            PressureIn = true;
            StartCoroutine(HeartBeatHaptic());
            // Wait for the specified delay time
            if (heartbeat == HeartBeat.Regular)
            {
                yield return new WaitForSeconds(InTimer);
            }
            else
            {
                yield return new WaitForSeconds(Random.Range(0.2f, 0.4f));
            }
            StartCoroutine(HeartBeatOut());
        }
        IEnumerator HeartBeatOut()
        {
            PressureIn = false;
            if (IndexHB || MiddleHB || RingHB || LittleHB || RightHB || LeftHB || PalmHB || HapticsIsActivated)
            {
                RightHandPhysics.RemoveAllHaptics();
                LeftHandPhysics.RemoveAllHaptics();
                IndexHB = MiddleHB = RingHB = LittleHB = RightHB = LeftHB = PalmHB = HapticsIsActivated = false;
            }
            if (heartbeat == HeartBeat.Regular)
            {
                yield return new WaitForSeconds(OutTimer);
            }
            else
            {
                yield return new WaitForSeconds(Random.Range(0.4f, 0.7f));
            }
            StartCoroutine(HeartBeatIn());
        }
        IEnumerator HeartBeatHaptic()
        {
            if (PressureIn)
            {
                if (RightHB)
                {
                    TriggerHapticForHeartBeat(RightHaptGloveHandler);
                }

                if (LeftHB)
                {
                    TriggerHapticForHeartBeat(LeftHaptGloveHandler);
                }

            }
            else
            {
                yield break;
            }
            yield return new WaitForSeconds(0.1f);


            StartCoroutine(HeartBeatHaptic());
        }
        private void HeartBeatTriggerEnter(Collider other)
        {
            if (other.name.Contains("Index"))
            {
                IndexHB = true;
            }
            if (other.name.Contains("Middle"))
            {
                MiddleHB = true;
            }
            if (other.name.Contains("Ring"))
            {
                RingHB = true;
            }
            if (other.name.Contains("Pinky") || other.name.Contains("Little"))
            {
                LittleHB = true;
            }
            if (other.name.Contains("L_"))
            {
                LeftHB = true;
            }
            if (other.name.Contains("R_"))
            {
                RightHB = true;
            }
            if (IncludePalm && other.name.Contains("Palm"))
            {
                PalmHB = true;
            }
        }
        private void HeartBeatTriggerExit(Collider other)
        {
            if (other.name.Contains("Index"))
            {
                IndexHB = false;
            }
            if (other.name.Contains("Middle"))
            {
                MiddleHB = false;
            }
            if (other.name.Contains("Ring"))
            {
                RingHB = false;
            }
            if (other.name.Contains("Pinky") || other.name.Contains("Little"))
            {
                LittleHB = false;
            }
            if (IncludePalm && other.name.Contains("Palm"))
            {
                PalmHB = false;
            }
        }

        private void TriggerHapticForHeartBeat(HaptGloveHandler gloveHandler)
        {

            if (IndexHB || MiddleHB || RingHB || LittleHB | PalmHB)
            {
                Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

                float[] ThePressure = new float[] { HeartBeatPressure, HeartBeatPressure, HeartBeatPressure, HeartBeatPressure, HeartBeatPressure, HeartBeatPressure };
                float[] TheSpeed = new float[] { 1, 1, 1, 1, 1, 1 };
                bool[] TheBool = new bool[] { false, IndexHB, MiddleHB, RingHB, LittleHB, PalmHB };

                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
                gloveHandler.BTSend(btData);

                HapticsIsActivated = true;
            }
        }

        public void ToggleHeartRegularity()
        {
            if (heartbeat == HeartBeat.Irregular)
            {
                heartbeat = HeartBeat.Regular;
            }
            else
            {
                heartbeat = HeartBeat.Irregular;
            }
        }

        #endregion

        #region Hand Squeeze Effect
        private void IsHandSqueezing(FingerUseTracking fingerUseTracking)
        {
            float index = fingerUseTracking.IndexUse;
            float middle = fingerUseTracking.MiddleUse;
            float ring = fingerUseTracking.RingUse;
            float little = fingerUseTracking.LittleUse;
            float thumb = fingerUseTracking.ThumbUse;
            if (index >= SqueezeTightness && middle >= SqueezeTightness && ring >= SqueezeTightness
                && little >= SqueezeTightness && thumb >= SqueezeTightness)
            {
                OnSqueezeEventTrigger?.Invoke();
            }
        }

        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SpecialHaptics))]
    public class HapticEffectControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Hand Physics Components", EditorStyles.boldLabel);
            // Get reference to the target script
            SpecialHaptics controller = (SpecialHaptics)target;

            // Add fields to assign RightHandPhysics and LeftHandPhysics
            controller.RightHandPhysics = (PressureTrackerMain)EditorGUILayout.ObjectField(
                "Right Hand Physics",
                controller.RightHandPhysics,
                typeof(PressureTrackerMain),
                true // Allow scene objects
            );

            controller.LeftHandPhysics = (PressureTrackerMain)EditorGUILayout.ObjectField(
                "Left Hand Physics",
                controller.LeftHandPhysics,
                typeof(PressureTrackerMain),
                true // Allow scene objects
            );

            GUILayout.Space(15); // Add vertical spacing
            EditorGUILayout.LabelField("Special Haptics Settings", EditorStyles.boldLabel);

            // Draw default fields
            controller.TypeOfHaptics = (SpecialHaptics.Options)EditorGUILayout.EnumPopup("Type of Haptics", controller.TypeOfHaptics);

            // Conditional fields for HeartBeatEffect
            if (controller.TypeOfHaptics == SpecialHaptics.Options.HeartBeatEffect)

            {
                // Create a tooltip for the slider
                GUIContent sliderContent = new GUIContent(
                    "Haptic Pressure",
                    "Set the Haptic Pressure between 0.1 and 1. 0.1 = lowest, 1 = strongest"
                );
                controller.HeartBeatPressure = EditorGUILayout.Slider(sliderContent, controller.HeartBeatPressure, 0.1f, 1f);


                // Round to nearest increment of 10
                controller.HeartBeatPressure = Mathf.Round(controller.HeartBeatPressure * 10) / 10;

                // Timers
                controller.InTimer = EditorGUILayout.FloatField("In Timer", controller.InTimer);
                controller.OutTimer = EditorGUILayout.FloatField("Out Timer", controller.OutTimer);
                // Type of Heartbeat
                controller.heartbeat = (SpecialHaptics.HeartBeat)EditorGUILayout.EnumPopup("Heart Beat Type", controller.heartbeat);
                controller.IncludePalm = EditorGUILayout.Toggle("Include Palm", controller.IncludePalm);
            }

            // Conditional fields for Custom Vibrations
            if (controller.TypeOfHaptics == SpecialHaptics.Options.CustomVibrations)
            {
                // Create a tooltip for the slider
                GUIContent sliderContent = new GUIContent(
                    "Frequency Speed",
                    "Set the vibration frequency speed between 0.1 and 40. 0.1 = Slowest, 40 = fastest"
                );
                controller.VibrationsFrequencyValue = EditorGUILayout.Slider(sliderContent, controller.VibrationsFrequencyValue, 0.1f, 40f);

                // Create a tooltip for the slider
                GUIContent sliderContent2 = new GUIContent(
                    "Haptic Strength",
                    "Set the Haptic strength between 0.1 and 1. 0.1 = Weakest, 1 = Strongest"
                );
                controller.HapticStrenngthValue = EditorGUILayout.Slider(sliderContent2, controller.HapticStrenngthValue, 0.1f, 1f);


                // Round to nearest increment of 10
                controller.VibrationsFrequencyValue = Mathf.Round(controller.VibrationsFrequencyValue * 10) / 10;
                // Round to nearest increment of 10
                controller.HapticStrenngthValue = Mathf.Round(controller.HapticStrenngthValue * 10) / 10;

            }

            if (controller.TypeOfHaptics == SpecialHaptics.Options.CustomHaptics)
            {
                // Create a tooltip for the slider
                GUIContent sliderContent = new GUIContent(
                    "Haptic Pressure",
                    "Set the Haptic Pressure between 10 and 60. 10 = lowest, 60 = strongest"
                );
                controller.HapticPressure = EditorGUILayout.Slider(sliderContent, controller.HapticPressure, 10f, 60f);


                // Round to nearest increment of 10
                controller.HapticPressure = Mathf.Round(controller.HapticPressure / 10) * 10;
            }

            if (controller.TypeOfHaptics == SpecialHaptics.Options.RainDropEffect)
            {
                // Create a tooltip for the slider
                GUIContent sliderContent2 = new GUIContent(
                    "Haptic Strength",
                    "Set the Haptic strength between 0.1 and 1. 0.1 = Weakest, 1 = Strongest"
                );
                controller.HapticStrenngthValue = EditorGUILayout.Slider(sliderContent2, controller.HapticStrenngthValue, 0.1f, 1f);

                // Round to nearest increment of 10
                controller.HapticStrenngthValue = Mathf.Round(controller.HapticStrenngthValue * 10) / 10;
            }
            // Conditional fields for Custom Vibrations
            if (controller.TypeOfHaptics == SpecialHaptics.Options.HandSqueezeEffect)
            {
                GUILayout.Space(15); // Add vertical spacing
                // Create a tooltip for the slider
                GUIContent sliderContent = new GUIContent(
                    "Squeeze Tightness",
                    "0.1 = tightest , 1 = Open Hand"
                );
                controller.VibrationsFrequencyValue = EditorGUILayout.Slider(sliderContent, controller.VibrationsFrequencyValue, 0.1f, 1f);

                GUILayout.Space(15); // Add vertical spacing

                // Expose the UnityEvent in the custom inspector
                SerializedProperty onHapticEventProp = serializedObject.FindProperty("OnSqueezeEventTrigger");
                EditorGUILayout.PropertyField(onHapticEventProp);

                // Apply changes to the serialized object
                serializedObject.ApplyModifiedProperties();
            }

            GUILayout.Space(15); // Add vertical spacing

            if (GUILayout.Button("Auto Find Hand Physics"))
            {
                try
                {
                    controller.RightHandPhysics = GameObject.Find("Right Hand Physics").GetComponent<PressureTrackerMain>(); // Replace with the name of your target object
                    controller.LeftHandPhysics = GameObject.Find("Left Hand Physics").GetComponent<PressureTrackerMain>(); // Replace with the name of your target object

                }
                catch
                {
                    Debug.Log("Pressure Tracker Main Not Found Remember to assign them.");
                }

                EditorUtility.SetDirty(controller); // Mark as dirty to save changes
            }
            // Save changes
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }

#endif
}
