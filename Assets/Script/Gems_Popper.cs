using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
/// <summary>
/// Gem demo stage manager (11 gems):
/// - Stage 1 starts with 3 active gems
/// - As stages progress, more gems become active (up to 11)
/// - Active gems are randomly set to Blue or Red
/// - Red gems are NEVER more than 60% of active gems
/// - Stage completes when ALL BLUE gems have been touched at least once
/// - Touching RED gems does NOT penalize
///
/// Works with per-gem trigger colliders (see GemTouchTrigger below).
/// </summary>
public class Gems_Popper : MonoBehaviour
{
    [Header("Gems (exactly 11)")]
    [SerializeField] private GameObject[] gems = new GameObject[11];

    [Header("Materials")]
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material redMaterial;

    [Tooltip("Optional: if set, blue gems switch to this material when touched. If null, they stay blue.")]
    [SerializeField] private Material blueTouchedMaterial;

    [Header("Stages")]
    [SerializeField] private int totalStages = 10;
    [SerializeField] private bool autoStart = true;

    [Tooltip("Optional fixed seed for repeatable randomization. Set to 0 for random.")]
    [SerializeField] private int randomSeed = 0;


    [Header("Debug (readonly)")]
    [SerializeField] private int currentStage = 0;
    [SerializeField] private int activeGemCount = 0;
    [SerializeField] private int redGemCount = 0;
    [SerializeField] private int remainingBlueToTouch = 0;

    [Header("Game Complete")]
    public UnityEvent onAllStagesComplete;

    [SerializeField] private bool loopAfterComplete = false;

    private System.Random rng;

    // Lookup: gem -> isBlue (meaningful only if gem is active this stage)
    private readonly Dictionary<GameObject, bool> isBlueLookup = new Dictionary<GameObject, bool>();

    // Blue gems that must still be touched to complete the stage
    private readonly HashSet<GameObject> remainingBlue = new HashSet<GameObject>();

    private void Awake()
    {
        if (gems == null || gems.Length != 11)
            Debug.LogWarning($"{nameof(Gems_Popper)} expects exactly 11 gems assigned.");

        rng = (randomSeed != 0) ? new System.Random(randomSeed) : new System.Random();

        isBlueLookup.Clear();
        foreach (var g in gems)
        {
            if (g == null) continue;
            if (!isBlueLookup.ContainsKey(g))
                isBlueLookup.Add(g, false);
        }
    }

    private void Start()
    {

    }

    public void StartGame()
    {
        ResetAllGems();
        currentStage = 1;
        GenerateStage(currentStage);
    }

    public void RestartGame()
    {
        currentStage = 1;
        GenerateStage(currentStage);
    }

    public void NextStage()
    {
        if (currentStage <= 0) currentStage = 1;
        else currentStage++;

        // If we passed the last stage -> game complete
        if (currentStage > totalStages)
        {
            Debug.Log("ALL STAGES COMPLETE!");
            onAllStagesComplete?.Invoke();

            if (loopAfterComplete)
            {
                RestartGame();
            }
            else
            {
                currentStage = totalStages; // clamp
            }

            return;
        }

        GenerateStage(currentStage);
    }

    /// <summary>
    /// Called by GemTouchTrigger when its trigger is entered.
    /// </summary>
    public void NotifyGemTouched(GameObject gem, Collider toucher)
    {
        if (gem == null) return;
        if (!gem.activeInHierarchy) return;

        // Red touches do nothing (no penalty)
        if (!isBlueLookup.TryGetValue(gem, out bool isBlue) || !isBlue)
            return;

        // If already counted, ignore
        if (!remainingBlue.Contains(gem))
            return;

        // Mark as touched
        remainingBlue.Remove(gem);
        remainingBlueToTouch = remainingBlue.Count;

        // Optional: change material to show completion
        if (blueTouchedMaterial != null)
            ApplyMaterial(gem, blueTouchedMaterial);

        // Optional: disable this gem’s trigger to avoid repeated spam
        var col = gem.GetComponent<Collider>();
        if (col == null) col = gem.GetComponentInChildren<Collider>();
        if (col != null) col.enabled = false;

        // Stage complete?
        if (remainingBlue.Count == 0)
        {
            Debug.Log($"Stage {currentStage} complete! All blue gems touched.");
            NextStage();
        }
    }


