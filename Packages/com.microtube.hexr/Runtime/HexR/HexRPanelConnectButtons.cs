using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HexR
{
    // Lets the HexR Panel prefab be dropped into any scene that already has a HexRManager
    // and just work, without wiring onClick in the Inspector per-scene. Waits for
    // HexRManager.Instance rather than assuming Awake order, same defensive pattern
    // PressureTrackerMain already uses for the same singleton.
    public class HexRPanelConnectButtons : MonoBehaviour
    {
        public Button leftConnectButton;
        public Button rightConnectButton;

        void Start()
        {
            StartCoroutine(WireWhenReady());
        }

        private IEnumerator WireWhenReady()
        {
            while (HexRManager.Instance == null)
            {
                yield return null;
            }

            if (leftConnectButton != null)
            {
                leftConnectButton.onClick.AddListener(HexRManager.Instance.ConnectLeftBT);
            }
            if (rightConnectButton != null)
            {
                rightConnectButton.onClick.AddListener(HexRManager.Instance.ConnectRightBT);
            }
        }
    }
}
