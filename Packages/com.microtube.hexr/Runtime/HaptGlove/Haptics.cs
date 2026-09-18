using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HaptGlove
{
    public class Haptics
    {
        public string whichHand;
        public static string GetGhostFingerName(byte buf)
        {
            switch (buf)
            {
                case 0:
                    return "GhostThumb";
                case 1:
                    return "GhostIndex";
                case 2:
                    return "GhostMiddle";
                case 3:
                    return "GhostRing";
                case 4:
                    return "GhostPinky";
                case 5:
                    return "GhostPalm";
                default:
                    return null;
            }
        }

        public static byte[] SetClutchState(String bufName, String bufState)
        {
            //触发Clutch
            byte[] clutchState = new byte[2] { 0xff, 0xff };
            switch (bufName)
            {
                case "GhostThumb":
                    clutchState[0] = 0;
                    break;
                case "GhostIndex":
                    clutchState[0] = 1;
                    break;
                case "GhostMiddle":
                    clutchState[0] = 2;
                    break;
                case "GhostRing":
                    clutchState[0] = 3;
                    break;
                case "GhostPinky":
                    clutchState[0] = 4;
                    break;
                case "GhostPalm":
                    clutchState[0] = 5;
                    break;
            }

            switch (bufState)
            {
                case "Enter":
                    clutchState[1] = 0;
                    break;
                case "Stay":
                    clutchState[1] = 1;
                    break;
                case "Exit":
                    clutchState[1] = 2;
                    break;
            }
            return clutchState;
        }

        private static bool IsHandValid(string whichHand)
        {
            switch (whichHand)
            {
                case "Left":
                    return true;
                case "Right":
                    return true;
                default:
                    return false;
            }
        }
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="finger">finger: Thumb, Index, Middle, Ring,  Pinky or Palm.</param>
        ///// <param name="isApply">applyHaptics: true means apply haptics, false means remove haptics.</param>
        ///// <returns></returns>
        //public byte[] SetClutchState(string finger, bool applyHaptics)
        //{
        //    byte[] clutchState = new byte[2];
        //    byte fingerID = 0xff;
        //    byte state = 0xff;
        //    switch (finger)
        //    {
        //        case "Thumb":
        //            fingerID = 0;
        //            break;
        //        case "Index":
        //            fingerID = 1;
        //            break;
        //        case "Middle":
        //            fingerID = 2;
        //            break;
        //        case "Ring":
        //            fingerID = 3;
        //            break;
        //        case "Pinky":
        //            fingerID = 4;
        //            break;
        //        case "Palm":
        //            fingerID = 5;
        //            break;
        //    }

        //    switch (applyHaptics)
        //    {
        //        case true:
        //            state = 0;
        //            break;
        //        case false:
        //            state = 2;
        //            break;
        //    }

        //    if ((fingerID != 0xff) & (state != 0xff))
        //    {
        //        clutchState[0] = fingerID;
        //        clutchState[1] = state;
        //        return clutchState;
        //    }
        //    else
        //    {
        //        Debug.Log("Invalid parameter");
        //        return null;
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="finger">finger: Array of Thumb, Index, Middle, Ring, Pinky or Palm.</param>
        /// <param name="applyHaptics">applyHaptics: true means apply haptics, false means remove haptics.</param>
        /// <returns></returns>
        //public byte[][] SetClutchState(string[] finger, bool applyHaptics)
        //{
        //    byte state = 0xff;
        //    byte[][] clutchState = new byte[finger.Length][];

        //    switch (applyHaptics)
        //    {
        //        case true:
        //            state = 0;
        //            break;
        //        case false:
        //            state = 2;
        //            break;
        //    }

        //    if (state == 0xff)
        //    {
        //        Debug.Log("Invalid parameter");
        //        return null;
        //    }

        //    for (int i = 0; i < finger.Length; i++)
        //    {
        //        switch (finger[i])
        //        {
        //            case "Thumb":
        //                clutchState[i] = new byte[] { 0, state };
        //                break;
        //            case "Index":
        //                clutchState[i] = new byte[] { 1, state };
        //                break;
        //            case "Middle":
        //                clutchState[i] = new byte[] { 2, state };
        //                break;
        //            case "Ring":
        //                clutchState[i] = new byte[] { 3, state };
        //                break;
        //            case "Pinky":
        //                clutchState[i] = new byte[] { 4, state };
        //                break;
        //            case "Palm":
        //                clutchState[i] = new byte[] { 5, state };
        //                break;
        //            default:
        //                Debug.Log("Invalid parameter");
        //                return null;
        //        }
        //    }

        //    return clutchState;
        //}

        public enum FunIndex
        {
            FI_AIR_PRESSURE = 0x01,
            FI_STABLE_PRESSURE_CTRL = 0x02,
            FI_SET_PRESSURE_DEPRECATED = 0x03,
            FI_SET_PRESSURE = 0x04,
            FI_SET_PID = 0x05,
            FI_SET_BATTERY_LED = 0x06,
            FI_SET_VIBRATION = 0x07,
            FI_SET_PULSE = 0x08,
            FI_SET_VIB_SPEED = 0x09,
            FI_SET_PULSE_SPEED = 0x0a,
        }

        public enum Finger
        {
            Thumb,
            Index,
            Middle,
            Ring,
            Pinky,
            Palm
        }

        public enum HapticMode
        {
            Off,
            Pressure,
            Vibration,
            Pulse
        }

        public struct ChannelState
        {
            public HapticMode Mode;
            public float Intensity;
            public float Frequency;
        }

        // Mirrors the last command sent to each channel. This is commanded intent, not
        // confirmed device state - there is no hardware acknowledgment for these commands.
        private ChannelState[] channelState = new ChannelState[6];

        public ChannelState GetChannelState(Finger finger)
        {
            return channelState[(int)finger];
        }

        private byte[] SetHapticsState(Finger finger, bool state)
        {
            byte[] hapticsState = new byte[2];
            if (state)
                hapticsState = new byte[] { (byte)finger, 0 };
            else
                hapticsState = new byte[] { (byte)finger, 2 };

            return hapticsState;
        }

        private byte[][] SetHapticsState(Finger[] fingers, bool[] states)
        {
            byte[][] hapticsStates = new byte[fingers.Length][];

            for (int i = 0; i < fingers.Length; i++)
            {
                if (states[i])
                    hapticsStates[i] = new byte[] { (byte)fingers[i], 0 };
                else
                    hapticsStates[i] = new byte[] { (byte)fingers[i], 2 };
            }
            
            return hapticsStates;
        }

        private byte[] SetPID(float kp, float ki, float kd)
        {
            Encode.Instance.add_f32(kp);
            Encode.Instance.add_f32(ki);
            Encode.Instance.add_f32(kd);
            byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_PID);       // FI = 5
            Encode.Instance.clear_list();

            return data;
        }

        /// <summary>
        /// Set pressure to one haptics channel
        /// </summary>
        /// <param name="finger">Select haptics channel</param>
        /// <param name="state">Set TRUE to generate pressure, set FALSE to remove pressure</param>
        /// <param name="intensity">Set pressure intensity from 0.1 to 1</param>
        /// <param name="speed">Set the pressure changing speed from 0.1 to 1</param>
        public byte[] HEXRPressure(Finger finger, bool state, float intensity, float speed)
        {
            byte[] hapticsState = SetHapticsState(finger, state);

            float frequency = 0;
            Encode.Instance.add_f32(frequency);
            Encode.Instance.add_u8(hapticsState[0]);             // which finger
            Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
            intensity = Clamp(0.1f, 1f, intensity);
            float pressure = LinerMapping(0.1f, 1f, intensity, 15, 50);
            Encode.Instance.add_f32(pressure);
            speed = Clamp(0.1f, 1f, speed);
            speed = LinerMapping(0.1f, 1f, speed, 0.1f, 1);
            speed *= 100;
            Encode.Instance.add_u8((byte)speed);
            byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_PRESSURE);       // FI = 4
            Encode.Instance.clear_list();

            Debug.Log("Intensity: " + intensity + "Pressure: " + pressure + "Speed: " + speed);

            channelState[(int)finger] = state
                ? new ChannelState { Mode = HapticMode.Pressure, Intensity = intensity, Frequency = 0 }
                : new ChannelState { Mode = HapticMode.Off, Intensity = 0, Frequency = 0 };

            return data;
        }


        /// <summary>
        /// Set pressure to one haptics channel
        /// </summary>
        /// <param name="fingers">Select haptics channels</param>
        /// <param name="states">Set TRUE to generate pressure, set FALSE to remove pressure</param>
        /// <param name="intensity">Set pressure intensity of each channel from 0.1 to 1</param>
        /// <param name="speeds">Set the pressure changing speed of each channel from 0.1 to 1</param>
        public byte[] HEXRPressure(Finger[] fingers, bool[] states, float[] intensity, float[] speeds)
        {
            byte[][] hapticsState = SetHapticsState(fingers, states);

            List<byte> HapticsFrame = new List<byte>();

            for (int i = 0; i < fingers.Length; i++)
            {
                HapticsFrame.AddRange(HEXRPressure(fingers[i], states[i], intensity[i], speeds[i]));
            }

            return HapticsFrame.ToArray();
        }


        /// <summary>
        /// Set low frequency (less than 2Hz) vibration with tunable intensity and peakRatio
        /// </summary>
        /// <param name="finger">Select haptics channel</param>
        /// <param name="state">Set TRUE to generate vibration, set FALSE to stop vibration</param>
        /// <param name="frequency">Set vibration frequency from 0.1Hz to 2Hz</param>
        /// <param name="intensity">Set vibration intensity from 0.1 to 1</param>
        /// <param name="peakRatio">Set peak ratio from 0.2 to 0.8</param>
        /// <param name="speed">Set the pressure changing speed from 0.1 to 1</param>
        /// <param name="endIntensity">Set the pressure intensity to keep after vibration from 0.1 to 1</param>
        public byte[] HEXRVibration(Finger finger, bool state, float frequency, float intensity, float peakRatio, float speed, float endIntensity)
        {
            byte[] hapticsState = SetHapticsState(finger, state);

            frequency = Clamp(0.1f, 2, frequency);
            Encode.Instance.add_f32(frequency);
            Encode.Instance.add_u8(hapticsState[0]);             // which finger
            Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
            intensity = Clamp(0.1f, 1f, intensity);
            float pressure = LinerMapping(0.1f, 1f, intensity, 15, 50);
            peakRatio = Clamp(0.2f, 0.8f, peakRatio);
            peakRatio = peakRatio * 100;
            Encode.Instance.add_f32(pressure);
            Encode.Instance.add_u8((byte)peakRatio);
            speed = Clamp(0.1f, 1f, speed);
            speed = LinerMapping(0.1f, 1f, speed, 0.1f, 1);
            speed *= 100;
            Encode.Instance.add_u8((byte)speed);
            float endPressure = 0;
            if (endIntensity < 0.1f)
            {
                endIntensity = 0;
                endPressure = 0;
            }
            else
            {
                endIntensity = Clamp(0.1f, 1f, endIntensity);
                endPressure = LinerMapping(0.1f, 1f, endIntensity, 15, 50);
            }
            Encode.Instance.add_f32(endPressure);
            byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_VIB_SPEED);       // FI = 9
            Encode.Instance.clear_list();

            Debug.Log("Frequency: " + frequency + "\tPressure: " + pressure + "\tPeakRatio: " + peakRatio + "\tSpeed: " + speed);

            channelState[(int)finger] = state
                ? new ChannelState { Mode = HapticMode.Vibration, Intensity = intensity, Frequency = frequency }
                : new ChannelState { Mode = HapticMode.Off, Intensity = 0, Frequency = 0 };

            return data;
        }

        /// <summary>
        /// Set low frequency (less than 2Hz) vibration with tunable intensity and peakRatio for multiple channels
        /// </summary>
        /// <param name="fingers">Select haptics channels</param>
        /// <param name="states">Set TRUE to generate vibration, set FALSE to stop vibration</param>
        /// <param name="frequencies">Set vibration frequency of each channel from 0.1Hz to 2Hz</param>
        /// <param name="intensity">Set vibration intensity of each channel from 0.1 to 0.7</param>
        /// <param name="peakRatios">Set peak ratio of each channel from 0.2 to 0.8</param>
        /// <param name="speeds">Set the pressure changing speed of each channel from 0.1 to 1</param>
        /// <param name="endIntensity">Set the pressure intensity of each channel to keep after vibration from 0.1 to 1</param>
        public byte[] HEXRVibration(Finger[] fingers, bool[] states, float[] frequencies, float[] intensity, float[] peakRatios, float[] speeds, float[] endIntensity)
        {
            byte[][] hapticsState = SetHapticsState(fingers, states);

            List<byte> HapticsFrame = new List<byte>();

            for (int i = 0; i < fingers.Length; i++)
            {
                HapticsFrame.AddRange(HEXRVibration(fingers[i], states[i], frequencies[i], intensity[i], peakRatios[i], speeds[i], endIntensity[i]));
            }

            return HapticsFrame.ToArray();
        }


        /// <summary>
        /// Set a number of low frequency (less than 2Hz) pulses with tunable intensity and peakRatio
        /// </summary>
        /// <param name="finger">Set haptics channel</param>
        /// <param name="state">Set TRUE to generate vibration, set FALSE to stop vibration</param>
        /// <param name="frequency">Set pulse frequency from 0.1Hz to 2Hz</param>
        /// <param name="intensity">Set pulse intensity from 0.1 to 1</param>
        /// <param name="peakRatio">Set peak ratio from 0.2 to 0.8</param>
        /// <param name="speed">Set the pressure changing speed from 0.1 to 1</param>
        /// <param name="pulseCount">Set the number of pulses to generate</param>
        /// <param name="endIntensity">Set the pressure intensity to keep after pulse generation from 0.1 to 1</param>
        public byte[] HEXRPulse(Finger finger, bool state, float frequency, float intensity, float peakRatio, float speed, UInt16 pulseCount, float endIntensity)
        {
            byte[] hapticsState = SetHapticsState(finger, state);

            frequency = Clamp(0.1f, 2, frequency);
            Encode.Instance.add_f32(frequency);
            Encode.Instance.add_u8(hapticsState[0]);             // which finger
            Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
            intensity = Clamp(0.1f, 1f, intensity);
            float pressure = LinerMapping(0.1f, 1f, intensity, 15, 50);
            peakRatio = Clamp(0.2f, 0.8f, peakRatio);
            peakRatio = peakRatio * 100;
            Encode.Instance.add_f32(pressure);
            Encode.Instance.add_u8((byte)peakRatio);
            pulseCount = Convert.ToUInt16(Clamp(1, 1000, pulseCount));
            Encode.Instance.add_u16(pulseCount);
            speed = Clamp(0.1f, 1f, speed);
            speed = LinerMapping(0.1f, 1f, speed, 0.1f, 1);
            speed *= 100;
            Encode.Instance.add_u8((byte)speed);
            float endPressure = 0;
            if (endIntensity < 0.1f)
            {
                endIntensity = 0;
                endPressure = 0;
            }
            else
            {
                endIntensity = Clamp(0.1f, 1f, endIntensity);
                endPressure = LinerMapping(0.1f, 1f, endIntensity, 15, 50);
            }
            Encode.Instance.add_f32(endPressure);
            byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_PULSE_SPEED);       // FI = 10
            Encode.Instance.clear_list();

            Debug.Log("Frequency: " + frequency + "\tPressure: " + pressure + "\tPeakRatio: " + peakRatio + "\tPulseCount: " + pulseCount + "\tSpeed: " + speed);

            channelState[(int)finger] = state
                ? new ChannelState { Mode = HapticMode.Pulse, Intensity = intensity, Frequency = frequency }
                : new ChannelState { Mode = HapticMode.Off, Intensity = 0, Frequency = 0 };

            return data;
        }

        /// <summary>
        /// Set a number of low frequency (less than 2Hz) pulses with tunable intensity and peakRatio
        /// </summary>
        /// <param name="fingers">Set haptics channels</param>
        /// <param name="states">Set TRUE to generate vibration, set FALSE to stop pulse generation</param>
        /// <param name="frequencies">Set pulse frequency of each channel from 0.1Hz to 2Hz</param>
        /// <param name="intensity">Set pulse intensity of each channel from 0.1 to 0.7</param>
        /// <param name="peakRatios">Set peak ratio of each channel from 0.2 to 0.8</param>
        /// <param name="speeds">Set the pressure changing speed of each channel from 0.1 to 1</param>
        /// <param name="pulseCounts">Set the number of pulses of each channel to generate</param>
        /// <param name="endIntensity">Set the pressure intensity of each channel to keep after pulse generation from 0.1 to 1</param>
        public byte[] HEXRPulse(Finger[] fingers, bool[] states, float[] frequencies, float[] intensity, float[] peakRatios, float[] speeds, UInt16[] pulseCounts, float[] endIntensity)
        {
            byte[][] hapticsState = SetHapticsState(fingers, states);

            List<byte> HapticsFrame = new List<byte>();

            for (int i = 0; i < fingers.Length; i++)
            {
                HapticsFrame.AddRange(HEXRPulse(fingers[i], states[i], frequencies[i], intensity[i], peakRatios[i], speeds[i], pulseCounts[i], endIntensity[i]));
            }

            return HapticsFrame.ToArray();
        }

        /// <summary>
        /// Set vibration with tunable intensity for one channel
        /// </summary>
        /// <param name="finger">Select haptics channel</param>
        /// <param name="state">Set TRUE to generate vibration, set FALSE to stop vibration</param>
        /// <param name="frequency">Set vibration frequency from 0.1Hz to 40Hz</param>
        /// <param name="intensity">Set vibration intensity from 0.1 to 1</param>
        public byte[] HEXRVibration(Finger finger, bool state, float frequency, float intensity)
        {
            byte[] hapticsState = SetHapticsState(finger, state);

            frequency = Clamp(0.1f, 40, frequency);
            Encode.Instance.add_f32(frequency);
            Encode.Instance.add_u8(hapticsState[0]);             // which finger
            Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
            float[] buf = GetVibIntensity(frequency, intensity);
            float pressure = buf[0];
            Encode.Instance.add_f32(pressure);                   // Pressure
            byte peakRatio = (byte)buf[1];
            Encode.Instance.add_u8(peakRatio);                   // Peak ratio
            byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_VIBRATION);       // FI = 7
            Encode.Instance.clear_list();

            Debug.Log("Frequency: " + frequency + "\tPressure: " + pressure + "\tPeakRatio: " + peakRatio);

            channelState[(int)finger] = state
                ? new ChannelState { Mode = HapticMode.Vibration, Intensity = intensity, Frequency = frequency }
                : new ChannelState { Mode = HapticMode.Off, Intensity = 0, Frequency = 0 };

            return data;
        }

        /// <summary>
        /// Set vibration with tunable intensity for multiple channel
        /// </summary>
        /// <param name="fingers">Select haptics channels</param>
        /// <param name="states">Set TRUE to generate vibration, set FALSE to stop vibration</param>
        /// <param name="frequencies">Set vibration frequency of each channel from 0.1Hz to 40Hz</param>
        /// <param name="intensity">Set vibration intensity of each channel from 0.1 to 1</param>
        public byte[] HEXRVibration(Finger[] fingers, bool[] states, float[] frequencies, float[] intensity)
        {
            byte[][] hapticsState = SetHapticsState(fingers, states);

            List<byte> HapticsFrame = new List<byte>();

            for (int i = 0; i < fingers.Length; i++)
            {
                HapticsFrame.AddRange(HEXRVibration(fingers[i], states[i], frequencies[i], intensity[i]));
            }

            return HapticsFrame.ToArray();
        }


        /// <summary>
        /// Set a number of pulse to one channel with tunable intensity
        /// </summary>
        /// <param name="finger">Select haptics channel</param>
        /// <param name="state">Set TRUE to generate vibration, set FALSE to stop pulse generation</param>
        /// <param name="frequency">Set pulse frequency from 0.1Hz to 40Hz</param>
        /// <param name="intensity">Set pulse intensity from 0.1 to 1</param>
        /// <param name="pulseCount">Set pulse count from 1 to 1000</param>
        public byte[] HEXRPulse(Finger finger, bool state, float frequency, float intensity, UInt16 pulseCount)
        {
            byte[] hapticsState = SetHapticsState(finger, state);

            frequency = Clamp(0.1f, 40, frequency);
            Encode.Instance.add_f32(frequency);
            Encode.Instance.add_u8(hapticsState[0]);             // which finger
            Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
            float[] buf = GetVibIntensity(frequency, intensity);
            float pressure = buf[0];
            Encode.Instance.add_f32(pressure);                   // Pressure
            byte peakRatio = (byte)buf[1];
            Encode.Instance.add_u8(peakRatio);                   // Peak ratio
            pulseCount = Convert.ToUInt16(Clamp(1, 1000, pulseCount));
            Encode.Instance.add_u16(pulseCount);
            byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_PULSE);       // FI = 8
            Encode.Instance.clear_list();

            Debug.Log("Frequency: " + frequency + "\tPressure: " + pressure + "\tPeakRatio: " + peakRatio + "\tPulseCount: " + pulseCount);

            channelState[(int)finger] = state
                ? new ChannelState { Mode = HapticMode.Pulse, Intensity = intensity, Frequency = frequency }
                : new ChannelState { Mode = HapticMode.Off, Intensity = 0, Frequency = 0 };

            return data;
        }

        /// <summary>
        /// Set a number of pulse to multiple channel with tunable intensity
        /// </summary>
        /// <param name="fingers">Select haptics channels</param>
        /// <param name="states">Set TRUE to generate vibration, set FALSE to stop pulse generation</param>
        /// <param name="frequencies">Set pulse frequency of one channel from 0.1Hz to 40Hz</param>
        /// <param name="intensity">Set pulse intensity of one channel from 0.1 to 1</param>
        /// <param name="pulseCounts">Set pulse count of one channel from 1 to 1000</param>
        public byte[] HEXRPulse(Finger[] fingers, bool[] states, float[] frequencies, float[] intensity, UInt16[] pulseCounts)
        {
            byte[][] hapticsState = SetHapticsState(fingers, states);

            List<byte> HapticsFrame = new List<byte>();

            for (int i = 0; i < fingers.Length; i++)
            {
                HapticsFrame.AddRange(HEXRPulse(fingers[i], states[i], frequencies[i], intensity[i], pulseCounts[i]));
            }

            return HapticsFrame.ToArray();
        }


        private float GetVibPressure(float frequency)
        {
            float pressure = 0;
            if (frequency >= 5)
                pressure = 50;
            else if (frequency >= 2)
                pressure = LinerMapping(2, 5, frequency, 20, 50);
            else if (frequency >= 0.1f)
                pressure = 20;

            return pressure;
        }

        private float[] GetVibIntensity(float frequency, float fakeIntensity)
        {
            float pressure = 0;
            byte peakRatio = 0;
            fakeIntensity = Clamp(0.1f, 1, fakeIntensity);
            if (frequency >= 10)
            {
                pressure = 50;
                peakRatio = (byte)LinerMapping(0.1f, 1, fakeIntensity, 20, 50);
            }
            else if (frequency >= 5)
            {
                pressure = 50;
                byte bound1 = (byte)LinerMapping(5, 10, frequency, 20, 20);
                peakRatio = (byte)LinerMapping(0.1f, 1, fakeIntensity, bound1, 50);
            }
            else if (frequency >= 1)
            {
                peakRatio = 50;
                pressure = LinerMapping(0.1f, 1, fakeIntensity, 15, 50);
            }
            else if (frequency >= 0.1f)
            {
                peakRatio = 50;
                pressure = LinerMapping(0.1f, 1, fakeIntensity, 15, 50);
            }

            float[] buf = new float[] { pressure, peakRatio };
            return buf;
        }

        private byte GetVibPeakRatio(float frequency)
        {
            byte peakRatio = 0;
            byte defaultRatio = 35;
            byte bound1 = 0;
            byte bound2 = 0;
            if (frequency >= 10)
                peakRatio = defaultRatio;
            else if (frequency >= 5)
            {
                bound1 = (byte)LinerMapping(5, 10, frequency, 10, 20);
                bound2 = 50;
                peakRatio = (byte)LinerMapping(20, 50, defaultRatio, bound1, bound2);
            }
            else if (frequency >= 2)
            {
                bound1 = 50;
                bound2 = 30;
                peakRatio = (byte)LinerMapping(2, 5, frequency, bound1, bound2);
            }
            else if (frequency >= 0.1f)
            {
                peakRatio = 50;
            }

            return peakRatio;
        }

        private float LinerMapping(float preBound1, float preBound2, float input, float bound1, float bound2)
        {
            if (preBound2 == preBound1)
                return 0;

            float output = (bound2 - bound1) * (input - preBound1) / (preBound2 - preBound1) + bound1;
            return output;
        }

        private static float Clamp(float lower, float upper, float input)
        {
            float output = 0;
            if (input > upper)
                output = upper;
            else if (input < lower)
                output = lower;
            else
                output = input;

            return output;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////// DEPRECATED ////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////// 

        /// <summary>
        /// DEPRECATED. To one finger
        /// </summary>
        /// <param name="clutchState"></param>
        /// <param name="targetPres"></param>
        public byte[] ApplyHaptics(byte[] clutchState, byte targetPres, bool compensateHysteresis)
        {
            if (!IsHandValid(whichHand))
            {
                Debug.Log("Invalid hand name: " + whichHand);
                return null;
            }

            byte frequency = 0;
            if ((targetPres > 0) & (targetPres < 10))
            {
                frequency = 0xff;
            }

            //BTCommu_Left bt = WhichBT(whichHand);
            //if (bt == null)
            //{
            //    return;
            //}
            int presSource = (int)pressureData[5];
            byte[] valveTiming = HaptGloveValvesCalibrationData.CalculateValveTiming(targetPres, clutchState[0], presSource, whichHand);

            if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
            {
                byte vOpen = valveTiming[0];
                byte vDelay = valveTiming[1];
                //编码+BT发送
                Encode.Instance.add_u8(frequency);
                Encode.Instance.add_u8(clutchState[0]);             // which finger
                Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
                Encode.Instance.add_u8(targetPres);
                Encode.Instance.add_u8(vOpen);
                Encode.Instance.add_u8(vDelay);
                Encode.Instance.add_b1(compensateHysteresis);
                byte[] buf = Encode.Instance.add_fun((Byte)FunIndex.FI_SET_PRESSURE_DEPRECATED);       // FI = 3
                Encode.Instance.clear_list();
                //bt.BTSend(buf);

                channelState[clutchState[0]] = clutchState[1] == 0
                    ? new ChannelState { Mode = HapticMode.Pressure, Intensity = targetPres, Frequency = 0 }
                    : new ChannelState { Mode = HapticMode.Off, Intensity = 0, Frequency = 0 };

                return buf;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// DEPRECATED. Apply vibration to one finger
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="clutchState"></param>
        /// <param name="targetPres"></param>
        public byte[] ApplyHaptics(byte frequency, byte[] clutchState, byte targetPres, bool compensateHysteresis)
        {
            if (!IsHandValid(whichHand))
            {
                Debug.Log("Invalid hand name: " + whichHand);
                return null;
            }
            //BTCommu_Left bt = WhichBT(whichHand);
            //if (bt == null)
            //{
            //    Debug.Log("Invalid hand name");
            //    return;
            //}
            int presSource = (int)pressureData[5];
            byte[] valveTiming = HaptGloveValvesCalibrationData.CalculateValveTiming(targetPres, clutchState[0], presSource, whichHand);

            if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
            {
                byte vOpen = valveTiming[0];
                byte vDelay = valveTiming[1];
                //编码+BT发送
                Encode.Instance.add_u8(frequency);
                Encode.Instance.add_u8(clutchState[0]);             // which finger
                Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
                Encode.Instance.add_u8(targetPres);
                Encode.Instance.add_u8(vOpen);
                Encode.Instance.add_u8(vDelay);
                Encode.Instance.add_b1(compensateHysteresis);
                byte[] buf = Encode.Instance.add_fun((Byte)FunIndex.FI_SET_PRESSURE_DEPRECATED);       // FI = 3
                Encode.Instance.clear_list();
                //bt.BTSend(buf);

                channelState[clutchState[0]] = clutchState[1] == 0
                    ? new ChannelState { Mode = HapticMode.Vibration, Intensity = targetPres, Frequency = frequency }
                    : new ChannelState { Mode = HapticMode.Off, Intensity = 0, Frequency = 0 };

                return buf;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// DEPRECATED. To multiple fingers
        /// </summary>
        /// <param name="clutchStates"></param>
        /// <param name="targetPres"></param>
        public byte[] ApplyHaptics(byte[][] clutchStates, byte targetPres, bool compensateHysteresis)
        {
            if (!IsHandValid(whichHand))
            {
                Debug.Log("Invalid hand name: " + whichHand);
                return null;
            }

            byte frequency = 0;
            if ((targetPres > 0) & (targetPres < 10))
            {
                frequency = 0xff;
            }

            //BTCommu_Left bt = WhichBT(whichHand);
            //if (bt == null)
            //{
            //    Debug.Log("Invalid hand name");
            //    return;
            //}
            int presSource = (int)pressureData[5];

            List<byte> HapticsFrame = new List<byte>();

            for (int i = 0; i < clutchStates.Length; i++)
            {
                HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], targetPres, presSource, whichHand, compensateHysteresis));
            }

            //bt.BTSend(HapticsFrame.ToArray());
            return HapticsFrame.ToArray();
        }

        /// <summary>
        /// DEPRECATED. To multiple fingers
        /// </summary>
        /// <param name="clutchStates"></param>
        /// <param name="targetPres"></param>
        public byte[] ApplyHaptics(byte[][] clutchStates, byte[] targetPres, bool compensateHysteresis)
        {
            if (!IsHandValid(whichHand))
            {
                Debug.Log("Invalid hand name: " + whichHand);
                return null;
            }

            byte[] frequency = new byte[5];
            for (int i = 0; i < 5; i++)
            {
                if ((targetPres[i] > 0) & (targetPres[i] < 10))
                {
                    frequency[i] = 0xff;
                }
            }

            //BTCommu_Left bt = WhichBT(whichHand);
            //if (bt == null)
            //{
            //    Debug.Log("Invalid hand name");
            //    return;
            //}
            int presSource = (int)pressureData[5];

            List<byte> HapticsFrame = new List<byte>();

            for (int i = 0; i < clutchStates.Length; i++)
            {
                HapticsFrame.AddRange(ConstructHapticsFrame(frequency[i], clutchStates[i], targetPres[i], presSource, whichHand, compensateHysteresis));
            }

            //bt.BTSend(HapticsFrame.ToArray());
            return HapticsFrame.ToArray();
        }

        /// <summary>
        /// DEPRECATED. Apply vibration to multiple fingers
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="clutchStates"></param>
        /// <param name="targetPres"></param>
        public byte[] ApplyHaptics(byte frequency, byte[][] clutchStates, byte targetPres, bool compensateHysteresis)
        {
            if (!IsHandValid(whichHand))
            {
                Debug.Log("Invalid hand name: " + whichHand);
                return null;
            }

            //BTCommu_Left bt = WhichBT(whichHand);
            //if (bt == null)
            //{
            //    Debug.Log("Invalid hand name");
            //    return;
            //}
            int presSource = (int)pressureData[5];

            List<byte> HapticsFrame = new List<byte>();

            for (int i = 0; i < clutchStates.Length; i++)
            {
                HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], targetPres, presSource, whichHand, compensateHysteresis));
            }

            //bt.BTSend(HapticsFrame.ToArray());
            return HapticsFrame.ToArray();
        }


        private byte[] ConstructHapticsFrame(byte frequency, byte[] clutchState, byte targetPres, int presSource, string whichHand, bool compensateHysteresis)
        {
            byte fingerID = clutchState[0];
            byte status = clutchState[1];

            byte[] valveTiming = HaptGloveValvesCalibrationData.CalculateValveTiming(targetPres, fingerID, presSource, whichHand);
            byte vOpen = valveTiming[0];
            byte vDelay = valveTiming[1];
            //编码+BT发送
            Encode.Instance.add_u8(frequency);
            Encode.Instance.add_u8(fingerID); // which finger
            Encode.Instance.add_u8(status); // enter, stay or exit
            Encode.Instance.add_u8(targetPres);
            Encode.Instance.add_u8(vOpen);
            Encode.Instance.add_u8(vDelay);
            Encode.Instance.add_b1(compensateHysteresis);
            byte[] buf = Encode.Instance.add_fun((Byte)FunIndex.FI_SET_PRESSURE_DEPRECATED); // FI = 3
            Encode.Instance.clear_list();

            // frequency == 0 is a plain pressure command; any non-zero frequency (including
            // the 0xff "low pressure" sentinel used by some callers) is treated as vibration.
            channelState[fingerID] = status == 0
                ? new ChannelState { Mode = frequency == 0 ? HapticMode.Pressure : HapticMode.Vibration, Intensity = targetPres, Frequency = frequency }
                : new ChannelState { Mode = HapticMode.Off, Intensity = 0, Frequency = 0 };

            return buf;
        }

        ///// <summary>
        ///// DEPRECATED. To one finger
        ///// </summary>
        ///// <param name="clutchState"></param>
        ///// <param name="valveTiming"></param>
        //public byte[] ApplyHapticsWithTiming(byte[] clutchState, byte[] valveTiming, bool compensateHysteresis)
        //{
        //    if (!IsHandValid(whichHand))
        //    {
        //        Debug.Log("Invalid hand name: " + whichHand);
        //        return null;
        //    }

        //    byte frequency = 0;
        //    //BTCommu_Left bt = WhichBT(whichHand);
        //    //if (bt == null)
        //    //{
        //    //    Debug.Log("Invalid hand name");
        //    //    return;
        //    //}
        //    int presSource = (int)pressureData[5];

        //    if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
        //    {
        //        byte vOpen = valveTiming[0];
        //        byte vDelay = valveTiming[1];
        //        //编码+BT发送
        //        Encode.Instance.add_u8(frequency);
        //        Encode.Instance.add_u8(clutchState[0]);             // which finger
        //        Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
        //        Encode.Instance.add_u8(0);
        //        Encode.Instance.add_u8(vOpen);
        //        Encode.Instance.add_u8(vDelay);
        //        Encode.Instance.add_b1(compensateHysteresis);
        //        byte[] buf = Encode.Instance.add_fun((Byte)FunIndex.FI_SET_PRESSURE_DEPRECATED);       // FI = 3
        //        Encode.Instance.clear_list();
        //        //bt.BTSend(buf);
        //        return buf;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        ///// <summary>
        ///// DEPRECATED. Apply vibration to one finger
        ///// </summary>
        ///// <param name="clutchState"></param>
        ///// <param name="valveTiming"></param>
        //public byte[] ApplyHapticsWithTiming(byte frequency, byte[] clutchState, byte[] valveTiming, bool compensateHysteresis)
        //{
        //    if (!IsHandValid(whichHand))
        //    {
        //        Debug.Log("Invalid hand name: " + whichHand);
        //        return null;
        //    }

        //    //BTCommu_Left bt = WhichBT(whichHand);
        //    //if (bt == null)
        //    //{
        //    //    Debug.Log("Invalid hand name");
        //    //    return;
        //    //}
        //    int presSource = (int)pressureData[5];
        //    //byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

        //    if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
        //    {
        //        byte vOpen = valveTiming[0];
        //        byte vDelay = valveTiming[1];
        //        //编码+BT发送
        //        Encode.Instance.add_u8(frequency);
        //        Encode.Instance.add_u8(clutchState[0]);             // which finger
        //        Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
        //        Encode.Instance.add_u8(0);
        //        Encode.Instance.add_u8(vOpen);
        //        Encode.Instance.add_u8(vDelay);
        //        Encode.Instance.add_b1(compensateHysteresis);
        //        byte[] buf = Encode.Instance.add_fun((Byte)FunIndex.FI_SET_PRESSURE_DEPRECATED);       // FI = 3
        //        Encode.Instance.clear_list();
        //        //bt.BTSend(buf);
        //        return buf;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        ///// <summary>
        ///// DEPRECATED. To multiple fingers
        ///// </summary>
        ///// <param name="clutchStates"></param>
        ///// <param name="valveTimings"></param>
        //public byte[] ApplyHapticsWithTiming(byte[][] clutchStates, byte[][] valveTimings, bool compensateHysteresis)
        //{
        //    if (!IsHandValid(whichHand))
        //    {
        //        Debug.Log("Invalid hand name: " + whichHand);
        //        return null;
        //    }

        //    byte frequency = 0;

        //    //BTCommu_Left bt = WhichBT(whichHand);
        //    //if (bt == null)
        //    //{
        //    //    Debug.Log("Invalid hand name");
        //    //    return;
        //    //}
        //    int presSource = (int)pressureData[5];

        //    List<byte> HapticsFrame = new List<byte>();

        //    for (int i = 0; i < clutchStates.Length; i++)
        //    {
        //        HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], valveTimings[i], presSource, whichHand, compensateHysteresis));
        //    }

        //    //bt.BTSend(HapticsFrame.ToArray());
        //    return HapticsFrame.ToArray();
        //}

        ///// <summary>
        ///// DEPRECATED. Apply vibration to multiple fingers
        ///// </summary>
        ///// <param name="frequency"></param>
        ///// <param name="clutchStates"></param>
        ///// <param name="valveTimings"></param>
        //public byte[] ApplyHapticsWithTiming(byte frequency, byte[][] clutchStates, byte[][] valveTimings, bool compensateHysteresis)
        //{
        //    if (!IsHandValid(whichHand))
        //    {
        //        Debug.Log("Invalid hand name: " + whichHand);
        //        return null;
        //    }

        //    //BTCommu_Left bt = WhichBT(whichHand);
        //    //if (bt == null)
        //    //{
        //    //    Debug.Log("Invalid hand name");
        //    //    return;
        //    //}
        //    int presSource = (int)pressureData[5];

        //    List<byte> HapticsFrame = new List<byte>();

        //    for (int i = 0; i < clutchStates.Length; i++)
        //    {
        //        HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], valveTimings[i], presSource, whichHand, compensateHysteresis));
        //    }

        //    //bt.BTSend(HapticsFrame.ToArray());
        //    return HapticsFrame.ToArray();
        //}

        //private byte[] ConstructHapticsFrame(byte frequency, byte[] clutchState, byte[] valveTiming, int presSource, string whichHand, bool compensateHysteresis)
        //{
        //    byte fingerID = clutchState[0];
        //    byte status = clutchState[1];

        //    //byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, fingerID, presSource);
        //    byte vOpen = valveTiming[0];
        //    byte vDelay = valveTiming[1];
        //    //编码+BT发送
        //    Encode.Instance.add_u8(frequency);
        //    Encode.Instance.add_u8(fingerID); // which finger
        //    Encode.Instance.add_u8(status); // enter, stay or exit
        //    Encode.Instance.add_u8(0);
        //    Encode.Instance.add_u8(vOpen);
        //    Encode.Instance.add_u8(vDelay);
        //    Encode.Instance.add_b1(compensateHysteresis);
        //    byte[] buf = Encode.Instance.add_fun((Byte)FunIndex.FI_SET_PRESSURE_DEPRECATED); // FI = 3
        //    Encode.Instance.clear_list();

        //    return buf;
        //}


        private enum funList : byte
        {
            FI_BMP280 = 0x01,
            FI_MICROTUBE = 0x04,
            FI_CLUTCHGOTACTIVATED = 0x05,
            FI_BATTERY = 0x06
        };
        private List<byte> buffer = new List<byte>(1024);
        private byte[] oneFrame = new byte[128];
        public void DecodeGloveData(byte[] gloveData)
        {
            // Convert incoming data to a hex string for debugging (if needed)
            string hexString = BitConverter.ToString(gloveData).Replace("-", "");

            // Add new data to the buffer
            buffer.AddRange(gloveData);

            // Process the buffer while it has enough data
            while (buffer.Count >= 5)
            {
                // Check if the second byte is a valid function
                if (Enum.IsDefined(typeof(funList), buffer[1]))
                {
                    // Get the length of the frame
                    int len = buffer[0]; // Frame length

                    // Check if the buffer has enough data for the specified length
                    if (len < 1 || len > buffer.Count)
                    {
                        // Handle the error, possibly by removing invalid data
                        buffer.RemoveAt(0); // Remove the first byte and continue
                        continue; // Skip to the next iteration
                    }

                    // Calculate the checksum
                    byte checkSum = 0;
                    for (int i = 0; i < len - 1; i++)
                    {
                        checkSum ^= buffer[i];
                    }

                    // Validate the checksum
                    if (checkSum != buffer[len - 1])
                    {
                        buffer.RemoveRange(0, len); // Remove the invalid frame
                        continue; // Skip to the next iteration
                    }

                    // Ensure the buffer has enough data before copying
                    if (buffer.Count >= len)
                    {
                        buffer.CopyTo(0, oneFrame, 0, len);
                        buffer.RemoveRange(0, len);
                        FrameDataAnalysis(oneFrame); // Process the valid frame
                    }
                }
                else
                {
                    // If not a valid function, remove the first byte
                    buffer.RemoveAt(0);
                }
            }
        }

        private void FrameDataAnalysis(byte[] frame)
        {
            switch (frame[1])
            {
                case (byte)funList.FI_BMP280:         //BMP280
                    DecodePressure(frame);
                    break;
                case (byte)funList.FI_MICROTUBE:      //Microtube
                    DecodeMicrotube(frame);
                    break;
                case (byte)funList.FI_CLUTCHGOTACTIVATED:
                    DecodeMicrotube(frame);
                    break;
                case (byte)funList.FI_BATTERY:
                    DecodeBatteryLevel(frame);
                    break;
            }
        }

        public int[] fingerPositionData = new int[5];
        public float[] hapticStartPosition = new float[5];
        public int[] pressureData = new int[7];

        public bool flag_MicrotubeDataReady = false;
        public bool flag_pressureDataReady = false;

        private void DecodePressure(byte[] frame)
        {
            pressureData[0] = (int)BitConverter.ToSingle(frame, 3);
            pressureData[1] = (int)BitConverter.ToSingle(frame, 8);
            pressureData[2] = (int)BitConverter.ToSingle(frame, 13);
            pressureData[3] = (int)BitConverter.ToSingle(frame, 18);
            pressureData[4] = (int)BitConverter.ToSingle(frame, 23);
            pressureData[5] = (int)BitConverter.ToSingle(frame, 28);
            pressureData[6] = (int)BitConverter.ToSingle(frame, 33);

            flag_pressureDataReady = true;

            //sw.WriteLine( "," + pressureData[0] );

            //Debug.Log("AirPressure:  "+ pressureData[0]+ "\t" + pressureData[1] + "\t" + pressureData[2] + "\t" + pressureData[3] + "\t" + pressureData[4] + "\t" + pressureData[5]);

            //Grapher.Log(pressureData[1], "Pressure Source", Color.white);
        }


        private void DecodeMicrotube(byte[] frame)
        {
            fingerPositionData[0] = (BitConverter.ToInt32(frame, 3));
            fingerPositionData[1] = (BitConverter.ToInt32(frame, 8));
            fingerPositionData[2] = (BitConverter.ToInt32(frame, 13));
            fingerPositionData[3] = (BitConverter.ToInt32(frame, 18));
            fingerPositionData[4] = (BitConverter.ToInt32(frame, 23));

            flag_MicrotubeDataReady = true;

            //fingerMappingLeftScript.UpdateFingerPosLeft();
            //graspingScript.GetCurrentMicrotubeData(fingerMappingLeftScript.normalizedData);

            //if (scissors != null)
            //{
            //    scissors.Scissors(fingerMappingLeftScript.normalizedData);
            //}

            //sw.Write(Environment.TickCount + "," + microtubeData[0]);
            //Debug.Log(DateTime.Now.ToString("HH:mm:ss.fff"));
            //sw.Flush();

            //Debug.Log(fingerMappingLeftScript.normalizedData[0] + "\t" + fingerMappingLeftScript.normalizedData[1] + "\t" + fingerMappingLeftScript.normalizedData[2] + "\t" + fingerMappingLeftScript.normalizedData[3] + "\t" + fingerMappingLeftScript.normalizedData[4]);

            //if (frame[1] == (byte)funList.FI_CLUTCHGOTACTIVATED)
            //{
            //    byte[] buf = { frame[2], frame[5], frame[8], frame[11], frame[14] };

            //    for (int i = 0; i < 5; i++)
            //    {
            //        if (buf[i] == (byte)0xff)
            //        {
            //            //graspingScript.hapticStartPosition[i] = microtubeData[i];
            //            graspingScript.hapticStartPosition[i] = fingerMappingLeftScript.normalizedData[i];
            //        }
            //    }



            //    //int clutchID = Array.IndexOf(buf, (byte)0xff);

            //    //graspingScript.hapticStartPosition[clutchID] = microtubeData[clutchID];
            //}
        }

        public float batteryLevel = 0f;
        //private Int16 batMin = 2200;
        //private Int16 batMax = 2950;    //3000
        private void DecodeBatteryLevel(byte[] frame)
        {
            batteryLevel = (BitConverter.ToSingle(frame, 3));

            //Int16 rawBattery = (BitConverter.ToInt16(frame, 3));
            //batteryLevel = (float)(rawBattery - batMin) / (batMax - batMin);

            //if (batteryLevel > 1)
            //{
            //    batteryLevel = 1;
            //}
            //else if (batteryLevel < 0.01f)
            //{
            //    batteryLevel = 0.01f;
            //}
        }

        public byte[] GetValveTimingFromVibIntensity(byte vibFrequency, byte vibIntensity, byte fingerID)
        {
            byte[] valveTiming = new byte[2];
            byte onTiming = 0;
            byte offTiming = 0;
            
            switch (vibIntensity)
            {
                case 1:
                    if (fingerID == 5)
                        onTiming = 7;
                    else
                        onTiming = 3;
                    break;
                case 2:
                    if (fingerID == 5)
                        onTiming = 7;
                    else
                        onTiming = 4;
                    break;
                case 3:
                    if (fingerID == 5)
                        onTiming = 7;
                    else
                        onTiming = 5;
                    break;
                default:
                    if (fingerID == 5)
                        onTiming = 7;
                    else
                        onTiming = 5;
                    break;
            }

            int maxExhaustTime = 500 / vibFrequency - 3;

            if (maxExhaustTime < 100)
            {
                offTiming = (byte)maxExhaustTime;
            }
            else
            {
                offTiming = 100;
            }

            valveTiming = new byte[] { onTiming, offTiming };

            return valveTiming;
        }

        private class HaptGloveValvesCalibrationData
        {
            private static byte[,,] valveCaliOn = new byte[5, 6, 6];
            private static byte[,] valveCaliOff = new byte[6, 6];

            private static byte[,,] valveCaliOn_Left = new byte[5, 6, 6]
        {
        {
            {13, 20, 27, 34, 44, 130},//under 69kpa
            {12, 18, 25, 32, 42, 130},
            {12, 17, 23, 30, 39, 130},
            {12, 17, 23, 30, 39, 130},
            {13, 18, 24, 31, 40, 130},
            {13, 18, 24, 31, 40, 130}
        },
        {
            {14, 21, 27, 35, 46, 200},//under 67kpa
            {12, 19, 25, 33, 44, 200},
            {12, 17, 24, 31, 41, 200},
            {12, 17, 23, 30, 41, 200},
            {13, 18, 25, 32, 41, 200},
            {13, 18, 25, 32, 41, 200}
        },
        {
            {14, 21, 28, 36, 48, 255},//under 65kpa
            {13, 19, 26, 34, 46, 255},
            {12, 18, 24, 32, 42, 255},
            {12, 18, 24, 31, 42, 255},
            {13, 19, 25, 33, 43, 255},
            {13, 19, 25, 33, 43, 255}
        },
        {
            {14, 21, 29, 37, 52, 255},//under 63kpa
            {13, 20, 27, 36, 49, 255},
            {12, 18, 25, 33, 46, 255},
            {12, 18, 24, 33, 46, 255},
            {13, 19, 26, 35, 47, 255},
            {13, 19, 26, 35, 47, 255}
        },
        {
            {14, 22, 30, 39, 56, 255},//under 61kpa
            {13, 20, 28, 37, 54, 255},
            {12, 19, 25, 34, 54, 255},
            {12, 19, 25, 34, 54, 255},
            {13, 20, 27, 36, 54, 255},
            {13, 20, 27, 36, 54, 255}
        }
        };

            private static byte[,] valveCaliOff_Left = new byte[6, 6]
            {
        {100, 130, 160, 190, 210, 230},
        {100, 130, 160, 190, 210, 230},
        {100, 130, 160, 190, 210, 230},
        {100, 130, 160, 190, 210, 230},
        {100, 130, 160, 190, 210, 230},
        {100, 130, 160, 190, 210, 230}
            };

            private static byte[,,] valveCaliOn_Right = new byte[5, 6, 6]
            {
        {
            {3, 4, 5, 6, 8, 20},//under 69kpa
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {7, 13, 22, 36, 50, 255}
        },
        {
            {3, 4, 5, 6, 8, 20},//under 67kpa
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {7, 13, 22, 36, 50, 255}
        },
        {
            {3, 4, 5, 6, 8, 20},//under 65kpa
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {7, 13, 22, 36, 50, 255}
        },
        {
            {3, 4, 5, 6, 8, 20},//under 63kpa
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {8, 15, 25, 40, 80, 255}
        },
        {
            {3, 4, 5, 6, 8, 20},//under 61kpa
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {3, 4, 5, 6, 8, 20},
            {9, 16, 27, 44, 90, 255}
        }
            };

            private static byte[,] valveCaliOff_Right = new byte[6, 6]
            {
        {50, 60, 70, 80, 90, 100},
        {50, 60, 70, 80, 90, 100},
        {50, 60, 70, 80, 90, 100},
        {50, 60, 70, 80, 90, 100},
        {50, 60, 70, 80, 90, 100},
        {60, 80, 110, 130, 160, 200}
            };

            //private static byte[,,] valveCaliOn_L_Right = new byte[5, 5, 6]
            //{
            //    {
            //        {12, 17, 23, 29, 38, 130},//under 69kpa
            //        {12, 17, 23, 30, 40, 130},
            //        {13, 19, 26, 34, 44, 130},
            //        {10, 15, 19, 25, 35, 130},
            //        {13, 19, 25, 33, 43, 130}
            //    },
            //    {
            //        {12, 18, 23, 30, 39, 255},//under 67kpa
            //        {12, 18, 24, 31, 42, 255},
            //        {13, 20, 27, 35, 46, 255},
            //        {10, 15, 20, 26, 36, 255},
            //        {13, 19, 26, 34, 45, 255}
            //    },
            //    {
            //        {13, 18, 24, 31, 42, 255},//under 65kpa
            //        {12, 18, 25, 32, 44, 255},
            //        {14, 20, 27, 36, 48, 255},
            //        {11, 16, 21, 27, 39, 255},
            //        {13, 20, 27, 35, 47, 255}
            //    },
            //    {
            //        {13, 19, 25, 32, 46, 255},//under 63kpa
            //        {12, 19, 26, 33, 49, 255},
            //        {14, 21, 28, 38, 52, 255},
            //        {11, 16, 22, 28, 41, 255},
            //        {14, 21, 28, 36, 51, 255}
            //    },
            //    {
            //        {13, 19, 26, 33, 52, 255},//under 61kpa
            //        {12, 19, 26, 34, 55, 255},
            //        {14, 22, 29, 39, 58, 255},
            //        {11, 17, 22, 30, 45, 255},
            //        {14, 22, 28, 37, 57, 255}
            //    }
            //};

            //private static byte[,] valveCaliOff_L_Right = new byte[5, 6]
            //{
            //    {110, 140, 170, 200, 230, 255},
            //    {110, 140, 170, 200, 230, 255},
            //    {110, 140, 170, 200, 230, 255},
            //    {140, 170, 190, 220, 240, 255},
            //    {110, 140, 170, 200, 230, 255}
            //};



            public static byte[] CalculateValveTiming(byte tarPres, byte fingerID, int presSource, string whichHand)
            {
                if (whichHand == "Left")
                {
                    valveCaliOn = valveCaliOn_Left;
                    valveCaliOff = valveCaliOff_Left;
                }
                else if (whichHand == "Right")
                {
                    valveCaliOn = valveCaliOn_Right;
                    valveCaliOff = valveCaliOff_Right;
                }
                else
                {
                    Debug.Log("Invalid Device: " + whichHand);
                    return null;
                }

                byte[] valveOnOff = new byte[2];

                int presDif = (170000 - presSource) / 2000;
                if (presDif < 0) { presDif = 0; }
                else if (presDif > 4) { presDif = 4; }

                byte[] valveSelectedOn = new byte[6];
                byte[] valveSelectedOff = new byte[6];
                for (int i = 0; i < 6; i++)
                {
                    valveSelectedOn[i] = valveCaliOn[presDif, fingerID, i];
                    valveSelectedOff[i] = valveCaliOff[fingerID, i];
                }

                valveOnOff = GetValveTiming(tarPres, valveSelectedOn, valveSelectedOff);


                return valveOnOff;
            }


            static byte[] GetValveTiming(byte tarPres, byte[] valveSelectedOn, byte[] valveSelectedOff)
            {
                byte[] valveOnOff = new byte[2];

                if (tarPres == 0)
                {
                    valveOnOff[0] = 0;
                    valveOnOff[1] = 0;
                }
                else if (tarPres < 10)
                {
                    valveOnOff[0] = 150; // vib motor on duration
                    valveOnOff[1] = tarPres; //pwm duty
                }
                else if (tarPres < 20)
                {
                    valveOnOff[0] = (byte)((float)(tarPres - 10) / 10 * (valveSelectedOn[1] - valveSelectedOn[0]) +
                                            valveSelectedOn[0]);
                    valveOnOff[1] = (byte)((float)(tarPres - 10) / 10 * (valveSelectedOff[1] - valveSelectedOff[0]) +
                                           valveSelectedOff[0]);
                }
                else if (tarPres < 30)
                {
                    valveOnOff[0] = (byte)((float)(tarPres - 20) / 10 * (valveSelectedOn[2] - valveSelectedOn[1]) +
                                           valveSelectedOn[1]);
                    valveOnOff[1] = (byte)((float)(tarPres - 20) / 10 * (valveSelectedOff[2] - valveSelectedOff[1]) +
                                           valveSelectedOff[1]);
                }
                else if (tarPres < 40)
                {
                    valveOnOff[0] = (byte)((float)(tarPres - 30) / 10 * (valveSelectedOn[3] - valveSelectedOn[2]) +
                                           valveSelectedOn[2]);
                    valveOnOff[1] = (byte)((float)(tarPres - 30) / 10 * (valveSelectedOff[3] - valveSelectedOff[2]) +
                                           valveSelectedOff[2]);
                }
                else if (tarPres < 50)
                {
                    valveOnOff[0] = (byte)((float)(tarPres - 40) / 10 * (valveSelectedOn[4] - valveSelectedOn[3]) +
                                           valveSelectedOn[3]);
                    valveOnOff[1] = (byte)((float)(tarPres - 40) / 10 * (valveSelectedOff[4] - valveSelectedOff[3]) +
                                           valveSelectedOff[3]);
                }
                else if (tarPres < 60)
                {
                    valveOnOff[0] = (byte)((float)(tarPres - 50) / 10 * (valveSelectedOn[5] - valveSelectedOn[4]) +
                                           valveSelectedOn[4]);
                    valveOnOff[1] = (byte)((float)(tarPres - 50) / 10 * (valveSelectedOff[5] - valveSelectedOff[4]) +
                                           valveSelectedOff[4]);
                }
                else
                {
                    valveOnOff[0] = valveSelectedOn[5];
                    valveOnOff[1] = valveSelectedOff[5];
                }

                return valveOnOff;
            }
        }
    }
}
