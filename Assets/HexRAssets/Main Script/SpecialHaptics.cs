using HaptGlove;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using HexR;

using static UnityEngine.GraphicsBuffer;

namespace HexR
{
    public class SpecialHaptics : MonoBehaviour
    {
        public PressureTrackerMain RightHandPhysics, LeftHandPhysics;
        private HaptGloveHandler RightHaptGloveHandler, LeftHaptGloveHandler;
        public enum Options { CustomVibrations, CustomHaptics, FountainEffect, RainDropEffect, HeartBeatEffect, HandSqueezeEffect }
        public Options TypeOfHaptics;
        private bool RemoveIt = false, ReadyToDrop = true, VibrationsIsOn = false, FountainIsOn = false;
        private float timer = 0.2f;

        [Range(0.1f, 1f)]
        public float HapticStrenngthValue = 0.5f;

        private bool Thumb_Bool = false, Index_Bool = false, Middle_Bool = false, Ring_Bool = false, Pinky_Bool = false, Palm_Bool = false, Right_Bool = false, Left_Bool = false;

        #region Custom Vibrations Fields

        [Range(0.1f, 40f)]
        public float VibrationsFrequencyValue = 1f;
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

        private void Start()
        {
            if (RightHandPhysics != null)
            {
                RfingerUseTracking = RightHandPhysics.gameObject.GetComponent<FingerUseTracking>();
                RightHaptGloveHandler = RightHandPhysics.gloveHandler;

            }
            else { Debug.Log("Right hand is not found"); }

            if (LeftHandPhysics != null)
            {
                LfingeruseTracking = LeftHandPhysics.gameObject.GetComponent<FingerUseTracking>();
                LeftHaptGloveHandler = LeftHandPhysics.gloveHandler;
            }
            else { Debug.Log("Left hand is not found"); }


        }

