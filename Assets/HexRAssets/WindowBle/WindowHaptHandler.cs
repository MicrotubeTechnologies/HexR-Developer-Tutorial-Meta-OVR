using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class WindowHaptHandler : MonoBehaviour
{
    [Header("BLE Connection")]
    public string targetDeviceName = "HaptGloveAR Right";
    public string serviceUuid = "{000000ff-0000-1000-8000-00805f9b34fb}";
    public string[] characteristicUuids = { "{0000ff01-0000-1000-8000-00805f9b34fb}" };

    BLE ble;
    BLE.BLEScan scan;

    IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
    int devicesCount = 0;
    public bool isScanning = false, btConnection = false, isTimerRunning = false, isCalibration = false;
    string deviceId = null;

    // BLE Threads 
    Thread scanningThread, connectionThread, readingThread, serialthread, calibrationThread;
    // Start is called before the first frame update
    void Start()
    {
        ble = new BLE();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            BTConnection();
        }
    }

    // Establish communication
    public void BTConnection()
    {
        if (ble.isConnected)
        {
            ble.Close();
            ble = new BLE();

            Debug.Log("STOP BLE. BLE isConnected: " + ble.isConnected.ToString());
        }
        else
        {
            devicesCount = 0;
            isScanning = true;
            discoveredDevices.Clear();
            deviceId = null;
            scanningThread = new Thread(ScanBleDevices);
            scanningThread.Start();
            Debug.Log("Scanning for..." + targetDeviceName);
        }
    }

    void ScanBleDevices()
    {
        scan = BLE.ScanDevices();
        Debug.Log("BLE.ScanDevices() started.");
        scan.Found = (_deviceId, deviceName) =>
        {
            Debug.Log("found device with name: " + deviceName);
            discoveredDevices.Add(_deviceId, deviceName);

            //if found the target device, immediately stop scan and attempt to connect
            if (deviceId == null && deviceName.Contains(targetDeviceName))
            {
                deviceId = _deviceId;
                //ble.deviceID = deviceId;
                StartConHandler();
            }
        };

        scan.Finished = () =>
        {
            isScanning = false;
            Debug.Log("scan finished");
            if (deviceId == null)
                deviceId = "-1";
        };
        while (deviceId == null)
            Thread.Sleep(500);
        scan.Cancel();
        scanningThread = null;
        isScanning = false;

        if (deviceId == "-1")
        {
            Debug.Log("no device found!");
            return;
        }

    }

    // Start establish BLE connection with
    // target device in dedicated thread.
    public void StartConHandler()
    {
        connectionThread = new Thread(ConnectBleDevice);
        connectionThread.Start();
    }

    private void ConnectBleDevice()
    {
        if (deviceId != null)
        {
            try
            {
                ble.Connect(deviceId,
                    serviceUuid,
                    characteristicUuids.ToArray());
            }
            catch (Exception e)
            {
                Debug.Log("Could not establish connection to device with ID " + deviceId + "\n" + e);
            }
        }
        if (ble.isConnected)
            Debug.Log("Connected to: " + targetDeviceName);
    }
}
