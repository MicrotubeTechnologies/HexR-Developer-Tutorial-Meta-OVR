using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HaptGlove
{
    public class HapMaterial : MonoBehaviour
    {
        void Start()
        {
            gameObject.GetComponent<Rigidbody>().centerOfMass = Vector3.zero;
        }

        //[Tooltip("Consider all colliders under one rigidbody as one collider that only trigger haptics once.")]
        //public bool isOneObject;
        [Tooltip("Select which channels you want haptics to be delivered to.")]
        public bool[] hapticsChannels = new bool[6] { true, true, true, true, true, true };
        [Tooltip(
            "TRUE indicates touching object meaning the object doesn't move. FALSE indicates grasping object meaning the object will move with hands once grasped.")]
        public bool isTouch;

        [Tooltip("Set to TRUE if grasping kinematic objects")]
        public bool iskinematicGrasp;

        [Header("Static pressure level")]
        [Tooltip("Range 0-50, step 10.")]
        [Range(0, 50)]
        public byte targetPressure = 0;

        [Header("Vibration")]
        [Tooltip("TRUE indicates a vibration haptic feedback.")]
        public bool isVibration;

        [Tooltip("Range 1-50.")]
        [Range(1, 50)]
        public byte vibFrequency = 10;

        [HideInInspector]
        [Tooltip("Range 1-3.")]
        public byte vibIntensity = 1;

        [HideInInspector]
        public bool isGrasped = false;
        [HideInInspector]
        public GameObject graspedHand = null;



    }
}
