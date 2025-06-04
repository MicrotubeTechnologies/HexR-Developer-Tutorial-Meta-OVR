using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HaptGlove;
using TMPro;

public class ARIS_Manager : MonoBehaviour
{
    public ARISHandler ARISHandler;
    public List<TextMeshProUGUI> DebugList = new List<TextMeshProUGUI>();

    private float[] stretchSensors = new float[5];
    private float[] normalizedSensors = new float[5];
    private float[] minValues = new float[5];
    private float[] maxValues = new float[5];
    private float[] smoothedValues = new float[5];

    [Header("Calibration Settings")]
    public float calibrationMargin = 0.01f;

    [Header("Smoothing Settings")]
    public bool useSmoothing = true;
    [Range(0f, 1f)]
    public float smoothingFactor = 0.1f;

    void Start()
    {
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
        if(ARISHandler._connected)
        {
            for (int i = 0; i < 5; i++)
            {
                if(ARISHandler.sensorArray[i] < 1000000)
                {
                    stretchSensors[i] = ARISHandler.sensorArray[i];

                    UpdateCalibration(i, stretchSensors[i]);

                    float normalized = NormalizeStretch(i, stretchSensors[i]);

                    if (useSmoothing)
                        smoothedValues[i] = Mathf.Lerp(smoothedValues[i], normalized, smoothingFactor);
                    else
                        smoothedValues[i] = normalized;

                    normalizedSensors[i] = smoothedValues[i];

                    // Display raw + normalized (you can customize this)
                    DebugList[i].text = $"Raw: {stretchSensors[i]:F1}   Norm: {normalizedSensors[i]:F1}";
                }
            }
        }
    }

    void UpdateCalibration(int index, float value)
    {
        if (value < minValues[index] - calibrationMargin)
            minValues[index] = value;

        if (value > maxValues[index] + calibrationMargin)
            maxValues[index] = value;
    }

    float NormalizeStretch(int index, float value)
    {
        value = Mathf.Max(value, minValues[index] + 0.0001f); // Prevent log(0)

        if (Mathf.Approximately(minValues[index], maxValues[index]))
            return 0f;

        float logMin = Mathf.Log(minValues[index]);
        float logMax = Mathf.Log(maxValues[index]);
        float logVal = Mathf.Log(value);

        float normalized = (logVal - logMin) / (logMax - logMin);
        return Mathf.Clamp01(normalized) * 100f;
    }

}

