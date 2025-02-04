using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using HaptGlove;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using UnityEditor;
using Unity.VisualScripting;


namespace HexR
{
    public class FingerUseTracking : MonoBehaviour
    {
        public GameObject IndexTip, IndexKnuckle, MiddleTip, MiddleKnuckle, RingTip, RingKnuckle, LittleTip, LittleKnuckle, ThumbTip, ThumbKnuckle;
        public TextMeshProUGUI DebugText;

        private float IndexDistance, MiddleDistance, RingDistance, LittleDistance, ThumbDistance;
        private float IndexLargest, MiddleLargest, RingLargest, LittleLargest, ThumbLargest;
        private float IndexSmallest, MiddleSmallest, RingSmallest, LittleSmallest, ThumbSmallest;

        internal HaptGloveManager haptGloveManager;
        internal PhysicsHandTracking haptHandTracking;
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
#if UNITY_EDITOR
    [CustomEditor(typeof(FingerUseTracking))]
    public class FingerUseTrackingEditorSetting : Editor
    {
        public override void OnInspectorGUI()
        {

            // Get reference to the target script
            FingerUseTracking controller = (FingerUseTracking)target;

         HaptGloveManager haptGloveManager = controller.GetComponentInParent<HaptGloveManager>();
         PhysicsHandTracking haptHandTracking= controller.gameObject.GetComponent<PhysicsHandTracking>();

            #region Editor GUI for hexr panel
            // Create a tooltip for the slider
            GUIContent IndexTipTool = new GUIContent(
                "Index Tip",
                "Tip of the index finger"
            );
            // Create a tooltip for the slider
            GUIContent IndexKnuckleTool = new GUIContent(
                "Index Knuckle",
                "Base of the index finger"
            );
        // Create a tooltip for the slider
        GUIContent MiddleTipTool = new GUIContent(
           "Index Tip",
           "Tip of the index finger"
       );
        // Create a tooltip for the slider
        GUIContent MiddleKnuckleTool = new GUIContent(
            "Index Knuckle",
            "Base of the index finger"
        );
        // Create a tooltip for the slider
        GUIContent RingTipTool = new GUIContent(
           "Index Tip",
           "Tip of the index finger"
       );
        // Create a tooltip for the slider
        GUIContent RingKnuckleTool = new GUIContent(
            "Index Knuckle",
            "Base of the index finger"
        );
        // Create a tooltip for the slider
        GUIContent LittleTipTool = new GUIContent(
           "Index Tip",
           "Tip of the index finger"
       );
        // Create a tooltip for the slider
        GUIContent LittleKnuckleTool = new GUIContent(
            "Index Knuckle",
            "Base of the index finger"
        );
        // Create a tooltip for the slider
        GUIContent ThumbTipTool = new GUIContent(
           "Index Tip",
           "Tip of the index finger"
       );
        // Create a tooltip for the slider
        GUIContent ThumbKnuckleTool = new GUIContent(
            "Thumb Knuckle",
            "Base of the thumb"
        );
            GUIContent DebugtextTool = new GUIContent(
    "Optional Debug Text",
    "Shows the value of each finger open or close."
);

            controller.IndexTip = (GameObject)EditorGUILayout.ObjectField(IndexTipTool, controller.IndexTip, typeof(GameObject), true);
            controller.IndexKnuckle = (GameObject)EditorGUILayout.ObjectField(IndexKnuckleTool, controller.IndexKnuckle, typeof(GameObject), true);
            controller.MiddleTip = (GameObject)EditorGUILayout.ObjectField(MiddleTipTool, controller.MiddleTip, typeof(GameObject), true);
            controller.MiddleKnuckle = (GameObject)EditorGUILayout.ObjectField(MiddleKnuckleTool, controller.MiddleKnuckle, typeof(GameObject), true);
            controller.RingTip = (GameObject)EditorGUILayout.ObjectField(RingTipTool, controller.RingTip, typeof(GameObject), true);
            controller.RingKnuckle = (GameObject)EditorGUILayout.ObjectField(RingKnuckleTool, controller.RingKnuckle, typeof(GameObject), true);
            controller.LittleTip = (GameObject)EditorGUILayout.ObjectField(LittleTipTool, controller.LittleTip, typeof(GameObject), true);
            controller.LittleKnuckle = (GameObject)EditorGUILayout.ObjectField(LittleKnuckleTool, controller.LittleKnuckle, typeof(GameObject), true);
            controller.ThumbTip = (GameObject)EditorGUILayout.ObjectField(ThumbTipTool, controller.ThumbTip, typeof(GameObject), true);
            controller.ThumbKnuckle = (GameObject)EditorGUILayout.ObjectField(ThumbKnuckleTool, controller.ThumbKnuckle, typeof(GameObject), true);
            controller.DebugText = (TextMeshProUGUI)EditorGUILayout.ObjectField(DebugtextTool, controller.DebugText, typeof(TextMeshProUGUI), true);
            #endregion

            // Add vertical spacing
            GUILayout.Space(15); // Adds 10 pixels of space

            if (GUILayout.Button("Auto Set Up "))
            {

                if (haptGloveManager.XRFramework == HaptGloveManager.Options.OpenXR)
                {
                    if(haptHandTracking.handType == PhysicsHandTracking.HandType.Left)
                    {
                        try
                        {
                            // Directly find inactive GameObjects
                            controller.IndexTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_IndexTip");
                            controller.IndexKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_IndexProximal");
                            controller.MiddleTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_MiddleTip");
                            controller.MiddleKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_MiddleProximal");
                            controller.RingTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_RingTip");
                            controller.RingKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_RingProximal");
                            controller.LittleTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_LittleTip");
                            controller.LittleKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_LittleProximal");
                            controller.ThumbTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_ThumbTip");
                            controller.ThumbKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "L_ThumbMetacarpal");

                            Debug.Log("Left Finger Use Tracking Set Up Complete");
                        }
                        catch
                        {
                            Debug.Log("FingerUseTracking is not set up, Manual Set up needed");

                        }
                    }
                    else //Right
                    {
                        try
                        {
                            // Directly find inactive GameObjects
                            controller.IndexTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_IndexTip");
                            controller.IndexKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_IndexProximal");
                            controller.MiddleTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_MiddleTip");
                            controller.MiddleKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_MiddleProximal");
                            controller.RingTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_RingTip");
                            controller.RingKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_RingProximal");
                            controller.LittleTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_LittleTip");
                            controller.LittleKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_LittleProximal");
                            controller.ThumbTip = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_ThumbTip");
                            controller.ThumbKnuckle = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "R_ThumbMetacarpal");


                            Debug.Log("Right Finger Use Tracking Set Up Complete");
                        }
                        catch
                        {
                            Debug.Log("FingerUseTracking is not set up, Manual Set up needed");

                        }
                    }
 
                }

