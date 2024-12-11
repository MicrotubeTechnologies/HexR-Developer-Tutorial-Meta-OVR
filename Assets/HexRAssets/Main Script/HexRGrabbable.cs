using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexR;
using TMPro;
using UnityEngine.Events;
using static HexR.MetaHapMaterial;
using UnityEngine.EventSystems;
using System;
using HaptGlove;
using UnityEditor;
public class HexRGrabbable : MonoBehaviour
{

    public enum Options { PinchGrab, PalmGrab }
    [Header("General Settings")]
    [Space(10)]
    public Options TypeOfGrab;
    public enum Option { On, Off }
    public Option Gravity;

    [Range(0f, 60f)]
    public float HapticStrength = 10f;

    [Header("HexR Events Triggers")]
    [Space(10)]
    public UnityEvent OnGrab, OnRelease;


    private GameObject RHandParent, LHandParent;
    private GameObject OriginalParent;
    bool RThumb, RIndex, RLittle, RMiddle, RRing, RPalm;
    bool LThumb, LIndex, LLittle, LMiddle, LRing, LPalm;
    bool RThumbHaptics, RIndexHaptics, RLittleHaptics, RMiddleHaptics, RRingHaptics, RPalmHaptics;
    bool LThumbHaptics, LIndexHaptics, LLittleHaptics, LMiddleHaptics, LRingHaptics, LPalmHaptics;
    private FingerUseTracking RfingerUseTracking,LfingeruseTracking;
    private PressureTrackerMain RightPressureTracker, LeftPressureTracker;
    private Rigidbody objectRigidbody;
    private bool HapticIsTriggered = false;
    [HideInInspector]
    public bool isGrab = false, InvokeReady = true;
    // Start is called before the first frame update
    void Start()
    {
        GameObject RightHand = GameObject.Find("Right Hand Physics");
        GameObject LeftHand = GameObject.Find("Left Hand Physics");

        // Create new empty objects with unique names for right and left hand parents
        RHandParent = new GameObject("RightHandParent");
        LHandParent = new GameObject("LeftHandParent");

        if (RightHand != null) { RfingerUseTracking = RightHand.GetComponent<FingerUseTracking>(); }
        else { Debug.Log("Right hand is not found"); }
        if (RightHand != null) { RightPressureTracker = RightHand.GetComponent<PressureTrackerMain>(); }
        else { Debug.Log("Right pressuretracker is not found"); }
        if (LeftHand != null) { LfingeruseTracking = LeftHand.GetComponent<FingerUseTracking>(); }
        else { Debug.Log("Left hand is not found"); }
        if (LeftHand != null) { LeftPressureTracker = LeftHand.GetComponent<PressureTrackerMain>(); }
        else { Debug.Log("Left pressuretracker is not found"); }

        objectRigidbody = gameObject.GetComponent<Rigidbody>();
        OriginalParent = gameObject.transform.parent.gameObject;
        RThumb = false; LThumb = false;
        RIndex = false; LIndex = false;
        RMiddle = false; LMiddle = false;
        RRing = false; LRing = false;
        RLittle = false; LLittle = false;
        RThumbHaptics = false; LThumbHaptics = false;
        RIndexHaptics = false; LIndexHaptics = false;
        RMiddleHaptics = false; LMiddleHaptics = false;
        RRingHaptics = false; LRingHaptics = false;
        RLittleHaptics = false; LLittleHaptics = false;

    }

