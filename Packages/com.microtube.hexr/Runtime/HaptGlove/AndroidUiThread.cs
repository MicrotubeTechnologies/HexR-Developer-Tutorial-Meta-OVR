using System;
using UnityEngine;

namespace HaptGlove
{
    /// <summary>
    /// Runs an action on the Android UI thread, which is where the bundled Bluetooth plugin
    /// has to be called from.
    /// </summary>
    /// <remarks>
    /// com.tony.bluetoothunityapi.BLEBluetoothHelper builds its scan-timeout handler with the
    /// no-argument <c>new Handler()</c>, in both <c>scanNearbyDevices()</c> and
    /// <c>connect()</c>. That constructor binds to the CALLING thread's Looper, and Unity's
    /// script thread on Android has none, so the call dies with:
    ///
    ///   java.lang.RuntimeException: Can't create handler inside thread ...
    ///   that has not called Looper.prepare()
    ///
    /// The base class BluetoothHelper gets this right -- it uses
    /// <c>new Handler(context.getMainLooper())</c> -- so this is an oversight in the BLE
    /// subclass, not something the plugin expects callers to arrange. The jar is third-party
    /// and shipped prebuilt, so the fix belongs on our side: call it from the Android UI
    /// thread, which does have a Looper, and the plugin's own postDelayed then lands where it
    /// meant to.
    ///
    /// Keep the wrapped region as small as possible: the single plugin call, nothing else.
    /// Anything touching the Unity API -- StartCoroutine, Time, PlayerPrefs, UI -- has to
    /// stay on Unity's thread, and most of it throws when it doesn't. Wrapping all of
    /// HaptGloveHandler.BTConnection here, as this was first used, meant the watchdog's
    /// StartCoroutine threw on the UI thread before the scan was ever started. The only
    /// callers now are HaptGloveHandler.PluginConnect and PluginScan, which wrap exactly
    /// <c>connect()</c> and <c>scanNearbyDevices()</c> and post their outcome back to
    /// Unity's thread.
    ///
    /// The post is asynchronous: Run returns before the action has executed. Nothing here
    /// waits on the UI thread, deliberately -- during a pause the UI thread blocks waiting
    /// for Unity's thread, so a synchronous hop from Unity's thread could deadlock.
    /// </remarks>
    public static class AndroidUiThread
    {
        public static void Run(Action action)
        {
            if (action == null)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity != null)
                    {
                        // The Runnable closes over the delegate, not over `activity`, so
                        // disposing the wrappers on the way out of the using blocks is safe.
                        activity.Call("runOnUiThread", new AndroidJavaRunnable(() => action()));
                        return;
                    }
                }

                Debug.LogWarning("[HaptGlove] No current Android activity, so this call runs on Unity's thread instead. "
                    + "If it reaches the Bluetooth plugin, expect \"Can't create handler inside thread\".");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[HaptGlove] Could not reach the Android UI thread (" + e.Message + ") -- running on "
                    + "Unity's thread instead.");
            }
#endif
            action();
        }
    }
}
