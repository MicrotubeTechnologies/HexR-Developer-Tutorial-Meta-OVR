using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using HaptGlove;

namespace HexR
{
    // "Make developing with HexR easy" toolbox: a Haptics Tester for triggering any
    // channel combo without touching gameplay objects, and a Setup tab that renders
    // HexRManager.ValidateSetup's checks as a checklist instead of console spam.
    //
    // Deliberately Play-Mode-only for the Tester -- BLE connection currently only runs
    // inside HaptGloveHandler's MonoBehaviour lifecycle (Start/Update/coroutines), and
    // this project's connection code has been reverted/destabilized before by exactly
    // this kind of "make it also work outside the normal lifecycle" change (see
    // ROADMAP.md). Not worth risking as a side effect of a dev tool.
    public class HexRToolsWindow : EditorWindow
    {
        [MenuItem("HexR/HexR Tools", false, -10)]
        public static void Open()
        {
            GetWindow<HexRToolsWindow>("HexR Tools");
        }

        private enum Tab { Tester, Setup, Project }
        private enum TestHand { Left, Right }
        private enum TestMode { Pressure, Vibration }

        private static readonly Haptics.Finger[] AllFingers =
        {
            Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle,
            Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm
        };

        [SerializeField] private Tab tab = Tab.Tester;
        [SerializeField] private TestHand targetHand = TestHand.Right;
        [SerializeField] private TestMode mode = TestMode.Pressure;
        [SerializeField] private bool[] selected = new bool[6] { false, true, false, false, false, false };
        [SerializeField] private float intensity = 0.6f;
        [SerializeField] private float speed = 0.5f;
        [SerializeField] private float frequency = 12f;
        [SerializeField] private bool bypassHandCheck = true;
        [SerializeField] private bool simulatorMode = false;
        [SerializeField] private bool armed = false;

        private List<HexRManager.SetupCheck> setupResults;
        private bool setupOk;

        // HaptGloveHandler keeps its connection flag private, and the SDK exposes no
        // public "am I connected" member -- only the onBluetoothConnected/Disconnected
        // UnityActions, which this window can't subscribe to without changing the
        // runtime connection wiring HexRManager owns (deliberately off-limits, see the
        // class comment). Read the field directly instead. BindingFlags covers both the
        // private field in the shipped DLL and a public one in any newer SDK build.
        private static readonly FieldInfo BleConnectedField = typeof(HaptGloveHandler)
            .GetField("bleConnected", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static bool IsConnected(HaptGloveHandler hand)
        {
            if (hand == null || BleConnectedField == null)
            {
                return false;
            }

            return BleConnectedField.GetValue(hand) is bool connected && connected;
        }

        // Palette lifted from the reference mockup (hexr-editor-mockup.html's --orange/etc
        // CSS variables) so this reads as the same tool, not a generic Inspector panel.
        private static readonly Color Orange = new Color32(0xff, 0x6a, 0x2b, 0xff);
        private static readonly Color OrangeDark = new Color32(0xd6, 0x4f, 0x1a, 0xff);
        private static readonly Color OrangeLight = new Color32(0xff, 0x9a, 0x5c, 0xff);
        private static readonly Color Green = new Color32(0x5b, 0xce, 0x6b, 0xff);
        private static readonly Color Red = new Color32(0xd9, 0x3a, 0x24, 0xff);
        private static readonly Color TrackDark = new Color32(0x23, 0x23, 0x23, 0xff);
        private static readonly Color PayloadBg = new Color32(0x10, 0x10, 0x10, 0xff);
        private static readonly Color PayloadGreen = new Color32(0x7f, 0xe0, 0xa0, 0xff);
        private static readonly Color DimText = new Color32(0x8f, 0x8f, 0x8f, 0xff);

        private GUIStyle sectionHeaderStyle;
        private GUIStyle payloadStyle;
        private GUIStyle tabStyle;

        private void EnsureStyles()
        {
            if (sectionHeaderStyle != null)
            {
                return;
            }

            sectionHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel);
            sectionHeaderStyle.normal.textColor = DimText;

            payloadStyle = new GUIStyle(EditorStyles.label);
            payloadStyle.wordWrap = true;
            payloadStyle.richText = true;
            payloadStyle.padding = new RectOffset(8, 8, 6, 6);
            payloadStyle.normal.textColor = PayloadGreen;

            tabStyle = new GUIStyle(EditorStyles.toolbarButton);
            tabStyle.fixedHeight = 22;
            tabStyle.fontStyle = FontStyle.Bold;
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
            RunValidation();
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            EnsureStyles();

            using (new EditorGUILayout.HorizontalScope())
            {
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = tab == Tab.Tester ? Orange : Color.white;
                if (GUILayout.Toggle(tab == Tab.Tester, "● HexR Haptics Tester", tabStyle))
                {
                    tab = Tab.Tester;
                }
                GUI.backgroundColor = tab == Tab.Setup ? OrangeLight : Color.white;
                if (GUILayout.Toggle(tab == Tab.Setup, "● HexR Setup", tabStyle))
                {
                    tab = Tab.Setup;
                }
                GUI.backgroundColor = tab == Tab.Project ? OrangeLight : Color.white;
                if (GUILayout.Toggle(tab == Tab.Project, "● Project Setup", tabStyle))
                {
                    tab = Tab.Project;
                }
                GUI.backgroundColor = prevBg;
            }
            EditorGUILayout.Space(8);

            if (tab == Tab.Tester)
            {
                DrawTesterTab();
            }
            else if (tab == Tab.Setup)
            {
                DrawSetupTab();
            }
            else
            {
                DrawProjectTab();
            }
        }

