using System;
using System.Collections;
using System.Collections.Generic;
using HaptGlove;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
namespace HexR
{
    public class HexRManager : MonoBehaviour
    {
        public static HexRManager Instance { get; private set; }  // ← add here
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

        // The raw hand rig isn't mirror-symmetric between hands in local space (confirmed --
        // the same offset landed in the wrong place on the right hand), and different
        // fingers on the same hand aren't interchangeable either (confirmed -- right index
        // needed its own value, not the shared one used for Middle/Ring/Little). So every
        // finger on every hand gets its own tunable center instead of one shared value.
        [System.Serializable]
        public class FingertipCenters
        {
            public Vector3 Thumb = Vector3.zero;
            public Vector3 Index = Vector3.zero;
            public Vector3 Middle = Vector3.zero;
            public Vector3 Ring = Vector3.zero;
            public Vector3 Little = Vector3.zero;

            public Vector3 Get(HapticFingerTrigger.FingerType finger)
            {
                switch (finger)
                {
                    case HapticFingerTrigger.FingerType.Thumb: return Thumb;
                    case HapticFingerTrigger.FingerType.Index: return Index;
                    case HapticFingerTrigger.FingerType.Middle: return Middle;
                    case HapticFingerTrigger.FingerType.Ring: return Ring;
                    case HapticFingerTrigger.FingerType.Little: return Little;
                    default: return Vector3.zero;
                }
            }
        }

        [Tooltip("Radius of the sphere trigger colliders Auto Setup adds to each fingertip on the raw tracked hand.")]
        public float FingertipColliderRadius = 0.008f;

        public FingertipCenters LeftFingertipCenters = new FingertipCenters
        {
            Thumb = Vector3.zero,
            Index = new Vector3(0.0102788536f, 0.004925685f, 0.00146568683f),
            Middle = new Vector3(0.0102788536f, 0.004925685f, 0.00146568683f),
            Ring = new Vector3(0.0102788536f, 0.004925685f, 0.00146568683f),
            Little = new Vector3(0.0102788536f, 0.004925685f, 0.00146568683f),
        };

        public FingertipCenters RightFingertipCenters = new FingertipCenters
        {
            Thumb = Vector3.zero,
            Index = new Vector3(-0.00710000005f, -0.00419999985f, 0f),
            Middle = new Vector3(-0.0102788536f, -0.004925685f, 0.00146568683f),
            Ring = new Vector3(-0.0102788536f, -0.004925685f, 0.00146568683f),
            Little = new Vector3(-0.0102788536f, -0.004925685f, 0.00146568683f),
        };

        [Tooltip("Local-space size (x/y/z) of the box trigger collider Auto Setup adds to the palm on the raw tracked hand.")]
        public Vector3 PalmColliderSize = new Vector3(0.06f, 0.017f, 0.065f);

        // Merged in from HaptGloveUI (2026-07-30) -- tracks which hand's button most
        // recently initiated a connection, for UI wired via ConnectLeftBT/ConnectRightBT.
        private List<string> controlledHandsList = new List<string>();

        void Update()
        {
#if UNITY_EDITOR
            // Play-mode testing convenience only -- lets you trigger the same connect flow
            // a controller/hand-menu tap on "Connect Left" would, without needing the actual
            // XR interaction to reach the button. Editor-only so it never ships to the
            // Quest build.
            if (Input.GetKeyDown(KeyCode.P))
            {
                ConnectLeftBT();
            }
#endif
        }

        public void ConnectRightBT()
        {
            controlledHandsList.Remove("Left");
            controlledHandsList.Add("Right");
            RightBtText.text = "Searching for device...";
            rightHand.BTConnection();
        }

