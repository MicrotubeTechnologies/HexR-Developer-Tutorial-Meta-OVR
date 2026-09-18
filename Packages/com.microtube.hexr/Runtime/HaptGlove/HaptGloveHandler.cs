using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using UnityEngine;
using TMPro;
using ArduinoBluetoothAPILocal;
//using ArduinoBluetoothAPI;
using UnityEngine.Events;

namespace HaptGlove
{
    public class HaptGloveHandler: MonoBehaviour
    {
        //public UnityAction onBluetoothLogChanged;
        public UnityAction<HandType> onBluetoothConnected;
        public UnityAction<HandType> onBluetoothDisconnected;
        public UnityAction<HandType> onBluetoothConnectionFailed;
        public UnityAction<HandType, bool> onPumpAction;
        public UnityAction<BatteryState> onBatteryStateChange;
        /// <summary>Fires for every device seen during a scan (deviceId, deviceName), Android and Windows alike -- for building a device picker instead of connecting to the first name match.</summary>
        public UnityAction<string, string> onDeviceFound;

        public enum HandType
        {
            Left,
            Right
        };
        public enum TargetPlatform
        {
            Android,
            Window
        };
        public HandType whichHand = HandType.Left;
        public TargetPlatform BuildPlatform = TargetPlatform.Android;

        public enum BatteryState
        {
            Full,
            Medium,
            Low
        }
        public BatteryState batteryState = BatteryState.Full;

        private BluetoothHelper btHelper;
        private static string serviceUUID = "00ff";
        private static string characteristicUUID = "ff01";
        private BluetoothHelperCharacteristic bluetoothHelperCharacteristic;

        [HideInInspector] public byte sourcePres = 80;
        [HideInInspector] public string targetDeviceName = "HaptGloveAR Right";
        public string targetDeviceAddress = "";
        public string OptionalCustomDeviceName = "HaptGloveAR Right";
        public bool UseCustomDeviceName = false;
        [HideInInspector] public string btText = "";
        [HideInInspector] public string btSendText = "";
        private string StatusText;

        public bool bleConnected = false;

        // Retry-until-connected bookkeeping (Android only). connectRequested is true from
        // the moment the user taps Connect until a connection succeeds, they cancel, or
        // they explicitly disconnect -- distinguishes "still trying" from "gave up".
        // pendingRetry is non-null only while waiting out the delay before the next
        // attempt; a tap while it's non-null cancels instead of connecting. Retries are
        // triggered exclusively from OnConnectionFailed (an actual plugin callback saying
        // an attempt concluded) -- deliberately never from a timer/poll guessing that a
        // previous attempt is "probably" done, which is what destabilized connections the
        // last time this was tried (re-invoking Connect() while the plugin might still be
        // settling from the previous call).
        private bool connectRequested = false;
        private Coroutine pendingRetry;

        // Bounded exponential backoff: 1, 2, 4, 8, 8... seconds, abandoned after
        // RetryGiveUpSeconds of trying. Bounded rather than retry-forever so a glove that is
        // genuinely switched off stops churning the radio and hands the user back a panel
        // they can act on, instead of appearing to hang.
        private static readonly float[] RetryBackoffSeconds = { 1f, 2f, 4f, 8f };
        private const float RetryGiveUpSeconds = 60f;
        private int retryAttempt = 0;
        private float campaignStartedAt = 0f;

        // Polls the link while connected, because the Android plugin has no disconnect
        // callback at all -- BluetoothHelper exposes OnConnected/OnConnectionFailed/
        // OnScanEnded and nothing for an unsolicited drop. Without this, a glove that goes
        // out of range or runs flat leaves bleConnected true forever and the app silently
        // sends haptics into a dead link.
        private Coroutine linkSupervision;
        private const float LinkPollSeconds = 1f;

        // Fallback for a scan/connect attempt the plugin never resolves at all -- observed
        // on-device as getting permanently stuck on "Searching..." with OnScanEnded simply
        // never firing, which OnConnectionFailed-triggered retry has no way to detect since
        // no callback ever happens. connectGeneration increments at the start of every
        // fresh attempt; the watchdog captures it and only acts if, after a long wait,
        // NOTHING has changed it and we're still not connected -- i.e. truly silent, not a
        // guess about a still-progressing attempt. The timeout is deliberately generous
        // (comfortably longer than any legitimate scan+connect should take) so this can't
        // fire while a real attempt is still settling -- that overlap is what destabilized
        // connections the last time a timeout-ish mechanism was tried here.
        private int connectGeneration = 0;
        private const float AttemptTimeoutSeconds = 20f;

        public Haptics haptics = new Haptics();
        [HideInInspector] public List<int> hapticsInteratableLayers;

        // Outcomes of the plugin calls made on the Android UI thread (PluginConnect /
        // PluginScan), handed back to Unity's thread and run at the top of Update().
        private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

        //Quest BLE compromise
        [HideInInspector] public bool isQuest, isScanning = false;
        private List<byte> questBleBuffer = new List<byte>();
        private int timeCurrent;
        private int timeLastSend;
        private int bleDelayThreshold = 100; //ms

