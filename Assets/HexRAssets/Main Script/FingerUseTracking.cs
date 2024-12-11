using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace HexR
{
    public class FingerUseTracking : MonoBehaviour
    {
        public GameObject IndexTip, IndexKnuckle, MiddleTip, MiddleKnuckle, RingTip, RingKnuckle, LittleTip, LittleKnuckle, ThumbTip, ThumbKnuckle;
        public TextMeshProUGUI DebugText;

        private float IndexDistance, MiddleDistance, RingDistance, LittleDistance, ThumbDistance;
        private float IndexLargest, MiddleLargest, RingLargest, LittleLargest, ThumbLargest;
        private float IndexSmallest, MiddleSmallest, RingSmallest, LittleSmallest, ThumbSmallest;

        [HideInInspector]
        public float IndexUse, MiddleUse, RingUse, LittleUse, ThumbUse;

        // Start is called before the first frame update
        void Start()
        {
            // Initialize smallest values to a very large number
            IndexSmallest = MiddleSmallest = RingSmallest = LittleSmallest = ThumbSmallest = float.MaxValue;

            // Initialize largest values to a very small number
            IndexLargest = MiddleLargest = RingLargest = LittleLargest = ThumbLargest = float.MinValue;
        }

        // Update is called once per frame
        void Update()
        {
            // Calculate distances
            IndexDistance = Vector3.Distance(IndexTip.transform.position, IndexKnuckle.transform.position);
            MiddleDistance = Vector3.Distance(MiddleTip.transform.position, MiddleKnuckle.transform.position);
            RingDistance = Vector3.Distance(RingTip.transform.position, RingKnuckle.transform.position);
            LittleDistance = Vector3.Distance(LittleTip.transform.position, LittleKnuckle.transform.position);
            ThumbDistance = Vector3.Distance(ThumbTip.transform.position, ThumbKnuckle.transform.position);

            // Check and update max and min distances
            CheckMaxDistance();
            CheckMinDistance();

            // Calculate normalized "use" values
            CheckFingerUse();

            // Update debug text
            if(DebugText != null)
            {
                DebugText.text = $"{IndexUse:F2} | {MiddleUse:F2} | {RingUse:F2} | {LittleUse:F2} | {ThumbUse:F2}";
            }
        }

        private void CheckMaxDistance()
        {
            IndexLargest = Mathf.Max(IndexLargest, IndexDistance);
            MiddleLargest = Mathf.Max(MiddleLargest, MiddleDistance);
            RingLargest = Mathf.Max(RingLargest, RingDistance);
            LittleLargest = Mathf.Max(LittleLargest, LittleDistance);
            ThumbLargest = Mathf.Max(ThumbLargest, ThumbDistance);
        }

        private void CheckMinDistance()
        {
            IndexSmallest = Mathf.Min(IndexSmallest, IndexDistance);
            MiddleSmallest = Mathf.Min(MiddleSmallest, MiddleDistance);
            RingSmallest = Mathf.Min(RingSmallest, RingDistance);
            LittleSmallest = Mathf.Min(LittleSmallest, LittleDistance);
            ThumbSmallest = Mathf.Min(ThumbSmallest, ThumbDistance);
        }

        private void CheckFingerUse()
        {
            IndexUse = Normalize(IndexDistance, IndexSmallest, IndexLargest);
            MiddleUse = Normalize(MiddleDistance, MiddleSmallest, MiddleLargest);
            RingUse = Normalize(RingDistance, RingSmallest, RingLargest);
            LittleUse = Normalize(LittleDistance, LittleSmallest, LittleLargest);
            ThumbUse = Normalize(ThumbDistance, ThumbSmallest, ThumbLargest);
        }

        private float Normalize(float value, float min, float max)
        {
            if (Mathf.Approximately(max, min))
                return 0; // Avoid division by zero
            return (value - min) / (max - min);
        }

        public bool isHandOpen()
        {
            if(IndexUse > 0.95 
                && MiddleUse > 0.95
                && RingUse > 0.95
                && ThumbUse > 0.9
                && LittleUse > 0.95)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void AutoFillFields()
        {

        }
         
    }
}

