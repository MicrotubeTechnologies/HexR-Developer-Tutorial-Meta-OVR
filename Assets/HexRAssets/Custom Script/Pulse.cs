using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HaptGlove;
using TMPro;
using HexR;
using Unity.VisualScripting;

public class Pulse : MonoBehaviour
{
    private byte AirPressure;
    public PressureTrackerMain leftPressureTrackerMain, rightPressureTrackerMain;
    private List<byte[]> fingerAffected = new List<byte[]>();
    private byte[][] totalFingerAffected;
    public float InTimer = 0.4f, OutTimer = 0f;
    public HeartBeat heartbeat;
    public bool PressureIn = true;
    public enum HeartBeat { Regular,Irregular};

    //This allows an object to send a haptic feedback to the hexr glove.
    //Place this script in the gameobject with a trigger collider.

    // Start is called before the first frame update
    void Start()
    {
        AirPressure = 40;
    }

    //Trigger 
    //0-5 (Thumb, Index, Middle, Ring, Pinky, Palm)
    private void OnTriggerEnter(Collider collider)
    {
        if(PressureIn == true)
        {
            if (collider.name.Contains("Index"))
            {
                fingerAffected.Add(new byte[] { 1, 0 });
            }
            if (collider.name.Contains("Middle"))
            {
                fingerAffected.Add(new byte[] { 2, 0 });
            }
            if (collider.name.Contains("Ring"))
            {
                fingerAffected.Add(new byte[] { 3, 0 });
            }
            if (collider.name.Contains("Pinky"))
            {
                fingerAffected.Add(new byte[] { 4, 0 });
            }
            foreach (var byteArray in fingerAffected)
            {
                Debug.Log(string.Join(", ", byteArray));
            }
            if (fingerAffected.Count > 0 )
            {
                totalFingerAffected = fingerAffected.ToArray();

                leftPressureTrackerMain.TriggerCustomHapticsIncrease(totalFingerAffected, AirPressure);
                rightPressureTrackerMain.TriggerCustomHapticsIncrease(totalFingerAffected, AirPressure);
                PressureIn = false;
                StartCoroutine(RemoveHaptic());
            }
        }

    }
    private void OnTriggerStay(Collider collider)
    {
        if (PressureIn == true)
        {
            if (collider.name.Contains("Index"))
            {
                fingerAffected.Add(new byte[] { 1, 0 });
            }
            if (collider.name.Contains("Middle"))
            {
                fingerAffected.Add(new byte[] { 2, 0 });
            }
            if (collider.name.Contains("Ring"))
            {
                fingerAffected.Add(new byte[] { 3, 0 });
            }
            if (collider.name.Contains("Pinky"))
            {
                fingerAffected.Add(new byte[] { 4, 0 });
            }
            foreach (var byteArray in fingerAffected)
            {
                Debug.Log(string.Join(", ", byteArray));
            }
            if (fingerAffected.Count > 0 )
            {
                totalFingerAffected = fingerAffected.ToArray();

                leftPressureTrackerMain.TriggerCustomHapticsIncrease(totalFingerAffected, AirPressure);
                rightPressureTrackerMain.TriggerCustomHapticsIncrease(totalFingerAffected, AirPressure);
                PressureIn = false;
                StartCoroutine(RemoveHaptic());
            }
        }
    }
    IEnumerator RemoveHaptic()
    {
        // Wait for the specified delay time
        if(heartbeat== HeartBeat.Regular)
        {
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            yield return new WaitForSeconds(Random.Range(0.2f, 0.4f));
        }
        fingerAffected.Clear();
        totalFingerAffected = null;
        leftPressureTrackerMain.RemoveAllHaptics();
        rightPressureTrackerMain.RemoveAllHaptics();
        StartCoroutine(ReadyHaptic());

    }
    IEnumerator ReadyHaptic()
    {
        if (heartbeat == HeartBeat.Regular)
        {
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return new WaitForSeconds(Random.Range(0.4f, 0.7f));
        }
        PressureIn = true;
    }

    public void ToggleHeartRegularity()
    {
        if(heartbeat == HeartBeat.Irregular)
        {
            heartbeat = HeartBeat.Regular;
        }
        else
        {
            heartbeat = HeartBeat.Irregular;
        }
    }

}
