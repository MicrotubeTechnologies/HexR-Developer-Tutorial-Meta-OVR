using System.Collections;
using System.Collections.Generic;
using HaptGlove;
using HexR;
using UnityEngine;
using UnityEngine.SceneManagement;

// Draws a red/green solid for every collider on the HexR hands, so you can see where touch
// detection actually is rather than inferring it from the Inspector.
//
// It finds those colliders itself, via HexRManager, instead of scanning its own children.
// The old version called GetComponentsInChildren<Collider>() on itself, which meant it only
// ever showed the colliders underneath whatever object it happened to be parented to -- so a
// working setup needed one component per hand root, each wired to its own button, and getting
// the parent wrong silently showed nothing. One instance anywhere in the scene now covers both
// hands.
//
// Kept in the global namespace and with ColliderToggle()'s exact name because scenes and
// prefabs bind to it through UnityEvents, which serialize the type and method by name --
// renaming either would quietly unhook every button already wired up.
[DisallowMultipleComponent]
public class HaptGloveCollidersVisualizer : MonoBehaviour
{
    [Header("What to show")]
    [Tooltip("Colliders on the raw tracked hands -- the ones Meta's hand tracking drives, and the only ones that fire haptics on the current rig.")]
    public bool showTrackedHands = true;

    [Tooltip("Colliders on the HexR ghost/physics hands. Worth turning on when you're checking whether a touch is being detected twice.")]
    public bool showGhostRigs = true;

    [Tooltip("Extra roots to visualize, on top of whatever HexRManager resolves. Usually left empty.")]
    public List<Transform> extraRoots = new List<Transform>();

    [Header("Appearance")]
    public Color trackedHandColor = new Color(0.2f, 0.9f, 0.3f);
    public Color ghostRigColor = new Color(0.9f, 0.15f, 0.15f);
    public Color extraRootColor = new Color(0.25f, 0.5f, 1f);

    // Marks the objects this component spawned, so a second visualizer in the scene doesn't
    // stack a second solid on top of the first.
    private const string VisualizerName = "HexRColliderVisualizer";

    private readonly List<GameObject> visualizers = new List<GameObject>();
    private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
    private Coroutine rebuildRoutine;
    private bool showing;

    public bool IsShowing { get { return showing; } }

    private void OnEnable()
    {
        // The tracked hands live in the scene while this may not, so a scene load destroys
        // every collider currently being drawn. HexRManager re-points its hands at the new
        // scene's rig; this follows it there rather than leaving the toggle stuck "on" with
        // nothing on screen.
        SceneManager.sceneLoaded += OnSceneLoaded;

        // OnDisable tears the solids down but leaves `showing` alone, so that disabling and
        // re-enabling the component restores what was on screen instead of desyncing the
        // toggle's state from what you can see.
        if (showing)
        {
            CreateVisualizer();
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (rebuildRoutine != null)
        {
            StopCoroutine(rebuildRoutine);
            rebuildRoutine = null;
        }
        DestroyVisualizer();
    }

    private void OnDestroy()
    {
        foreach (Material material in materials.Values)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
        materials.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!showing || mode != LoadSceneMode.Single)
        {
            return;
        }

        // The old solids went down with the old rig; the list is full of destroyed references.
        visualizers.Clear();

        if (rebuildRoutine != null)
        {
            StopCoroutine(rebuildRoutine);
        }
        rebuildRoutine = StartCoroutine(RebuildWhenHandsAreBack());
    }

    // HexRManager re-points its hand roots from its own sceneLoaded handler, retrying across
    // frames until the incoming rig's joints exist. Rebuilding on this frame would resolve the
    // roots it is still in the middle of replacing, so wait for them to settle.
    private IEnumerator RebuildWhenHandsAreBack()
    {
        float deadline = Time.unscaledTime + 5f;

        while (Time.unscaledTime < deadline)
        {
            yield return null;

            List<RootTarget> roots = new List<RootTarget>();
            CollectRoots(roots);
            if (roots.Count > 0)
            {
                CreateVisualizer();
                break;
            }
        }

        rebuildRoutine = null;
    }