    // Update is called once per frame
    void Update()
    {

        if (TypeOfGrab == Options.PinchGrab)
        {
            if (RThumb)
            {
                if (RIndex || RMiddle || RRing || RLittle)
                {
                    isGrab = true;
                    IsGrab(RHandParent, RfingerUseTracking,RightPressureTracker,false);
                }
                else
                {
                    isGrab = false;
                    NotGrab(RightPressureTracker);
                }

            }
            if (LThumb)
            {
                if (LIndex || LMiddle || LRing || LLittle)
                {
                    isGrab = true;
                    IsGrab(LHandParent, LfingeruseTracking,LeftPressureTracker, true);
                }
                else
                {
                    isGrab = false;
                    NotGrab(LeftPressureTracker);
                }

            }
        }
        else if(TypeOfGrab == Options.PalmGrab)
        {
            if (RPalm)
            {
                if (RIndex || RMiddle || RRing || RLittle)
                {
                    isGrab = true;
                    IsGrab(RHandParent, RfingerUseTracking, RightPressureTracker, false);
                }
                else
                {
                    isGrab = false;
                    NotGrab(RightPressureTracker);
                }

            }
            if (LPalm)
            {
                if (LIndex || LMiddle || LRing || LLittle)
                {
                    isGrab = true;
                    IsGrab(LHandParent, LfingeruseTracking, LeftPressureTracker, true);
                }
                else
                {
                    isGrab = false;
                    NotGrab(LeftPressureTracker);
                }

            }
        }

    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.parent.name == "R_IndexTip")
        {
            RIndex = true;
            StartCoroutine(ResetFinger(RIndex));
        }
        if (collision.transform.parent.name == "R_LittleTip")
        {
            RLittle = true;
            StartCoroutine(ResetFinger(RLittle));
        }
        if (collision.transform.parent.name == "R_MiddleTip")
        {
            RMiddle = true;
            StartCoroutine(ResetFinger(RMiddle));
        }
        if (collision.transform.parent.name == "R_RingTip")
        {
            RRing = true;
            StartCoroutine(ResetFinger(RRing));
        }
        if (collision.transform.parent.name == "R_ThumbTip")
        {
            if(TypeOfGrab == Options.PinchGrab)
            {
                Vector3 contactPoint = collision.ClosestPoint(transform.position);
                RHandParent.transform.position = contactPoint;
                RHandParent.transform.parent = collision.transform;
            }
            RThumb = true;
        }
        if (collision.transform.name == "R_Palm")
        {
            if (TypeOfGrab == Options.PalmGrab)
            {
                Vector3 contactPoint = collision.ClosestPoint(transform.position);
                RHandParent.transform.position = contactPoint;
                RHandParent.transform.parent = collision.transform;
            }
            RPalm = true;
        }

