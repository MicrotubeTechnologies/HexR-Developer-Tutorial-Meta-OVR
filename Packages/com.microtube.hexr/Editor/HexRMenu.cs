using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HexR
{
    /// <summary>
    /// Top-level "HexR" menu in the Unity toolbar, next to File/Edit/Assets/GameObject --
    /// the entry point for one-click setup and (later) other editor-side tooling. Add
    /// more [MenuItem("HexR/...")] entries here as that grows; this is the one place
    /// editor-only HexR tooling should live, rather than #if UNITY_EDITOR blocks
    /// scattered through runtime scripts.
    /// </summary>
    public static class HexRMenu
    {
        // The HexR Main rig prefabs now ship inside this package, so Create HexR Rig works on
        // a fresh install. They used to live in the consuming project's Assets/HexRAssets/,
        // which meant a clean install got the scripts but nothing to instantiate -- and the
        // Pressure Controller is not optional, every HapticFingerTrigger needs one per scene.
        private const string OpenXRPrefabName = "HexR Main (Open XR)";
        private const string MetaOVRPrefabName = "HexR Main (OVR)";

        // Packages/... is a virtual path Unity resolves whether the package is embedded (this
        // repo) or installed from a git URL/registry, so a fixed path beats a name search.
        // FindPrefabByExactName stays as a fallback for projects that still keep their own
        // copies under Assets/, which must win -- someone who customised the rig in place
        // should keep getting their version.
        private const string PrefabFolder = "Packages/com.microtube.hexr/Runtime/Prefabs/";

        // The standalone panel, for adding to a scene that already has a rig. Resolved by fixed
        // path rather than name search for the same reason as the rig prefabs above.
        private const string PanelPrefabPath = "Packages/com.microtube.hexr/Runtime/UI/HexR Panel.prefab";

        // Deliberately NOT part of either rig prefab. HexRManager marks the rig
        // DontDestroyOnLoad, while HapticFingerTrigger, HexRGrabbable, ProximityCheck and
        // AutoSetup all resolve their tracker with GameObject.Find("Left/Right Pressure
        // Controller") at runtime. A tracker riding the persistent rig would be found by the
        // first scene and then be the wrong object for every scene loaded after it -- so it
        // goes in per scene instead, which is what Add Pressure Controller does.
        private const string PressureControllerPrefabPath = PrefabFolder + "Pressure Controller.prefab";

        // Named for every runtime it covers, because the commonest PICO question is "where is
        // the PICO rig". There isn't one and there shouldn't be: PICO is OpenXR, so a separate
        // entry would instantiate this identical prefab with this identical Options value and
        // imply a PICO-specific rig exists.
        [MenuItem("HexR/Create HexR Rig/Open XR (Quest, PICO, SteamVR)", false, 0)]
        private static void CreateHexRRigOpenXR() => CreateHexRRig(OpenXRPrefabName, HexRManager.Options.OpenXR);

        [MenuItem("HexR/Create HexR Rig/Meta OVR", false, 1)]
        private static void CreateHexRRigMetaOVR() => CreateHexRRig(MetaOVRPrefabName, HexRManager.Options.MetaOVR);

        [MenuItem("HexR/Add HexR Panel", false, 2)]
        private static void AddHexRPanel()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[HexR] Could not load the HexR Panel prefab at \"{PanelPrefabPath}\" -- the package install may be incomplete or the prefab was moved.");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add HexR Panel");

            // HexRPanelConnectButtons finds HexRManager.Instance on its own at runtime
            // regardless of parenting, so this is just a tidy default -- not required for
            // the panel to work.
            HexRManager controller = HexRCompat.FindAny<HexRManager>();
            if (controller != null)
            {
                Undo.SetTransformParent(instance.transform, controller.transform, "Add HexR Panel");
            }
            else
            {
                Debug.LogWarning("[HexR] No HexRManager found in the open scene -- added the panel at the scene root. It will still self-connect once a HexRManager exists (e.g. after HexR > Create HexR Rig).");
            }

            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            EditorSceneManager.MarkSceneDirty(instance.scene);
            Debug.Log("[HexR] Added HexR Panel to the scene.");
        }

        // Separate from Create HexR Rig because it is per scene, not per rig: additional
        // scenes in a multi-scene setup each need their own, and they get the rig by way of
        // the DontDestroyOnLoad one already loaded.
        [MenuItem("HexR/Add Pressure Controller", false, 3)]
        private static void AddPressureControllerMenu()
        {
            GameObject existing = FindPressureControllerInActiveScene();
            if (existing != null)
            {
                Debug.LogWarning("[HexR] \"" + existing.name + "\" is already in this scene -- not adding a second one. "
                    + "GameObject.Find returns whichever duplicate it reaches first, so two in one scene make the "
                    + "haptics bind unpredictably.");
                Selection.activeGameObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            GameObject instance = AddPressureController();
            if (instance == null)
            {
                return;
            }

            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log("[HexR] Added Pressure Controller to the scene. Run HexR > Auto Setup Scene to wire it to the hands.");
        }

        // Left at the scene root on purpose -- parenting it under the rig would put it on the
        // DontDestroyOnLoad object and defeat the per-scene lookup described above.
        private static GameObject AddPressureController()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PressureControllerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[HexR] Could not load the Pressure Controller prefab at \"{PressureControllerPrefabPath}\" -- "
                    + "the com.microtube.hexr install may be incomplete or the prefab was moved. Without one, no "
                    + "HapticFingerTrigger in this scene can produce haptics.");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add Pressure Controller");
            EditorSceneManager.MarkSceneDirty(instance.scene);
            return instance;
        }

        // Scoped to the active scene rather than GameObject.Find: with scenes open additively
        // each one needs its own controller, and a scene-wide search would report the
        // neighbouring scene's copy and refuse a legitimate add. Includes inactive objects,
        // which GameObject.Find skips -- an inactive duplicate is still a duplicate.
        private static GameObject FindPressureControllerInActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return null;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                PressureTrackerMain tracker = root.GetComponentInChildren<PressureTrackerMain>(true);
                if (tracker != null)
                {
                    return tracker.gameObject;
                }
            }
            return null;
        }

        // Create Demo Scene ---------------------------------------------------------------
        //
        // The Tutorial Content sample ships props but no scene, and deliberately so: a saved
        // scene has to reference one backend's camera rig, which means it arrives broken for
        // everyone on the other backend. Generating the scene sidesteps that entirely -- it
        // runs the same Create HexR Rig path against whichever backend is actually installed,
        // so nothing cross-backend is ever serialised.
        //
        // The one thing it cannot supply is the hand-tracking rig itself. That is backend SDK
        // content, which this package does not depend on, so the command says so at the end
        // rather than leaving you to wonder why the hands never appear.
        [MenuItem("HexR/Create Demo Scene", false, 4)]
        private static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Assembly probe rather than a manifest read, for the same reason Project Setup
            // uses one: it answers "can HexR compile against this backend right now", and it
            // stays correct however the SDK arrived.
            bool metaAvailable = IsAssemblyLoaded("Oculus.Interaction");
            // Only ever affects what the log below says. PICO takes the OpenXR rig, the OpenXR
            // prefab and Options.OpenXR like any other OpenXR runtime -- naming it here just
            // saves a PICO user wondering whether the "OpenXR" in the log means it missed them.
            bool picoAvailable = IsAssemblyLoaded("Unity.XR.OpenXR.Features.PICOSupport");
            string backendLabel = metaAvailable ? "Meta OVR" : (picoAvailable ? "OpenXR (PICO)" : "OpenXR");
            string prefabName = metaAvailable ? MetaOVRPrefabName : OpenXRPrefabName;
            HexRManager.Options framework = metaAvailable ? HexRManager.Options.MetaOVR : HexRManager.Options.OpenXR;

            CreateHexRRig(prefabName, framework);

            HexRManager controller = HexRCompat.FindAny<HexRManager>();
            if (controller == null)
            {
                Debug.LogError("[HexR] Demo scene: the HexR rig could not be created, so the rest of the scene was skipped. See the error above.");
                return;
            }

            PressureTrackerMain right = FindDemoTracker("Right Pressure Controller");
            PressureTrackerMain left = FindDemoTracker("Left Pressure Controller");

            CreateDemoFloor();
            GameObject grabbable = CreateDemoGrabbable(right, left);
            CreateDemoHapticZone(right, left);

            Selection.activeGameObject = grabbable;
            EditorGUIUtility.PingObject(grabbable);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);

            Debug.Log("[HexR] Demo scene created for " + backendLabel + ".\n"
                + "Three things are left, because the package cannot ship them for you:\n"
                + "  1. Add your hand-tracking rig -- Meta Building Blocks' \"Hand Tracking\" block, or an OpenXR rig with com.unity.xr.hands."
                + (picoAvailable && !metaAvailable
                    ? " On PICO that rig is an ordinary XR Origin with com.unity.xr.hands; the PICO plugin supplies the runtime, not the rig."
                    : string.Empty) + "\n"
                + "  2. Run HexR > Auto Setup Scene again so it picks that rig up.\n"
                + "  3. Run HexR > Validate Scene Setup, save the scene, then press Play.");
        }

        private static void CreateDemoFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = Vector3.one * 0.4f;
        }

        private static GameObject CreateDemoGrabbable(PressureTrackerMain right, PressureTrackerMain left)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Grabbable Cube";
            cube.transform.position = new Vector3(-0.12f, 1f, 0.35f);
            cube.transform.localScale = Vector3.one * 0.07f;

            // HexRGrabbable is physics-driven: it wants a Rigidbody and a *trigger* collider on
            // the same object. A primitive arrives with a solid one, so flip it. Gravity stays
            // off until release, so the cube waits in front of the hands instead of dropping.
            cube.GetComponent<BoxCollider>().isTrigger = true;
            Rigidbody body = cube.AddComponent<Rigidbody>();
            body.useGravity = false;

            HexRGrabbable grabbable = cube.AddComponent<HexRGrabbable>();
            grabbable.TypeOfGrab = HexRGrabbable.Options.PalmGrab;
            grabbable.Gravity = HexRGrabbable.Option.On;
            grabbable.HapticStrength = 20f;

            // This is the part of the demo that matters most. On OpenXR a ProximityCheck is the
            // only thing that ever reports a hand as near, so a scene without one is completely
            // silent -- which reads as broken hardware. Shipping a wired example is the cheapest
            // way to stop that being the first thing a new developer hits.
            GameObject proximity = new GameObject("Proximity Check");
            proximity.transform.SetParent(cube.transform, false);
            BoxCollider reach = proximity.AddComponent<BoxCollider>();
            reach.isTrigger = true;
            reach.size = Vector3.one * 2.5f;

            ProximityCheck check = proximity.AddComponent<ProximityCheck>();
            check.rightpressureTrackerMain = right;
            check.leftpressureTrackerMain = left;

            return cube;
        }

        private static void CreateDemoHapticZone(PressureTrackerMain right, PressureTrackerMain left)
        {
            GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            zone.name = "Haptic Zone";
            zone.transform.position = new Vector3(0.12f, 1f, 0.35f);
            zone.transform.localScale = Vector3.one * 0.12f;
            zone.GetComponent<SphereCollider>().isTrigger = true;

            SpecialHaptics haptics = zone.AddComponent<SpecialHaptics>();
            haptics.TypeOfHaptics = SpecialHaptics.Options.CustomHaptics;
            haptics.HapticPressure = 30f;
            haptics.RPressureTracker = right;
            haptics.LPressureTracker = left;
        }

        // Matches how ProximityCheck's own Auto Set Up and HexRManager.AutoSetup find the rig.
        private static PressureTrackerMain FindDemoTracker(string objectName)
        {
            GameObject found = GameObject.Find(objectName);
            if (found == null)
            {
                Debug.LogWarning("[HexR] Demo scene: no \"" + objectName + "\" in the scene, so the demo objects were left unwired. "
                    + "Assign them by hand, or press Auto Set Up on each one.");
                return null;
            }
            return found.GetComponent<PressureTrackerMain>();
        }

        private static bool IsAssemblyLoaded(string assemblyName)
        {
            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == assemblyName)
                {
                    return true;
                }
            }
            return false;
        }

        [MenuItem("HexR/Auto Setup Scene", false, 20)]
        private static void AutoSetupScene()
        {
            HexRManager controller = HexRCompat.FindAny<HexRManager>();
            if (controller == null)
            {
                Debug.LogWarning("[HexR] No HexRManager found in the open scene -- use HexR > Create HexR Rig first, or add the HexR Main prefab manually.");
                return;
            }

            // Same logic as the Inspector's "Auto Set Up HexR" button (HexRManager.AutoSetup)
            // -- this just means you don't need to find and select that GameObject first.
            HexRManager.AutoSetup(controller);

            Selection.activeGameObject = controller.gameObject;
            EditorGUIUtility.PingObject(controller.gameObject);
            Debug.Log("[HexR] Auto setup complete for '" + controller.gameObject.name + "'.");
        }

        // Grays out the menu item instead of letting it silently no-op when there's
        // nothing to set up.
        [MenuItem("HexR/Auto Setup Scene", true)]
        private static bool ValidateAutoSetupScene()
        {
            return HexRCompat.FindAny<HexRManager>() != null;
        }

        [MenuItem("HexR/Validate Scene Setup", false, 21)]
        private static void ValidateSceneSetup()
        {
            HexRManager controller = HexRCompat.FindAny<HexRManager>();
            if (controller == null)
            {
                Debug.LogWarning("[HexR] No HexRManager found in the open scene -- nothing to validate.");
                return;
            }

            // Read-only -- reports what's missing/misconfigured without touching anything,
            // so this is safe to run on a scene Auto Setup Scene was never run on, or after
            // manual edits (e.g. a hand-root rewire) to check nothing broke.
            HexRManager.ValidateSetup(controller);

            Selection.activeGameObject = controller.gameObject;
            EditorGUIUtility.PingObject(controller.gameObject);
        }

        [MenuItem("HexR/Validate Scene Setup", true)]
        private static bool ValidateValidateSceneSetup()
        {
            return HexRCompat.FindAny<HexRManager>() != null;
        }

        private static void CreateHexRRig(string prefabName, HexRManager.Options framework)
        {
            if (HexRCompat.FindAny<HexRManager>() != null)
            {
                Debug.LogWarning("[HexR] A HexRManager already exists in this scene -- not creating a second HexR Main rig. Delete the existing one first if you want to replace it.");
                return;
            }

            GameObject prefab = FindRigPrefab(prefabName);
            if (prefab == null)
            {
                Debug.LogError($"[HexR] Could not find a prefab named \"{prefabName}\". It ships at \"{PrefabFolder}\" -- the com.microtube.hexr install may be incomplete. (A copy under Assets/ would also be used if one existed.)");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create HexR Rig");

            HexRManager controller = instance.GetComponent<HexRManager>();
            if (controller == null)
            {
                Debug.LogError($"[HexR] \"{prefabName}\" does not have a HexRManager component on its root -- cannot continue setup. The prefab may have changed; wiring stops here, the rig is still in the scene.");
                return;
            }

            controller.XRFramework = framework;

            // Before AutoSetup, which wires the hands to the trackers by name -- they have to
            // exist first. Skipped if the scene already has one, so re-running against a scene
            // set up by hand does not leave two.
            if (FindPressureControllerInActiveScene() == null)
            {
                AddPressureController();
            }

            HexRManager.AutoSetup(controller);

            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            EditorSceneManager.MarkSceneDirty(instance.scene);
            Debug.Log($"[HexR] Created and set up \"{prefabName}\" in the scene. Check the console above for anything Auto Setup couldn't find (e.g. no XR camera rig in the scene yet) -- those need manual wiring.");
        }

        // Ghost-rig joint names that used to carry a manually-placed HapticFingerTrigger +
        // trigger Collider before HexRManager.AutoSetup started adding them directly on the
        // raw tracked hand instead (PhysicsHandTracking.ResolveRawFingerJoint/
        // ResolveRawPalmJoint). Left over here, both hands would double-fire haptics/grab
        // for the same touch -- one-time cleanup, not something Auto Setup itself should do
        // automatically since it has no way to know these specific legacy names are safe to
        // touch on an arbitrary project's rig.
        private static readonly string[] LegacyGhostRigJointNames =
        {
            "L_Index_3", "L_Middle_3", "L_Ring_3", "L_Pinky_1", "L_Thumb_2", "L_GhostPalm",
            "R_Index_3", "R_Middle_3", "R_Ring_3", "R_Pinky_1", "R_Thumb_2", "R_GhostPalm",
            "GhostIndex", "GhostMiddle", "GhostRing", "GhostPinky", "GhostThumb", "L_Palm", "R_Palm"
        };

        [MenuItem("HexR/Migration/Remove Legacy Ghost-Rig Haptic Triggers", false, 40)]
        private static void RemoveLegacyGhostRigHapticTriggers()
        {
            RemoveLegacyHapticTriggersFromPrefab(MetaOVRPrefabName);
            RemoveLegacyHapticTriggersFromPrefab(OpenXRPrefabName);
        }

        private static void RemoveLegacyHapticTriggersFromPrefab(string prefabName)
        {
            GameObject prefab = FindRigPrefab(prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"[HexR] Migration: could not find \"{prefabName}\" -- skipping.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int removedTriggers = 0, removedColliders = 0;

            try
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (System.Array.IndexOf(LegacyGhostRigJointNames, t.name) < 0) continue;

                    HapticFingerTrigger trigger = t.GetComponent<HapticFingerTrigger>();
                    if (trigger != null)
                    {
                        Object.DestroyImmediate(trigger);
                        removedTriggers++;
                    }

                    Collider collider = t.GetComponent<Collider>();
                    if (collider != null && collider.isTrigger)
                    {
                        Object.DestroyImmediate(collider);
                        removedColliders++;
                    }
                }

                if (removedTriggers > 0 || removedColliders > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[HexR] Migration: removed {removedTriggers} HapticFingerTrigger and {removedColliders} trigger Collider component(s) from \"{prefabName}\" -- haptics/grab now detect via the raw hand instead.");
                }
                else
                {
                    Debug.Log($"[HexR] Migration: \"{prefabName}\" had no legacy ghost-rig haptic triggers to remove -- already clean.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Deletes whatever each hand's PhysicsHandTracking.HexrRoot currently points to --
        // that field is, by the code's own definition, the root of the ghost-rig hierarchy
        // (MetaOVRStart/OpenXRStart search under HexrRoot.gameObject for the ghost joints
        // they mirror the real hand onto). Using the field itself as the deletion boundary,
        // rather than guessing object names, means this stays correct even if a given rig's
        // ghost hierarchy isn't named/shaped the way you'd expect -- confirmed necessary
        // here, since one hand's HexrRoot turned out to point at an object literally named
        // "b_r_wrist", not anything obviously "ghost"-named.
        //
        // PhysicsHandTracking degrades gracefully once HexrRoot is null (see MetaOVRStart/
        // OpenXRStart/*Update/*FixedUpdate's early-out guards) -- handRoot and everything the
        // new raw-hand haptics/grab system depends on are untouched.
        [MenuItem("HexR/Migration/Remove Ghost Hand Rig", false, 41)]
        private static void RemoveGhostHandRig()
        {
            RemoveGhostHandRigFromPrefab(MetaOVRPrefabName);
            RemoveGhostHandRigFromPrefab(OpenXRPrefabName);
        }

        private static void RemoveGhostHandRigFromPrefab(string prefabName)
        {
            GameObject prefab = FindRigPrefab(prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"[HexR] Migration: could not find \"{prefabName}\" -- skipping.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int removed = 0;

            try
            {
                foreach (PhysicsHandTracking tracking in root.GetComponentsInChildren<PhysicsHandTracking>(true))
                {
                    if (tracking.HexrRoot == null) continue;

                    GameObject ghostRoot = tracking.HexrRoot.gameObject;
                    int descendantCount = CountDescendants(ghostRoot.transform);
                    Debug.Log($"[HexR] Migration: removing ghost-rig root \"{ghostRoot.name}\" ({descendantCount} descendant object(s)) from \"{prefabName}\", referenced by {tracking.gameObject.name}'s PhysicsHandTracking.HexrRoot.");

                    tracking.HexrRoot = null;
                    Object.DestroyImmediate(ghostRoot);
                    removed++;
                }

                if (removed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[HexR] Migration: removed {removed} ghost-rig root(s) from \"{prefabName}\". Review the change (e.g. git diff) before committing.");
                }
                else
                {
                    Debug.Log($"[HexR] Migration: \"{prefabName}\" has no HexrRoot assigned on any hand -- nothing to remove.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int CountDescendants(Transform t)
        {
            int count = 0;
            foreach (Transform child in t)
            {
                count += 1 + CountDescendants(child);
            }
            return count;
        }

        // A project-local copy under Assets/ wins over the packaged one, so a rig someone
        // customised in place keeps being the one they get. Falls back to the package copy,
        // which is what a fresh install has.
        private static GameObject FindRigPrefab(string prefabName)
        {
            GameObject projectCopy = FindPrefabByExactName(prefabName, "Assets");
            if (projectCopy != null)
            {
                return projectCopy;
            }

            GameObject packaged = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + prefabName + ".prefab");
            if (packaged != null)
            {
                return packaged;
            }

            // Last resort: the package may be installed somewhere this fixed path doesn't
            // describe (a renamed embed, say), so fall back to searching everywhere.
            return FindPrefabByExactName(prefabName, null);
        }

        private static GameObject FindPrefabByExactName(string prefabName, string searchFolder)
        {
            // FindAssets does a loose match, and the parentheses in "HexR Main (OVR)" /
            // "HexR Main (Open XR)" aren't reliable search-query syntax -- search broadly
            // on the safe part of the name, then confirm the exact filename ourselves.
            string[] guids = searchFolder == null
                ? AssetDatabase.FindAssets("HexR Main t:Prefab")
                : AssetDatabase.FindAssets("HexR Main t:Prefab", new[] { searchFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == prefabName)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            return null;
        }

        // ---------------------------------------------------------------------------------
        // HaptGlove.dll -> source migration
        // ---------------------------------------------------------------------------------
        //
        // The HaptGlove runtime used to ship as a precompiled HaptGlove.dll. A MonoBehaviour
        // living in a managed plugin is referenced by (the DLL's GUID, a fileID derived from
        // the type), where a MonoBehaviour from a .cs file is referenced by (the .cs meta's
        // GUID, the constant 11500000). Those forms are not interchangeable, so every scene
        // and prefab that carried a HaptGloveHandler from the DLL points at an asset that no
        // longer exists, and Unity shows "the associated script can not be loaded" with the
        // component's serialised values still on disk but unreachable.
        //
        // The rig prefabs inside this package were remapped when the runtime was converted.
        // A consuming project's own scenes were not -- this fixes those. It is a plain text
        // substitution on the YAML rather than an object-model edit, because a missing script
        // cannot be resolved through the object model: GetComponent returns null for it, so
        // there is nothing to reassign m_Script on.
        private const string LegacyHaptGloveDllGuid = "dffea59c290622e438fc4f11872007ec";
        private const string LegacyHaptGloveHandlerFileId = "-1140640302";
        private const string HaptGloveHandlerScriptGuid = "31d314e6cfab95f44b0183e301ce3bd7";

        [MenuItem("HexR/Migration/Repoint HaptGlove Scripts To Source", false, 42)]
        private static void RepointHaptGloveScriptsToSource()
        {
            string legacy = $"m_Script: {{fileID: {LegacyHaptGloveHandlerFileId}, guid: {LegacyHaptGloveDllGuid}, type: 3}}";
            string replacement = $"m_Script: {{fileID: 11500000, guid: {HaptGloveHandlerScriptGuid}, type: 3}}";

            // Serialised assets only, and only under Assets/ -- a package folder is not ours
            // to rewrite, and Library/ is regenerated.
            string[] guids = AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { "Assets" });
            int filesChanged = 0;
            int referencesChanged = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string full = Path.GetFullPath(path);
                if (!File.Exists(full))
                {
                    continue;
                }

                string text;
                try
                {
                    text = File.ReadAllText(full);
                }
                catch (IOException e)
                {
                    Debug.LogWarning($"[HexR] Migration: could not read \"{path}\" -- skipping. {e.Message}");
                    continue;
                }

                if (text.IndexOf(legacy, System.StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                // A YAML asset saved in binary or text-with-force-text both read as text here;
                // if it isn't text the substring simply won't match and we skip it above.
                int occurrences = 0;
                int scan = 0;
                while ((scan = text.IndexOf(legacy, scan, System.StringComparison.Ordinal)) >= 0)
                {
                    occurrences++;
                    scan += legacy.Length;
                }

                File.WriteAllText(full, text.Replace(legacy, replacement));
                filesChanged++;
                referencesChanged += occurrences;
                Debug.Log($"[HexR] Migration: repointed {occurrences} HaptGloveHandler reference(s) in \"{path}\".");
            }

            if (filesChanged == 0)
            {
                Debug.Log("[HexR] Migration: no scene or prefab under Assets/ referenced the old "
                    + "HaptGlove.dll HaptGloveHandler -- nothing to do.");
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[HexR] Migration: repointed {referencesChanged} reference(s) across {filesChanged} "
                + "asset(s) from HaptGlove.dll to the in-package HaptGlove source. The components keep "
                + "their serialised values. Review the change (e.g. git diff) before committing.");
        }
    }
}