    /// <summary>Show the colliders if hidden, hide them if shown. Wired to the HexR Panel's toggle.</summary>
    public void ColliderToggle()
    {
        if (showing)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>
    /// Drive from a UI Toggle's onValueChanged. Preferred over ColliderToggle() for a Toggle,
    /// because it takes the state rather than flipping it -- a flip desynchronises the tick box
    /// from the solids the moment anything else changes visibility (a scene load, the component
    /// being disabled, a second control).
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public void Show()
    {
        showing = true;
        CreateVisualizer();
    }

    public void Hide()
    {
        showing = false;
        DestroyVisualizer();
    }

    /// <summary>Rebuild against the current hands. Call after anything that swaps the rig.</summary>
    public void Refresh()
    {
        if (showing)
        {
            CreateVisualizer();
        }
    }

    public void CreateVisualizer()
    {
        DestroyVisualizer();
        showing = true;

        List<RootTarget> roots = new List<RootTarget>();
        CollectRoots(roots);

        if (roots.Count == 0)
        {
            Debug.LogWarning("[HexR] " + name + ": nothing to visualize -- no HexRManager in the scene with hand "
                + "roots assigned, and no extraRoots set. Run HexR > Validate Scene Setup to see what's missing.");
            return;
        }

        // One collider can sit under two roots at once (a ghost rig nested under a tracked
        // hand, say), and drawing it twice would z-fight rather than look like two things.
        HashSet<Collider> seen = new HashSet<Collider>();
        int drawn = 0;

        foreach (RootTarget target in roots)
        {
            if (target.Root == null)
            {
                continue;
            }

            foreach (Collider collider in target.Root.GetComponentsInChildren<Collider>())
            {
                // Deliberately the includeInactive: false default. A collider on an inactive
                // object cannot raise a trigger event, so leaving it out keeps "what this draws"
                // equal to "what can actually fire" -- which is the whole point of looking.
                if (collider == null || !collider.enabled || !seen.Add(collider))
                {
                    continue;
                }

                // Another visualizer instance already drew this one.
                if (HasVisualizerChild(collider.transform))
                {
                    continue;
                }

                GameObject solid = BuildSolid(collider, target.Color);
                if (solid != null)
                {
                    visualizers.Add(solid);
                    drawn++;
                }
            }
        }

        Debug.Log("[HexR] " + name + ": drew " + drawn + " collider(s) across " + roots.Count + " root(s).");
    }

    public void DestroyVisualizer()
    {
        foreach (GameObject obj in visualizers)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        visualizers.Clear();
    }

    // A root to scan plus the colour its colliders get drawn in, so tracked-hand and ghost-rig
    // colliders are told apart on sight -- the two overlap in space, and which one you are
    // looking at is exactly the question this component exists to answer.
    private struct RootTarget
    {
        public Transform Root;
        public Color Color;

        public RootTarget(Transform root, Color color)
        {
            Root = root;
            Color = color;
        }
    }

    private void CollectRoots(List<RootTarget> roots)
    {
        HexRManager manager = HexRManager.Instance != null ? HexRManager.Instance : HexRCompat.FindAny<HexRManager>();

        if (manager != null)
        {
            AddHandRoots(roots, manager.leftHand);
            AddHandRoots(roots, manager.rightHand);
        }

        foreach (Transform extra in extraRoots)
        {
            if (extra != null)
            {
                roots.Add(new RootTarget(extra, extraRootColor));
            }
        }

        // Backwards compatibility: instances that predate this rewrite were added directly to a
        // hand root and relied on scanning their own children. If nothing else resolved, keep
        // doing that rather than drawing nothing.
        if (roots.Count == 0)
        {
            roots.Add(new RootTarget(transform, trackedHandColor));
        }
    }

    private void AddHandRoots(List<RootTarget> roots, HaptGloveHandler hand)
    {
        if (hand == null)
        {
            return;
        }

        PhysicsHandTrackingOpenXR openXR = hand.GetComponent<PhysicsHandTrackingOpenXR>();
        PhysicsHandTracking legacy = hand.GetComponent<PhysicsHandTracking>();

        // Prefer the OpenXR component's root: on a v201 rig it is the one being re-pointed at
        // each scene's hands, and the legacy component is switched off behind it.
        if (showTrackedHands)
        {
            Transform tracked = openXR != null && openXR.handRoot != null ? openXR.handRoot
                : legacy != null ? legacy.handRoot : null;
            if (tracked != null)
            {
                roots.Add(new RootTarget(tracked, trackedHandColor));
            }
        }

        if (showGhostRigs)
        {
            Transform ghost = openXR != null && openXR.HexrRoot != null ? openXR.HexrRoot
                : legacy != null ? legacy.HexrRoot : null;
            if (ghost != null)
            {
                roots.Add(new RootTarget(ghost, ghostRigColor));
            }
        }
    }

    private static bool HasVisualizerChild(Transform colliderTransform)
    {
        foreach (Transform child in colliderTransform)
        {
            if (child.name == VisualizerName)
            {
                return true;
            }
        }
        return false;
    }

    private GameObject BuildSolid(Collider collider, Color color)
    {
        Mesh mesh = CreateColliderMesh(collider);
        if (mesh == null)
        {
            return null;
        }

        GameObject solid = new GameObject(VisualizerName);
        solid.transform.SetParent(collider.transform, false);
        solid.transform.localPosition = GetColliderLocalPosition(collider);
        solid.transform.localRotation = GetColliderLocalRotation(collider);
        solid.transform.localScale = GetColliderLocalScale(collider);
        // So a hand rig on a layer the camera renders keeps its solids visible, and one on a
        // hidden layer hides them too -- matching whatever the collider itself does.
        solid.layer = collider.gameObject.layer;

        solid.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = solid.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = GetMaterial(color);
        // Diagnostic geometry -- it should never darken the scene it is being inspected in.
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

        return solid;
    }

    private Material GetMaterial(Color color)
    {
        Material material;
        if (materials.TryGetValue(color, out material) && material != null)
        {
            return material;
        }

        // Explicit null checks rather than ??, because Unity overloads == on Object and the
        // null-coalescing operator bypasses that overload.
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("[HexR] " + name + ": couldn't find a shader to draw collider solids with. "
                + "On a built player, add the shader you want to Project Settings > Graphics > Always Included Shaders.");
            return null;
        }

        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        material.color = color;
        // Material.color writes _Color, which URP's Lit does not have -- without this the solid
        // renders white there and every collider looks the same.
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        materials[color] = material;
        return material;
    }

