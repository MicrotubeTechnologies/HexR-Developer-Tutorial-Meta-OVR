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

        // HexRManager owns the connect flow -- the one-at-a-time latch, the Android runtime
        // permission request, and the connection events -- so a press wired here is handed
        // to it. Previously this ran its own copy of the flow, which skipped the permission
        // request and, being wired on the panel prefab, ran *before* HexRManager's own
        // listener and so won the latch every time. The direct path is only for a scene
        // with no HexRManager at all.
        public void ConnectRightBT()
        {
            HexRManager manager = HexRManager.Instance;
            if (manager != null)
            {
                manager.ConnectRightBT();
                return;
            }

            controlledHandsList.Remove("Left");
            controlledHandsList.Add("Right");
            if (RightBtText != null)
            {
                RightBtText.text = "Searching for HexR Right…";
            }
            if (RightHandPhysics != null)
            {
                RightHandPhysics.BTConnection();
            }
        }
        public void ConnectLeftBT()
        {
            HexRManager manager = HexRManager.Instance;
            if (manager != null)
            {
                manager.ConnectLeftBT();
                return;
            }

            controlledHandsList.Add("Left");
            controlledHandsList.Remove("Right");
            if (LeftBtText != null)
            {
                LeftBtText.text = "Searching for HexR Left…";
            }
            if (LeftHandPhysics != null)
            {
                LeftHandPhysics.BTConnection();
            }
        }
    }


}
