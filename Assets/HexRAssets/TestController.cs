using HexR;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HaptGlove;
using HexR;
using static OVRPlugin;
public class TestController : MonoBehaviour
{
    public TextMeshProUGUI PressureString, SpeedString, FrequencyString;
    public PressureTrackerMain RightPTM, LeftPTM;
    private bool Thumb = true, Index = true, Middle = true, Ring = true, Pinky = true, Palm = true;
    public Image[] FingerBoolBackGround;
    private bool[] Finger = new bool[6];
    public TextMeshProUGUI[] RightAirPressure, LeftAirPressure; 
    // Start is called before the first frame update
    void Start()
    {
        UpdateFingerArray();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRightAirPressure();
        UpdateLeftAirPressure();

        if (Input.GetKeyDown(KeyCode.K))
        {
            TriggerHapticsTest();
            Debug.Log("Applying");
        }

    }

    public void TriggerHapticsTest()
    {
        float Pressure;
        if (float.TryParse(PressureString.text.ToString(), out Pressure))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }

        float Speed;
        if (float.TryParse(SpeedString.text.ToString(), out Speed))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }

        RightPTM.TriggerCustomHapticsIncrease(Finger, Pressure, Speed);
        LeftPTM.TriggerCustomHapticsIncrease(Finger, Pressure, Speed);
    }

    public void TriggerVibrationTest()
    {

        float Pressure;

        if (float.TryParse(PressureString.text.ToString(), out Pressure))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }


        float Frequency;
        if (float.TryParse(FrequencyString.text.ToString(), out Frequency))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }

        RightPTM.TriggerCustomlVibrations(Finger, Pressure, Frequency);

        LeftPTM.TriggerCustomlVibrations(Finger, Pressure, Frequency);

    }


    public void DecreasePressure()
    {
        float Pressure;
        if (float.TryParse(PressureString.text.ToString(), out Pressure))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }
        if(Pressure > 0.1)
        {
            Pressure = Pressure - 0.1f;
        }

        PressureString.text = Pressure.ToString();
    }
    public void IncreasePressure()
    {
        float Pressure;
        if (float.TryParse(PressureString.text.ToString(), out Pressure))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }
        if (Pressure < 1)
        {
            Pressure = Pressure + 0.1f;
        }

        PressureString.text = Pressure.ToString();
    }


    public void DecreaseSpeed()
    {
        float Speed;
        if (float.TryParse(SpeedString.text.ToString(), out Speed))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }
        if (Speed > 0.1)
        {
            Speed = Speed - 0.1f;
        }

        SpeedString.text = Speed.ToString();
    }
    public void IncreaseSpeed()
    {
        float Speed;
        if (float.TryParse(SpeedString.text.ToString(), out Speed))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }
        if (Speed < 1)
        {
            Speed = Speed + 0.1f;
        }

        SpeedString.text = Speed.ToString();
    }

    public void DecreaseFrequency()
    {
        float Frequency;
        if (float.TryParse(FrequencyString.text.ToString(), out Frequency))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }
        if (Frequency > 0.1)
        {
            Frequency = Frequency - 0.1f;
        }

        FrequencyString.text = Frequency.ToString();
    }
    public void DecreaseFrequencyOne()
    {
        float Frequency;
        if (float.TryParse(FrequencyString.text.ToString(), out Frequency))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }
        if (Frequency > 0.1)
        {
            Frequency = Frequency - 1f;
        }

        FrequencyString.text = Frequency.ToString();
    }
    public void IncreaseFrequency()
    {
        float Frequency;
        if (float.TryParse(FrequencyString.text.ToString(), out Frequency))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }
        if (Frequency < 40)
        {
            Frequency = Frequency + 0.1f;
        }

        FrequencyString.text = Frequency.ToString();
    }
    public void IncreaseFrequencyOne()
    {
        float Frequency;
        if (float.TryParse(FrequencyString.text.ToString(), out Frequency))
        {
            // success, use myValue
        }
        else
        {
            // handle error
        }
        if (Frequency < 40)
        {
            Frequency = Frequency + 1f;
        }

        FrequencyString.text = Frequency.ToString();
    }
    #region Finger Text Controller

    private void UpdateFingerArray()
    {
        Finger[0] = Thumb;
        Finger[1] = Index;
        Finger[2] = Middle;
        Finger[3] = Ring;
        Finger[4] = Pinky;
        Finger[5] = Palm;
    }

    public void ThumbBool()
    {
        Thumb = !Thumb;
        FingerBoolBackGround[0].color = Thumb ? Color.green : Color.red;
        UpdateFingerArray();
    }
    public void IndexBool()
    {
        Index = !Index;
        FingerBoolBackGround[1].color = Index ? Color.green : Color.red;
        UpdateFingerArray();
    }

    public void MiddleBool()
    {
        Middle = !Middle;
        FingerBoolBackGround[2].color = Middle ? Color.green : Color.red;
        UpdateFingerArray();
    }

    public void RingBool()
    {
        Ring = !Ring;
        FingerBoolBackGround[3].color = Ring ? Color.green : Color.red;
        UpdateFingerArray();
    }

    public void PinkyBool()
    {
        Pinky = !Pinky;
        FingerBoolBackGround[4].color = Pinky ? Color.green : Color.red;
        UpdateFingerArray();
    }

    public void PalmBool()
    {
        Palm = !Palm;
        FingerBoolBackGround[5].color = Palm ? Color.green : Color.red;
        UpdateFingerArray();
    }

    public void UpdateRightAirPressure()
    {
        RightAirPressure[0].text = RightPTM.ThumbPressure.ToString();
        RightAirPressure[1].text = RightPTM.IndexPressure.ToString();
        RightAirPressure[2].text = RightPTM.MiddlePressure.ToString();
        RightAirPressure[3].text = RightPTM.RingPressure.ToString();
        RightAirPressure[4].text = RightPTM.LittlePressure.ToString();
        RightAirPressure[5].text = RightPTM.PalmPressure.ToString();
    }
    public void UpdateLeftAirPressure()
    {
        LeftAirPressure[0].text = LeftPTM.ThumbPressure.ToString();
        LeftAirPressure[1].text = LeftPTM.IndexPressure.ToString();
        LeftAirPressure[2].text = LeftPTM.MiddlePressure.ToString();
        LeftAirPressure[3].text = LeftPTM.RingPressure.ToString();
        LeftAirPressure[4].text = LeftPTM.LittlePressure.ToString();
        LeftAirPressure[5].text = LeftPTM.PalmPressure.ToString();
    }
    #endregion

}