        public void ConnectLeftBT()
        {
            controlledHandsList.Add("Left");
            controlledHandsList.Remove("Right");
            LeftBtText.text = "Searching for device...";
            leftHand.BTConnection();
        }

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
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
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
                StartCoroutine(TriggerFunctionEvery8Seconds("Left"));
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
                StartCoroutine(TriggerFunctionEvery8Seconds("Right"));
            }

        }
        IEnumerator TriggerFunctionEvery8Seconds(String LeftOrRight)
        {
            while (true) // Infinite loop to keep the coroutine running
            {
                // Call your function here
                BatteryState(LeftOrRight);

                // Wait for 8 seconds before continuing the loop
                yield return new WaitForSeconds(8f);
            }
        }
        private void BatteryState(String LeftOrRight)
        {
            if(LeftOrRight == "Right")
            {
                float BatteryLevel = rightHand.GetBatteryLevel();
                if(BatteryLevel == 0)
                {
                    RightBtText.text = "Right Glove Ready";
                }
                else
                {
                    RightBtText.text = "Right Glove Ready: " + Math.Round(BatteryLevel * 100) + "%";
                }
            }
            else if(LeftOrRight == "Left")
            {
                float BatteryLevel = leftHand.GetBatteryLevel();
                if (BatteryLevel == 0)
                {
                    LeftBtText.text = "Left Glove Ready";
                }
                else
                {
                    LeftBtText.text = "Left Glove Ready: " + Math.Round(BatteryLevel * 100) + "%";
                }

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
        // Extracted so both the Inspector's "Auto Set Up HexR" button and the top-level
        // HexR > Auto Setup Scene menu item (Editor/HexRMenu.cs) run the exact same
        // logic instead of it being duplicated in two places.
        public static void AutoSetup(HexRManager controller)
        {
            try
            {
                controller.rightHand = GameObject.Find("Right Hand Physics").GetComponent<HaptGloveHandler>();
                controller.leftHand = GameObject.Find("Left Hand Physics").GetComponent<HaptGloveHandler>();
                Debug.Log("Right Hand Physics Found And Assigned.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[HexR] AutoSetup: couldn't find/assign Left/Right Hand Physics -- " + e.Message + ". Remember to assign them manually.");
            }

            if (controller.XRFramework == Options.OpenXR)
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
                    controller.HexRPanel = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "HexR Panel");

                    Debug.Log("HexR Hand Menu Set Up Complete");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[HexR] AutoSetup: HexR panel is not set up -- " + e.Message + ". Manual set up needed.");
                }
                //Set up hand menu bluetooth buttons
                try
                {
                    Button RightBluetoothButton = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Right Bluetooth Button").GetComponent<Button>();
                    Button LeftBluetoothButton = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "Left Bluetooth Button").GetComponent<Button>();

                    RightBluetoothButton.onClick.AddListener(controller.ConnectRightBT);
                    LeftBluetoothButton.onClick.AddListener(controller.ConnectLeftBT);
                    Debug.Log("HexR panel button set up complete.");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[HexR] AutoSetup: HexR panel button is not set up -- " + e.Message + ". Manual set up needed.");
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

                    // The SDK's own hand-visual object, one level above handRoot -- a second
                    // visualizer here so the panel's other collider-toggle button has its own
                    // target (GetComponentsInChildren covers handRoot's colliders too, since
                    // handRoot is nested underneath).
                    EnsureColliderVisualizer(LeftXR);
                    EnsureColliderVisualizer(RightXR);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[HexR] AutoSetup: XR hand is not linked to Physics hand tracking -- " + e.Message + ". Manual link needed: drag the hand root of your VR hand to the left and right PhysicsHandTracking script.");
                }
            }

            else if (controller.XRFramework == Options.MetaOVR)
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
                    controller.HexRPanel = GameObject.FindObjectsOfType<GameObject>(true).FirstOrDefault(obj => obj.name == "HexR Panel");

                    Debug.Log("HexR Panel Set Up Complete");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[HexR] AutoSetup: HexR panel is not set up -- " + e.Message + ". Manual set up needed.");
                }
                // Find hand root for physics hand
                try
                {
                    PhysicsHandTracking LeftP = controller.leftHand.gameObject.GetComponent<PhysicsHandTracking>();
                    PhysicsHandTracking RightP = controller.rightHand.gameObject.GetComponent<PhysicsHandTracking>();
                    LeftP.handRoot = null;
                    RightP.handRoot = null;

                    // "OculusHand_L/R" is the legacy Oculus Integration naming. Projects built
                    // with Meta's newer "Building Blocks" hand-tracking block instead have
                    // "LeftOVRHand"/"RightOVRHand" (under "[BuildingBlock] Hand Tracking
                    // left/right") -- try that name too rather than silently failing and
                    // leaving handRoot unassigned (or stuck on whatever it was manually set to,
                    // e.g. a Synthetic Hand object that isn't necessarily what's active during
                    // real hand-tracking gameplay).
                    GameObject leftHandObj = GameObject.Find("OculusHand_L") ?? GameObject.Find("LeftOVRHand");
                    GameObject rightHandObj = GameObject.Find("OculusHand_R") ?? GameObject.Find("RightOVRHand");

                    LeftP.handRoot = leftHandObj.transform;
                    RightP.handRoot = rightHandObj.transform;
                    EditorUtility.SetDirty(LeftP); // Mark as dirty to save changes
                    EditorUtility.SetDirty(RightP); // Mark as dirty to save changes

                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[HexR] AutoSetup: XR hand is not linked to Physics hand tracking -- " + e.Message + ". Manual link needed: drag the hand root of your VR hand to the left and right PhysicsHandTracking script.");
                }
            }

            // Add trigger colliders + HapticFingerTrigger to each fingertip/palm directly on
            // the raw tracked hand -- this is now the one and only haptics/grab
            // touch-detection path (see PhysicsHandTracking.ResolveRawFingerJoint /
            // ResolveRawPalmJoint). Runs after handRoot is wired above, and is safe to
            // re-run: existing colliders are never touched, existing HapticFingerTrigger
            // components just get their config refreshed.
            AutoAddFingerHaptics(controller);

            // Wires handType (and, for Meta OVR, the grab/poke interactors used to gate
            // hand-near haptics) on each "Left/Right Pressure Controller" -- previously only
            // validated as present, never actually configured by Auto Setup.
            AutoSetupPressureControllers(controller);

            EditorUtility.SetDirty(controller); // Mark as dirty to save changes

            ValidateSetup(controller);
        }

        private static void AutoAddFingerHaptics(HexRManager controller)
        {
            AutoAddFingerHapticsForHand(controller, controller.leftHand, HaptGloveHandler.HandType.Left);
            AutoAddFingerHapticsForHand(controller, controller.rightHand, HaptGloveHandler.HandType.Right);
        }

        private static readonly HapticFingerTrigger.FingerType[] Fingers = new[]
        {
            HapticFingerTrigger.FingerType.Thumb, HapticFingerTrigger.FingerType.Index,
            HapticFingerTrigger.FingerType.Middle, HapticFingerTrigger.FingerType.Ring,
            HapticFingerTrigger.FingerType.Little
        };

        private static void AutoSetupPressureControllers(HexRManager controller)
        {
            AutoSetupPressureController(controller, controller.leftHand, HaptGloveHandler.HandType.Left);
            AutoSetupPressureController(controller, controller.rightHand, HaptGloveHandler.HandType.Right);
        }

        private static void AutoSetupPressureController(HexRManager controller, HaptGloveHandler hand, HaptGloveHandler.HandType handType)
        {
            string label = handType == HaptGloveHandler.HandType.Left ? "Left" : "Right";

            GameObject controllerObj = GameObject.Find(label + " Pressure Controller");
            if (controllerObj == null)
            {
                // Already flagged by ValidateSetup's "Pressure Controller present" check --
                // not created here since there's nothing sensible to attach it to on its own.
                return;
            }

            PressureTrackerMain pressureTracker = controllerObj.GetComponent<PressureTrackerMain>();
            if (pressureTracker == null)
            {
                Debug.LogWarning("[HexR] AutoSetup: \"" + label + " Pressure Controller\" has no PressureTrackerMain component -- skipping its setup.");
                return;
            }

            pressureTracker.handType = handType == HaptGloveHandler.HandType.Left ? PressureTrackerMain.HandType.Left : PressureTrackerMain.HandType.Right;

            // handGrabInteractor/pokeInteractor only matter for Meta OVR's hand-near gating
            // (IsHandGrabbing/IsPokeHover) -- best-effort find under the hand's own root
            // rather than left permanently unassigned; PressureTrackerMain already degrades
            // gracefully (treats as "not grabbing/poking") if these stay unassigned.
            if (controller.XRFramework == Options.MetaOVR && hand != null)
            {
                PhysicsHandTracking tracking = hand.GetComponent<PhysicsHandTracking>();
                if (tracking != null && tracking.handRoot != null)
                {
                    if (pressureTracker.handGrabInteractor == null)
                    {
                        pressureTracker.handGrabInteractor = tracking.handRoot.GetComponentInChildren<HandGrabInteractor>(true);
                    }
                    if (pressureTracker.pokeInteractor == null)
                    {
                        pressureTracker.pokeInteractor = tracking.handRoot.GetComponentInChildren<PokeInteractor>(true);
                    }

                    if (pressureTracker.handGrabInteractor == null || pressureTracker.pokeInteractor == null)
                    {
                        Debug.LogWarning("[HexR] AutoSetup: couldn't auto-find " + label + " Pressure Controller's HandGrabInteractor/PokeInteractor under its hand root -- manual wiring may be needed for Meta OVR grab/poke-based haptics gating.");
                    }
                }
            }

            EditorUtility.SetDirty(pressureTracker);
        }

        // Adds HaptGloveCollidersVisualizer to `target` if it doesn't already have one --
        // shared by the raw hand root (where the fingertip/palm colliders live) and, for
        // OpenXR, the SDK's own "Hand Interaction Visual" object one level up, so both of
        // the HexR Panel's collider-toggle buttons have something to point at.
        private static void EnsureColliderVisualizer(GameObject target)
        {
            if (target.GetComponent<HaptGloveCollidersVisualizer>() == null)
            {
                target.AddComponent<HaptGloveCollidersVisualizer>();
                EditorUtility.SetDirty(target);
            }
        }

        private static void AutoAddFingerHapticsForHand(HexRManager controller, HaptGloveHandler hand, HaptGloveHandler.HandType handType)
        {
            if (hand == null) return;
            PhysicsHandTracking tracking = hand.GetComponent<PhysicsHandTracking>();
            if (tracking == null) return;

            // So the new colliders can actually be seen (trigger colliders don't render) --
            // GetComponentsInChildren<Collider>() in HaptGloveCollidersVisualizer only picks
            // up colliders under whatever it's attached to, so it has to live on handRoot
            // itself (the raw hand's root), not on the glove-handler object -- that's a
            // separate hierarchy from where the new fingertip/palm colliders just got added.
            if (tracking.handRoot != null)
            {
                EnsureColliderVisualizer(tracking.handRoot.gameObject);
            }

            foreach (HapticFingerTrigger.FingerType finger in Fingers)
            {
                try
                {
                    Transform joint = tracking.ResolveRawFingerJoint(finger);
                    if (joint == null)
                    {
                        Debug.LogWarning("[HexR] AutoSetup: couldn't resolve " + handType + " " + finger + " tip joint on the raw hand -- skipping.");
                        continue;
                    }

                    // For the thumb, prefer the rig's own "..._thumb_null" locator (if it has
                    // one) over the tunable center -- it's an authored answer, not a guess.
                    Vector3? centerOverride = null;
                    if (finger == HapticFingerTrigger.FingerType.Thumb)
                    {
                        Transform thumbMarker = tracking.ResolveRawThumbCenterMarker();
                        if (thumbMarker != null)
                        {
                            centerOverride = joint.InverseTransformPoint(thumbMarker.position);
                        }
                    }

                    AddOrConfigureFingerHaptics(joint.gameObject, controller, handType, finger, centerOverride);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[HexR] AutoSetup: failed to set up " + handType + " " + finger + " haptics -- " + e.Message);
                }
            }

            try
            {
                Transform palm = tracking.ResolveRawPalmJoint();
                if (palm == null)
                {
                    Debug.LogWarning("[HexR] AutoSetup: couldn't resolve " + handType + " palm joint on the raw hand -- skipping.");
                }
                else
                {
                    AddOrConfigureFingerHaptics(palm.gameObject, controller, handType, HapticFingerTrigger.FingerType.Palm, null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[HexR] AutoSetup: failed to set up " + handType + " palm haptics -- " + e.Message);
            }
        }

        // Never touches a collider that's already there (respects manual tuning/re-runs).
        // HapticFingerTrigger is added if missing, otherwise just has its config refreshed --
        // so re-running Auto Setup after reassigning hands fixes stale wiring.
        private static void AddOrConfigureFingerHaptics(GameObject target, HexRManager controller, HaptGloveHandler.HandType handType, HapticFingerTrigger.FingerType finger, Vector3? centerOverride)
        {
            if (target.GetComponent<Collider>() == null)
            {
                if (finger == HapticFingerTrigger.FingerType.Palm)
                {
                    BoxCollider box = target.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = controller.PalmColliderSize;
                }
                else
                {
                    SphereCollider sphere = target.AddComponent<SphereCollider>();
                    sphere.isTrigger = true;
                    sphere.radius = controller.FingertipColliderRadius;
                    if (centerOverride.HasValue)
                    {
                        sphere.center = centerOverride.Value;
                    }
                    else
                    {
                        FingertipCenters centers = handType == HaptGloveHandler.HandType.Left
                            ? controller.LeftFingertipCenters
                            : controller.RightFingertipCenters;
                        sphere.center = centers.Get(finger);
                    }
                }
            }

            HapticFingerTrigger trigger = target.GetComponent<HapticFingerTrigger>();
            if (trigger == null)
            {
                trigger = target.AddComponent<HapticFingerTrigger>();
            }
            trigger.handType = handType == HaptGloveHandler.HandType.Left ? HapticFingerTrigger.HandType.Left : HapticFingerTrigger.HandType.Right;
            trigger.fingertype = finger;
            trigger.HexrLeftOrRight = handType == HaptGloveHandler.HandType.Left ? controller.leftHand.gameObject : controller.rightHand.gameObject;

            EditorUtility.SetDirty(target);
        }

        // A single check's outcome -- lets tooling (the HexR Tools window's Setup tab)
        // render the same checks ValidateSetup logs to the console as a structured
        // checklist, instead of re-implementing the checks a second time and risking the
        // two copies drifting apart.
        public struct SetupCheck
        {
            public bool Ok;
            public string Title;
            public string Detail;

            public SetupCheck(bool ok, string title, string detail)
            {
                Ok = ok;
                Title = title;
                Detail = detail;
            }
        }

        // Read-only sanity pass over everything AutoSetup is supposed to wire up, run
        // automatically at the end of AutoSetup and also callable on its own (Inspector
        // button / HexR > Validate Scene Setup) to re-check a scene without redoing the
        // GameObject.Find-based rewiring above -- e.g. after someone hand-edits a hand root,
        // or on a scene AutoSetup was never run on in the first place. AutoSetup's own
        // try/catch blocks intentionally swallow failures and move on (a missing hand menu
        // shouldn't abort finding the hand roots), so this is the one place that adds up
        // everything left unassigned and reports it together instead of one log line at a
        // time buried in the console.
        //
        // results is optional -- pass a list to also collect a structured SetupCheck per
        // item (used by the HexR Tools window); existing callers that only want the
        // console log / bool can keep calling this with just controller.
        public static bool ValidateSetup(HexRManager controller, List<SetupCheck> results = null)
        {
            bool ok = true;

            ok &= Check(results, controller.leftHand != null, "Left Hand Physics assigned", "Left Hand Physics (HaptGloveHandler) is not assigned.");
            ok &= Check(results, controller.rightHand != null, "Right Hand Physics assigned", "Right Hand Physics (HaptGloveHandler) is not assigned.");

            // Unlike the indicators/text below (all read via ?. or null-checked at their call
            // sites), HexRPanel.SetActive(...) is called unguarded from
            // HaptGlove_OnConnectedFailed/HaptGlove_OnDisconnected -- leaving this unassigned
            // isn't a blank UI element, it's a NullReferenceException the first time a
            // connection fails or drops.
            ok &= Check(results, controller.HexRPanel != null, "HexRPanel assigned",
                "HexRPanel is not assigned -- this will throw a NullReferenceException the first time a connection fails or drops (HaptGlove_OnConnectedFailed/OnDisconnected call HexRPanel.SetActive(true) unguarded).");

            ok &= Check(results, controller.LeftBtText != null, "Left Bluetooth status text assigned", "Left Bluetooth status text is not assigned -- connection status won't be visible.");
            ok &= Check(results, controller.RightBtText != null, "Right Bluetooth status text assigned", "Right Bluetooth status text is not assigned -- connection status won't be visible.");
            ok &= Check(results, controller.BluetoothIndicatorL != null, "Left Bluetooth indicator assigned", "Left Bluetooth indicator is not assigned.");
            ok &= Check(results, controller.BluetoothIndicatorR != null, "Right Bluetooth indicator assigned", "Right Bluetooth indicator is not assigned.");
            ok &= Check(results, controller.pumpIndicator_L != null, "Left pump indicator assigned", "Left pump indicator is not assigned.");
            ok &= Check(results, controller.pumpIndicator_R != null, "Right pump indicator assigned", "Right pump indicator is not assigned.");

            if (controller.XRFramework == Options.OpenXR)
            {
                ok &= Check(results, controller.HandMenu != null, "HexR Hand Menu assigned (OpenXR)", "HexR Hand Menu is not assigned (required for OpenXR).");
            }

            ok &= ValidateHand(controller.leftHand, HaptGloveHandler.HandType.Left, results);
            ok &= ValidateHand(controller.rightHand, HaptGloveHandler.HandType.Right, results);

            Debug.Log(ok
                ? "[HexR] Setup check passed -- all essential references are linked."
                : "[HexR] Setup check found issues -- see warnings/errors above.");
            return ok;
        }

        // Logs (matching prior behavior exactly) and, if results != null, records the
        // check for the Setup tab's checklist.
        private static bool Check(List<SetupCheck> results, bool ok, string title, string detail)
        {
            if (!ok)
            {
                Debug.LogWarning("[HexR] Setup check: " + detail);
            }
            results?.Add(new SetupCheck(ok, title, ok ? "OK" : detail));
            return ok;
        }

        private static bool ValidateHand(HaptGloveHandler hand, HaptGloveHandler.HandType expected, List<SetupCheck> results = null)
        {
            string label = expected == HaptGloveHandler.HandType.Left ? "Left" : "Right";
            if (hand == null) return true; // already reported by the caller

            PhysicsHandTracking tracking = hand.GetComponent<PhysicsHandTracking>();
            if (tracking == null)
            {
                return Check(results, false, label + " hand has PhysicsHandTracking", label + " hand has no PhysicsHandTracking component -- it won't move.");
            }

            bool ok = true;
            ok &= Check(results, tracking.handRoot != null, label + " hand's handRoot assigned",
                label + " hand's PhysicsHandTracking.handRoot is not assigned -- this hand won't track, and any collider-based haptics on it will never trigger.");

            if (tracking.handRoot != null)
            {
                // Naming mismatch is a warning, not a hard failure -- flagged separately from
                // the "is it assigned at all" check above so both surface independently
                // (deliberately not folded into `ok`, matching the original behavior).
                Check(results, NameSuggestsHand(tracking.handRoot.name, expected), label + " hand's handRoot name looks right",
                    label + " hand's PhysicsHandTracking.handRoot is '" + tracking.handRoot.name + "', which doesn't look like a " + label + "-hand object -- double-check this isn't wired to the other hand's transform.");
            }

            // HexrRoot is intentionally not required -- it's only used for the (now
            // optional) ghost-rig mirroring; unassigned just means no ghost rig, which
            // PhysicsHandTracking already handles gracefully.

            // HapticFingerTrigger/HexRGrabbable/SpecialHaptics all find this by
            // GameObject.Find("Left/Right Pressure Controller") at their own runtime Start()
            // -- not something AutoSetup wires or creates, so a missing/renamed one won't
            // show up as a broken reference anywhere else, just a NullReferenceException the
            // first time any fingertip HapticFingerTrigger fires. Flag it here instead.
            GameObject pressureControllerObj = GameObject.Find(label + " Pressure Controller");
            bool pressureControllerOk = pressureControllerObj != null && pressureControllerObj.GetComponent<PressureTrackerMain>() != null;
            ok &= Check(results, pressureControllerOk, label + " Pressure Controller present",
                "\"" + label + " Pressure Controller\" (with a PressureTrackerMain component) was not found in the scene -- every " + label.ToLowerInvariant() + "-hand HapticFingerTrigger will NullReferenceException the first time it tries to fire.");

            // Grasping.haptGloveHandler is a manually-wired reference, not derived from the
            // hand it's on -- the easiest way to end up with wrong-hand haptics is this being
            // dragged onto the other hand's HaptGloveHandler by mistake.
            Grasping grasping = hand.GetComponent<Grasping>();
            if (grasping != null)
            {
                string pointsAt = grasping.haptGloveHandler == null ? "nothing" : grasping.haptGloveHandler.gameObject.name;
                ok &= Check(results, grasping.haptGloveHandler == hand, label + " hand's Grasping points at its own HaptGloveHandler",
                    label + " hand's Grasping.haptGloveHandler points at " + pointsAt + " instead of its own HaptGloveHandler -- this will send that hand's physics-grasp haptics to the wrong glove.");
            }

            ok &= ValidateFingerHaptics(tracking, expected, label, results);

            return ok;
        }

        // Confirms every fingertip + palm joint on the raw tracked hand has both a trigger
        // Collider and a correctly configured HapticFingerTrigger -- what AutoAddFingerHaptics
        // is supposed to have wired up.
        private static bool ValidateFingerHaptics(PhysicsHandTracking tracking, HaptGloveHandler.HandType expected, string label, List<SetupCheck> results)
        {
            if (tracking.handRoot == null) return true; // already reported by the handRoot check above

            bool ok = true;
            HapticFingerTrigger.HandType expectedTriggerHand = expected == HaptGloveHandler.HandType.Left ? HapticFingerTrigger.HandType.Left : HapticFingerTrigger.HandType.Right;

            foreach (HapticFingerTrigger.FingerType finger in Fingers)
            {
                Transform joint = tracking.ResolveRawFingerJoint(finger);
                ok &= CheckFingerHapticJoint(joint, finger, expectedTriggerHand, label, results);
            }

            ok &= CheckFingerHapticJoint(tracking.ResolveRawPalmJoint(), HapticFingerTrigger.FingerType.Palm, expectedTriggerHand, label, results);

            return ok;
        }

        private static bool CheckFingerHapticJoint(Transform joint, HapticFingerTrigger.FingerType finger, HapticFingerTrigger.HandType expectedTriggerHand, string label, List<SetupCheck> results)
        {
            string title = label + " " + finger + " haptic trigger";
            if (joint == null)
            {
                return Check(results, false, title, label + " " + finger + " joint could not be resolved on the raw hand -- run Auto Set Up HexR, or check the hand rig's joint naming.");
            }

            Collider collider = joint.GetComponent<Collider>();
            if (!Check(results, collider != null, title + " collider", label + " " + finger + " (" + joint.name + ") has no trigger Collider."))
            {
                return false;
            }

            HapticFingerTrigger trigger = joint.GetComponent<HapticFingerTrigger>();
            if (!Check(results, trigger != null, title + " component", label + " " + finger + " (" + joint.name + ") has no HapticFingerTrigger."))
            {
                return false;
            }

            bool configOk = trigger.fingertype == finger && trigger.handType == expectedTriggerHand;
            return Check(results, configOk, title + " configured", label + " " + finger + " (" + joint.name + ")'s HapticFingerTrigger has the wrong fingertype/handType -- re-run Auto Set Up HexR.");
        }

        private static bool NameSuggestsHand(string name, HaptGloveHandler.HandType expected)
        {
            string n = name.ToLowerInvariant();
            bool looksLeft = n.StartsWith("l_") || n.Contains("_l") || n.Contains("left");
            bool looksRight = n.StartsWith("r_") || n.Contains("_r") || n.Contains("right");
            return expected == HaptGloveHandler.HandType.Left ? !looksRight : !looksLeft;
        }

        [CustomEditor(typeof(HexRManager))]
        public class HexRSettingEditorGUI : Editor
        {
            // Foldout open/closed state -- plain instance fields, not serialized. Resets to
            // these defaults on reselection/domain reload, which is fine for editor-only UI
            // state; Setup starts open since that's what you touch first on a fresh rig,
            // the tuning/panel sections start collapsed since they're occasional-use.
            private bool showSetup = true;
            private bool showColliderTuning = false;
            private bool showPanelUI = false;

            public override void OnInspectorGUI()
            {
                HexRManager controller = (HexRManager)target;

                showSetup = EditorGUILayout.BeginFoldoutHeaderGroup(showSetup, "XR Framework & Hand Physics");
                if (showSetup)
                {
                    EditorGUI.indentLevel++;
                    controller.XRFramework = (HexRManager.Options)EditorGUILayout.EnumPopup(
                        new GUIContent("XR Framework", "Which XR hand-tracking convention this rig uses. Controls how Auto Setup finds the raw hand root, and which joint-naming path it tries (OpenXR joint names, falling back to legacy Meta OVR bone names)."),
                        controller.XRFramework);

                    controller.isQuest = EditorGUILayout.Toggle(
                        new GUIContent("Quest Headset", "Whether this build runs standalone on a Quest headset -- toggles Quest-specific behavior in both hands' HaptGloveHandler."),
                        controller.isQuest);

                    GUILayout.Space(4);
                    controller.rightHand = (HaptGloveHandler)EditorGUILayout.ObjectField(
                        new GUIContent("Right Hand Physics", "The right hand's HaptGloveHandler (glove/haptics object). Auto Setup finds this via GameObject.Find(\"Right Hand Physics\") and wires everything else -- colliders, panel, validation -- relative to it."),
                        controller.rightHand, typeof(HaptGloveHandler), true);
                    controller.leftHand = (HaptGloveHandler)EditorGUILayout.ObjectField(
                        new GUIContent("Left Hand Physics", "The left hand's HaptGloveHandler (glove/haptics object). Auto Setup finds this via GameObject.Find(\"Left Hand Physics\") and wires everything else -- colliders, panel, validation -- relative to it."),
                        controller.leftHand, typeof(HaptGloveHandler), true);

                    if (controller.XRFramework == Options.OpenXR)
                    {
                        controller.HandMenu = (GameObject)EditorGUILayout.ObjectField(
                            new GUIContent("HexR Hand Menu", "The hand-attached menu prefab used in OpenXR mode. Auto Setup re-parents an instance of it under the camera rig and reads its Bluetooth buttons/indicators from it -- only relevant when XR Framework is OpenXR."),
                            controller.HandMenu, typeof(GameObject), true);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                GUILayout.Space(6);

                showColliderTuning = EditorGUILayout.BeginFoldoutHeaderGroup(showColliderTuning, "Fingertip & Palm Collider Tuning");
                if (showColliderTuning)
                {
                    EditorGUI.indentLevel++;
                    controller.FingertipColliderRadius = EditorGUILayout.FloatField(
                        new GUIContent("Fingertip Collider Radius", "Radius of the sphere trigger collider Auto Setup adds to each fingertip on the raw tracked hand."),
                        controller.FingertipColliderRadius);

                    GUILayout.Space(4);
                    EditorGUILayout.LabelField(
                        new GUIContent("Fingertip Centers (Left)", "Local-space center offset of each left-hand fingertip's sphere collider, relative to that finger's own joint origin. Tune per-finger -- fingers aren't interchangeable and the rig isn't necessarily symmetric."),
                        EditorStyles.boldLabel);
                    DrawFingertipCenters(controller.LeftFingertipCenters);

                    GUILayout.Space(5);
                    EditorGUILayout.LabelField(
                        new GUIContent("Fingertip Centers (Right)", "Same as Left, for the right hand. Tune independently -- the raw hand rig isn't guaranteed to be mirror-symmetric in local space, so left-hand values don't reliably carry over."),
                        EditorStyles.boldLabel);
                    DrawFingertipCenters(controller.RightFingertipCenters);

                    GUILayout.Space(5);
                    controller.PalmColliderSize = EditorGUILayout.Vector3Field(
                        new GUIContent("Palm Collider Size", "Local-space box size of the trigger collider Auto Setup adds to the palm on the raw tracked hand."),
                        controller.PalmColliderSize);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                GUILayout.Space(6);

                showPanelUI = EditorGUILayout.BeginFoldoutHeaderGroup(showPanelUI, "HexR Panel UI Components");
                if (showPanelUI)
                {
                    EditorGUI.indentLevel++;
                    controller.BluetoothIndicatorL = (GameObject)EditorGUILayout.ObjectField(
                        new GUIContent("Bluetooth Indicator L", "Visual indicator shown once the left glove is connected over Bluetooth."),
                        controller.BluetoothIndicatorL, typeof(GameObject), true);
                    controller.BluetoothIndicatorR = (GameObject)EditorGUILayout.ObjectField(
                        new GUIContent("Bluetooth Indicator R", "Visual indicator shown once the right glove is connected over Bluetooth."),
                        controller.BluetoothIndicatorR, typeof(GameObject), true);
                    controller.pumpIndicator_L = (GameObject)EditorGUILayout.ObjectField(
                        new GUIContent("Pump Indicator L", "Visual indicator shown while the left glove's air pump is actively running."),
                        controller.pumpIndicator_L, typeof(GameObject), true);
                    controller.pumpIndicator_R = (GameObject)EditorGUILayout.ObjectField(
                        new GUIContent("Pump Indicator R", "Visual indicator shown while the right glove's air pump is actively running."),
                        controller.pumpIndicator_R, typeof(GameObject), true);
                    controller.LeftBtText = (TextMeshProUGUI)EditorGUILayout.ObjectField(
                        new GUIContent("Left Bluetooth Text", "Status text on the HexR panel reflecting the left glove's connection state (e.g. \"Searching...\", \"Left Glove Connected\")."),
                        controller.LeftBtText, typeof(TextMeshProUGUI), true);
                    controller.RightBtText = (TextMeshProUGUI)EditorGUILayout.ObjectField(
                        new GUIContent("Right Bluetooth Text", "Status text on the HexR panel reflecting the right glove's connection state (e.g. \"Searching...\", \"Right Glove Connected\")."),
                        controller.RightBtText, typeof(TextMeshProUGUI), true);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                GUILayout.Space(15);

                if (GUILayout.Button(new GUIContent("Auto Set Up HexR", "Finds and wires everything above automatically -- hand roots, panel UI, fingertip/palm colliders -- then runs Validate.")))
                {
                    AutoSetup(controller);
                }
                if (GUILayout.Button(new GUIContent("Validate HexR Setup", "Read-only check that everything above is wired correctly. Reports what's missing/misconfigured without changing anything -- safe to run any time.")))
                {
                    ValidateSetup(controller);
                }

                if (GUI.changed)
                {
                    EditorUtility.SetDirty(target);
                }
            }

            // Thumb is shown too, but only used as a fallback -- if the rig has a
            // "..._thumb_null" locator, Auto Setup uses that instead (see
            // ResolveRawThumbCenterMarker / AutoAddFingerHapticsForHand).
            private static void DrawFingertipCenters(FingertipCenters centers)
            {
                centers.Thumb = EditorGUILayout.Vector3Field(
                    new GUIContent("Thumb", "Fallback center used only if the rig has no \"..._thumb_null\" locator -- when one exists, Auto Setup uses its position instead of this value."),
                    centers.Thumb);
                centers.Index = EditorGUILayout.Vector3Field(new GUIContent("Index", "Center offset for the index fingertip's sphere collider."), centers.Index);
                centers.Middle = EditorGUILayout.Vector3Field(new GUIContent("Middle", "Center offset for the middle fingertip's sphere collider."), centers.Middle);
                centers.Ring = EditorGUILayout.Vector3Field(new GUIContent("Ring", "Center offset for the ring fingertip's sphere collider."), centers.Ring);
                centers.Little = EditorGUILayout.Vector3Field(new GUIContent("Little", "Center offset for the little (pinky) fingertip's sphere collider."), centers.Little);
            }
        }

#endif
    }
}