        #region Window Connection Variable
        BLE ble;
        BLE.BLEScan scan;

        // BLE Threads 
        Thread scanningThread, connectionThread, readingThread, sendingThread, serialthread, calibrationThread;
        string deviceId = null;

        IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
        private bool  isTimerRunning = false, isCalibration = false;
        private string serviceUuid = "{000000ff-0000-1000-8000-00805f9b34fb}";
        private string[] characteristicUuids = { "{0000ff01-0000-1000-8000-00805f9b34fb}" };
        #endregion
        void Start()
        {
            ble = new BLE();
            haptics.whichHand = whichHand.ToString();
            bluetoothHelperCharacteristic = new BluetoothHelperCharacteristic(characteristicUUID, serviceUUID);

            if (whichHand == HandType.Left)
            {
                targetDeviceName = "HaptGloveAR Left"; //HaptGloveAR Left
            }
            else if (whichHand == HandType.Right)
            {
                targetDeviceName = "HaptGloveAR Right";
            }

            rememberedDeviceId = PlayerPrefs.GetString(DeviceSelectionKey, "");
            if (!string.IsNullOrEmpty(rememberedDeviceId) && BuildPlatform == TargetPlatform.Android)
            {
                targetDeviceAddress = rememberedDeviceId;
            }

            BluetoothHelper.BLE = true;

            if(BuildPlatform == TargetPlatform.Android)
            {
                try
                {
                    btHelper = BluetoothHelper.GetNewInstance(targetDeviceName);
                    btHelper.OnScanEnded += OnScanEnded;
                    btHelper.OnConnected += OnConnected;
                    btHelper.OnConnectionFailed += OnConnectionFailed;
                    btHelper.OnCharacteristicChanged += OnCharacteristicChanged;
                    btHelper.OnCharacteristicNotFound += OnCharacteristicNotFound;
                    btHelper.OnServiceNotFound += OnServiceNotFound;
                }
                catch (Exception e)
                {
                    btText = e.ToString();
                }
            }
            else
            {
                readingThread = new Thread(ReadBleData);
            }

        }

