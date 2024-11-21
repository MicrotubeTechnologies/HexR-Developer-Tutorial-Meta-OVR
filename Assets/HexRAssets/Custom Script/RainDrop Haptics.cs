using HaptGlove;
using HexR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainDropHaptics : MonoBehaviour
{
    public PressureTrackerMain Rightpressuretracker, LeftPressureTracker;
    private bool ReadyToDrop = true, RemoveIt = false;
    // Start is called before the first frame update

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "RightGhostPalm" && ReadyToDrop == true)
        {
            ReadyToDrop = false;
            RemoveIt = false;
            HaptGloveHandler gloveHandler = Rightpressuretracker.GetComponent<HaptGloveHandler>();
            RaindropEffect(Random.Range(1, 9), gloveHandler);
            StartCoroutine(RestartHaptic());
            StartCoroutine(RemoveHaptic(Rightpressuretracker));
        }
        if (other.gameObject.name == "LeftGhostPalm" && ReadyToDrop == true)
        {
            ReadyToDrop = false;
            RemoveIt = false;
            HaptGloveHandler gloveHandler = LeftPressureTracker.GetComponent<HaptGloveHandler>();
            RaindropEffect(Random.Range(1, 9), gloveHandler);
            StartCoroutine(RestartHaptic());
            StartCoroutine(RemoveHaptic(Rightpressuretracker));
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name == "RightGhostPalm" && ReadyToDrop == true)
        {
            ReadyToDrop = false;
            RemoveIt = false;
            HaptGloveHandler gloveHandler = Rightpressuretracker.GetComponent<HaptGloveHandler>();
            RaindropEffect(Random.Range(1, 9), gloveHandler);
            StartCoroutine(RestartHaptic());
            StartCoroutine(RemoveHaptic(Rightpressuretracker));
        }
        if (other.gameObject.name == "LeftGhostPalm" && ReadyToDrop == true)
        {
            ReadyToDrop = false;
            RemoveIt = false;
            HaptGloveHandler gloveHandler = LeftPressureTracker.GetComponent<HaptGloveHandler>();
            RaindropEffect(Random.Range(1, 9), gloveHandler);
            StartCoroutine(RestartHaptic());
            StartCoroutine(RemoveHaptic(Rightpressuretracker));
        }
    }
    IEnumerator RestartHaptic()
    {
        yield return new WaitForSeconds(0.2f);
        ReadyToDrop = true;
    }
    IEnumerator RemoveHaptic(PressureTrackerMain PressureTracker)
    {
        RemoveIt = true;
        // Wait for the specified delay time
        yield return new WaitForSeconds(0.4f);
        if (RemoveIt == true)
        {
            PressureTracker?.RemoveAllHaptics();
            RemoveIt = false;
        }
        else
        {
            RemoveHaptic(PressureTracker);
        }
        // Wait for the specified delay time
    }
    public void RaindropEffect(int Pattern, HaptGloveHandler gloveHandler)
    {
        byte Pressure = (byte)40; // 10 to 60

        // ClutchState affecting all indenters
        if (Pattern == 1)
        {
            // thumb Pinky
            byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 2 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }, new byte[] { 4, 0 }, new byte[] { 5, 2 } };
            byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
            gloveHandler.BTSend(btData);
        }
        else if (Pattern == 2)
        {
            // Index middle ring
            byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 0 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 2 }, new byte[] { 5, 2 } };
            byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
            gloveHandler.BTSend(btData);
        }
        else if (Pattern == 3)
        {
            // Palm Middle
            byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 0 }, new byte[] { 3, 2 }, new byte[] { 4, 2 }, new byte[] { 5, 0 } };
            byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
            gloveHandler.BTSend(btData);
        }
        else if (Pattern == 4)
        {
            // Index Thumb
            byte[][] ClutchState = new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }, new byte[] { 4, 2 }, new byte[] { 5, 2 } };
            byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
            gloveHandler.BTSend(btData);
        }
        else if (Pattern == 5)
        {
            // ring middle
            byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 0 }, new byte[] { 3, 0 }, new byte[] { 4, 2 }, new byte[] { 5, 2 } };
            byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
            gloveHandler.BTSend(btData);
        }
        else if (Pattern == 6)
        {
            // Palm
            byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }, new byte[] { 4, 2 }, new byte[] { 5, 0 } };
            byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
            gloveHandler.BTSend(btData);
        }
        else if (Pattern == 7)
        {
            //middle little
            byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 }, new byte[] { 2, 0 }, new byte[] { 3, 2 }, new byte[] { 4, 0 }, new byte[] { 5, 2 } };
            byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
            gloveHandler.BTSend(btData);
        }
        else if (Pattern == 8)
        {
            //Index little
            byte[][] ClutchState = new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 0 }, new byte[] { 2, 2 }, new byte[] { 3, 2 }, new byte[] { 4, 0 }, new byte[] { 5, 2 } };
            byte[] btData = gloveHandler.haptics.ApplyHaptics(ClutchState, Pressure, false);
            gloveHandler.BTSend(btData);
        }
    }

}