        // Small-caps-ish dim header matching the mockup's .sec-title treatment.
        private void SectionHeader(string text)
        {
            EditorGUILayout.LabelField(text.ToUpperInvariant(), sectionHeaderStyle);
        }

        // Full-color-control progress bar (EditorGUI.ProgressBar can't be recolored) --
        // used for battery and the live channel monitor so they pick up the orange/green
        // accent instead of Unity's default gray/blue.
        private static void DrawColoredBar(Rect rect, float value01, Color fillColor, string label)
        {
            EditorGUI.DrawRect(rect, TrackDark);
            float w = rect.width * Mathf.Clamp01(value01);
            if (w > 0.5f)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, w, rect.height), fillColor);
            }
            GUI.Label(rect, "  " + label, EditorStyles.whiteMiniLabel);
        }

        // ==================== Haptics Tester ====================

        private void DrawTesterTab()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test haptics live -- BLE connection only runs inside the normal Play-mode lifecycle.", MessageType.Info);
                return;
            }

            HexRManager mgr = HexRManager.Instance;
            if (mgr == null)
            {
                EditorGUILayout.HelpBox("No HexRManager.Instance found. Make sure a HexR Main rig is in the scene and has started.", MessageType.Warning);
                return;
            }

            simulatorMode = EditorGUILayout.ToggleLeft("Use simulator (no hardware) -- preview the call, skip sending it", simulatorMode);
            EditorGUILayout.Space(4);

            DrawConnectionSection(mgr);
            EditorGUILayout.Space(8);
            DrawTargetAndChannels();
            EditorGUILayout.Space(8);
            DrawParameters();
            EditorGUILayout.Space(8);
            DrawTriggerButtons();
            EditorGUILayout.Space(6);
            DrawPayloadPreview();
            EditorGUILayout.Space(10);
            DrawLiveMonitor(mgr);
        }

        private void DrawConnectionSection(HexRManager mgr)
        {
            SectionHeader("Connection");
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawHandConnectionCard("Left glove", mgr.leftHand, mgr.ConnectLeftBT);
                DrawHandConnectionCard("Right glove", mgr.rightHand, mgr.ConnectRightBT);
            }
        }

        private void DrawHandConnectionCard(string label, HaptGloveHandler hand, System.Action connectToggle)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (hand == null)
                {
                    EditorGUILayout.LabelField(label + " -- not assigned");
                    return;
                }

                bool connected = IsConnected(hand);
                using (new EditorGUILayout.HorizontalScope())
                {
                    Color prev = GUI.color;
                    GUI.color = connected ? Green : Color.gray;
                    GUILayout.Label("●", GUILayout.Width(14));
                    GUI.color = prev;
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    Color prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = connected ? Color.white : OrangeDark;
                    if (GUILayout.Button(connected ? "Reconnect" : "Connect", GUILayout.Width(80)))
                    {
                        // Android's BLE plugin can't run inside the Editor process at all --
                        // there's no Android runtime to call into, no matter what the actual
                        // build target is set to. Force this hand onto the Windows BLE path
                        // (already-present, unmodified branch in HaptGloveHandler) so testing
                        // from the Editor can reach real hardware. This is a plain field flip
                        // on a live Play-mode instance -- it reverts automatically the moment
                        // Play mode stops, so it can never leak into the real Android build.
                        // No changes to any connection-lifecycle code.
                        if (hand.BuildPlatform != HaptGloveHandler.TargetPlatform.Window)
                        {
                            hand.BuildPlatform = HaptGloveHandler.TargetPlatform.Window;
                        }
                        connectToggle();
                    }
                    GUI.backgroundColor = prevBg;
                }
                if (hand.BuildPlatform == HaptGloveHandler.TargetPlatform.Window)
                {
                    EditorGUILayout.LabelField("Forced to Windows BLE for Editor testing (Play-mode only, reverts on Stop)", EditorStyles.miniLabel);
                }
                EditorGUILayout.LabelField(string.IsNullOrEmpty(hand.btText) ? (connected ? "Connected" : "Not connected") : hand.btText, EditorStyles.miniLabel);

                float battery = hand.GetBatteryLevel();
                string battText = battery > 0 ? Mathf.RoundToInt(battery * 100) + "%" : "--";
                DrawColoredBar(EditorGUILayout.GetControlRect(false, 14), battery, battery > 0 ? Green : Color.gray, battText);
            }
        }

        private void DrawTargetAndChannels()
        {
            SectionHeader("Target hand & channels");
            targetHand = (TestHand)GUILayout.Toolbar((int)targetHand, new[] { "Left", "Right" }, GUILayout.Width(140));

            EditorGUILayout.Space(2);
            Color prevBg = GUI.backgroundColor;
            for (int row = 0; row < 2; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int col = 0; col < 3; col++)
                    {
                        int i = row * 3 + col;
                        bool wasOn = selected[i];
                        GUI.backgroundColor = wasOn ? Orange : prevBg;
                        bool isOn = GUILayout.Toggle(wasOn, AllFingers[i].ToString(), "Button", GUILayout.Height(28));
                        if (isOn != wasOn)
                        {
                            selected[i] = isOn;
                        }
                    }
                }
            }
            GUI.backgroundColor = prevBg;
        }

        private void DrawParameters()
        {
            SectionHeader("Haptic parameters");
            mode = (TestMode)GUILayout.Toolbar((int)mode, new[] { "Pressure", "Vibration" }, GUILayout.Width(160));

            intensity = EditorGUILayout.Slider("Intensity (0.1-1.0)", intensity, 0.1f, 1f);

            if (mode == TestMode.Pressure)
            {
                speed = EditorGUILayout.Slider("Speed (0.1-1.0)", speed, 0.1f, 1f);
            }
            else
            {
                frequency = EditorGUILayout.Slider("Frequency (0.1-40 Hz)", frequency, 0.1f, 40f);
            }

            bypassHandCheck = EditorGUILayout.ToggleLeft("Bypass hand-near check", bypassHandCheck);
        }

        private void DrawTriggerButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = armed ? Orange : OrangeDark;
                GUIStyle big = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 12 };
                if (GUILayout.Button(armed ? "■ Release OUT  (state = false)" : "● Trigger IN  (state = true)", big, GUILayout.Height(32)))
                {
                    armed = !armed;
                    SendTrigger(armed);
                }
                GUI.backgroundColor = prev;

                if (GUILayout.Button("All OFF", GUILayout.Width(80), GUILayout.Height(32)))
                {
                    armed = false;
                    SendAllOff();
                }
            }
        }

        private void DrawPayloadPreview()
        {
            SectionHeader("Call preview");
            string text = BuildPayloadPreview();
            float height = payloadStyle.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth - 20) + 8;
            Rect r = EditorGUILayout.GetControlRect(false, Mathf.Max(height, 20));
            EditorGUI.DrawRect(r, PayloadBg);
            GUI.Label(r, text, payloadStyle);
        }

        private string BuildPayloadPreview()
        {
            List<Haptics.Finger> fingers = SelectedFingers();
            if (fingers.Count == 0)
            {
                return "// no channel selected";
            }

            string handVar = targetHand == TestHand.Left ? "left" : "right";
            string fn = mode == TestMode.Pressure ? "CustomSingleHaptics" : "CustomSingleVibrations";
            StringBuilder sb = new StringBuilder();
            foreach (Haptics.Finger f in fingers)
            {
                sb.Append(handVar).Append('.').Append(fn).Append('(').Append(f)
                  .Append(", state=").Append(armed)
                  .Append(", intensity=").Append(intensity.ToString("0.00"));
                sb.Append(mode == TestMode.Pressure
                    ? ", speed=" + speed.ToString("0.00")
                    : ", freq=" + frequency.ToString("0.0"));
                sb.Append(", bypass=").Append(bypassHandCheck).Append(")\n");
            }
            if (simulatorMode)
            {
                sb.Append("// simulator on -- not actually sent");
            }
            return sb.ToString().TrimEnd('\n');
        }

        // The glove reports absolute air pressure in pascals; haptics operate on the
        // gauge pressure above ambient, which is what's worth showing.
        private const float AmbientPa = 100000f;
        private const float MaxGaugeKPa = 60f;

        // The SDK has no per-channel "what am I currently driving" query, so this shows
        // the closest real thing it does report: measured per-finger air pressure. Same
        // array PressureTrackerMain.Update reads, same index order as AllFingers
        // (Thumb..Palm), with the tank reading appended after the six channels.
        private void DrawLiveMonitor(HexRManager mgr)
        {
            SectionHeader("Live channel monitor (air pressure measured by the glove)");
            HaptGloveHandler hand = targetHand == TestHand.Left ? mgr.leftHand : mgr.rightHand;
            if (hand == null)
            {
                EditorGUILayout.HelpBox("Target hand not assigned.", MessageType.None);
                return;
            }

            int[] pressure = hand.GetAirPressure();
            if (pressure == null || pressure.Length < AllFingers.Length)
            {
                EditorGUILayout.HelpBox(
                    IsConnected(hand)
                        ? "Connected, but no pressure data has arrived from the glove yet."
                        : "Not connected -- no live pressure data.",
                    MessageType.None);
                return;
            }

            for (int i = 0; i < AllFingers.Length; i++)
            {
                DrawPressureBar(AllFingers[i].ToString(), pressure[i], Orange);
            }
            if (pressure.Length > AllFingers.Length)
            {
                DrawPressureBar("Tank", pressure[AllFingers.Length], OrangeLight);
            }
        }

        private static void DrawPressureBar(string label, int rawPa, Color fill)
        {
            float kpa = (rawPa - AmbientPa) / 1000f;
            float shown = Mathf.Clamp01(kpa / MaxGaugeKPa);
            DrawColoredBar(
                EditorGUILayout.GetControlRect(false, 16),
                shown,
                shown > 0f ? fill : Color.gray,
                label + "   " + kpa.ToString("0.0") + " kPa");
        }

        private List<Haptics.Finger> SelectedFingers()
        {
            List<Haptics.Finger> result = new List<Haptics.Finger>();
            for (int i = 0; i < AllFingers.Length; i++)
            {
                if (selected[i])
                {
                    result.Add(AllFingers[i]);
                }
            }
            return result;
        }

        private PressureTrackerMain GetTracker()
        {
            string name = targetHand == TestHand.Left ? "Left Pressure Controller" : "Right Pressure Controller";
            GameObject go = GameObject.Find(name);
            return go != null ? go.GetComponent<PressureTrackerMain>() : null;
        }

        private void SendTrigger(bool state)
        {
            if (simulatorMode)
            {
                return;
            }

            PressureTrackerMain tracker = GetTracker();
            if (tracker == null)
            {
                Debug.LogWarning("[HexR Tools] Couldn't find \"" + (targetHand == TestHand.Left ? "Left" : "Right") + " Pressure Controller\" in the scene.");
                return;
            }

            foreach (Haptics.Finger f in SelectedFingers())
            {
                if (mode == TestMode.Pressure)
                {
                    tracker.CustomSingleHaptics(f, state, intensity, speed, bypassHandCheck);
                }
                else
                {
                    tracker.CustomSingleVibrations(f, state, intensity, frequency, bypassHandCheck);
                }
            }
        }

        private void SendAllOff()
        {
            if (simulatorMode)
            {
                return;
            }

            PressureTrackerMain tracker = GetTracker();
            if (tracker == null)
            {
                return;
            }

            tracker.RemoveAllHaptics();
            tracker.RemoveAllVibrations();
        }

        // ==================== Setup ====================

        private void RunValidation()
        {
            HexRManager controller = FindObjectOfType<HexRManager>();
            if (controller == null)
            {
                setupResults = null;
                setupOk = false;
                return;
            }

            setupResults = new List<HexRManager.SetupCheck>();
            setupOk = HexRManager.ValidateSetup(controller, setupResults);
        }

        private void DrawSetupTab()
        {
            HexRManager controller = FindObjectOfType<HexRManager>();
            if (controller == null)
            {
                EditorGUILayout.HelpBox("No HexRManager found in the open scene -- use HexR > Create HexR Rig first, or add the HexR Main prefab manually.", MessageType.Warning);
                return;
            }

            if (setupResults == null)
            {
                RunValidation();
            }

            EditorGUILayout.HelpBox(
                setupOk
                    ? "All checks pass -- this scene is ready to build to the headset."
                    : setupResults.Count(c => !c.Ok) + " check(s) need attention before this scene will run on the headset.",
                setupOk ? MessageType.Info : MessageType.Warning);

            foreach (HexRManager.SetupCheck check in setupResults)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Color prev = GUI.color;
                    GUI.color = check.Ok ? Green : Red;
                    GUILayout.Label(check.Ok ? "✓" : "!", EditorStyles.boldLabel, GUILayout.Width(16));
                    GUI.color = prev;
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(check.Title);
                        if (!check.Ok)
                        {
                            EditorGUILayout.LabelField(check.Detail, EditorStyles.wordWrappedMiniLabel);
                        }
                    }
                }
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = setupOk ? prevBg : OrangeDark;
                if (GUILayout.Button("Fix all (re-run Auto Setup)"))
                {
                    HexRManager.AutoSetup(controller);
                    RunValidation();
                }
                GUI.backgroundColor = prevBg;
                if (GUILayout.Button("Re-scan scene"))
                {
                    RunValidation();
                }
            }
            EditorGUILayout.HelpBox("\"Fix all\" re-runs the same logic as the Inspector's \"Auto Set Up HexR\" button. There's no per-check fix yet -- AutoSetup isn't split into individually-invokable pieces.", MessageType.None);

            DrawHandOrientationSection();
        }

        // ==================== Hand orientation ====================

        // Rotating the hand GameObject can't fix a mis-oriented hand: MetaOVRFixedUpdate drives
        // the rigidbody toward targeRotation every physics step and MetaOVRUpdate rewrites every
        // finger's localRotation every frame, so PhysicsHandTracking's own offsets are the only
        // hand rotation left under the user's control. Solving for them beats scrubbing Euler
        // values, especially after the OpenXR skeleton switch, where the tracked wrist no longer
        // uses the axis convention the legacy b_l_/b_r_ bones did.
        private const string OffsetPrefKeyPrefix = "HexR.HandRotOffset.";

        [System.Serializable]
        private class OffsetPair
        {
            public Vector3 palm;
            public Vector3 finger;
        }

        private void DrawHandOrientationSection()
        {
            EditorGUILayout.Space(10);
            SectionHeader("Hand orientation");

            PhysicsHandTracking left = FindTracking(PhysicsHandTracking.HandType.Left);
            PhysicsHandTracking right = FindTracking(PhysicsHandTracking.HandType.Right);
            if (left == null && right == null)
            {
                EditorGUILayout.HelpBox("No PhysicsHandTracking components in this scene.", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(
                "Rotating the hand GameObject has no effect -- the update loop overwrites it every frame. "
                + "These two offsets are the only hand rotation you control.", MessageType.None);

            DrawHandOrientationRow("Left", left);
            DrawHandOrientationRow("Right", right);

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                {
                    Color prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = EditorApplication.isPlaying ? Orange : prevBg;
                    if (GUILayout.Button("Solve from tracked hands", GUILayout.Height(24)))
                    {
                        SolveAndApply(left);
                        SolveAndApply(right);
                    }
                    GUI.backgroundColor = prevBg;
                }
                if (GUILayout.Button("Snap to 90°", GUILayout.Width(90), GUILayout.Height(24)))
                {
                    SnapOffset(left);
                    SnapOffset(right);
                }
            }

            EditorGUILayout.LabelField(
                EditorApplication.isPlaying
                    ? "Solve compares the tracked hand's base finger joints against the ghost hand's -- positions only, so it can't be fooled by the two skeletons' different rotation conventions. Click it twice if the first pass lands slightly off: the rigidbody drive lags a frame."
                    : "Solve needs Play Mode with hands actually tracked -- it reads live joint positions. Out of Play Mode you can still type offsets in by hand.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Remember offsets"))
                {
                    RememberOffset(left);
                    RememberOffset(right);
                }
                if (GUILayout.Button("Restore remembered"))
                {
                    RestoreOffset(left);
                    RestoreOffset(right);
                }
            }
            EditorGUILayout.LabelField(
                "Play Mode throws away component edits on Stop. \"Remember\" stashes both offsets and re-applies them automatically the moment you're back in Edit Mode -- then save the scene.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawHandOrientationRow(string label, PhysicsHandTracking tracking)
        {
            if (tracking == null)
            {
                EditorGUILayout.LabelField(label + " -- no PhysicsHandTracking found");
                return;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(label + " hand", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                Vector3 palm = EditorGUILayout.Vector3Field("Palm offset", tracking.rotOffsetPalm);
                Vector3 finger = EditorGUILayout.Vector3Field("Finger offset", tracking.rotOffsetFinger);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tracking, "Change hand rotation offset");
                    tracking.rotOffsetPalm = palm;
                    tracking.rotOffsetFinger = finger;
                    EditorUtility.SetDirty(tracking);
                }
            }
        }

        private static PhysicsHandTracking FindTracking(PhysicsHandTracking.HandType handType)
        {
            PhysicsHandTracking[] all = FindObjectsByType<PhysicsHandTracking>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (PhysicsHandTracking t in all)
            {
                if (t.handType == handType)
                {
                    return t;
                }
            }
            return null;
        }

        private static void SolveAndApply(PhysicsHandTracking tracking)
        {
            if (tracking == null)
            {
                return;
            }

            Vector3 solved;
            string error;
            if (!tracking.TrySolvePalmOffset(out solved, out error))
            {
                Debug.LogWarning("[HexR Tools] Couldn't solve " + tracking.handType + " hand orientation -- " + error);
                return;
            }

            Undo.RecordObject(tracking, "Solve hand rotation offset");
            tracking.rotOffsetPalm = solved;
            EditorUtility.SetDirty(tracking);
            Debug.Log("[HexR Tools] " + tracking.handType + " hand palm offset solved: " + solved.ToString("0.0"));
        }

        private static void SnapOffset(PhysicsHandTracking tracking)
        {
            if (tracking == null)
            {
                return;
            }

            Undo.RecordObject(tracking, "Snap hand rotation offset");
            tracking.rotOffsetPalm = PhysicsHandTracking.SnapTo90(tracking.rotOffsetPalm);
            EditorUtility.SetDirty(tracking);
        }

        private static string OffsetKey(PhysicsHandTracking tracking)
        {
            return OffsetPrefKeyPrefix + tracking.gameObject.scene.name + "." + tracking.handType;
        }

        private static void RememberOffset(PhysicsHandTracking tracking)
        {
            if (tracking == null)
            {
                return;
            }

            OffsetPair pair = new OffsetPair { palm = tracking.rotOffsetPalm, finger = tracking.rotOffsetFinger };
            EditorPrefs.SetString(OffsetKey(tracking), JsonUtility.ToJson(pair));
        }

        private static void RestoreOffset(PhysicsHandTracking tracking)
        {
            if (tracking == null)
            {
                return;
            }

            string json = EditorPrefs.GetString(OffsetKey(tracking), string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            OffsetPair pair = JsonUtility.FromJson<OffsetPair>(json);
            if (pair == null || (tracking.rotOffsetPalm == pair.palm && tracking.rotOffsetFinger == pair.finger))
            {
                return;
            }

            Undo.RecordObject(tracking, "Restore hand rotation offset");
            tracking.rotOffsetPalm = pair.palm;
            tracking.rotOffsetFinger = pair.finger;
            EditorUtility.SetDirty(tracking);
            Debug.Log("[HexR Tools] Restored remembered " + tracking.handType + " hand offsets after Play Mode: palm "
                + pair.palm.ToString("0.0") + ", finger " + pair.finger.ToString("0.0") + " -- save the scene to keep them.");
        }

        // ==================== Project Setup ====================

        // Now that package.json no longer hard-depends on the Meta SDK, nothing pulls in either
        // backend's packages -- the package installs into a bare project, compiles, and leaves
        // you with no XR at all. This tab is what closes that gap: pick a backend, see what's
        // missing, install it.
        private sealed class BackendSpec
        {
            public readonly string Label;
            public readonly string ProbeAssembly;
            public readonly string[] Packages;

            public BackendSpec(string label, string probeAssembly, string[] packages)
            {
                Label = label;
                ProbeAssembly = probeAssembly;
                Packages = packages;
            }
        }

        private static readonly BackendSpec OpenXRBackend = new BackendSpec(
            "OpenXR",
            "Unity.XR.Hands",
            new[]
            {
                "com.unity.xr.openxr",
                "com.unity.xr.hands",
                "com.unity.xr.interaction.toolkit",
                "com.unity.xr.management",
                "com.unity.textmeshpro"
            });

        private static readonly BackendSpec MetaOVRBackend = new BackendSpec(
            "Meta OVR",
            "Oculus.Interaction",
            new[]
            {
                "com.meta.xr.sdk.interaction",
                "com.unity.xr.management",
                "com.unity.textmeshpro"
            });

        private const string OpenXRDocsUrl = "https://github.com/MicrotubeTechnologies/HexR-developer-tutorial-XR";
        private const string MetaOVRDocsUrl = "https://github.com/MicrotubeTechnologies/HexR-Developer-Tutorial-Meta-OVR";
        private const string MicrotubeUrl = "https://microtube.tech/hexr-glove/";

        [SerializeField] private BackendChoice backendChoice = BackendChoice.OpenXR;
        private enum BackendChoice { OpenXR, MetaOVR }

        private static HashSet<string> installedPackages;
        private static ListRequest listRequest;

        // Sequential, unlike the original SetUpManger.InstallRequiredPackages, which called
        // Client.Add for every package inside one loop while reassigning a single `addRequest`
        // field and subscribing its progress callback once per iteration. Every subscription
        // then read whichever request happened to land in the field last, and the first one to
        // complete unsubscribed for all of them -- so it reported on the wrong package and
        // stopped watching the rest. UPM also can't service concurrent Adds. One at a time.
        private static readonly Queue<string> installQueue = new Queue<string>();
        private static AddRequest activeAdd;
        private static string activeAddName;

        private void DrawProjectTab()
        {
            SectionHeader("XR backend");
            EditorGUILayout.HelpBox(
                "HexR runs on either backend. This package no longer depends on the Meta SDK, so pick the one this "
                + "project targets and install what's missing -- then create a rig with HexR > Create HexR Rig.",
                MessageType.None);

            backendChoice = (BackendChoice)GUILayout.Toolbar((int)backendChoice, new[] { "OpenXR", "Meta OVR" }, GUILayout.Width(200));
            EditorGUILayout.Space(4);

            DrawDetectedRow(OpenXRBackend);
            DrawDetectedRow(MetaOVRBackend);

            EditorGUILayout.Space(10);
            BackendSpec spec = backendChoice == BackendChoice.OpenXR ? OpenXRBackend : MetaOVRBackend;
            DrawPackageList(spec);

            EditorGUILayout.Space(12);
            DrawQuickLinks();
        }

        // Assembly probe rather than a manifest read: it answers the question that actually
        // matters ("can HexR compile against this backend right now"), and it stays correct
        // whether the SDK arrived via UPM, a .unitypackage, or a loose folder under Assets --
        // Meta's SDK ships all three ways.
        private static bool IsBackendPresent(BackendSpec spec)
        {
            foreach (Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == spec.ProbeAssembly)
                {
                    return true;
                }
            }
            return false;
        }

        private void DrawDetectedRow(BackendSpec spec)
        {
            bool present = IsBackendPresent(spec);
            using (new EditorGUILayout.HorizontalScope())
            {
                Color prev = GUI.color;
                GUI.color = present ? Green : Color.gray;
                GUILayout.Label("●", GUILayout.Width(14));
                GUI.color = prev;
                EditorGUILayout.LabelField(spec.Label, GUILayout.Width(80));
                EditorGUILayout.LabelField(
                    present ? "detected (" + spec.ProbeAssembly + ")" : "not installed",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawPackageList(BackendSpec spec)
        {
            SectionHeader("Required packages for " + spec.Label);

            if (installedPackages == null)
            {
                if (listRequest == null)
                {
                    RefreshInstalledPackages();
                }
                EditorGUILayout.LabelField("Reading the project's package list...", EditorStyles.miniLabel);
                return;
            }

            List<string> missing = new List<string>();
            foreach (string package in spec.Packages)
            {
                bool installed = installedPackages.Contains(package);
                if (!installed)
                {
                    missing.Add(package);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    Color prev = GUI.color;
                    GUI.color = installed ? Green : Red;
                    GUILayout.Label(installed ? "✓" : "!", EditorStyles.boldLabel, GUILayout.Width(16));
                    GUI.color = prev;
                    EditorGUILayout.LabelField(package);
                }
            }

            EditorGUILayout.Space(6);

            if (activeAdd != null || installQueue.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Installing " + (activeAddName ?? "...") + "  (" + installQueue.Count + " left in queue)",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(missing.Count == 0))
                {
                    Color prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = missing.Count == 0 ? prevBg : OrangeDark;
                    if (GUILayout.Button(
                        missing.Count == 0 ? "Nothing missing" : "Install " + missing.Count + " missing package(s)",
                        GUILayout.Height(24)))
                    {
                        StartInstall(missing);
                    }
                    GUI.backgroundColor = prevBg;
                }
                if (GUILayout.Button("Re-scan", GUILayout.Width(90), GUILayout.Height(24)))
                {
                    RefreshInstalledPackages();
                }
            }
        }

        private static void RefreshInstalledPackages()
        {
            // Offline: this only needs what the project already resolved, and going to the
            // registry would stall the window on a slow or absent network.
            listRequest = Client.List(true, true);
            EditorApplication.update += PollList;
        }

        private static void PollList()
        {
            if (listRequest == null || !listRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= PollList;

            if (listRequest.Status == StatusCode.Success)
            {
                installedPackages = new HashSet<string>();
                foreach (UnityEditor.PackageManager.PackageInfo package in listRequest.Result)
                {
                    installedPackages.Add(package.name);
                }
            }
            else
            {
                Debug.LogError("[HexR] Couldn't read the project's package list: "
                    + (listRequest.Error != null ? listRequest.Error.message : "unknown error"));
                installedPackages = new HashSet<string>();
            }

            listRequest = null;
        }

        private static void StartInstall(List<string> packages)
        {
            foreach (string package in packages)
            {
                installQueue.Enqueue(package);
            }
            PumpInstallQueue();
        }

        private static void PumpInstallQueue()
        {
            if (activeAdd != null || installQueue.Count == 0)
            {
                return;
            }

            activeAddName = installQueue.Dequeue();
            Debug.Log("[HexR] Installing package: " + activeAddName);
            activeAdd = Client.Add(activeAddName);
            EditorApplication.update += PollAdd;
        }

        private static void PollAdd()
        {
            if (activeAdd == null || !activeAdd.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= PollAdd;

            if (activeAdd.Status == StatusCode.Success)
            {
                Debug.Log("[HexR] Installed " + activeAdd.Result.packageId + ".");
            }
            else
            {
                // Carry on rather than abort: one unavailable package (the Meta SDK isn't on the
                // public registry, for instance) shouldn't block the rest of the queue.
                Debug.LogError("[HexR] Failed to install " + activeAddName + ": "
                    + (activeAdd.Error != null ? activeAdd.Error.message : "unknown error"));
            }

            activeAdd = null;
            activeAddName = null;
            installedPackages = null;

            PumpInstallQueue();
        }

        private void DrawQuickLinks()
        {
            SectionHeader("Quick links");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("OpenXR docs"))
                {
                    Application.OpenURL(OpenXRDocsUrl);
                }
                if (GUILayout.Button("Meta OVR docs"))
                {
                    Application.OpenURL(MetaOVRDocsUrl);
                }
                if (GUILayout.Button("Microtube"))
                {
                    Application.OpenURL(MicrotubeUrl);
                }
            }
        }

        // Play Mode discards component edits on Stop, which is exactly when a solved offset is
        // most likely to be lost. Re-apply whatever was remembered as soon as we're back.
        [InitializeOnLoadMethod]
        private static void HookPlayModeOffsetRestore()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode)
                {
                    return;
                }
                RestoreOffset(FindTracking(PhysicsHandTracking.HandType.Left));
                RestoreOffset(FindTracking(PhysicsHandTracking.HandType.Right));
            };
        }
    }
}