    private static Mesh CreateColliderMesh(Collider collider)
    {
        if (collider is BoxCollider) return PrimitiveMesh(PrimitiveType.Cube);
        if (collider is SphereCollider) return PrimitiveMesh(PrimitiveType.Sphere);
        if (collider is CapsuleCollider) return PrimitiveMesh(PrimitiveType.Capsule);

        MeshCollider meshCollider = collider as MeshCollider;
        return meshCollider != null ? meshCollider.sharedMesh : null;
    }

    private static Vector3 GetColliderLocalPosition(Collider collider)
    {
        BoxCollider box = collider as BoxCollider;
        if (box != null) return box.center;
        SphereCollider sphere = collider as SphereCollider;
        if (sphere != null) return sphere.center;
        CapsuleCollider capsule = collider as CapsuleCollider;
        if (capsule != null) return capsule.center;
        return Vector3.zero;
    }

    // Unity's capsule primitive stands on Y. CapsuleCollider.direction can be X (0) or Z (2)
    // instead, which the old version ignored -- a sideways capsule was drawn upright.
    private static Quaternion GetColliderLocalRotation(Collider collider)
    {
        CapsuleCollider capsule = collider as CapsuleCollider;
        if (capsule == null)
        {
            return Quaternion.identity;
        }

        switch (capsule.direction)
        {
            case 0: return Quaternion.Euler(0f, 0f, 90f);
            case 2: return Quaternion.Euler(90f, 0f, 0f);
            default: return Quaternion.identity;
        }
    }

    private static Vector3 GetColliderLocalScale(Collider collider)
    {
        BoxCollider box = collider as BoxCollider;
        if (box != null) return box.size;

        SphereCollider sphere = collider as SphereCollider;
        if (sphere != null) return Vector3.one * (sphere.radius * 2f);

        // The primitive is 2 units tall, so half the collider's height gives the right length.
        CapsuleCollider capsule = collider as CapsuleCollider;
        if (capsule != null) return new Vector3(capsule.radius * 2f, capsule.height * 0.5f, capsule.radius * 2f);

        return Vector3.one;
    }

    private static readonly Dictionary<PrimitiveType, Mesh> primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();

    // GameObject.CreatePrimitive ships a Collider with the primitive, and Destroy only takes
    // effect at end of frame -- so the old version briefly parked a 1m solid collider at the
    // world origin every single time you pressed the toggle, once per collider drawn. On a rig
    // sitting near the origin that is enough to fire real haptics. Deactivate it the moment it
    // exists, and cache the mesh so it happens once per primitive type per session rather than
    // dozens of times per toggle.
    private static Mesh PrimitiveMesh(PrimitiveType type)
    {
        Mesh cached;
        if (primitiveMeshes.TryGetValue(type, out cached) && cached != null)
        {
            return cached;
        }

        GameObject temp = GameObject.CreatePrimitive(type);
        temp.SetActive(false);
        // sharedMesh is one of Unity's built-in assets, so it outlives the object it came from.
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);

        primitiveMeshes[type] = mesh;
        return mesh;
    }
}
