using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HexR
{
    // TEMPORARY calibration aid -- deleting the debug panel it drives plus this file removes it
    // completely, and nothing else references it.
    //
    // Exists because the hand offsets can only be solved where hand tracking actually runs,
    // which on Quest means inside a build -- where the Editor tool and the Console are both out
    // of reach. So the trigger has to be in-world and the answer has to be readable in the
    // headset; logging it would be logging into the void.
    //
    // Every action is a public no-arg method, so any of them can be wired to a uGUI
    // Toggle/Button on a HexR panel (which already routes poke and ray input through
    // PointableCanvas) or to an Interaction SDK InteractableUnityEventWrapper. The toggle
    // fields below just save doing that by hand in the Inspector.
    public class HexRHandOrientationCalibrator : MonoBehaviour
    {
        [Header("Buttons (optional -- or wire the methods yourself)")]
        [Tooltip("Solves both hands from the current tracked pose.")]
        public Toggle solveToggle;

        [Tooltip("Clears both hands' offsets back to zero.")]
        public Toggle resetToggle;

        [Tooltip("Manual fallback: steps the LEFT hand's offset by Nudge Step. Use if the solve misbehaves.")]
        public Toggle nudgeLeftToggle;

        [Tooltip("Manual fallback: steps the RIGHT hand's offset by Nudge Step.")]
        public Toggle nudgeRightToggle;

        [Header("Readout")]
        [Tooltip("Where results are shown. Required in a build -- there is no Console to read.")]
        public TextMeshProUGUI resultLabel;

        [Header("Behaviour")]
        [Tooltip("Round the solved offset to the nearest 90 degrees. The correction is normally axis-aligned, so rounding keeps tracking noise out of it.")]
        public bool snapTo90 = true;

        [Tooltip("Re-apply the last solved offsets on start, so a solve survives relaunching the build.")]
        public bool restoreSavedOnStart = true;

        [Tooltip("How far one Nudge press rotates the hand.")]
        public Vector3 nudgeStep = new Vector3(0f, 90f, 0f);

        private const string PrefKeyPrefix = "HexR.HandRotOffsetPalm.";

        private PhysicsHandTracking left;
        private PhysicsHandTracking right;

        private void Start()
        {
            StartCoroutine(WireWhenReady());
        }

        // Same defensive pattern HexRPanelConnectButtons and PressureTrackerMain use: the hands
        // are wired up by HexRManager, so don't assume Awake order.
        private IEnumerator WireWhenReady()
        {
            while (HexRManager.Instance == null)
            {
                yield return null;
            }

            PhysicsHandTracking[] all = HexRCompat.FindAll<PhysicsHandTracking>(true);
            foreach (PhysicsHandTracking tracking in all)
            {
                if (tracking.handType == PhysicsHandTracking.HandType.Left)
                {
                    left = tracking;
                }
                else
                {
                    right = tracking;
                }
            }

            // onValueChanged rather than a Button, because the panel's controls are Toggles.
            // The value itself carries no meaning here -- any change means "pressed".
            Bind(solveToggle, Solve);
            Bind(resetToggle, ResetOffsets);
            Bind(nudgeLeftToggle, NudgeLeft);
            Bind(nudgeRightToggle, NudgeRight);

            if (restoreSavedOnStart)
            {
                RestoreSaved(left);
                RestoreSaved(right);
            }

            if (left == null || right == null)
            {
                Report("Only " + (left != null ? "LEFT" : "RIGHT") + " hand found");
                yield break;
            }

            Report("Ready\n" + CurrentOffsetsText());
        }

        private void Bind(Toggle toggle, UnityEngine.Events.UnityAction action)
        {
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(delegate { action(); });
            }
        }

        public void Solve()
        {
            Report(SolveHand(left, "L") + "\n" + SolveHand(right, "R"));
        }

        public void ResetOffsets()
        {
            ApplyOffset(left, Vector3.zero);
            ApplyOffset(right, Vector3.zero);
            Report("Reset\n" + CurrentOffsetsText());
        }

        public void NudgeLeft()
        {
            Nudge(left);
        }

        public void NudgeRight()
        {
            Nudge(right);
        }

        private void Nudge(PhysicsHandTracking tracking)
        {
            if (tracking == null)
            {
                return;
            }

            ApplyOffset(tracking, PhysicsHandTracking.NormalizeEuler(tracking.rotOffsetPalm + nudgeStep));
            Report("Nudged " + tracking.handType + "\n" + CurrentOffsetsText());
        }

        private string SolveHand(PhysicsHandTracking tracking, string label)
        {
            if (tracking == null)
            {
                return label + ": no hand";
            }

            Vector3 solved;
            string error;
            if (!tracking.TrySolvePalmOffset(out solved, out error))
            {
                return label + ": " + error;
            }

            if (snapTo90)
            {
                solved = PhysicsHandTracking.SnapTo90(solved);
            }

            ApplyOffset(tracking, solved);
            return label + ": " + Format(solved);
        }

        private void ApplyOffset(PhysicsHandTracking tracking, Vector3 offset)
        {
            if (tracking == null)
            {
                return;
            }

            tracking.rotOffsetPalm = offset;
            PlayerPrefs.SetString(PrefKeyPrefix + tracking.handType, offset.x + "," + offset.y + "," + offset.z);
            PlayerPrefs.Save();
        }

        private void RestoreSaved(PhysicsHandTracking tracking)
        {
            if (tracking == null)
            {
                return;
            }

            string saved = PlayerPrefs.GetString(PrefKeyPrefix + tracking.handType, string.Empty);
            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            string[] parts = saved.Split(',');
            float x, y, z;
            if (parts.Length != 3
                || !float.TryParse(parts[0], out x)
                || !float.TryParse(parts[1], out y)
                || !float.TryParse(parts[2], out z))
            {
                return;
            }

            tracking.rotOffsetPalm = new Vector3(x, y, z);
        }

        private string CurrentOffsetsText()
        {
            return "L " + (left != null ? Format(left.rotOffsetPalm) : "--")
                + "   R " + (right != null ? Format(right.rotOffsetPalm) : "--");
        }

        private static string Format(Vector3 v)
        {
            return Mathf.RoundToInt(v.x) + "," + Mathf.RoundToInt(v.y) + "," + Mathf.RoundToInt(v.z);
        }

        private void Report(string message)
        {
            if (resultLabel != null)
            {
                resultLabel.text = message;
            }
            Debug.Log("[HexR] " + message.Replace("\n", "  |  "));
        }
    }
}