        private float updateBatLevelInterval = 5f;//
        private float timerInUpdate = 0;
        void Update()
        {
            // Android plugin-call outcomes, posted from the UI thread by PluginConnect/PluginScan.
            while (mainThreadActions.TryDequeue(out Action pending))
            {
                try
                {
                    pending();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }

            // Windows/Editor connection results, raised here so subscribers run on the main
            // thread. Without this the Windows path never raised onBluetoothConnected at all,
            // so HexRManager never learned it had connected -- leaving its per-hand
            // "connect in flight" latch stuck on and silently refusing every later attempt.
            if (windowsConnectSucceeded)
            {
                windowsConnectSucceeded = false;
                if (!string.IsNullOrEmpty(deviceId) && deviceId != "-1")
                {
                    RememberDevice(deviceId);
                }
                if (onBluetoothConnected != null)
                {
                    onBluetoothConnected(whichHand);
                }
            }
            if (windowsConnectFailed)
            {
                windowsConnectFailed = false;
                if (onBluetoothConnectionFailed != null)
                {
                    onBluetoothConnectionFailed(whichHand);
                }
            }

            if (deviceSelectionDirty)
            {
                deviceSelectionDirty = false;
                if (string.IsNullOrEmpty(rememberedDeviceId))
                    PlayerPrefs.DeleteKey(DeviceSelectionKey);
                else
                    PlayerPrefs.SetString(DeviceSelectionKey, rememberedDeviceId);
                PlayerPrefs.Save();
            }

            if(UseCustomDeviceName)
            {
                targetDeviceName = OptionalCustomDeviceName;
            }
            else
            {
                if (whichHand == HandType.Left)
                {
                    targetDeviceName = "HaptGloveAR Left"; //HaptGloveAR Left
                }
                else if (whichHand == HandType.Right)
                {
                    targetDeviceName = "HaptGloveAR Right";
                }
            }

            if (bleConnected)
            {
                timerInUpdate++;
                if (timerInUpdate < updateBatLevelInterval)
                {
                    float batLevel = GetBatteryLevel();
                    if (batLevel >= 1)
                    {
                        if (batteryState != BatteryState.Full)
                        {
                            batteryState = BatteryState.Full;
                            Debug.Log(BatteryState.Full);
                            if (onBatteryStateChange != null)
                                onBatteryStateChange(batteryState);
                        }
                    }
                    else if (batLevel >= 0.2f)
                    {
                        if (batteryState != BatteryState.Medium)
                        {
                            batteryState = BatteryState.Medium;
                            Debug.Log(BatteryState.Medium);
                            if (onBatteryStateChange != null)
                                onBatteryStateChange(batteryState);
                        }
                    }
                    else
                    {
                        if (batteryState != BatteryState.Low)
                        {
                            batteryState = BatteryState.Low;
                            Debug.Log(BatteryState.Low);
                            if (onBatteryStateChange != null)
                                onBatteryStateChange(batteryState);
                        }
                    }
                    timerInUpdate = 0;
                }




                // Target device is connected and GUI knows.
                if (BuildPlatform == TargetPlatform.Window)
                {
                    if (!readingThread.IsAlive)
                    {
                        readingThread = new Thread(ReadBleData);
                        readingThread.Start();
                    }
                }
            }

            //if (bleConnected)
            //{
            //    sensorText =
            //        "\n" + "Pressure: " +
            //        "\n" + "Thumb: " + pressureData[0] +
            //        "\n" + "Index: " + pressureData[1] +
            //        "\n" + "Middle: " + pressureData[2] +
            //        "\n" + "Ring: " + pressureData[3] +
            //        "\n" + "Pinky: " + pressureData[4] +
            //        "\n\n" + "Pump: " + pressureData[5];
            //}


            //btTextLog.text = btText;
            //sensorTextLog.text = sensorText;
        }

        private bool isSubscribed;
        public void BTConnection()
        {
            if(BuildPlatform == TargetPlatform.Android)
            {
                AndroidConnection();
            }
            else if(BuildPlatform == TargetPlatform.Window)
            {
                WindowConnection();
            }
        }

        #region Device Selection
        // Lets a specific physical glove be targeted instead of "first device whose
        // advertised name matches" -- the problem when more than one glove for the
        // same hand is in range. Persisted per hand so left/right don't collide.
        //
        // rememberedDeviceId is an in-memory cache read/written from RememberDevice,
        // ForgetDevice and ScanBleDevices' background scanningThread alike -- a plain
        // string field is safe across threads, but PlayerPrefs is not, so the actual
        // disk write is deferred to the main-thread Update() via deviceSelectionDirty.
        // This matters because RememberDevice/ConnectToDevice are meant to be called
        // from an onDeviceFound handler, which itself can fire from that same
        // background scan thread on the Windows path.
        private string rememberedDeviceId = "";
        private bool deviceSelectionDirty = false;
        private string DeviceSelectionKey => "HexR_" + whichHand + "_DeviceId";

        public string GetRememberedDeviceId() => rememberedDeviceId;

        public void RememberDevice(string deviceId)
        {
            rememberedDeviceId = deviceId;
            deviceSelectionDirty = true;
            if (BuildPlatform == TargetPlatform.Android)
            {
                targetDeviceAddress = deviceId;
            }
        }

        public void ForgetDevice()
        {
            rememberedDeviceId = "";
            deviceSelectionDirty = true;
            if (BuildPlatform == TargetPlatform.Android)
            {
                targetDeviceAddress = "";
            }
        }

        /// <summary>Remembers deviceId (from an onDeviceFound callback) and connects to it directly.</summary>
        public void ConnectToDevice(string deviceId)
        {
            RememberDevice(deviceId);
            BTConnection();
        }
        #endregion

        public void BTDisconnection()
        {
            if (BuildPlatform == TargetPlatform.Android)
            {
                AndroidQuit();
            }
            else if (BuildPlatform == TargetPlatform.Window)
            {
                //
            }
        }

        #region Window Connection Handler
        private void WindowConnection()
        {
            if (ble.isConnected)
            {
                ble.Close();
                ble = new BLE();
                bleConnected = false;
                Debug.Log("STOP BLE. BLE isConnected: " + ble.isConnected.ToString());
            }
            else
            {
                isScanning = true;
                discoveredDevices.Clear();
                deviceId = null;
                scanningThread = new Thread(ScanBleDevices);
                scanningThread.Start();
                Debug.Log("Scanning for..." + targetDeviceName);
            }
        }
        // Signalled by whichever comes first: the target device turning up in the scan, or the
        // scan ending without it. Replaces a 500 ms polling loop.
        private readonly System.Threading.ManualResetEventSlim scanSettled =
            new System.Threading.ManualResetEventSlim(false);

        void ScanBleDevices()
        {
            scanSettled.Reset();
            scan = ble.ScanDevices();
            Debug.Log("BLE.ScanDevices() started.");
            btText = "Scanning for Devices";

            // rememberedDeviceId is read here, not via PlayerPrefs -- this closure runs on
            // BLE.cs's own background scan thread, and PlayerPrefs is main-thread-only.
            string targetDeviceId = rememberedDeviceId;

            scan.Found = (_deviceId, deviceName) =>
            {
                Debug.Log("Found device with name: " + deviceName);

                // ✅ Prevent duplicate entries
                if (!discoveredDevices.ContainsKey(_deviceId))
                {
                    discoveredDevices.Add(_deviceId, deviceName);
                }
                else
                {
                    Debug.Log($"Duplicate device ignored: {_deviceId} - {deviceName}");
                }

                onDeviceFound?.Invoke(_deviceId, deviceName);

                // If a specific device was remembered (RememberDevice/ConnectToDevice),
                // only that exact device may auto-connect -- otherwise fall back to the
                // original first-name-match behaviour, unchanged for single-glove setups.
                bool isTargetDevice = !string.IsNullOrEmpty(targetDeviceId)
                    ? _deviceId == targetDeviceId
                    : deviceName.Contains(targetDeviceName);

                if (deviceId == null && isTargetDevice)
                {
                    deviceId = _deviceId;
                    StartConHandler();
                    scanSettled.Set();
                }
            };

            scan.Finished = () =>
            {
                isScanning = false;
                Debug.Log("Scan finished.");
                if (deviceId == null)
                    deviceId = "-1";
                scanSettled.Set();
            };

            // Wait for either outcome to signal, rather than waking twice a second to ask.
            // The old `while (deviceId == null) Thread.Sleep(500);` added up to half a second
            // to every single connect -- including the common case where the glove is the
            // first device the scan reports.
            scanSettled.Wait();

            scan.Cancel(); // Stop scanning once we found or gave up
            scanningThread = null;
            isScanning = false;

            if (deviceId == "-1")
            {
                btText = "No device found!";
                Debug.Log("No device found!");
                return;
            }
        }
        // Start establish BLE connection with
        // target device in dedicated thread.
        public void StartConHandler()
        {

            btText = "Connecting to " + targetDeviceName;
            connectionThread = new Thread(ConnectBleDevice);
            connectionThread.Start();
        }

        // Set on the connection thread, consumed on the main thread in Update(). The Android
        // path gets main-thread marshalling for free from the plugin's Synchronizer; the
        // Windows path runs Connect() on a raw Thread, so raising the UnityActions from here
        // directly would land subscriber code (which touches TextMeshPro and SetActive) on a
        // background thread.
        private volatile bool windowsConnectSucceeded;
        private volatile bool windowsConnectFailed;

        private void ConnectBleDevice()
        {
            bool connected = false;

            if (deviceId != null)
            {
                try
                {
                    ble.Connect(deviceId,
                        serviceUuid,
                        characteristicUuids.ToArray());
                    connected = true;
                }
                catch (Exception e)
                {
                    Debug.Log("Could not establish connection to device with ID " + deviceId + "\n" + e);
                }
            }

            // Report what actually happened. This used to set bleConnected = true inside the
            // try and then log "Connected to: ..." unconditionally, so a Connect() that threw
            // still left the status text claiming success.
            bleConnected = connected;

            if (connected)
            {
                Debug.Log("Connected to: " + targetDeviceName);
                btText = "Connected to: " + targetDeviceName;
                windowsConnectSucceeded = true;
            }
            else
            {
                btText = "Could not connect to " + targetDeviceName;
                windowsConnectFailed = true;
            }
        }

        private void ReadBleData()
        {

            try
            {
                byte[] buf = ble.ReadBytes(); //data input via bytes
                if (buf != null)
                {
                    Debug.Log("OnCharacteristicChanged" + buf);
                    haptics.DecodeGloveData(buf);

                    Debug.Log("Received data length: " + buf.Length);
                }
                Thread.Sleep(1); // Add delay to avoid busy loop; adjust to your needs
            }
            catch (Exception e)
            {
                Debug.LogError("Exception during BLE read: " + e.Message);
            }
        }
        private void CheckConnection()
        {
            try
            {
                byte[] buf = ble.ReadBytes(); //data input via bytes
                if (buf != null)
                {
                    Debug.Log("OnCharacteristicChanged" + buf);
                    haptics.DecodeGloveData(buf);

                    Debug.Log("Received data length: " + buf.Length);
                }
                Thread.Sleep(1); // Add delay to avoid busy loop; adjust to your needs
            }
            catch (Exception e)
            {
                Debug.LogError("Exception during BLE read: " + e.Message);
            }
        }

        public void DeviceDetailScan()
        {

        }
        #endregion

        #region Android Connection Handler
        private void AndroidConnection()
        {
            // Only push an address we actually have. setDeviceAddress() nulls the helper's
            // deviceName as a side effect, so calling it with "" -- the default, i.e. no glove
            // remembered yet -- wipes the name GetNewInstance() was constructed with. After
            // that searchForDevice() can match neither name nor address, devicePaired never
            // goes true, and the Connect() at the end of the scan throws
            // BlueToothNotReadyException. Guard on emptiness, not null.
            if (!string.IsNullOrEmpty(targetDeviceAddress))
            {
                btHelper.setDeviceAddress(targetDeviceAddress);
            }
            else
            {
                btHelper.setDeviceName(targetDeviceName);
            }
            Debug.Log("BTConnection");
            if (bleConnected)
            {
                Debug.Log("bleConnected");
                connectRequested = false;
                retryAttempt = 0;
                // The user asked for this one, so stop watching for a drop before it happens --
                // otherwise the supervisor sees the link go down and starts reconnecting to the
                // glove they just switched off.
                StopLinkSupervision();
                btHelper.Disconnect();
                bleConnected = false;
                if (onBluetoothDisconnected != null)
                {
                    onBluetoothDisconnected(whichHand);
                }

                btText = targetDeviceName + " Disconnected!";
            }
            else if (pendingRetry != null)
            {
                // A retry is currently scheduled (waiting out the delay after a failed
                // attempt) -- a tap now means cancel, not "try again immediately".
                Debug.Log("Cancelling pending retry");
                StopCoroutine(pendingRetry);
                pendingRetry = null;
                connectRequested = false;
                btText = "Cancelled, tap to retry";
            }
            else
            {
                Debug.Log("Not bleConnected");
                if (!connectRequested)
                {
                    // A fresh campaign (a tap), not a scheduled retry -- reset the backoff.
                    retryAttempt = 0;
                }
                connectRequested = true;
                StartAttemptWatchdog();
                // Skip the scan when the plugin can already resolve the target. This is the
                // whole of the reconnect latency: ScanNearbyDevices() runs a fixed 8 s window
                // inside the plugin and only reports at the end of it, so a glove found in the
                // first 200 ms still costs the full 8 s before Connect() is even attempted.
                //
                // isDevicePaired() is a live query -- it re-runs the plugin's searchForDevice()
                // against its device cache, and that cache is a static field that survives
                // between scans -- so once any scan has seen this glove, a later connect can go
                // straight to Connect(). Deliberately a live query: the cached flag this used to
                // branch on went stale the moment anything else touched the helper, which is
                // why it has been removed rather than kept alongside.
                bool resolvable = false;
                try
                {
                    resolvable = btHelper.isDevicePaired();
                }
                catch (Exception e)
                {
                    Debug.Log("isDevicePaired threw, treating as unresolvable: " + e.Message);
                }

                if (resolvable)
                {
                    btText = "Connecting to " + targetDeviceName;
                    PluginConnect(e =>
                    {
                        // The cache was stale after all (glove powered off, or it changed
                        // address). Fall back to a full scan rather than giving up.
                        Debug.Log("Direct connect failed, falling back to a scan: " + e.Message);
                        StartPluginScan();
                    });
                    return;
                }

                StartPluginScan();
            }
        }

        private void StartPluginScan()
        {
            // The user may have cancelled while a direct-connect attempt was in flight.
            if (!connectRequested)
            {
                return;
            }

            btText = "BLE Start Scanning for: " + targetDeviceName;
            PluginScan(
                started =>
                {
                    if (started)
                    {
                        return;
                    }

                    // A scan is already in flight. isScanning is static across every helper
                    // instance, so this is normally the other hand holding the radio. Calling
                    // Connect() here -- as this used to -- can only throw, because devicePaired
                    // goes true only once a scan has populated the shared cache, and we already
                    // know it is false. Wait and retry instead.
                    btText = "Bluetooth busy, retrying shortly...";
                    ScheduleRetry();
                },
                e =>
                {
                    // Adapter off, permission revoked mid-session, and the like. The plugin
                    // will never report OnScanEnded for a scan it refused to start, so this is
                    // the definitive end of the attempt -- retry from here.
                    Debug.LogWarning("ScanNearbyDevices threw: " + e.Message);
                    btText = "Bluetooth scan failed: " + e.Message;
                    ScheduleRetry();
                });
        }

        // The two plugin calls that must run on the Android UI thread. The bundled Java BLE
        // helper builds a Handler with the no-argument constructor inside scanNearbyDevices()
        // and connect(), which binds to the calling thread's Looper -- Unity's thread has none,
        // so called from here they die with "Can't create handler inside thread that has not
        // called Looper.prepare()". Everything else this class asks of the helper
        // (setDeviceName, isDevicePaired, Disconnect, Subscribe, WriteCharacteristic) is a
        // plain JNI call and stays on Unity's thread.
        //
        // The hop is asynchronous, so the outcome comes back through mainThreadActions and is
        // handled on Unity's thread in Update(). That matters because everything downstream --
        // StartCoroutine for the watchdog and the retries, Time.unscaledTime, PlayerPrefs, the
        // UnityActions HexRManager answers by touching TextMeshPro -- is main-thread-only. The
        // previous arrangement marshalled the whole of BTConnection() onto the UI thread, and
        // the first StartCoroutine in it threw there, so the scan was never even started: the
        // panel sat on "Searching..." with the connect slot latched shut. And the plugin's
        // callbacks (OnScanEnded and friends) arrive on Unity's thread via the wrapper's
        // Synchronizer, so the Connect() issued from OnScanEnded needs this hop just as much
        // as the one issued from a tap.
        private void PluginConnect(Action<Exception> onFailed)
        {
            BluetoothHelper helper = btHelper;
            AndroidUiThread.Run(() =>
            {
                try
                {
                    helper.Connect();
                }
                catch (Exception e)
                {
                    mainThreadActions.Enqueue(() => onFailed(e));
                }
            });
        }

        private void PluginScan(Action<bool> onStarted, Action<Exception> onFailed)
        {
            BluetoothHelper helper = btHelper;
            AndroidUiThread.Run(() =>
            {
                bool started;
                try
                {
                    started = helper.ScanNearbyDevices();
                }
                catch (Exception e)
                {
                    mainThreadActions.Enqueue(() => onFailed(e));
                    return;
                }

                mainThreadActions.Enqueue(() => onStarted(started));
            });
        }

        // Connect() threw before the plugin ever attempted the link (BlueToothNotReady, adapter
        // off...), so no OnConnectionFailed is coming for it. Report it as one so the same
        // subscribers hear about it and the same retry path takes over.
        private void OnConnectThrew(Exception e)
        {
            Debug.Log("Connect threw: " + e.Message);
            OnConnectionFailed(btHelper);
        }

        private void OnScanEnded(BluetoothHelper helper, LinkedList<BluetoothDevice> devices)
        {
            btText = "BLE Scan Ended";

            foreach (BluetoothDevice device in devices)
            {
                onDeviceFound?.Invoke(device.DeviceAddress, device.DeviceName);
            }

            if (helper.isDevicePaired() & !bleConnected)
            {
                btText = "Device start to connect.";
                PluginConnect(OnConnectThrew);

                //StartCoroutine(CheckReConnect(1, helper));
            }
            else if (!helper.isDevicePaired())
            {
                // isDevicePaired() reflects Android OS-level bonding, which this glove
                // doesn't need to already have to accept a GATT connection -- bonding (if
                // the plugin/peripheral do it at all) happens as a side effect of
                // Connect(), not a precondition for it. Attempt the connect anyway instead
                // of giving up; OnConnectionFailed will fire and schedule a retry if it
                // genuinely can't connect.
                btText = "Device not paired, connecting anyway.";
                PluginConnect(OnConnectThrew);
            }
            else if (helper.isDevicePaired() & bleConnected)
            {
                btText = targetDeviceName + " connected.";
            }

        }


        IEnumerator CheckReConnect(float sec, BluetoothHelper helper)
        {
            yield return new WaitForSeconds(sec);

            if (!helper.isConnected())
            {
                PluginConnect(OnConnectThrew);
                btText = "Device start to Re-connect.";
            }

        }

        private void OnConnected(BluetoothHelper helper)
        {
            btText = targetDeviceName + " Connected!";
            //dialogText = deviceName + " Connected!";
            //Dialog.Open(dialog, DialogButtonType.OK, "Bluetooth", dialogText, true);
            bleConnected = true;
            //btIndicator.SetActive(true);
            
            //List<BluetoothHelperService> services = helper.getGattServices();
            //foreach (BluetoothHelperService s in services)
            //{
            //    Debug.Log($"Service : [{s.getName()}]");
            //    //btText = btText + "\nOnConnected: " + $"Service : [{s.getName()}]";
            //    foreach (BluetoothHelperCharacteristic c in s.getCharacteristics())
            //    {
            //        Debug.Log($"Characteristic : [{c.getName()}]");
            //        //btText = btText + "\nOnConnected: " + $"Characteristic : [{c.getName()}]";
            //    }
            //}
            
            helper.Subscribe(bluetoothHelperCharacteristic);
            isSubscribed = true;

            // The campaign is over: stop retrying, and let the next one use the fast path
            // again from its first attempt.
            connectRequested = false;
            retryAttempt = 0;
            if (pendingRetry != null)
            {
                StopCoroutine(pendingRetry);
                pendingRetry = null;
            }

            // Prime the direct-connect fast path for next time. Nothing else writes this --
            // the plumbing already existed but was never primed, so every connect paid for a
            // full scan even when the same glove had just been connected.
            try
            {
                string address = helper.getDeviceAddress();
                if (!string.IsNullOrEmpty(address))
                {
                    RememberDevice(address);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Could not read the connected device address: " + e.Message);
            }

            StartLinkSupervision();

            if (onBluetoothConnected != null)
            {
                onBluetoothConnected(whichHand);
            }
            
        }

        private void OnConnectionFailed(BluetoothHelper helper)
        {
            btText = targetDeviceName + "Connection Failed, please reconnect.";
            //dialogText = deviceName + "Connection Failed";
            //Dialog.Open(dialog, DialogButtonType.OK, "Bluetooth", dialogText, true);
            bleConnected = false;
            //btIndicator.SetActive(false);

            //btText = btText + "\nBLE Start Scanning...";
            //dialogText = "Bluetooth Scanning...";
            //Dialog.Open(dialog, DialogButtonType.OK, "Device", dialogText, true);
            //btHelper.ScanNearbyDevices();


            if (onBluetoothConnectionFailed != null)
            {
                onBluetoothConnectionFailed(whichHand);
            }

            ScheduleRetry();
        }

        // Retry-until-connected: only ever called from a real, definitive signal that an
        // attempt concluded -- OnConnectionFailed (the plugin), or the attempt watchdog
        // (a long timeout with zero callback activity at all, i.e. genuine silence, not a
        // guess about a still-progressing attempt). Never from a timer guessing a
        // still-in-flight attempt is "probably" done -- that's what destabilized
        // connections the last time this was tried. connectRequested is false if the user
        // already cancelled (or never asked in the first place), in which case this is a
        // no-op.
        private void ScheduleRetry()
        {
            if (!connectRequested || BuildPlatform != TargetPlatform.Android)
            {
                return;
            }

            // Guard against this firing twice before a scheduled retry completes --
            // without this, the second call would overwrite pendingRetry and orphan the
            // first coroutine, risking two overlapping retry attempts (the exact failure
            // mode this whole design exists to avoid).
            if (pendingRetry != null)
            {
                StopCoroutine(pendingRetry);
            }

            if (retryAttempt == 0)
            {
                campaignStartedAt = Time.unscaledTime;
            }
            else if (Time.unscaledTime - campaignStartedAt >= RetryGiveUpSeconds)
            {
                Debug.Log("Giving up reconnecting to " + targetDeviceName + " after "
                    + RetryGiveUpSeconds + "s.");
                btText = targetDeviceName + " not found. Tap to try again.";
                connectRequested = false;
                pendingRetry = null;
                retryAttempt = 0;
                if (onBluetoothConnectionFailed != null)
                {
                    onBluetoothConnectionFailed(whichHand);
                }
                return;
            }

            float delay = RetryBackoffSeconds[Mathf.Min(retryAttempt, RetryBackoffSeconds.Length - 1)];
            retryAttempt++;
            btText += " Retrying in " + delay + "s...";
            pendingRetry = StartCoroutine(RetryConnectAfterDelay(delay));
        }

        private IEnumerator RetryConnectAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            pendingRetry = null;
            BTConnection();
        }

        // Started on connect, stopped on drop. Only ever *reports* the drop -- it never calls
        // Connect() itself. That distinction is the point: the previous attempt at auto-
        // reconnect was reverted because it re-entered Connect() from a timer that was
        // guessing an attempt had finished, which destabilised connections. Reconnection here
        // still happens only from a definitive callback, via the normal retry path.
        private void StartLinkSupervision()
        {
            StopLinkSupervision();
            if (BuildPlatform == TargetPlatform.Android)
            {
                linkSupervision = StartCoroutine(SuperviseLink());
            }
        }

        private void StopLinkSupervision()
        {
            if (linkSupervision != null)
            {
                StopCoroutine(linkSupervision);
                linkSupervision = null;
            }
        }

        private IEnumerator SuperviseLink()
        {
            while (bleConnected)
            {
                yield return new WaitForSeconds(LinkPollSeconds);

                if (!bleConnected)
                {
                    yield break;
                }

                bool stillUp;
                try
                {
                    stillUp = btHelper != null && btHelper.isConnected();
                }
                catch (Exception e)
                {
                    Debug.Log("isConnected threw, treating as dropped: " + e.Message);
                    stillUp = false;
                }

                if (stillUp)
                {
                    continue;
                }

                Debug.Log(targetDeviceName + " dropped.");
                bleConnected = false;
                isSubscribed = false;
                linkSupervision = null;

                btText = targetDeviceName + " disconnected. Reconnecting...";
                if (onBluetoothDisconnected != null)
                {
                    onBluetoothDisconnected(whichHand);
                }

                // Reopen a connect campaign so the backoff retry takes over. The remembered
                // address means the first attempt skips the scan entirely.
                connectRequested = true;
                retryAttempt = 0;
                ScheduleRetry();
                yield break;
            }

            linkSupervision = null;
        }

        private void StartAttemptWatchdog()
        {
            connectGeneration++;
            StartCoroutine(AttemptWatchdog(connectGeneration));
        }

        private IEnumerator AttemptWatchdog(int generation)
        {
            yield return new WaitForSeconds(AttemptTimeoutSeconds);

            // Still the same attempt (no OnScanEnded/OnConnected/OnConnectionFailed ever
            // advanced connectGeneration by starting a new one) and still not connected --
            // the plugin has gone silent on this attempt with no callback forthcoming.
            if (generation == connectGeneration && !bleConnected)
            {
                Debug.Log("Connect attempt timed out with no plugin callback -- retrying.");
                btText = "Connection attempt timed out.";
                ScheduleRetry();
            }
        }

        private void OnCharacteristicChanged(BluetoothHelper helper, byte[] data, BluetoothHelperCharacteristic characteristic)
        {
            Debug.Log("OnCharacteristicChanged");
            haptics.DecodeGloveData(data);
        }

        private void OnCharacteristicNotFound(BluetoothHelper helper, string service, string characteristic)
        {
            btText = btText + "\nData Characteristic Not Found";
        }

        private void OnServiceNotFound(BluetoothHelper helper, string service)
        {
            btText = btText + "\nData Service Not Found";
            //bleConnected = false;
            //btIndicator.SetActive(false);

            //helper.Subscribe(bluetoothHelperCharacteristic);
            //btText = btText + "\nSubscribing services";
        }

        #endregion



        void FixedUpdate()
        {
            if (isQuest)
            {
                timeCurrent = Environment.TickCount;

                // Haptics are queued for both hands whether or not either is connected, so
                // without this the hand that isn't connected writes into a dead helper every
                // 100 ms and logs a BlueToothNotConnectedException each time. Drop what
                // accumulated while disconnected rather than flushing it on reconnect: it is
                // haptics for a grab that finished long ago.
                if (!bleConnected)
                {
                    if (questBleBuffer.Count != 0)
                    {
                        questBleBuffer.Clear();
                    }
                    return;
                }

                if (questBleBuffer.Count != 0)
                {
                    if ((timeCurrent - timeLastSend) > bleDelayThreshold)
                    {
                        Debug.Log("Data sent " + questBleBuffer.Count + ": " + BitConverter.ToString(questBleBuffer.ToArray()));
                        btSendText += "\nSend: " + (timeCurrent - timeLastSend) + ":  " + BitConverter.ToString(questBleBuffer.ToArray());
                        try
                        {
                            this.btHelper.WriteCharacteristic(this.bluetoothHelperCharacteristic, this.questBleBuffer.ToArray());
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e.ToString());
                        }

                        timeLastSend = timeCurrent;
                        questBleBuffer.Clear();
                    }
                }
            }
        }

