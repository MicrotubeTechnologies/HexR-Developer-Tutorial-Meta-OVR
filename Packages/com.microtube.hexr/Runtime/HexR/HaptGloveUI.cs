    using System.Collections;
using System.Collections.Generic;
using HaptGlove;
//using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.XR;
//using UnityEngine.XR.Management;
using TMPro;

namespace HexR
{
    public class HaptGloveUI : MonoBehaviour
    {
        private HaptGloveHandler LeftHandPhysics, RightHandPhysics;
        private TextMeshProUGUI RightBtText, LeftBtText;
        private HexRManager haptGloveManager;

        private List<string> controlledHandsList = new List<string>();

        void Start()
        {
            try { haptGloveManager = gameObject.GetComponent<HexRManager>(); }
            catch { Debug.Log("HaptGlove manager is not found."); }

            if (haptGloveManager!=null)
            {
                RightBtText = haptGloveManager.RightBtText;
                LeftBtText = haptGloveManager.LeftBtText;
                LeftHandPhysics = haptGloveManager.leftHand;
                RightHandPhysics = haptGloveManager.rightHand;
            }
            else
            {
                Debug.Log("Please place HexRManager in the same gameObject as HaptGloveUIOpenXR");
            }


        }

        void Update()
        {

        }

        public void ConnectRightBT()
        {
            // HexRManager subscribes to the connection events, so it is the only thing that
            // can tell when a request finishes -- ask it rather than tracking a second copy
            // of the same state here.
            HexRManager manager = HexRManager.Instance;
            if (manager != null && !manager.BeginConnect(HaptGloveHandler.HandType.Right))
            {
                return;
            }

            controlledHandsList.Remove("Left");
            controlledHandsList.Add("Right");
            RightBtText.text = "Searching for HexR Right…";
            // GetComponent stays on Unity's thread; only the plugin call is marshalled.
            HaptGloveHandler right = RightHandPhysics.GetComponent<HaptGloveHandler>();
            AndroidUiThread.Run(right.BTConnection);  
        }
        public void ConnectLeftBT()
        {
            HexRManager manager = HexRManager.Instance;
            if (manager != null && !manager.BeginConnect(HaptGloveHandler.HandType.Left))
            {
                return;
            }

            controlledHandsList.Add("Left");
            controlledHandsList.Remove("Right");
            LeftBtText.text = "Searching for HexR Left…";
            HaptGloveHandler left = LeftHandPhysics.GetComponent<HaptGloveHandler>();
            AndroidUiThread.Run(left.BTConnection);
        }
    }


}