        if (collision.transform.parent.name == "L_IndexTip")
        {
            LIndex = true;
            StartCoroutine(ResetFinger(LIndex));
        }
        if (collision.transform.parent.name == "L_LittleTip")
        {
            LLittle = true;
            StartCoroutine(ResetFinger(LLittle));
        }
        if (collision.transform.parent.name == "L_MiddleTip")
        {
            LMiddle = true;
            StartCoroutine(ResetFinger(LMiddle));
        }
        if (collision.transform.parent.name == "L_RingTip")
        {
            LRing = true;
            StartCoroutine(ResetFinger(LRing));
        }
        if (collision.transform.parent.name == "L_ThumbTip")
        {
            if(TypeOfGrab == Options.PinchGrab)
            {
                Vector3 contactPoint = collision.ClosestPoint(transform.position);
                LHandParent.transform.position = contactPoint;
                LHandParent.transform.parent = collision.transform;
            }
            LThumb = true;
        }
        if (collision.transform.name == "L_Palm")
        {
            if (TypeOfGrab == Options.PalmGrab)
            {
                Vector3 contactPoint = collision.ClosestPoint(transform.position);
                LHandParent.transform.position = contactPoint;
                LHandParent.transform.parent = collision.transform;
            }
            LPalm = true;
        }
    }
    private void OnTriggerStay(Collider collision)
    {
        if (collision.transform.parent.name == "R_IndexTip")
        {
            RIndex = true;
            StartCoroutine(ResetFinger(RIndex));
        }
        if (collision.transform.parent.name == "R_LittleTip")
        {
            RLittle = true;
            StartCoroutine(ResetFinger(RLittle));
        }
        if (collision.transform.parent.name == "R_MiddleTip")
        {
            RMiddle = true;
            StartCoroutine(ResetFinger(RMiddle));
        }
        if (collision.transform.parent.name == "R_RingTip")
        {
            RRing = true;
            StartCoroutine(ResetFinger(RRing));
        }
        if (collision.transform.parent.name == "R_ThumbTip")
        {
            if (TypeOfGrab == Options.PinchGrab)
            {
                Vector3 contactPoint = collision.ClosestPoint(transform.position);
                RHandParent.transform.position = contactPoint;
                RHandParent.transform.parent = collision.transform;
            }
            RThumb = true;
        }
        if (collision.transform.name == "R_Palm")
        {
            if (TypeOfGrab == Options.PalmGrab)
            {
                Vector3 contactPoint = collision.ClosestPoint(transform.position);
                RHandParent.transform.position = contactPoint;
                RHandParent.transform.parent = collision.transform;
            }
            RPalm = true;
        }

        if (collision.transform.parent.name == "L_IndexTip")
        {
            LIndex = true;
            StartCoroutine(ResetFinger(LIndex));
        }
        if (collision.transform.parent.name == "L_LittleTip")
        {
            LLittle = true;
            StartCoroutine(ResetFinger(LLittle));
        }
        if (collision.transform.parent.name == "L_MiddleTip")
        {
            LMiddle = true;
            StartCoroutine(ResetFinger(LMiddle));
        }
        if (collision.transform.parent.name == "L_RingTip")
        {
            LRing = true;
            StartCoroutine(ResetFinger(LRing));
        }
        if (collision.transform.parent.name == "L_ThumbTip")
        {
            if (TypeOfGrab == Options.PinchGrab)
            {
                Vector3 contactPoint = collision.ClosestPoint(transform.position);
                LHandParent.transform.position = contactPoint;
                LHandParent.transform.parent = collision.transform;
            }
            LThumb = true;
        }
        if (collision.transform.name == "L_Palm")
        {
            if (TypeOfGrab == Options.PalmGrab)
            {
                Vector3 contactPoint = collision.ClosestPoint(transform.position);
                LHandParent.transform.position = contactPoint;
                LHandParent.transform.parent = collision.transform;
            }
            LPalm = true;
        }
    }
    private void OnTriggerExit(Collider collision)
    {
        if (collision.transform.parent.name == "R_IndexTip")
        {
            RIndex = false;
        }
        if (collision.transform.parent.name == "R_LittleTip")
        {
            RLittle = false;
        }
        if (collision.transform.parent.name == "R_MiddleTip")
        {
            RMiddle = false;
        }
        if (collision.transform.parent.name == "R_RingTip")
        {
            RRing = false;
        }
        if (collision.transform.parent.name == "R_ThumbTip")
        {
            RThumb = false;
        }
        if (collision.transform.name == "R_Palm")
        {
            RPalm = false;
        }

        if (collision.transform.parent.name == "L_IndexTip")
        {
            LIndex = false;
        }
        if (collision.transform.parent.name == "L_LittleTip")
        {
            LLittle = false;
        }
        if (collision.transform.parent.name == "L_MiddleTip")
        {
            LMiddle = false;
        }
        if (collision.transform.parent.name == "L_RingTip")
        {
            LRing = false;
        }
        if (collision.transform.parent.name == "L_ThumbTip")
        {
            LThumb = false;
        }
        if (collision.transform.name == "L_Palm")
        {
            LPalm = false;
        }
    }
    private void IsGrab(GameObject HandParent, FingerUseTracking fingerUseTracking, PressureTrackerMain ThePressureTracker, bool IsLeft)
    {
        ThePressureTracker?.HandGrabbingCheck(true);
        gameObject.transform.SetParent(HandParent.transform);

        #region Rigidbody Settings
        objectRigidbody.isKinematic = true;
        objectRigidbody.useGravity = false;
        objectRigidbody.interpolation = RigidbodyInterpolation.None;
        #endregion

        //Trigger Events
        if (isGrab && InvokeReady) 
        { OnGrab?.Invoke(); 
          InvokeReady = false;         
        }
        TriggerHaptics(ThePressureTracker, IsLeft);
        StartCoroutine(ResetGrab( fingerUseTracking, ThePressureTracker));
    }
    private void NotGrab(PressureTrackerMain ThePressureTracker)
    {
        ThePressureTracker?.HandGrabbingCheck(false);
        objectRigidbody.isKinematic = false;
        if (Gravity == Option.On) { objectRigidbody.useGravity = true; }
        objectRigidbody.interpolation = RigidbodyInterpolation.Extrapolate;

        gameObject.transform.SetParent(OriginalParent.transform);

        if (!InvokeReady) 
        { OnRelease?.Invoke(); 
            InvokeReady = true;
            RemoveHaptics(ThePressureTracker);
        }
    }
    private void TriggerHaptics(PressureTrackerMain pressureTrackerMain, bool IsLeft)
    {
        if(HapticStrength != 0 && !HapticIsTriggered)
        {
            HapticIsTriggered = true;
            // Boolean states for right hand and left hand
            byte[][] ClutchState = new byte[0][]; // Start with an empty array
            if (IsLeft)
            {
                bool[] fingerStates = { LThumb, LIndex, LMiddle, LRing, LLittle, LPalm }; // array of fingers touching object
                bool[] HapticStates = { LThumbHaptics, LIndexHaptics, LMiddleHaptics, LRingHaptics, LLittleHaptics, LPalmHaptics }; // array of which haptic is triggered
                // Check each boolean and add its clutch state if true
                for (int i = 0; i < fingerStates.Length; i++)
                {
                    if (fingerStates[i] && !HapticStates[i])
                    {
                        HapticStates[i] = true;
                        // Expand the ClutchState array and add the new byte[]
                        Array.Resize(ref ClutchState, ClutchState.Length + 1);
                        ClutchState[ClutchState.Length - 1] = new byte[] { (byte)i, 0 };
                    }
                    else if (!fingerStates[i] && HapticStates[i])
                    {
                        HapticStates[i] = false;
                        // Expand the ClutchState array and add the new byte[]
                        Array.Resize(ref ClutchState, ClutchState.Length + 1);
                        ClutchState[ClutchState.Length - 1] = new byte[] { (byte)i, 2 };
                    }
                }
            }
            else
            {
                bool[] fingerStates = { RThumb, RIndex, RMiddle, RRing, RLittle, RPalm };
                bool[] HapticStates = { RThumbHaptics, RIndexHaptics, RMiddleHaptics, RRingHaptics, RLittleHaptics, RPalmHaptics };
                // Check each boolean and add its clutch state if true
                for (int i = 0; i < fingerStates.Length; i++)
                {
                    if (fingerStates[i] && !HapticStates[i])
                    {
                        HapticStates[i] = true;
                        // Expand the ClutchState array and add the new byte[]
                        Array.Resize(ref ClutchState, ClutchState.Length + 1);
                        ClutchState[ClutchState.Length - 1] = new byte[] { (byte)i, 0 };
                    }
                    else if(!fingerStates[i] && HapticStates[i])
                    {
                        HapticStates[i] = false;
                        // Expand the ClutchState array and add the new byte[]
                        Array.Resize(ref ClutchState, ClutchState.Length + 1);
                        ClutchState[ClutchState.Length - 1] = new byte[] { (byte)i, 2 };
                    }
                }
            }

            // Send haptics data if there are any clutch states
            if (ClutchState.Length > 0)
            {
                pressureTrackerMain.TriggerCustomHapticsIncrease(ClutchState, (byte)HapticStrength);
            }
        }

    }
    private void RemoveHaptics(PressureTrackerMain pressureTrackerMain)
    {
        if (HapticStrength != 0 && HapticIsTriggered)
        {
            HapticIsTriggered = false;
            pressureTrackerMain.RemoveAllHaptics();
            bool[] HapticStates = { LThumbHaptics, LIndexHaptics, LMiddleHaptics, LRingHaptics, LLittleHaptics, LPalmHaptics, RThumbHaptics, RIndexHaptics, RMiddleHaptics, RRingHaptics, RLittleHaptics, RPalmHaptics };
            for (int i = 0; i < HapticStates.Length; i++)
            {
                HapticStates[i] = false;
            }
        }
    }
    IEnumerator ResetFinger(bool whichbool)
    {
        // Wait for the specified delay time
        yield return new WaitForSeconds(0.2f);
        whichbool = false;

    }
    IEnumerator ResetGrab(FingerUseTracking fingerUseTracking, PressureTrackerMain ThePressureTracker)
    {
        // Wait for the specified delay time
        yield return new WaitForSeconds(0.2f);
        if(fingerUseTracking.isHandOpen() == true)
        {
            NotGrab(ThePressureTracker);
        }
        else
        {
            isGrab = false;
            StartCoroutine(ResetGrab(fingerUseTracking, ThePressureTracker));
        }

    }
    private void OnValidate()
    {
        // Snap HapticStrength to the nearest increment of 10
        HapticStrength = Mathf.Round(HapticStrength / 10) * 10;
    }

}