        private int lastTick;

        public int? BTSend(byte[] data)
        {

            if(BuildPlatform == TargetPlatform.Android)
            {
                int length;
                if (isQuest)
                {
                    //Add data to buffer
                    questBleBuffer.AddRange(data);
                    //Debug.Log("Add to buffer: " + BitConverter.ToString(data));
                    return null;
                }
                else
                {
                    Debug.Log("Data sent: " + BitConverter.ToString(data));

                    length = data.Length;
                    try
                    {
                        btHelper.WriteCharacteristic(bluetoothHelperCharacteristic, data);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.ToString());
                        return null;
                    }

                    Debug.Log("Data sent: " + BitConverter.ToString(data));
                    btSendText += "\nSend: " + (Environment.TickCount - lastTick) + ":  " + BitConverter.ToString(data);
                    lastTick = Environment.TickCount;
                    return length;
                }
            }
            else //Window build
            {
                try
                {
                    int length = data.Length;
                    ble.WritePackageAsync(deviceId, serviceUuid, characteristicUuids[0], data);

                    Debug.Log("发送的数据：" + BitConverter.ToString(data));
                    return length;
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                    return null;
                }
            }



        }

        private bool airPresSourceCtrlStarted = false;
        public void AirPressureSourceControl()
        {
            if (bleConnected)
            {
                if (airPresSourceCtrlStarted == false)
                {
                    try
                    {
                        Encode.Instance.add_u8(0x01);               // Pressure source control start
                        Encode.Instance.add_u8(sourcePres);         // set pressure source to 50kPa
                        byte[] buf = Encode.Instance.add_fun(0x02); // FI_STABLE_PRESSURE_CTRL
                        Encode.Instance.clear_list();
                        BTSend(buf);
                        airPresSourceCtrlStarted = true;

                        //pumpIndicator.SetActive(true);
                        if (onPumpAction != null)
                        {
                            onPumpAction(whichHand, true);
                        }
                        
                    }
                    catch (Exception e) { Debug.Log(e); }

                }
                else
                {
                    try
                    {
                        Encode.Instance.add_u8(0x00);               // Pressure source control stop
                        Encode.Instance.add_u8(0);                  // set pressure source to 0
                        byte[] buf = Encode.Instance.add_fun(0x02); // FI_STABLE_PRESSURE_CTRL
                        Encode.Instance.clear_list();
                        BTSend(buf);
                        airPresSourceCtrlStarted = false;

                        //pumpIndicator.SetActive(false);
                        if (onPumpAction != null)
                        {
                            onPumpAction(whichHand, false);
                        }
                        
                    }
                    catch (Exception e) { Debug.Log(e); }

                }

            }

        }

