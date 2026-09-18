using System;
using UnityEngine;

namespace HexR
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
    /// Keep the wrapped region as small as possible. Anything touching the Unity API has to
    /// stay on Unity's thread, so wrap the plugin call only, never the surrounding UI updates.
    ///
    /// One caveat: HaptGloveHandler.AndroidConnection invokes a UnityAction&lt;HandType&gt; on
    /// its already-connected branch (the disconnect path), so that invoke runs on the UI
    /// thread too -- subscribers to the disconnect event must not touch the Unity API
    /// directly. Everything else that method does is Debug.Log, which is safe off the main
    /// thread.
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

                Debug.LogWarning("[HexR] No current Android activity, so this call runs on Unity's thread instead. "
                    + "If it reaches the Bluetooth plugin, expect \"Can't create handler inside thread\".");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[HexR] Could not reach the Android UI thread (" + e.Message + ") -- running on "
                    + "Unity's thread instead.");
            }
#endif
            action();
        }
    }
}
