using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HexR
{
    // Lets the HexR Panel prefab be dropped into any scene that already has a HexRManager
    // and just work, without wiring onClick in the Inspector per-scene. Waits for
    // HexRManager.Instance rather than assuming Awake order, same defensive pattern
    // PressureTrackerMain already uses for the same singleton.
    //
    // This is the only wiring the panel should need. The alternative -- UnityEvent targets
    // serialized into the prefab -- cannot work for a panel shipped in a package: the objects
    // it has to call (HexRManager, and the collider visualizer that sits on it) live outside
    // the panel's own prefab, so there is nothing valid to serialize a reference to. Both
    // panels shipped with every one of those targets set to None for exactly that reason, and
    // only worked where a scene happened to override them.
    public class HexRPanelConnectButtons : MonoBehaviour
    {
        // Toggles, not Buttons, despite reading as buttons on the panel -- they are UI.Toggle
        // with an Animator driving the pressed look, and they fire onValueChanged. Typing these
        // as Button would leave them unassignable and silently null.
        [Tooltip("Calls HexRManager.ConnectLeftBT when switched on.")]
        public Toggle leftConnectToggle;

        [Tooltip("Calls HexRManager.ConnectRightBT when switched on.")]
        public Toggle rightConnectToggle;

        [Tooltip("Shows/hides the hand collider solids. The visualizer is found on (or added to) the HexRManager, so this works no matter which scene's rig is loaded.")]
        public Toggle visualizerToggle;

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

            HexRManager manager = HexRManager.Instance;

            // Only on the way on. onValueChanged fires for both directions, so subscribing
            // ConnectLeftBT directly would start a second connection attempt when the toggle
            // switches back off.
            if (leftConnectToggle != null)
            {
                leftConnectToggle.onValueChanged.AddListener(isOn => { if (isOn) manager.ConnectLeftBT(); });
            }
            if (rightConnectToggle != null)
            {
                rightConnectToggle.onValueChanged.AddListener(isOn => { if (isOn) manager.ConnectRightBT(); });
            }

            if (visualizerToggle != null)
            {
                WireVisualizerToggle(manager);
            }
        }

        // One visualizer, on the manager. It resolves both hands through HexRManager rather
        // than scanning its own children, so a single instance there covers every hand root --
        // and it rides the manager's DontDestroyOnLoad, so the toggle keeps working across a
        // scene change instead of pointing at a visualizer that went down with the old scene.
        private void WireVisualizerToggle(HexRManager manager)
        {
            HaptGloveCollidersVisualizer visualizer = manager.GetComponent<HaptGloveCollidersVisualizer>();
            if (visualizer == null)
            {
                visualizer = manager.gameObject.AddComponent<HaptGloveCollidersVisualizer>();
            }

            // SetVisible rather than ColliderToggle: the tick box owns the state, so they cannot
            // drift apart the way a flip-on-each-press would.
            visualizerToggle.onValueChanged.AddListener(visualizer.SetVisible);

            // Honour however the toggle was authored, so the panel doesn't open showing a ticked
            // box with nothing drawn.
            visualizer.SetVisible(visualizerToggle.isOn);
        }
    }
}
