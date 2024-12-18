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
        public enum Options { CustomVibrations,CustomHaptics, FountainEffect, RainDropEffect, HeartBeatEffect, HandSqueezeEffect }
        public Options TypeOfHaptics;
        private bool RemoveIt = false, IsTriggered = false, ReadyToDrop = true;
        private float timer = 0.2f;

        #region Custom Vibrations Fields

        [Range(10f, 60f)]
        public float VibrationsFrequencyValue = 10f;
        [Range(10f, 60f)]
        public float HapticStrenngthValue = 10f;
        private bool RemoveCustomVibrationCheck = false;
        #endregion

        #region Custom Haptic Fields
        private HapticFingerTrigger hapticFingerTrigger2;
        [Range(10f, 60f)]
        public float HapticPressure = 10f;
        [Range(10f, 60f)]
        private bool RemoveHap = false;
        #endregion
        #region Heart Beat Fields
        public float InTimer = 0.4f, OutTimer = 0f;
        public HeartBeat heartbeat;
        private bool PressureIn = true;
        public enum HeartBeat { Regular, Irregular };
        #endregion

        #region Hand Squeeze Fields
        public UnityEvent OnSqueezeEventTrigger, OnReleaseEventTrigger;
        private FingerUseTracking RfingerUseTracking, LfingeruseTracking;

        [Range(0.1f, 1f)]
        public float SqueezeTightness = 0.2f;
        #endregion

        private List<byte[]> fingerAffected = new List<byte[]>();
        private byte[][] totalFingerAffected;
        void Start()
        {
            if (RightHandPhysics != null) { RfingerUseTracking = RightHandPhysics.gameObject.GetComponent<FingerUseTracking>(); }
            else { Debug.Log("Right hand is not found"); }
            if (LeftHandPhysics != null) { LfingeruseTracking = LeftHandPhysics.gameObject.GetComponent<FingerUseTracking>(); }
            else { Debug.Log("Left hand is not found"); }
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
            if(TypeOfHaptics == Options.FountainEffect)
            {
                FountainHapticTriggerEnter(other);
            }
            else if(TypeOfHaptics == Options.RainDropEffect)
            {
                RaindropHapticTriggerEnter(other);
            }
            else if(TypeOfHaptics == Options.HeartBeatEffect)
            {
                HeartBeatHapticTriggerEnter(other);
            }
            else if(TypeOfHaptics == Options.CustomVibrations)
            {
                CustomVibrationsTriggerEnter(other);
            }
            else if(TypeOfHaptics == Options.HandSqueezeEffect)
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
                HeartBeatHapticTriggerEnter(other);
            }
            else if (TypeOfHaptics == Options.CustomVibrations)
            {
                CustomVibrationsBool(other,false);
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
                return;
            }
            else if (TypeOfHaptics == Options.CustomVibrations)
            {
                CustomVibrationsBool(other, true);
            }
        }


        #region Custom Vibrations
        public void CustomVibrationsTriggerEnter(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger) && timer <= 0)
            {
                RemoveCustomVibrationCheck = false;
                hapticFingerTrigger.TriggerVibrationPressure((byte)VibrationsFrequencyValue, (byte)HapticStrenngthValue);
                timer = 0.1f;
                StartCoroutine(RemoveCustomVibrations(hapticFingerTrigger));
            }
        }
        private void CustomVibrationsBool(Collider collider, bool Choice)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger))
            {
                RemoveCustomVibrationCheck = Choice;
            }
        }
        IEnumerator RemoveCustomVibrations(HapticFingerTrigger hapticFingerTrigger)
        {
            // Wait for the specified delay time
            yield return new WaitForSeconds(0.2f);

            if (RemoveCustomVibrationCheck == true)
            {
                hapticFingerTrigger.RemoveVibration((byte)VibrationsFrequencyValue);
            }
            else
            {
                StartCoroutine(RemoveCustomVibrations(hapticFingerTrigger));
                RemoveCustomVibrationCheck = true;
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
                    hapticFingerTrigger.TriggerFixPressure((byte)HapticPressure);
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
                if (other.gameObject.name == "RightGhostPalm")
                {
                    IsTriggered = true;
                    HaptGloveHandler gloveHandler = RightHandPhysics.GetComponent<HaptGloveHandler>();
                    FountainEffect(gloveHandler);
                    StartCoroutine(RemoveFountainHaptic(RightHandPhysics));
                }
                if (other.gameObject.name == "LeftGhostPalm")
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
            if (other.gameObject.name == "RightGhostPalm")
            {
                RightHandPhysics.RemoveAllVibrations();
                RemoveIt = false;
                IsTriggered = false;
            }
            if (other.gameObject.name == "LeftGhostPalm")
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
            byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 0 }, new byte[] { 5, 0 } };
            byte[] btData = gloveHandler.haptics.ApplyHaptics(15, ClutchState, (byte)(30), false);
            gloveHandler.BTSend(btData);
        }
        #endregion

        #region RainDrop Haptics

        private void RaindropHapticTriggerEnter(Collider other)
        {
            if (other.gameObject.name == "RightGhostPalm" && ReadyToDrop == true)
            {
                ReadyToDrop = false;
                RemoveIt = false;
                HaptGloveHandler gloveHandler = RightHandPhysics.GetComponent<HaptGloveHandler>();
                RaindropEffect(Random.Range(1, 9), gloveHandler);
                StartCoroutine(RestartRaindropHaptic());
                StartCoroutine(RemoveRaindropHaptic(RightHandPhysics));
            }
            if (other.gameObject.name == "LeftGhostPalm" && ReadyToDrop == true)
            {
                ReadyToDrop = false;
                RemoveIt = false;
                HaptGloveHandler gloveHandler = LeftHandPhysics.GetComponent<HaptGloveHandler>();
                RaindropEffect(Random.Range(1, 9), gloveHandler);
                StartCoroutine(RestartRaindropHaptic());
                StartCoroutine(RemoveRaindropHaptic(LeftHandPhysics));
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
            byte Pressure = (byte)40; // 10 to 60

            // ClutchState affecting all indenters
            if (Pattern == 1)
            {
                // thumb Pinky
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 2 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }, new byte[] { 4, 0 }, new byte[] { 5, 2 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 2)
            {
                // Index middle ring
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 0 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 2 }, new byte[] { 5, 2 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 3)
            {
                // Palm Middle
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 0 }, new byte[] { 3, 2 }, new byte[] { 4, 2 }, new byte[] { 5, 0 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 4)
            {
                // Index Thumb
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }, new byte[] { 4, 2 }, new byte[] { 5, 2 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 5)
            {
                // ring middle
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 2 }, new byte[] { 5, 2 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 6)
            {
                // Palm
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }, new byte[] { 4, 2 }, new byte[] { 5, 0 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 7)
            {
                //middle little
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 0 }, new byte[] { 3, 2 }, new byte[] { 4, 0 }, new byte[] { 5, 2 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
                gloveHandler.BTSend(btData);
            }
            else if (Pattern == 8)
            {
                //Index little
                byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 0 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }, new byte[] { 4, 0 }, new byte[] { 5, 2 } };
                byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
                gloveHandler.BTSend(btData);
            }
        }

        #endregion

        #region HeartBeat Pulse (Fingers Only) Haptics
        private void HeartBeatHapticTriggerEnter(Collider other)
        {
            if (PressureIn == true)
            {
                if (other.name.Contains("Index"))
                {
                    fingerAffected.Add(new byte[] { 1, 0 });
                }
                if (other.name.Contains("Middle"))
                {
                    fingerAffected.Add(new byte[] { 2, 0 });
                }
                if (other.name.Contains("Ring"))
                {
                    fingerAffected.Add(new byte[] { 3, 0 });
                }
                if (other.name.Contains("Pinky") || other.name.Contains("Little"))
                {
                    fingerAffected.Add(new byte[] { 4, 0 });
                }
                foreach (var byteArray in fingerAffected)
                {
                    Debug.Log(string.Join(", ", byteArray));
                }


                if (fingerAffected.Count > 0)
                {
                    totalFingerAffected = fingerAffected.ToArray();
                    HaptGloveHandler gloveHandler = FindParent(other.transform);
                    if(gloveHandler != null)
                    {
                        byte[] btData = gloveHandler.haptics.ApplyHaptics(totalFingerAffected, 40, false);
                        gloveHandler.BTSend(btData);
                        PressureIn = false;
                        StartCoroutine(RemoveHeartBeatHaptic());
                    }
                }
            }
        }
        HaptGloveHandler FindParent(Transform childTransform)
        {
            // This is to determine if Right or Left Hand is touching the Component
            Transform parentTransform = childTransform.parent;

            while (parentTransform != null)
            {
                if (parentTransform.name == "Right Hand Physics")
                {
                    HaptGloveHandler gloveHandler = RightHandPhysics.GetComponent<HaptGloveHandler>();
                    return gloveHandler;
                }
                if (parentTransform.name == "Left Hand Physics")
                {
                    HaptGloveHandler gloveHandler = LeftHandPhysics.GetComponent<HaptGloveHandler>();
                    return gloveHandler;
                }
                parentTransform = parentTransform.parent;
            }
            Debug.Log("No hand is found, check if the name of Hapt Glove Handler is changed");
            return null; // Return null if parent not found
        }
        IEnumerator RemoveHeartBeatHaptic()
        {
            // Wait for the specified delay time
            if (heartbeat == HeartBeat.Regular)
            {
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                yield return new WaitForSeconds(Random.Range(0.2f, 0.4f));
            }
            fingerAffected.Clear();
            totalFingerAffected = null;
            RightHandPhysics.RemoveAllHaptics();
            LeftHandPhysics.RemoveAllHaptics();
            StartCoroutine(ReadyHeartBeatHaptic());

        }
        IEnumerator ReadyHeartBeatHaptic()
        {
            if (heartbeat == HeartBeat.Regular)
            {
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return new WaitForSeconds(Random.Range(0.4f, 0.7f));
            }
            PressureIn = true;
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
            if(index >= SqueezeTightness && middle >=SqueezeTightness && ring >= SqueezeTightness 
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
                // Timers
                controller.InTimer = EditorGUILayout.FloatField("In Timer", controller.InTimer);
                controller.OutTimer = EditorGUILayout.FloatField("Out Timer", controller.OutTimer);
                // Type of Heartbeat
                controller.heartbeat = (SpecialHaptics.HeartBeat)EditorGUILayout.EnumPopup("Heart Beat Type", controller.heartbeat);

            }

            // Conditional fields for Custom Vibrations
            if(controller.TypeOfHaptics == SpecialHaptics.Options.CustomVibrations)
            {
                // Create a tooltip for the slider
                GUIContent sliderContent = new GUIContent(
                    "Frequency Speed",
                    "Set the vibration frequency speed between 10 and 60. 10 = Slowest, 60 = fastest"
                );
                controller.VibrationsFrequencyValue = EditorGUILayout.Slider(sliderContent, controller.VibrationsFrequencyValue,10f,60f);

                // Create a tooltip for the slider
                GUIContent sliderContent2 = new GUIContent(
                    "Haptic Strength",
                    "Set the Haptic strength between 10 and 60. 10 = Weakest, 60 = Strongest"
                );
                controller.HapticStrenngthValue = EditorGUILayout.Slider(sliderContent2, controller.HapticStrenngthValue, 10f, 60f);

                // Round to nearest increment of 10
                controller.VibrationsFrequencyValue = Mathf.Round(controller.VibrationsFrequencyValue / 10) *10;
                // Round to nearest increment of 10
                controller.HapticStrenngthValue = Mathf.Round(controller.HapticStrenngthValue / 10) *10;
            }

            if(controller.TypeOfHaptics == SpecialHaptics.Options.CustomHaptics)
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
