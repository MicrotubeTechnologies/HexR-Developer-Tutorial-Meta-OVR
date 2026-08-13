using UnityEngine;
using UnityEngine.SceneManagement;

namespace HexR.MetaOVR
{
    /// <summary>
    /// Wires the Meta OVR half of the hand-near gating, from both ends:
    /// <list type="bullet">
    /// <item>at edit time, by registering into <see cref="HexRManager.PressureTrackerBackendSetup"/>
    /// so AutoSetup attaches and fills in <see cref="MetaOVRHandNearSource"/>;</item>
    /// <item>at run time, by attaching that component to any Pressure Controller that was wired
    /// before it existed.</item>
    /// </list>
    ///
    /// The registration is the whole point of the first half: HexR.Runtime can't reference this
    /// assembly (that would drag Oculus.Interaction back into the core and undo the split), so
    /// the dependency runs the other way and this side announces itself. When the Meta SDK isn't
    /// installed, HEXR_META_OVR is undefined, this assembly is excluded, nothing registers, and
    /// AutoSetup's Meta branch degrades to a warning.
    /// </summary>
    public static class MetaOVRBackendSetup
    {
        // AutoSetup runs from the Editor (menu item / Inspector button), so the editor-load hook
        // is the one that matters. The runtime hook covers a build that calls AutoSetup itself.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void Register()
        {
            // Domain reloads re-run this, and both attributes can fire in the same session --
            // unsubscribe first so the wiring can't be invoked twice per AutoSetup.
            HexRManager.PressureTrackerBackendSetup -= Wire;
            HexRManager.PressureTrackerBackendSetup += Wire;
        }

        private static void Wire(PressureTrackerMain tracker, Transform handRoot, string label)
        {
            if (tracker == null)
            {
                return;
            }

            MetaOVRHandNearSource source = Attach(tracker);
            source.searchRoot = handRoot;
            source.AutoFind(handRoot, label);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(source);
            UnityEditor.EditorUtility.SetDirty(tracker);
#endif
        }

        private static MetaOVRHandNearSource Attach(PressureTrackerMain tracker)
        {
            MetaOVRHandNearSource source = tracker.GetComponent<MetaOVRHandNearSource>();
            return source != null ? source : tracker.gameObject.AddComponent<MetaOVRHandNearSource>();
        }

        // Scenes authored before the assembly split have their interactors wired on the tracker
        // but no MetaOVRHandNearSource to read them -- nothing polls HasInteractable and grab/poke
        // gating goes quiet. Rather than make every such scene depend on someone remembering to
        // re-run Auto Setup, attach the component wherever the wiring is already present.
        //
        // Only touches trackers that have at least one interactor assigned, so an OpenXR rig that
        // happens to be running in a Meta-SDK project is left alone.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToExistingScenes()
        {
            SceneManager.sceneLoaded += (scene, mode) => AttachAll();
            AttachAll();
        }

        private static void AttachAll()
        {
            PressureTrackerMain[] trackers = Object.FindObjectsByType<PressureTrackerMain>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (PressureTrackerMain tracker in trackers)
            {
                bool alreadyWired = tracker.handGrabInteractor != null || tracker.pokeInteractor != null;
                if (alreadyWired && tracker.GetComponent<MetaOVRHandNearSource>() == null)
                {
                    Attach(tracker);
                }
            }
        }
    }
}