                else if (haptGloveManager.XRFramework == HaptGloveManager.Options.MetaOVR)
                {
                    if (haptHandTracking.handType == PhysicsHandTracking.HandType.Left)
                    {
                        try
                        {
                            Transform commonParent = GameObject.Find("Left Hand Physics").transform;

                            // Get all child objects recursively (including inactive objects)
                            Transform[] children = commonParent.GetComponentsInChildren<Transform>(true);

                            // Find child objects by their names
                            controller.IndexTip = children.FirstOrDefault(t => t.name == "l_index_finger_tip_marker")?.gameObject;
                            controller.IndexKnuckle = children.FirstOrDefault(t => t.name == "L_Index_1")?.gameObject;
                            controller.MiddleTip = children.FirstOrDefault(t => t.name == "l_middle_finger_tip_marker")?.gameObject;
                            controller.MiddleKnuckle = children.FirstOrDefault(t => t.name == "L_Middle_1")?.gameObject;
                            controller.RingTip = children.FirstOrDefault(t => t.name == "l_ring_finger_tip_marker")?.gameObject;
                            controller.RingKnuckle = children.FirstOrDefault(t => t.name == "L_Ring_1")?.gameObject;
                            controller.LittleTip = children.FirstOrDefault(t => t.name == "l_pinky_finger_tip_marker")?.gameObject;
                            controller.LittleKnuckle = children.FirstOrDefault(t => t.name == "L_Pinky_1")?.gameObject;
                            controller.ThumbTip = children.FirstOrDefault(t => t.name == "l_thumb_finger_tip_marker")?.gameObject;
                            controller.ThumbKnuckle = children.FirstOrDefault(t => t.name == "L_Thumb_1")?.gameObject;

                            Debug.Log("Left Finger Use Tracking Set Up Complete");
                        }
                        catch
                        {
                            Debug.Log("FingerUseTracking is not set up, Manual Set up needed");

                        }
                    }
                    else //Right
                    {
                        try
                        {
                            Transform commonParent = GameObject.Find("Right Hand Physics").transform;

                            // Get all child objects recursively (including inactive objects)
                            Transform[] children = commonParent.GetComponentsInChildren<Transform>(true);

                            // Find child objects by their names
                            controller.IndexTip = children.FirstOrDefault(t => t.name == "r_index_finger_tip_marker")?.gameObject;
                            controller.IndexKnuckle = children.FirstOrDefault(t => t.name == "R_Index_1")?.gameObject;
                            controller.MiddleTip = children.FirstOrDefault(t => t.name == "r_middle_finger_tip_marker")?.gameObject;
                            controller.MiddleKnuckle = children.FirstOrDefault(t => t.name == "R_Middle_1")?.gameObject;
                            controller.RingTip = children.FirstOrDefault(t => t.name == "r_ring_finger_tip_marker")?.gameObject;
                            controller.RingKnuckle = children.FirstOrDefault(t => t.name == "R_Ring_1")?.gameObject;
                            controller.LittleTip = children.FirstOrDefault(t => t.name == "r_pinky_finger_tip_marker")?.gameObject;
                            controller.LittleKnuckle = children.FirstOrDefault(t => t.name == "R_Pinky_1")?.gameObject;
                            controller.ThumbTip = children.FirstOrDefault(t => t.name == "r_thumb_finger_tip_marker")?.gameObject;
                            controller.ThumbKnuckle = children.FirstOrDefault(t => t.name == "R_Thumb_1")?.gameObject;



                            Debug.Log("Right Finger Use Tracking Set Up Complete");
                        }
                        catch
                        {
                            Debug.Log("FingerUseTracking is not set up, Manual Set up needed");

                        }
                    }

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

