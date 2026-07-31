using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
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

        private enum Tab { Tester, Setup }
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
                GUI.backgroundColor = prevBg;
            }
            EditorGUILayout.Space(8);

            if (tab == Tab.Tester)
            {
                DrawTesterTab();
            }
            else
            {
                DrawSetupTab();
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

                using (new EditorGUILayout.HorizontalScope())
                {
                    Color prev = GUI.color;
                    GUI.color = hand.bleConnected ? Green : Color.gray;
                    GUILayout.Label("●", GUILayout.Width(14));
                    GUI.color = prev;
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    Color prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = hand.bleConnected ? Color.white : OrangeDark;
                    if (GUILayout.Button(hand.bleConnected ? "Disconnect" : "Connect", GUILayout.Width(80)))
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
                EditorGUILayout.LabelField(string.IsNullOrEmpty(hand.btText) ? (hand.bleConnected ? "Connected" : "Not connected") : hand.btText, EditorStyles.miniLabel);

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

        private void DrawLiveMonitor(HexRManager mgr)
        {
            SectionHeader("Live channel monitor (what the glove is doing right now)");
            HaptGloveHandler hand = targetHand == TestHand.Left ? mgr.leftHand : mgr.rightHand;
            if (hand == null)
            {
                EditorGUILayout.HelpBox("Target hand not assigned.", MessageType.None);
                return;
            }

            foreach (Haptics.Finger f in AllFingers)
            {
                Haptics.ChannelState state = hand.haptics.GetChannelState(f);
                float shown = state.Mode == Haptics.HapticMode.Off ? 0f : Mathf.Clamp01(state.Intensity);
                string label = f + (state.Mode == Haptics.HapticMode.Off ? "" : " (" + state.Mode + ")");
                Color fill = state.Mode == Haptics.HapticMode.Vibration ? OrangeLight : Orange;
                DrawColoredBar(EditorGUILayout.GetControlRect(false, 16), shown, fill, label);
            }
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
        }
    }
}
