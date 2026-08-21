using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

public class BLE
{

    public static Thread scanThread;
    public static BLEScan currentScan = new BLEScan();
    public bool isConnected = false;

    public class BLEScan
    {
        public delegate void FoundDel(string deviceId, string deviceName);
        public delegate void FinishedDel();
        public FoundDel Found;
        public FinishedDel Finished;
        internal bool cancelled = false;

        public void Cancel()
        {
            cancelled = true;
            Impl.StopDeviceScan();
        }
    }

    // don't block the thread in the Found or Finished callback; it would disturb cancelling the scan
    public static BLEScan ScanDevices()    //public static BLEScan ScanDevices()
    {
        if (scanThread == Thread.CurrentThread)
            throw new InvalidOperationException("a new scan can not be started from a callback of the previous scan");
        else if (scanThread != null)
            throw new InvalidOperationException("the old scan is still running");
        currentScan.Found = null;
        currentScan.Finished = null;
        scanThread = new Thread(() =>
        {
            Impl.StartDeviceScan();
            Impl.DeviceUpdate res = new Impl.DeviceUpdate();
            List<string> deviceIds = new List<string>();
            Dictionary<string, string> deviceNames = new Dictionary<string, string>();
            //Impl.ScanStatus status;
            while (Impl.PollDevice(out res, true) != Impl.ScanStatus.FINISHED)
            {
                if (res.nameUpdated)
                {
                    deviceIds.Add(res.id);
                    deviceNames.Add(res.id, res.name);
                }
                // connectable device
                if (deviceIds.Contains(res.id) && res.isConnectable)
                    currentScan.Found?.Invoke(res.id, deviceNames[res.id]);
                // check if scan was cancelled in callback
                if (currentScan.cancelled)
                    break;
            }
            currentScan.Finished?.Invoke();
            scanThread = null;
        });
        scanThread.Start();
        return currentScan;
    }

    public static void RetrieveProfile(string deviceId, string serviceUuid)
    {
        Impl.ScanServices(deviceId);
        Impl.Service service = new Impl.Service();
        while (Impl.PollService(out service, true) != Impl.ScanStatus.FINISHED)
            Debug.Log("service found: " + service.uuid);
        // wait some delay to prevent error
        Thread.Sleep(200);
        Impl.ScanCharacteristics(deviceId, serviceUuid);
        Impl.Characteristic c = new Impl.Characteristic();
        while (Impl.PollCharacteristic(out c, true) != Impl.ScanStatus.FINISHED)
            Debug.Log("characteristic found: " + c.uuid + ", user description: " + c.userDescription);
    }

    public static bool Subscribe(string deviceId, string serviceUuids, string[] characteristicUuids)
    {
        foreach (string characteristicUuid in characteristicUuids)
        {
            bool res = Impl.SubscribeCharacteristic(deviceId, serviceUuids, characteristicUuid);
            // wait some delay to prevent error
            Thread.Sleep(500);
            if (!res)
                return false;
        }
        return true;
    }

    public bool Connect(string deviceId, string serviceUuid, string[] characteristicUuids)
    {
        if (isConnected)
            return false;
        Debug.Log("retrieving ble profile...");
        RetrieveProfile(deviceId, serviceUuid);
        if (GetError() != "Ok")
            throw new Exception("Retrieve failed: " + GetError());
        Debug.Log("subscribing to characteristics...");
        bool result = Subscribe(deviceId, serviceUuid, characteristicUuids);
        if (GetError() != "Ok" || !result)
            throw new Exception("Connection failed: " + GetError());
        isConnected = true;
        return true;
    }

    public static bool WritePackage(string deviceId, string serviceUuid, string characteristicUuid, byte[] data)
    {
        try
        {
            Impl.BLEData packageSend;
            packageSend.buf = data;
            packageSend.size = (short)data.Length;
            packageSend.deviceId = deviceId;
            packageSend.serviceUuid = serviceUuid;
            packageSend.characteristicUuid = characteristicUuid;
            bool rslt = false;
            rslt = Impl.SendData(packageSend);
            return rslt;
        }
        catch (Exception e)
        {
            Debug.Log("Write error: " + e);
            return false;
        }

    }

    // For testing
    public static string ReadPackage()
    {
        string value;

        Impl.BLEData packageReceived;
        bool result = Impl.PollData(out packageReceived, true);
        if (result)
        {
            if (packageReceived.size > 512)
                throw new ArgumentOutOfRangeException("Please keep your ble package at a size of maximum 512, cf. spec!\n"
                    + "This is to prevent package splitting and minimize latency.");

            List<byte> data = new List<byte>();
            while (data.Count < 16)
            {
                data.Add(packageReceived.buf[data.Count]);
            }
            value = packageReceived.characteristicUuid + ":" + packageReceived.size + ":" + BitConverter.ToString(data.ToArray());
            return value;
        }

        return null;
    }

    public static string deviceID;
    public static byte[] ReadBytes()
    {
        Impl.BLEData packageReceived;
        bool result = Impl.PollData(out packageReceived, true);

        if (result)// &(deviceID == packageReceived.deviceId)
        {
            //Debug.Log("Size: " + packageReceived.size);
            //Debug.Log("From "+ deviceID + ": " + packageReceived.deviceId);

            if (packageReceived.size > 512)
                throw new ArgumentOutOfRangeException("Package too large.");

            byte[] bufData = new byte[packageReceived.size];
            Array.Copy(packageReceived.buf, bufData, packageReceived.size);

            return bufData;
        }
        else
        {
            return new byte[] { 0x0 };
        }
    }

    public static byte[] ReadBytes(int size)
    {
        Impl.BLEData packageReceived;
        bool result = Impl.PollData(out packageReceived, true);

        if (result)
        {
            if (packageReceived.size > 512)
                throw new ArgumentOutOfRangeException("Package too large.");

            List<byte> data = new List<byte>();
            while (data.Count < size)
            {
                data.Add(packageReceived.buf[data.Count]);
            }

            return data.ToArray();
        }
        else
        {
            return new byte[] { 0x0 };
        }
    }

    public void Close()
    {
        Impl.Quit();
        isConnected = false;
    }

    public static string GetError()
    {
        Impl.ErrorMessage buf;
        Impl.GetError(out buf);
        return buf.msg;
    }

    ~BLE()
    {
        Close();
    }
}