using System.Collections;
using System.Collections.Generic;
using HaptGlove;
//using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.XR;
//using UnityEngine.XR.Management;
using TMPro;
using UnityEditor;

namespace HexR
{
    public class HaptGloveUIOpenXR : MonoBehaviour
    {
        private HaptGloveHandler LeftHandPhysics, RightHandPhysics;
        private TextMeshProUGUI RightBtText, LeftBtText;
        private HaptGloveManager haptGloveManager;

        private List<string> controlledHandsList = new List<string>();

        void Start()
        {
            haptGloveManager = gameObject.GetComponent<HaptGloveManager>();
            if(haptGloveManager!=null)
            {
                RightBtText = haptGloveManager.RightBtText;
                LeftBtText = haptGloveManager.LeftBtText;
                LeftHandPhysics = haptGloveManager.leftHand;
                RightHandPhysics = haptGloveManager.rightHand;
            }
            else
            {
                Debug.Log("Please place HaptGloveManager in the same gameObject as HaptGloveUIOpenXR");
            }
        }

        void Update()
        {
            // Check if the "P" key is pressed
            if (Input.GetKeyDown(KeyCode.P))
            {
                // Call the function you want to trigger
                ConnectRightBT();
            }
        }

        public void ConnectRightBT()
        {
            controlledHandsList.Remove("Left");
            controlledHandsList.Add("Right");
            RightBtText.text = "Searching for device...";
            RightHandPhysics.GetComponent<HaptGloveHandler>().BTConnection();
        }
        public void ConnectLeftBT()
        {
            controlledHandsList.Add("Left");
            controlledHandsList.Remove("Right");
            LeftBtText.text = "Searching for device...";
            LeftHandPhysics.GetComponent<HaptGloveHandler>().BTConnection();
        }
    }


}
