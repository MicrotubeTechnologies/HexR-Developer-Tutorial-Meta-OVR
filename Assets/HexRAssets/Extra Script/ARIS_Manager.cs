using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HaptGlove;
using TMPro;
using System;
using HexR;
using UnityEngine.EventSystems;

public class ARIS_Manager : MonoBehaviour
{
    public TextMeshProUGUI Connected;
    public ARISHandler ARISHandler;
    public HaptGloveHandler RGloveHandler, LGloveHandler;
    public List<TextMeshProUGUI> DebugList = new List<TextMeshProUGUI>();

    private float[] stretchSensors = new float[5];
    private float[] normalizedSensors = new float[5];
    private float[] minValues = new float[5];
    private float[] maxValues = new float[5];
    private float[] smoothedValues = new float[5];

    [Header("Calibration Settings")]
    public float calibrationMargin = 0.01f;

    private bool HapticsToggle = false;
    [Range(0f, 1f)]
    public float smoothingFactor = 0.1f;

    void Start()
    {
        StartCoroutine((TriggerHaptics()));
        // Initialize min/max
        for (int i = 0; i < 5; i++)
        {
            minValues[i] = float.MaxValue;
            maxValues[i] = float.MinValue;
            smoothedValues[i] = 0f;
        }
    }

    void Update()
    {
        
        if (ARISHandler._connected)
        {
            Connected.text = "Connected | Haptics:" + HapticsToggle.ToString();
            for (int i = 0; i < 5; i++)
            {
                if(ARISHandler.sensorArray[i] < 1000000)
                {
                    stretchSensors[i] = ARISHandler.sensorArray[i];

                    UpdateCalibration(i, stretchSensors[i]);
                    float NormalizeValue = Normalize(i);
                    if(NormalizeValue > 0.1)
                    {
                        normalizedSensors[i] = NormalizeValue;
                    }
                    else
                    {
                        normalizedSensors[i] = 0;
                    }
                    // Display raw + normalized (you can customize this)
                    DebugList[i].text = $"Raw: {stretchSensors[i]:F1}   Norm%: {normalizedSensors[i]:F1}";
                }
            }
        }
        else
        {
            Connected.text = "Not Connected | Haptics:" + HapticsToggle.ToString();
        }
    }

    public void ReZero()
    {
        for (int i = 0; i < 5; i++)
        {
            minValues[i] = ARISHandler.sensorArray[i];
            maxValues[i] = ARISHandler.sensorArray[i];
            normalizedSensors[i] = 0f;
        }
    }
    void UpdateCalibration(int index, float value)
    {
        if (value < minValues[index])
        {
            minValues[index] = value;
        }


        if (value > maxValues[index])
        {
            maxValues[index] = value;
        }

    }
    private float Normalize(int index)
    {
        float Normalize = 0;
        if (maxValues[index] - minValues[index] != 0)
        {
            Normalize = (stretchSensors[index] - minValues[index]) / (maxValues[index] - minValues[index]);
        }
        
        return Normalize;
    }

    IEnumerator TriggerHaptics()
    {
        if (HapticsToggle)
        {
            TriggerLeft();
            TriggerRight();
        }
        yield return new WaitForSeconds(0.4f);
        StartCoroutine((TriggerHaptics()));
    }
    public void ToggleHaptics()
    {
        if(HapticsToggle)
        {
            HapticsToggle = false;
        }
        else
        {
            HapticsToggle = true;
        }
    }
    private void TriggerLeft()
    {
        Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

        float[] ThePressure = new float[] { normalizedSensors[0], normalizedSensors[1], normalizedSensors[2], normalizedSensors[3], normalizedSensors[4], normalizedSensors[4] };
        float[] TheSpeed = new float[] { 1, 1, 1, 1, 1, 1 };
        bool[] TheBool = new bool[] { true, true, true, true, true, true };

        byte[] btData = LGloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
        LGloveHandler.BTSend(btData);
    }
    private void TriggerRight()
    {
        Haptics.Finger[] AllFingers = new Haptics.Finger[] { Haptics.Finger.Thumb, Haptics.Finger.Index, Haptics.Finger.Middle, Haptics.Finger.Ring, Haptics.Finger.Pinky, Haptics.Finger.Palm };

        float[] ThePressure = new float[] { normalizedSensors[0], normalizedSensors[1], normalizedSensors[2], normalizedSensors[3], normalizedSensors[4], normalizedSensors[4] };
        float[] TheSpeed = new float[] { 1, 1, 1, 1, 1, 1 };
        bool[] TheBool = new bool[] { true, true, true, true, true, true };

        byte[] btData = RGloveHandler.haptics.HEXRPressure(AllFingers, TheBool, ThePressure, TheSpeed);
        RGloveHandler.BTSend(btData);
    }

}

