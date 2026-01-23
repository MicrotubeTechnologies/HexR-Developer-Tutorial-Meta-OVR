using UnityEngine;
using UnityEngine.Events;
using HexR;
/// <summary>
/// Calls events when the gem is ASSIGNED to Blue/Red by the stage manager.
/// (Not on touch.)
/// </summary>
public class GemColorAssignedAction : MonoBehaviour
{
    [Header("Current Role (set by Stage Manager)")]
    [SerializeField] public bool isBlue = true;

    [Header("Events fired on assignment")]
    public UnityEvent onAssignedBlue;
    public UnityEvent onAssignedRed;

    [Header("Options")]
    [Tooltip("If true, will not re-fire events if the role is assigned to the same value again.")]
    [SerializeField] private bool onlyFireWhenChanged = true;

    public void SetIsBlue(bool value)
    {
        if (onlyFireWhenChanged && value == isBlue)
            return;

        isBlue = value;

        if (isBlue)
            onAssignedBlue?.Invoke();
        else
            onAssignedRed?.Invoke();
    }
}

