using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexR;
using UnityEngine.Events;

public class HexRUsable : MonoBehaviour
{
    [Tooltip("Select the finger that is use to trigger an event")]
    public enum Options { Needed, NotNeeded }

    public Options Thumb, IndexFinger, MiddleFinger, RingFinger, LittleFinger;

    [Tooltip("0 is when finger is fully curled, 1 is when finger is fully extended")]
    [Range(0f, 1f)] // Creates a slider from 0 to 100
    public float UseThreshold = 0.3f;

    public UnityEvent WhenUseTriggerEvents, WhenStopUsingTriggerEvents;

    private FingerUseTracking RfingerUseTracking, LfingeruseTracking, Currentfingerusetracking;
    private bool beinguse = false;

    // Start is called before the first frame update
    void Start()
    {
        FingerUseTrackingSetUp();
        Currentfingerusetracking = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (Currentfingerusetracking != null)
        {
            if (beinguse == false)
            {
                UseChecker();
            }
            else
            {
                NotUseChecker();
            }
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.name.Contains("R_"))
        {
            Currentfingerusetracking = RfingerUseTracking;
        }
        if (other.name.Contains("L_"))
        {
            Currentfingerusetracking = LfingeruseTracking;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.name.Contains("R_"))
        {
            Currentfingerusetracking = RfingerUseTracking;
        }
        if (other.name.Contains("L_"))
        {
            Currentfingerusetracking = LfingeruseTracking;
        }
    }
    public void UseChecker()
    {
        bool Check = false;
        if(Thumb == Options.Needed)
        {
            if(Currentfingerusetracking.ThumbUse < UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if (IndexFinger == Options.Needed)
        {
            if (Currentfingerusetracking.IndexUse < UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if (MiddleFinger == Options.Needed)
        {
            if (Currentfingerusetracking.MiddleUse < UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if (RingFinger == Options.Needed)
        {
            if (Currentfingerusetracking.RingUse < UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if (LittleFinger == Options.Needed)
        {
            if (Currentfingerusetracking.LittleUse < UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if(Check ==true)
        {
            beinguse = true;
            WhenUseTriggerEvents?.Invoke();
            Currentfingerusetracking = null;
        }
        NotUseChecker();
    }
    public void NotUseChecker()
    {
        bool Check = false;
        if (Thumb == Options.Needed)
        {
            if (Currentfingerusetracking.ThumbUse > UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if (IndexFinger == Options.Needed)
        {
            if (Currentfingerusetracking.IndexUse > UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if (MiddleFinger == Options.Needed)
        {
            if (Currentfingerusetracking.MiddleUse > UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if (RingFinger == Options.Needed)
        {
            if (Currentfingerusetracking.RingUse > UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if (LittleFinger == Options.Needed)
        {
            if (Currentfingerusetracking.LittleUse > UseThreshold)
            {
                Check = true;
            }
            else
            {
                Check = false;
            }
        }

        if (Check == true)
        {
            beinguse = false;
            WhenStopUsingTriggerEvents?.Invoke();
            Currentfingerusetracking = null;
        }
    }
    private void FingerUseTrackingSetUp()
    {
        GameObject RightHand = GameObject.Find("Right Hand Physics");
        GameObject LeftHand = GameObject.Find("Left Hand Physics");

        if (RightHand != null) { RfingerUseTracking = RightHand.GetComponent<FingerUseTracking>(); }
        else { Debug.Log("Right hand is not found"); }

        if (LeftHand != null) { LfingeruseTracking = LeftHand.GetComponent<FingerUseTracking>(); }
        else { Debug.Log("Left hand is not found"); }
    }
    
}
