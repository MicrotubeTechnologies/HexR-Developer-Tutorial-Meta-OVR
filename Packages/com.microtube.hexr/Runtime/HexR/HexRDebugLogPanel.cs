using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HexR
{
    // In-VR debug console for the HexR Panel -- lets a developer read recent Debug.Log
    // output on the headset itself, without a PC/adb tether. Only subscribes to
    // Application.logMessageReceived while debugModeToggle is on, so it costs nothing
    // when not in use.
    public class HexRDebugLogPanel : MonoBehaviour
    {
        public Toggle debugModeToggle;
        public GameObject logView;
        public TextMeshProUGUI logText;
        public int maxLines = 20;

        private readonly List<string> lines = new List<string>();

        void Awake()
        {
            if (logView != null)
            {
                logView.SetActive(false);
            }
        }

        void OnEnable()
        {
            if (debugModeToggle != null)
            {
                debugModeToggle.onValueChanged.AddListener(SetDebugMode);
            }
        }

        void OnDisable()
        {
            if (debugModeToggle != null)
            {
                debugModeToggle.onValueChanged.RemoveListener(SetDebugMode);
            }
            Application.logMessageReceived -= HandleLog;
        }

        private void SetDebugMode(bool isOn)
        {
            if (logView != null)
            {
                logView.SetActive(isOn);
            }

            Application.logMessageReceived -= HandleLog;
            if (isOn)
            {
                Application.logMessageReceived += HandleLog;
            }
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            string color = (type == LogType.Error || type == LogType.Exception) ? "#ff5555"
                : type == LogType.Warning ? "#ffcc00"
                : "#ffffff";

            lines.Add("<color=" + color + ">" + condition + "</color>");
            if (lines.Count > maxLines)
            {
                lines.RemoveAt(0);
            }

            if (logText != null)
            {
                logText.text = string.Join("\n", lines);
            }
        }
    }
}