    private void GenerateStage(int stage)
    {
        if (blueMaterial == null || redMaterial == null)
        {
            Debug.LogError("Assign Blue and Red materials in the inspector.");
            return;
        }

        // Stage 1 starts with 3 active gems, stage 10 ramps to 11
        int minActive = 3;
        int maxActive = Mathf.Min(11, gems.Length);
        float t = (totalStages <= 1) ? 1f : (stage - 1) / (float)(totalStages - 1);
        int activeCount = Mathf.RoundToInt(Mathf.Lerp(minActive, maxActive, t));
        activeCount = Mathf.Clamp(activeCount, minActive, maxActive);

        // Red ratio increases with stage but hard capped at 60%
        float targetRedRatio = Mathf.Lerp(0.10f, 0.60f, t);
        targetRedRatio = Mathf.Clamp(targetRedRatio, 0f, 0.60f);

        int hardMaxRed = Mathf.FloorToInt(0.60f * activeCount);
        int desiredRed = Mathf.RoundToInt(activeCount * targetRedRatio);
        int redCount = Mathf.Clamp(desiredRed, 0, hardMaxRed);

        // Always leave at least 1 blue so the stage can be completed
        int blueCount = activeCount - redCount;
        if (blueCount <= 0)
        {
            redCount = Mathf.Max(0, redCount - 1);
            blueCount = activeCount - redCount;
        }

        activeGemCount = activeCount;
        redGemCount = redCount;

        // Reset stage state
        remainingBlue.Clear();
        ResetAllGems();

        // Choose active gems
        List<int> indices = MakeIndexList(gems.Length);
        Shuffle(indices);
        List<int> activeIndices = indices.GetRange(0, activeCount);

        // Choose which actives are red
        Shuffle(activeIndices);
        HashSet<int> redIndices = new HashSet<int>();
        for (int i = 0; i < redCount; i++)
            redIndices.Add(activeIndices[i]);

        // Apply actives + colors + reset colliders
        for (int i = 0; i < gems.Length; i++)
        {
            var gem = gems[i];
            if (gem == null) continue;

            bool isActive = activeIndices.Contains(i);
            gem.SetActive(isActive);

            if (!isActive) continue;

            bool isRed = redIndices.Contains(i);
            bool isBlue = !isRed;

            // NEW: fire event when assigned
            GemColorAssignedAction assignedAction = gem.GetComponent<GemColorAssignedAction>();
            if (assignedAction == null) assignedAction = gem.GetComponentInChildren<GemColorAssignedAction>();
            if (assignedAction != null)
                assignedAction.SetIsBlue(isBlue);

            isBlueLookup[gem] = isBlue;

            ApplyMaterial(gem, isRed ? redMaterial : blueMaterial);

            // Ensure trigger collider is enabled (for the new stage)
            var col = gem.GetComponent<Collider>();
            if (col == null) col = gem.GetComponentInChildren<Collider>();
            if (col != null) col.enabled = true;

            // Track remaining blue gems
            if (isBlue)
                remainingBlue.Add(gem);
        }

        remainingBlueToTouch = remainingBlue.Count;

        Debug.Log(
            $"Stage {stage}/{totalStages} -> Active: {activeCount}, " +
            $"Red: {redCount} ({(activeCount > 0 ? 100f * redCount / activeCount : 0f):0.0}%), " +
            $"Blue to touch: {remainingBlueToTouch}"
        );
    }

    private void ResetAllGems()
    {
        for (int i = 0; i < gems.Length; i++)
        {
            if (gems[i] == null) continue;
            gems[i].SetActive(false);
        }
    }

    private void ApplyMaterial(GameObject gem, Material mat)
    {
        Renderer r = gem.GetComponent<Renderer>();
        if (r == null) r = gem.GetComponentInChildren<Renderer>();
        if (r == null)
        {
            Debug.LogWarning($"No Renderer found on gem '{gem.name}' or its children.");
            return;
        }

        // For demo simplicity. If you need per-instance changes, use r.material instead.
        r.sharedMaterial = mat;
    }

    private List<int> MakeIndexList(int count)
    {
        var list = new List<int>(count);
        for (int i = 0; i < count; i++) list.Add(i);
        return list;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