        public int[] GetAirPressure()
        {
            return haptics.pressureData;
        }

        public int[] GetFingerPosition()
        {
            return haptics.fingerPositionData;
        }

        public float GetBatteryLevel()
        {
            return haptics.batteryLevel;
        }

        public void SetBatteryLevelUpdateInterval(float seconds)
        {
            updateBatLevelInterval = seconds;
        }

        void OnDestroy()
        {
            try
            {
                if (BuildPlatform == TargetPlatform.Android)
                {
                    AndroidQuit();
                }
                else if (BuildPlatform == TargetPlatform.Window)
                {
                    WindowQuit();
                }

            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
            
        }
        void OnApplicationQuit()
        {
            try
            {
                if (BuildPlatform == TargetPlatform.Android)
                {
                    AndroidQuit();
                }
                else if (BuildPlatform == TargetPlatform.Window)
                {
                    WindowQuit();
                }

            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
        private void WindowQuit()
        {
            ble.Close();
            bleConnected = false;
        }
        private void AndroidQuit()
        {
            btHelper.OnScanEnded -= OnScanEnded;
            btHelper.OnConnected -= OnConnected;
            btHelper.OnConnectionFailed -= OnConnectionFailed;
            btHelper.OnCharacteristicChanged -= OnCharacteristicChanged;
            btHelper.OnCharacteristicNotFound -= OnCharacteristicNotFound;
            btHelper.OnServiceNotFound -= OnServiceNotFound;
            btHelper.Disconnect();
            if (onBluetoothDisconnected != null)
            {
                onBluetoothDisconnected(whichHand);
            }
            bleConnected = false;
            isSubscribed = false;
            Debug.Log("destroy");
        }
    }
}

