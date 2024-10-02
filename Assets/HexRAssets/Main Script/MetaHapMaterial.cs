using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HaptGlove;
using TMPro;
namespace HexR
{
    public class MetaHapMaterial : MonoBehaviour
    {
        private byte AirPressure;
        private HaptGloveHandler gloveHandler;
        private HapticFingerTrigger hapticFingerTrigger2;
        public TargetPressure targetpressure;
        private float timer = 0.2f;
        private bool RemoveHap = false;
        public enum TargetPressure
        {
            Low,
            Medium,
            High,
        };

        //This allows an object to send a haptic feedback to the hexr glove.
        //Place this script in the gameobject with a trigger collider.

        // Start is called before the first frame update
        void Start()
        {

            if (targetpressure == TargetPressure.Low)
            {
                AirPressure = 20;
            }
            else if (targetpressure == TargetPressure.Medium)
            {
                AirPressure = 40;
            }
            else if (targetpressure == TargetPressure.High)
            {
                AirPressure = 60;
            }

        }

        // Update is called once per frame
        void Update()
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;

            }
        }

        //Trigger 
        //0-6 (Thumb, Index, Middle, Ring, Pinky, Palm)
        private void OnTriggerEnter(Collider collider)
        {

            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger) && timer <= 0)
            {

                try
                {
                    hapticFingerTrigger2 = hapticFingerTrigger;
                    RemoveHap = false;
                    hapticFingerTrigger.TriggerFixPressure(AirPressure);
                    timer = 0.1f;

                }
                catch (System.Exception ex)
                {

                }
            }
        }

        private void OnTriggerExit(Collider collider)
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
        }

        public void ApplyHaptics()
        {
            hapticFingerTrigger2?.TriggerFixPressure(AirPressure);
        }
        public void RemoveHaptics()
        {
            StartCoroutine(RemoveHaptic(hapticFingerTrigger2));
        }
    }
}
