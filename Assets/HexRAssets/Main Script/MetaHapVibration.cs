using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HaptGlove;

namespace HexR
{
    public class MetaHapVibration : MonoBehaviour
    {
        private byte Frequency;
        private HaptGloveHandler gloveHandler;
        public TargetVibration targetVibration;
        private float timer = 0.2f;
        private bool RemoveHap = false;
        public enum TargetVibration
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
            if (targetVibration == TargetVibration.Low)
            {
                Frequency = 10;
            }
            else if (targetVibration == TargetVibration.Medium)
            {
                Frequency = 30;
            }
            else if (targetVibration == TargetVibration.High)
            {
                Frequency = 45;
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
                RemoveHap = false;
                hapticFingerTrigger.TriggerVibrationPressure(Frequency);
                timer = 0.1f;
                StartCoroutine(RemoveHaptic(hapticFingerTrigger));
            }
        }
        private void OnTriggerStay(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger))
            {
                RemoveHap = false;
            }
        }
        private void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger))
            {
                RemoveHap = true;
            }

        }

        IEnumerator RemoveHaptic(HapticFingerTrigger hapticFingerTrigger1)
        {
            // Wait for the specified delay time
            yield return new WaitForSeconds(0.2f);

            if (RemoveHap == true)
            {
                hapticFingerTrigger1.RemoveVibration(Frequency);
            }
            else
            {
                StartCoroutine(RemoveHaptic(hapticFingerTrigger1));
                RemoveHap = true;
            }
        }
    }
}
