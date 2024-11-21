using HaptGlove;
using HexR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FountainHaptics : MonoBehaviour
{
    public PressureTrackerMain Rightpressuretracker, LeftPressureTracker;
    private bool RemoveIt = false, IsTriggered = false;
    private byte[][] ClutchState;

    void Start()
    {
        ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 0 }, new byte[] { 5, 0 } };

    }
    private void OnTriggerEnter(Collider other)
    {
        if (IsTriggered == false)
        {
            if (other.gameObject.name == "RightGhostPalm")
            {
                IsTriggered = true;
                HaptGloveHandler gloveHandler = Rightpressuretracker.GetComponent<HaptGloveHandler>();
                FountainEffect(gloveHandler);
                StartCoroutine(RemoveHaptic(Rightpressuretracker));
            }
            if (other.gameObject.name == "LeftGhostPalm")
            {
                IsTriggered = true;
                HaptGloveHandler gloveHandler = LeftPressureTracker.GetComponent<HaptGloveHandler>();
                FountainEffect(gloveHandler);
                StartCoroutine(RemoveHaptic(LeftPressureTracker));
            }
            //StartCoroutine(FlipHaptic());
        }

        RemoveIt = false;
    }
    private void OnTriggerStay(Collider other)
    {
        if (IsTriggered == false)
        {
            if (other.gameObject.name == "RightGhostPalm")
            {
                IsTriggered = true;
                HaptGloveHandler gloveHandler = Rightpressuretracker.GetComponent<HaptGloveHandler>();
                FountainEffect(gloveHandler);
                StartCoroutine(RemoveHaptic(Rightpressuretracker));
            }
            if (other.gameObject.name == "LeftGhostPalm")
            {
                IsTriggered = true;
                HaptGloveHandler gloveHandler = LeftPressureTracker.GetComponent<HaptGloveHandler>();
                FountainEffect(gloveHandler);
                StartCoroutine(RemoveHaptic(LeftPressureTracker));
            }
            //StartCoroutine(FlipHaptic());
        }
        RemoveIt = false;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "RightGhostPalm")
        {
            Rightpressuretracker.RemoveAllVibrations();
            RemoveIt = false;
            IsTriggered = false;
        }
        if (other.gameObject.name == "LeftGhostPalm")
        {
            LeftPressureTracker.RemoveAllVibrations();
            RemoveIt = false;
            IsTriggered = false;
        }
    }
    IEnumerator RemoveHaptic(PressureTrackerMain PressureTracker)
    {
        RemoveIt = true;
        // Wait for the specified delay time
        yield return new WaitForSeconds(0.3f);
        if (RemoveIt == true)
        {
            PressureTracker?.RemoveAllVibrations();
            RemoveIt = false;
            IsTriggered = false;
        }
        else
        {
            StartCoroutine(RemoveHaptic(PressureTracker));
        }
        // Wait for the specified delay time
    }
    public void FountainEffect(HaptGloveHandler gloveHandler)
    {
        byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 0 }, new byte[] { 5, 0 } };
        byte[] btData = gloveHandler.haptics.ApplyHaptics(15, ClutchState, (byte)(30), false);
        gloveHandler.BTSend(btData);
    }
}
