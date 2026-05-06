using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexR;
using HaptGlove;
public class CustomTrigger : MonoBehaviour
{
    public PressureTrackerMain RPressureTracker, LPressureTracker;
    public float HapticStrenngthValue = 0.5f;
    private HaptGloveHandler RightHaptGloveHandler, LeftHaptGloveHandler;

    private bool Thumb_Bool = false, Index_Bool = false, Middle_Bool = false, Ring_Bool = false, Pinky_Bool = false, Palm_Bool = false, Right_Bool = false, Left_Bool = false;
    private float timer = 0.2f;

    // Index=0, Middle=1, Pinky=2, Ring=3, Thumb=4, Palm=5

    void Start()
    {
        if (RPressureTracker != null)
        {
            RightHaptGloveHandler = GameObject.Find("Right Hand Physics").GetComponent<HaptGloveHandler>();

        }
        else { Debug.Log("Right hand is not found"); }

        if (LPressureTracker != null)
        {
            LeftHaptGloveHandler = GameObject.Find("Left Hand Physics").GetComponent<HaptGloveHandler>();
        }
        else { Debug.Log("Left hand is not found"); }
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
    }
    private void OnEnable()
    {
        StartCoroutine(Haptic()); 
    }
    private void OnTriggerEnter(Collider other)
    {
        CustomPressureEnter(other);
    }
    private void OnTriggerStay(Collider other)
    {
        CustomPressureEnter(other);
    }
    private void OnTriggerExit(Collider other)
    {
        CustomPressureExit(other);
    }

    #region Custom Pressure
    public void CustomPressureEnter(Collider collider)
    {
        if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger))
        {
            TurnOnFingerBool(collider);
        }
    }
    private void CustomPressureExit(Collider collider)
    {
        if (collider.gameObject.TryGetComponent(out HapticFingerTrigger hapticFingerTrigger))
        {
            TurnOffFingerBool(collider);
        }
    }

    IEnumerator Haptic()
    {
        if (timer <= 0)
        {
            if (Right_Bool)
            {
                TriggerHaptic(LeftHaptGloveHandler); // wrong on purpose for mexico university having only left arm module
            }
            else if (Left_Bool)
            {
                TriggerHaptic(RightHaptGloveHandler);
            }
            timer = 0.2f;
        }
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(Haptic());
    }

    private void TriggerHaptic(HaptGloveHandler gloveHandler)
    {
        if (Thumb_Bool || Index_Bool || Middle_Bool || Ring_Bool || Pinky_Bool || Palm_Bool)
        {
            Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

            float[] ThePressure = new float[] { HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue, HapticStrenngthValue };
            float[] TheSpeed = new float[] { 1, 1, 1, 1, 1, 1 };
            bool[] FingerToTrigger = new bool[] { Thumb_Bool, Index_Bool, Middle_Bool, Ring_Bool, Pinky_Bool, Palm_Bool };
            byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, FingerToTrigger, ThePressure, TheSpeed);
            gloveHandler.BTSend(btData);
            ResetFingerBool();
        }
        else
        {
            Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

            float[] TheSpeed = new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
            float[] ThePressure = new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
            bool[] FingerToTrigger = new bool[] { false, false, false, false, false, false };
            byte[] btData = gloveHandler.haptics.HEXRPressure(AllFingers, FingerToTrigger, ThePressure, TheSpeed);
            gloveHandler.BTSend(btData);
            ResetFingerBool();
        }
    }

    #endregion

    #region Helper Functions

    private void TurnOnFingerBool(Collider collider)
    {
        if (collider.name.Contains("Thumb"))
        {
            Thumb_Bool = true;
        }
        if (collider.name.Contains("Index"))
        {
            Index_Bool = true;
        }
        if (collider.name.Contains("Middle"))
        {
            Middle_Bool = true;
        }
        if (collider.name.Contains("Ring"))
        {
            Ring_Bool = true;
        }
        if (collider.name.Contains("Pinky") || collider.name.Contains("Little"))
        {
            Pinky_Bool = true;
        }
        if (collider.name.Contains("L_"))
        {
            Left_Bool = true;
        }
        if (collider.name.Contains("R_"))
        {
            Right_Bool = true;
        }
        if (collider.name.Contains("Palm"))
        {
            Palm_Bool = true;
        }
    }

    private void TurnOffFingerBool(Collider collider)
    {
        if (collider.name.Contains("Thumb"))
        {
            Thumb_Bool = false;
        }
        if (collider.name.Contains("Index"))
        {
            Index_Bool = false;
        }
        if (collider.name.Contains("Middle"))
        {
            Middle_Bool = false;
        }
        if (collider.name.Contains("Ring"))
        {
            Ring_Bool = false;
        }
        if (collider.name.Contains("Pinky") || collider.name.Contains("Little"))
        {
            Pinky_Bool = false;
        }
        if (collider.name.Contains("Palm"))
        {
            Palm_Bool = false;
        }
    }

    private void ResetFingerBool()
    {
        Thumb_Bool = Index_Bool = Middle_Bool = Ring_Bool = Pinky_Bool = Palm_Bool = Right_Bool = Left_Bool = false;
    }
    #endregion
}
