    using System;
using System.Collections;
using System.Collections.Generic;
using HaptGlove;
using UnityEditor;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using Unity.VisualScripting;
namespace HexR
{
    public class HaptGloveManager : MonoBehaviour
    {
        public enum Options { OpenXR, MetaOVR } //MRTK not included yet
        public Options XRFramework;
        public bool isQuest;

        public HaptGloveHandler leftHand;
        public HaptGloveHandler rightHand;
        public GameObject HandMenu;
        public GameObject NewHandMenu;

        public GameObject BluetoothIndicatorL, BluetoothIndicatorR, pumpIndicator_L, pumpIndicator_R, HexRPanel;
        private string bluetoothLog;
        public TextMeshProUGUI RightBtText, LeftBtText;
        void Start()
        {
            if (isQuest)
            {
                leftHand.isQuest = true;
                rightHand.isQuest = true;
            }
            else
            {
                leftHand.isQuest = false;
                rightHand.isQuest = false;
            }

            leftHand.onBluetoothConnected += HaptGlove_OnConnected;
            leftHand.onBluetoothConnectionFailed += HaptGlove_OnConnectedFailed;
            leftHand.onBluetoothDisconnected += HaptGlove_OnDisconnected;
            leftHand.onPumpAction += HaptGlove_OnPumpAction;

            rightHand.onBluetoothConnected += HaptGlove_OnConnected;
            rightHand.onBluetoothConnectionFailed += HaptGlove_OnConnectedFailed;
            rightHand.onBluetoothDisconnected += HaptGlove_OnDisconnected;
            rightHand.onPumpAction += HaptGlove_OnPumpAction;

            //Add all layers that you want to interact with HaptGlove
            AddHaptGloveInteractableLayer("HaptGloveInteractable");
        }


        private void HaptGlove_OnConnected(HaptGloveHandler.HandType hand)
        {
            if (hand == HaptGloveHandler.HandType.Left)
            {
                BluetoothIndicatorL?.SetActive(true);
                if (LeftBtText != null)
                {
                    LeftBtText.text = "Left Glove Connected";
                }
                bluetoothLog = "Left glove connected: " + "HaptGLove " + hand.ToString();
                StartCoroutine(Pump(leftHand.GetComponent<HaptGloveHandler>()));
            }
            else if (hand == HaptGloveHandler.HandType.Right)
            {
                BluetoothIndicatorR?.SetActive(true);
                if (RightBtText != null)
                {
                    RightBtText.text = "Right Glove Connected";
                }
                bluetoothLog = "Right glove connected: " + "HaptGLove " + hand.ToString();
                StartCoroutine(Pump(rightHand.GetComponent<HaptGloveHandler>()));
            }

        }

        IEnumerator Pump(HaptGloveHandler haptGloveHandler)
        {
            // Wait for the specified delay time
            yield return new WaitForSeconds(2f);

            haptGloveHandler.AirPressureSourceControl();
        }

        private void HaptGlove_OnConnectedFailed(HaptGloveHandler.HandType hand)
        {
            if (hand == HaptGloveHandler.HandType.Left)
            {
                BluetoothIndicatorL?.SetActive(false);
                if(LeftBtText != null)
                {
                    LeftBtText.text = "Connection failed, try again";
                }
                bluetoothLog = "Left glove connection failed: " + "HaptGlove " + hand.ToString();
            }
            else if (hand == HaptGloveHandler.HandType.Right)
            {
                BluetoothIndicatorR?.SetActive(false);
                if (RightBtText != null)
                {
                    RightBtText.text = "Connection failed, try again";
                }
                bluetoothLog = "Right glove connection failed: " + "HaptGlove " + hand.ToString();
            }
            HexRPanel.SetActive(true);
        }

        private void HaptGlove_OnDisconnected(HaptGloveHandler.HandType hand)
        {
            if (hand == HaptGloveHandler.HandType.Left)
            {
                BluetoothIndicatorL?.SetActive(false);
                if(LeftBtText!=null)
                {
                    LeftBtText.text = "HexR Left Disconnected";
                }
                bluetoothLog = "Left glove disconnected: " + "HaptGlove " + hand.ToString();
            }
            else if (hand == HaptGloveHandler.HandType.Right)
            {
                BluetoothIndicatorR?.SetActive(false);
                if (RightBtText != null)
                {
                    RightBtText.text = "HexR Right Disconnected";
                }
                bluetoothLog = "Right glove disconnected: " + "HaptGlove " + hand.ToString();
            }
            HexRPanel.SetActive(true);
        }

        private void HaptGlove_OnPumpAction(HaptGloveHandler.HandType hand, bool state)
        {
            if (hand == HaptGloveHandler.HandType.Left)
            {
                if (LeftBtText != null)
                {
                    LeftBtText.text = "Left Glove Ready";
                }
                if (state)
                    pumpIndicator_L?.SetActive(true);
                else
                    pumpIndicator_L?.SetActive(false);
            }
            else if (hand == HaptGloveHandler.HandType.Right)
            {
                if (RightBtText != null)
                {
                    RightBtText.text = "Right Glove Ready";
                }
                if (state)
                    pumpIndicator_R?.SetActive(true);
                else
                    pumpIndicator_R?.SetActive(false);
            }
        }

