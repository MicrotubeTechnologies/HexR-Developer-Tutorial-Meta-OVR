using UnityEngine;
using System;
using System.Collections.Generic;
using HexR;
/// <summary>
/// Put this on EACH gem GameObject (or the child that has the trigger collider).
/// Requires a trigger collider.
/// On trigger enter, it notifies the GemStageManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GemTouchTrigger : MonoBehaviour
{
    [SerializeField] private Gems_Popper gems_Popper;

    private GameObject gemRoot;
    private SpecialHaptics specialHaptics;
    private GemColorAssignedAction GCAA;
    public AudioSource PositiveAudio;
    private void Awake()
    {
        gemRoot = gameObject;
        GCAA = gameObject.GetComponent<GemColorAssignedAction>();
        // If this script is on a child collider object, you might want the parent as the "gem"
        // Uncomment the next line if you prefer parent to be the gem object:
        // gemRoot = transform.root.gameObject;

        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"{name}: Collider is not set as Trigger. Touch won't work unless isTrigger = true.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gems_Popper == null) return;
        gems_Popper.NotifyGemTouched(gemRoot, other);
        if(GCAA.isBlue)
        {
            PositiveAudio.Play();
        }
    }
}

