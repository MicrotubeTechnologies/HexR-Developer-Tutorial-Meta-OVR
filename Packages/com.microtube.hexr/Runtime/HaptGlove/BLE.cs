using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

public class BLE
{
    private Thread scanThread;
    public Thread ReadingThread;
    private Thread WrittingThread;
    public BLEScan currentScan = new BLEScan();
    public bool isConnected = false;
    public string deviceID;
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
    public BLEScan ScanDevices()    //public static BLEScan ScanDevices()
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
                    if (!deviceNames.ContainsKey(res.id))
                    {
                        deviceIds.Add(res.id);
                        deviceNames.Add(res.id, res.name);
                        if(res.name.Contains("HaptGloveAR") )
                        {
                            Debug.Log("Found |" + res.id.ToString());
                        }
                    }
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

    public void RetrieveProfile(string deviceId, string serviceUuid)
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
            Debug.Log("characteristic found: " + c.uuid + ", user description: " + c.userDescription );
    }

    public bool Subscribe(string deviceId, string serviceUuids, string[] characteristicUuids)
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
        this.deviceID = deviceId;
        if (isConnected)
        {
            return false;
        }

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

    public bool WritePackage(string deviceId, string serviceUuid, string characteristicUuid, byte[] data)
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
    public void WritePackageAsync(string deviceId, string serviceUuid, string characteristicUuid, byte[] data)
    {
        Thread sendThread = new Thread(() =>
        {
            try
            {
                Impl.BLEData packageSend;
                packageSend.buf = data;
                packageSend.size = (short)data.Length;
                packageSend.deviceId = deviceId;
                packageSend.serviceUuid = serviceUuid;
                packageSend.characteristicUuid = characteristicUuid;
                bool rslt = Impl.SendData(packageSend);
                Debug.Log("Write result: " + rslt);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Write error (thread): " + e);
            }
        });
        sendThread.Start();
    }
    // For testing
    public string ReadPackage()
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

    public byte[] ReadBytes()
    {
        try
        {
            Impl.BLEData packageReceived;
            bool result = Impl.PollData(out packageReceived, true);

            if (!result)
            {
                // Normal case: no data yet
                return null;
            }

            if (packageReceived.deviceId != this.deviceID)
            {
                // Warn only once per unexpected device
                Debug.LogWarning($"Ignoring data from unexpected device ID: {packageReceived.deviceId}");
                return null;
            }

            if (packageReceived.size > 512)
            {
                Debug.LogWarning($"Package size too large: {packageReceived.size} bytes.");
                return null;
            }

            byte[] bufData = new byte[packageReceived.size];
            Array.Copy(packageReceived.buf, bufData, packageReceived.size);
            return bufData;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Exception during BLE read: " + e.Message);
            return null;
        }
    }
    public byte[] ReadBytes(int size)
    {
        Impl.BLEData packageReceived;
        bool result = Impl.PollData(out packageReceived, true);

        // 1) If no packet, exit early
        if (!result)
            return null;

        // 2) Validate device match (only after we know we have a packet)
        if (packageReceived.deviceId != this.deviceID)
            return null;

        // 3) Validate packet size
        if (packageReceived.size > 512)

        // 4) Ensure requested size doesn't exceed received size
        if (size > packageReceived.size)
            return null; // or size = packageReceived.size;

        byte[] data = new byte[size];
        for (int i = 0; i < size; i++)
            data[i] = packageReceived.buf[i];

        return data;
    }


    public void Close()
    {
        Impl.Quit();
        isConnected = false;
    }

    public string GetError()
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