        public void AddHaptGloveInteractableLayer(string layerName)
        {
            leftHand.hapticsInteratableLayers.Add(LayerMask.NameToLayer(layerName));
            rightHand.hapticsInteratableLayers.Add(LayerMask.NameToLayer(layerName));
        }

        public string[] GetHaptGloveInteractableLayer()
        {
            int[] layers = rightHand.hapticsInteratableLayers.ToArray();
            string[] layerNames = new string[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                layerNames[i] = LayerMask.LayerToName(layers[i]);
            }

            return layerNames;
        }



#if UNITY_EDITOR
        [CustomEditor(typeof(HaptGloveManager))]
        public class HexRSettingEditorGUI : Editor
        {
            public override void OnInspectorGUI()
            {

                // Get reference to the target script
                HaptGloveManager controller = (HaptGloveManager)target;

                // Draw default fields
                controller.XRFramework = (HaptGloveManager.Options)EditorGUILayout.EnumPopup("XR Framework", controller.XRFramework);

                EditorGUILayout.LabelField("Hand Physics Components", EditorStyles.boldLabel);

                controller.rightHand = (HaptGloveHandler)EditorGUILayout.ObjectField("Right Hand Physics", controller.rightHand,
                    typeof(HaptGloveHandler), // Corrected type
                    true
                );

                controller.leftHand = (HaptGloveHandler)EditorGUILayout.ObjectField("Left Hand Physics", controller.leftHand,
                    typeof(HaptGloveHandler), // Corrected type
                    true
                );
                if (controller.XRFramework == Options.OpenXR)
                {
                    controller.HandMenu = (GameObject)EditorGUILayout.ObjectField("HexR Hand Menu", controller.HandMenu, typeof(GameObject), true);
                }
                // Add vertical spacing
                GUILayout.Space(15); // Adds 10 pixels of space

                EditorGUILayout.LabelField("HexR Panel Components", EditorStyles.boldLabel);

                #region Editor GUI for hexr panel
                // Create a tooltip for the slider
                GUIContent btIndicator_LTool = new GUIContent(
                    "Bluetooth Indicator L",
                    "The Visual Indicator for Left Bluetooth Connections"
                );
                // Create a tooltip for the slider
                GUIContent btIndicator_RTool = new GUIContent(
                    "Bluetooth Indicator R",
                    "The Visual Indicator for Right Bluetooth Connections"
                );
                // Create a tooltip for the slider
                GUIContent pumpIndicator_LTool = new GUIContent(
                    "Pump Indicator L",
                    "The Visual Indicator for Left Pump Status"
                );
                // Create a tooltip for the slider
                GUIContent pumpIndicator_RTool = new GUIContent(
                    "Pump Indicator R",
                    "The Visual Indicator for Right Pump Status"
                );
                // Create a tooltip for the slider
                GUIContent LeftBtTextTool = new GUIContent(
                    "Left Bluetooth Text",
                    "Text to indicate Left HexR Connection Status"
                );
                // Create a tooltip for the slider
                GUIContent RightBtTextTool = new GUIContent(
                    "Right Bluetooth Text",
                    "Text to indicate Right HexR Connection Status"
                );

                controller.BluetoothIndicatorL = (GameObject)EditorGUILayout.ObjectField(btIndicator_LTool, controller.BluetoothIndicatorL, typeof(GameObject), true);
                controller.BluetoothIndicatorR = (GameObject)EditorGUILayout.ObjectField(btIndicator_RTool, controller.BluetoothIndicatorR, typeof(GameObject), true);
                controller.pumpIndicator_L = (GameObject)EditorGUILayout.ObjectField(pumpIndicator_LTool, controller.pumpIndicator_L, typeof(GameObject), true);
                controller.pumpIndicator_R = (GameObject)EditorGUILayout.ObjectField(pumpIndicator_RTool, controller.pumpIndicator_R, typeof(GameObject), true);
                controller.LeftBtText = (TextMeshProUGUI)EditorGUILayout.ObjectField(LeftBtTextTool, controller.LeftBtText, typeof(TextMeshProUGUI), true);
                controller.RightBtText = (TextMeshProUGUI)EditorGUILayout.ObjectField(RightBtTextTool, controller.RightBtText, typeof(TextMeshProUGUI), true);
                #endregion

                // Add vertical spacing
                GUILayout.Space(15); // Adds 10 pixels of space

                if (GUILayout.Button("Auto Set Up HexR"))
                {
                    try
                    {
                        controller.rightHand = GameObject.Find("Right Hand Physics").GetComponent<HaptGloveHandler>(); 
                        controller.leftHand = GameObject.Find("Left Hand Physics").GetComponent<HaptGloveHandler>();
                        Debug.Log("Right Hand Physics Found And Assigned.");
                    }
                    catch
                    {
                        Debug.Log("HaptGloveHandler Not Found Remember to assign them.");
                    }



                    if(controller.XRFramework == Options.OpenXR)
                    {
                        //Set up hand menu
                        try
                        {
                            controller.NewHandMenu = Instantiate(controller.HandMenu);
                            DestroyImmediate(controller.HandMenu);
                            controller.NewHandMenu.transform.SetParent(GameObject.Find("Camera Offset").transform);
                            controller.NewHandMenu.transform.localPosition = Vector3.zero;
                            controller.HandMenu = controller.NewHandMenu;
                            // Directly find inactive GameObjects
                            controller.BluetoothIndicatorL = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Bluetooth Indicator L");
                            controller.pumpIndicator_L = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Pump Indicator L");
                            controller.BluetoothIndicatorR = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Bluetooth Indicator R");
                            controller.pumpIndicator_R = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Pump Indicator R");
                            controller.LeftBtText = GameObject.FindObjectsOfType<TextMeshProUGUI>(true).FirstOrDefault(obj => obj.name == "Left HexR Text");
                            controller.RightBtText = GameObject.FindObjectsOfType<TextMeshProUGUI>(true).FirstOrDefault(obj => obj.name == "Right HexR Text");

                            Debug.Log("HexR Hand Menu Set Up Complete");
                        }
                        catch
                        {
                            Debug.Log("HexR panel is not set up, Manual Set up needed");

                        }
                        //Set up hand menu bluetooth buttons
                        try
                        {
                            Button RightBluetoothButton = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Right Bluetooth Button").GetComponent<Button>();
                            Button LeftBluetoothButton = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Left Bluetooth Button").GetComponent<Button>();

                            HaptGloveUIOpenXR haptGloveUIOpenXR = controller.gameObject.GetComponent<HaptGloveUIOpenXR>();
                            RightBluetoothButton.onClick.AddListener(haptGloveUIOpenXR.ConnectRightBT);
                            LeftBluetoothButton.onClick.AddListener(haptGloveUIOpenXR.ConnectLeftBT);
                            Debug.Log("HexR panel button set up complete.");
                        }
                        catch
                        {
                            Debug.Log("HexR panel button is not set up, Manual Set up needed");
                        }
                        // Find hand root for physics hand
                        try
                        {
                            GameObject LeftXR = GameObject.Find("Left Hand Interaction Visual");
                            GameObject RightXR = GameObject.Find("Right Hand Interaction Visual");
                            PhysicsHandTracking LeftP = controller.leftHand.gameObject.GetComponent<PhysicsHandTracking>();
                            PhysicsHandTracking RightP = controller.rightHand.gameObject.GetComponent<PhysicsHandTracking>();
                            LeftP.handRoot = LeftXR.transform.Find("L_Wrist");
                            RightP.handRoot = RightXR.transform.Find("R_Wrist");
                            EditorUtility.SetDirty(LeftP); // Mark as dirty to save changes
                            EditorUtility.SetDirty(RightP); // Mark as dirty to save changes

                        }
                        catch
                        {
                            Debug.Log("XR hand is linked not linked to Physics hand tracking, manual link needed, Drag the hand root of your vr hand to the left and right physicshandtracking script");
                        }
                    }

                    else if(controller.XRFramework == Options.MetaOVR)
                    {
                        //Set up HexR Panel
                        try
                        {
                            // Directly find inactive GameObjects
                            controller.BluetoothIndicatorL = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Bluetooth Indicator L");
                            controller.pumpIndicator_L = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Pump Indicator L");
                            controller.BluetoothIndicatorR = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Bluetooth Indicator R");
                            controller.pumpIndicator_R = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Pump Indicator R");
                            controller.LeftBtText = GameObject.FindObjectsOfType<TextMeshProUGUI>(true).FirstOrDefault(obj => obj.name == "Left HexR Text");
                            controller.RightBtText = GameObject.FindObjectsOfType<TextMeshProUGUI>(true).FirstOrDefault(obj => obj.name == "Right HexR Text");

                            Debug.Log("HexR Panel Set Up Complete");
                        }
                        catch
                        {
                            Debug.Log("HexR panel is not set up, Manual Set up needed");

                        }
                        // Find hand root for physics hand
                        try
                        {
                            GameObject LeftXR = GameObject.Find("Left Hand Interaction Visual");
                            GameObject RightXR = GameObject.Find("Right Hand Interaction Visual");
                            PhysicsHandTracking LeftP = controller.leftHand.gameObject.GetComponent<PhysicsHandTracking>();
                            PhysicsHandTracking RightP = controller.rightHand.gameObject.GetComponent<PhysicsHandTracking>();
                            LeftP.handRoot = LeftXR.transform.Find("OculusHand_L");
                            RightP.handRoot = RightXR.transform.Find("OculusHand_R");
                            EditorUtility.SetDirty(LeftP); // Mark as dirty to save changes
                            EditorUtility.SetDirty(RightP); // Mark as dirty to save changes

                        }
                        catch
                        {
                            Debug.Log("XR hand is linked not linked to Physics hand tracking, manual link needed, Drag the hand root of your vr hand to the left and right physicshandtracking script");
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
}