        private void OnEnable()
        {
            if (TypeOfHaptics == Options.HeartBeatEffect)
            {
                StartCoroutine(HeartBeatIn());
            }
            if (TypeOfHaptics == Options.CustomVibrations)
            {
                StartCoroutine(VibrationHaptic());
            }
            if (TypeOfHaptics == Options.FountainEffect)
            {
                StartCoroutine(FountainHaptic());
            }
        }
        private void OnDisable()
        {
            StopAllCoroutines();
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
        public void CustomVibrationsTriggerEnter(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger))
            {
                TurnOnFingerBool(collider);
            }
        }
        private void CustomVibrationsExit(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger))
            {
                TurnOffFingerBool(collider);
            }
        }

        IEnumerator VibrationHaptic()
        {
            if (timer <= 0)
            {
                if (Right_Bool)
                {
                    TriggerHapticForVibrations(RightHaptGloveHandler);
                }
                else if (Left_Bool)
                {
                    TriggerHapticForVibrations(LeftHaptGloveHandler);
                }
                timer = 0.2f;
            }
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(VibrationHaptic());
        }

        private void TriggerHapticForVibrations(HaptGloveHandler gloveHandler)
        {
            if (Thumb_Bool || Index_Bool || Middle_Bool || Ring_Bool || Pinky_Bool || Palm_Bool)
            {
                Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

                float[] ThePressure = new float[] { HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue };
                float[] TheFrequency = new float[] { VibrationsFrequencyValue, VibrationsFrequencyValue, VibrationsFrequencyValue, VibrationsFrequencyValue, VibrationsFrequencyValue, VibrationsFrequencyValue };
                bool[] FingerToTrigger = new bool[] { Thumb_Bool, Index_Bool, Middle_Bool, Ring_Bool, Pinky_Bool, Palm_Bool };
                byte[] btData = gloveHandler.haptics.HEXRVibration(AllFingers, FingerToTrigger, TheFrequency, ThePressure);
                gloveHandler.BTSend(btData);
                ResetFingerBool();
            }
            else
            {
                Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

                float[] TheFrequency = new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
                float[] ThePressure = new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
                bool[] FingerToTrigger = new bool[] { false, false, false, false, false, false };
                byte[] btData = gloveHandler.haptics.HEXRVibration(AllFingers, FingerToTrigger, TheFrequency, ThePressure);
                gloveHandler.BTSend(btData);
                ResetFingerBool();
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
        private void FountainHapticTriggerEnter(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger)) // Only triggering this using the Tip of the finger
            {
                TurnOnFingerBool(collider);
            }
        }
        private void FountainHapticTriggerExit(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger)) // Only triggering this using the Tip of the finger
            {
                TurnOffFingerBool(collider);
            }
        }
        IEnumerator FountainHaptic()
        {
            if (timer <= 0)
            {
                if (Right_Bool)
                {
                    FountainEffect(RightHaptGloveHandler);
                }
                else if (Left_Bool)
                {
                    FountainEffect(LeftHaptGloveHandler);
                }
                timer = 0.3f;
            }

            yield return new WaitForSeconds(0.3f);
            StartCoroutine(FountainHaptic());

        }
        public void FountainEffect(HaptGloveHandler gloveHandler)
        {
            if (Thumb_Bool || Index_Bool || Middle_Bool || Ring_Bool || Pinky_Bool || Palm_Bool)
            {
                Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

                float[] TheFrequency = new float[] { 18f, 18f, 18f, 18f, 18f, 18f };
                float[] ThePressure = new float[] { 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f };
                bool[] FingerToTrigger = new bool[] { Thumb_Bool, Index_Bool, Middle_Bool, Ring_Bool, Pinky_Bool, Palm_Bool };
                byte[] btData = gloveHandler.haptics.HEXRVibration(AllFingers, FingerToTrigger, TheFrequency, ThePressure);
                gloveHandler.BTSend(btData);
                ResetFingerBool();
            }
            else
            {
                Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

                float[] TheFrequency = new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
                float[] ThePressure = new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
                bool[] FingerToTrigger = new bool[] { false, false, false, false, false, false };
                byte[] btData = gloveHandler.haptics.HEXRVibration(AllFingers, FingerToTrigger, TheFrequency, ThePressure);
                gloveHandler.BTSend(btData);
                ResetFingerBool();
            }
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
            if (Thumb_Bool || Index_Bool || Middle_Bool || Ring_Bool || Pinky_Bool || Palm_Bool || HapticsIsActivated)
            {
                RightHandPhysics.RemoveAllHaptics();
                LeftHandPhysics.RemoveAllHaptics();
                Thumb_Bool = Index_Bool = Middle_Bool = Ring_Bool = Pinky_Bool = Palm_Bool = HapticsIsActivated = false;
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
                if (Right_Bool)
                {
                    TriggerHapticForHeartBeat(RightHaptGloveHandler);
                }

                if (Left_Bool)
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
        private void HeartBeatTriggerEnter(Collider collider)
        {
            TurnOnFingerBool(collider);
        }
        private void HeartBeatTriggerExit(Collider collider)
        {
            TurnOffFingerBool(collider);
        }

        private void TriggerHapticForHeartBeat(HaptGloveHandler gloveHandler)
        {

            if (Thumb_Bool || Index_Bool || Middle_Bool || Ring_Bool || Pinky_Bool || Palm_Bool)
            {
                Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

                float[] ThePressure = new float[] { HeartBeatPressure, HeartBeatPressure, HeartBeatPressure, HeartBeatPressure, HeartBeatPressure, HeartBeatPressure };
                float[] TheSpeed = new float[] { 1, 1, 1, 1, 1, 1 };
                bool[] FingerToTrigger = new bool[] { Thumb_Bool, Index_Bool, Middle_Bool, Ring_Bool, Pinky_Bool, Palm_Bool };
                byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, FingerToTrigger, ThePressure, TheSpeed);
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

        #region Helper Functions

        private void TurnOnFingerBool(Collider collider)
        {
            if (collider.name.Contains("Thumb"))
            {
                Thumb_Bool = true;
            }
            if (collider.name.Contains("Index"))
            {
                Index_Bool = true;
            }
            if (collider.name.Contains("Middle"))
            {
                Middle_Bool = true;
            }
            if (collider.name.Contains("Ring"))
            {
                Ring_Bool = true;
            }
            if (collider.name.Contains("Pinky") || collider.name.Contains("Little"))
            {
                Pinky_Bool = true;
            }
            if (collider.name.Contains("L_"))
            {
                Left_Bool = true;
            }
            if (collider.name.Contains("R_"))
            {
                Right_Bool = true;
            }
            if (collider.name.Contains("Palm"))
            {
                Palm_Bool = true;
            }
        }

        private void TurnOffFingerBool(Collider collider)
        {
            if (collider.name.Contains("Thumb"))
            {
                Thumb_Bool = false;
            }
            if (collider.name.Contains("Index"))
            {
                Index_Bool = false;
            }
            if (collider.name.Contains("Middle"))
            {
                Middle_Bool = false;
            }
            if (collider.name.Contains("Ring"))
            {
                Ring_Bool = false;
            }
            if (collider.name.Contains("Pinky") || collider.name.Contains("Little"))
            {
                Pinky_Bool = false;
            }
            if (IncludePalm && collider.name.Contains("Palm"))
            {
                Palm_Bool = false;
            }
        }

        private void ResetFingerBool()
        {
            Thumb_Bool = Index_Bool = Middle_Bool = Ring_Bool = Pinky_Bool = Palm_Bool = Right_Bool = Left_Bool = false;
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