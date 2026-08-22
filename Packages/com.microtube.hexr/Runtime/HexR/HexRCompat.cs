using UnityEngine;
using Object = UnityEngine.Object;

namespace HexR
{
    /// <summary>
    /// Shims for engine APIs Unity has renamed or deprecated between 2022.3 and Unity 6.x.
    /// Everything HexR calls that differs across that range goes through here, so the version
    /// guards live in one file instead of being sprinkled through the runtime.
    /// </summary>
    /// <remarks>
    /// Why this exists: Unity's pattern is to mark an API <c>[Obsolete]</c> as a warning for a
    /// few releases and then flip it to <c>[Obsolete(..., error: true)]</c>, at which point
    /// packages still calling it stop compiling outright. That is what happened to
    /// <c>Object.GetInstanceID()</c> in 6000.3 (CS0619), and it is what will eventually happen
    /// to the APIs shimmed below. Routing every call through one file means the next flip is a
    /// one-line change here rather than a hunt across the package.
    ///
    /// Verified against the actual reference assemblies for 2022.3, 2023.2 and 6000.5:
    ///
    ///   API                                                  2022.3  2023.2  6000.5
    ///   FindAnyObjectByType&lt;T&gt;(FindObjectsInactive)          ok      ok      ok
    ///   FindObjectsByType&lt;T&gt;(inactive, FindObjectsSortMode)  ok      ok      CS0618
    ///   FindObjectsByType&lt;T&gt;(inactive)                       absent  absent  ok
    ///   FindObjectOfType&lt;T&gt;(bool)                            ok      CS0618  CS0618
    ///   FindObjectsOfType&lt;T&gt;(bool)                           ok      CS0618  CS0618
    ///   Rigidbody.velocity                                   ok      ok      CS0618
    ///   Rigidbody.linearVelocity                             absent  absent  ok
    /// </remarks>
    public static class HexRCompat
    {
        /// <summary>
        /// Replacement for <c>FindObjectOfType&lt;T&gt;()</c>. Returns an arbitrary matching
        /// object, not a defined "first" one -- fine for the singleton-style lookups HexR uses
        /// it for, but do not rely on which instance comes back when a scene holds several.
        /// </summary>
        public static T FindAny<T>(bool includeInactive = false) where T : Object
        {
            // Present and non-obsolete on every version in the supported range, so no guard.
            return Object.FindAnyObjectByType<T>(Inactive(includeInactive));
        }

        /// <summary>
        /// Replacement for <c>FindObjectsOfType&lt;T&gt;(bool)</c>.
        /// </summary>
        /// <remarks>
        /// Ordering is NOT stable across the supported range. Up to 6000.4 this sorts by
        /// InstanceID, matching what the old <c>FindObjectsOfType</c> guaranteed. From 6000.5
        /// Unity deprecated <see cref="FindObjectsSortMode"/> outright (InstanceID is being
        /// replaced by EntityId) and the remaining overload gives no ordering guarantee at all.
        /// Callers that pick a result by name will therefore choose arbitrarily among
        /// duplicates on 6000.5+, where older editors picked deterministically.
        ///
        /// The 6000_5 guard is set to the version this was actually verified against. The
        /// no-sort-mode overload does not exist in 6000.0 or 6000.3; if you confirm which
        /// release introduced it, this can be lowered.
        /// </remarks>
        public static T[] FindAll<T>(bool includeInactive = false) where T : Object
        {
#if UNITY_6000_5_OR_NEWER
            return Object.FindObjectsByType<T>(Inactive(includeInactive));
#else
            return Object.FindObjectsByType<T>(Inactive(includeInactive), FindObjectsSortMode.InstanceID);
#endif
        }

        /// <summary>Reads <c>Rigidbody.linearVelocity</c>, or <c>velocity</c> before Unity 6.</summary>
        public static Vector3 GetLinearVelocity(Rigidbody body)
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }

        /// <summary>Writes <c>Rigidbody.linearVelocity</c>, or <c>velocity</c> before Unity 6.</summary>
        public static void SetLinearVelocity(Rigidbody body, Vector3 value)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = value;
#else
            body.velocity = value;
#endif
        }

        private static FindObjectsInactive Inactive(bool includeInactive)
        {
            return includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
        }
    }
}
