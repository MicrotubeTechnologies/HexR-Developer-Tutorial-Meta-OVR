using System;
using UnityEngine;

namespace HexR
{
    // Drives the HexR ghost hand from Meta's OpenXR (XRHand_*) skeleton.
    //
    // A separate component rather than another branch inside PhysicsHandTracking, because the
    // two skeletons don't line up bone-for-bone and pretending they do is what broke the legacy
    // path: the OVR rig gives index/middle/ring no metacarpal, so those fingers hang straight
    // off the wrist, while the OpenXR rig inserts one. Copying localRotation between them
    // compares rotations measured against *different parents* and silently discards the
    // metacarpal -- which is why thumb and pinky (both rigs give those a metacarpal) looked
    // better than the other three fingers.
    //
    // So this drives WORLD rotations instead. A world rotation means the same thing in both
    // hierarchies no matter how the parents are arranged, which makes the whole
    // metacarpal-mismatch question go away rather than needing to be corrected for.
    //
    // Each joint carries a constant correction captured from the two rigs' bind poses. That is
    // valid here because the rigs were measured to be the same hand: after a single best-fit
    // rotation (180 degrees on the left, 90 on the right) every finger joint's residual is
    // 0.0 degrees and the thumb's is 2.1, with identical bone lengths. They differ only in bone
    // axis convention -- exactly what a constant per-joint correction absorbs.
    [DisallowMultipleComponent]
    public class PhysicsHandTrackingOpenXR : MonoBehaviour
    {
        public enum HandType
        {
            Left,
            Right
        }

        public HandType handType;

        [Tooltip("The OpenXR hand root or wrist to follow -- e.g. OpenXRLeftHand, or XRHand_Wrist itself. Assign the exact object: the scene holds several with the same name (raw vs synthetic hand), so name-based lookup picks one at random.")]
        public Transform handRoot;

        [Tooltip("Root of the HexR ghost hand this drives, e.g. b_l_wrist under Left Hand Physics.")]
        public Transform HexrRoot;

        [Tooltip("Drive this object's Rigidbody toward the tracked wrist. Turn off to leave the physics hand where it is and only animate the fingers.")]
        public bool driveRigidbody = true;

        // Captured from the bind poses, in the same order as JointPairs. Serialized so a build
        // uses corrections baked in the Editor, where both rigs are guaranteed to be sitting at
        // their bind pose -- capturing at runtime would silently bake in whatever pose the hand
        // happened to be in when the scene loaded.
        [SerializeField, HideInInspector] private Quaternion[] jointCorrections;
        [SerializeField, HideInInspector] private Quaternion wristCorrection = Quaternion.identity;
        [SerializeField, HideInInspector] private bool bindCaptured;

        // ghost joint, OpenXR joint, then the child of each -- the child is what gives the bone
        // its DIRECTION, which is what the correction has to align (see CaptureBindCorrections).
        // Anatomically matched: the OVR/ghost rig's three-bone fingers map onto the OpenXR
        // proximal/intermediate/distal, skipping the metacarpal it has no bone for -- that
        // metacarpal's rotation still reaches the ghost, because world rotations already
        // include it.
        //
        // A leading "@" on a ghost child means the rig's lowercase marker naming
        // ("l_index_finger_tip_marker") rather than its joint naming ("L_Index_2"). An empty
        // child means the joint is a leaf: it inherits the alignment of the joint above it.
        private static readonly string[,] JointPairs =
        {
            { "_Thumb_0",   "ThumbMetacarpal",    "_Thumb_00",                  "ThumbProximal"      },
            { "_Thumb_00",  "ThumbProximal",      "_Thumb_1",                   "ThumbDistal"        },
            { "_Thumb_1",   "ThumbDistal",        "_Thumb_2",                   "ThumbTip"           },
            { "_Thumb_2",   "ThumbTip",           "",                           ""                   },
            { "_Index_1",   "IndexProximal",      "_Index_2",                   "IndexIntermediate"  },
            { "_Index_2",   "IndexIntermediate",  "_Index_3",                   "IndexDistal"        },
            { "_Index_3",   "IndexDistal",        "@_index_finger_tip_marker",  "IndexTip"           },
            { "_Middle_1",  "MiddleProximal",     "_Middle_2",                  "MiddleIntermediate" },
            { "_Middle_2",  "MiddleIntermediate", "_Middle_3",                  "MiddleDistal"       },
            { "_Middle_3",  "MiddleDistal",       "@_middle_finger_tip_marker", "MiddleTip"          },
            { "_Ring_1",    "RingProximal",       "_Ring_2",                    "RingIntermediate"   },
            { "_Ring_2",    "RingIntermediate",   "_Ring_3",                    "RingDistal"         },
            { "_Ring_3",    "RingDistal",         "@_ring_finger_tip_marker",   "RingTip"            },
            { "_Pinky_0",   "LittleMetacarpal",   "_Pinky_00",                  "LittleProximal"     },
            { "_Pinky_00",  "LittleProximal",     "_Pinky_000",                 "LittleIntermediate" },
            { "_Pinky_000", "LittleIntermediate", "_Pinky_1",                   "LittleDistal"       },
            { "_Pinky_1",   "LittleDistal",       "@_pinky_finger_tip_marker",  "LittleTip"          },
        };

        private const string XRPrefix = "XRHand_";

        private Transform[] ghostJoints;
        private Transform[] trackedJoints;
        private Transform[] ghostChildren;
        private Transform[] trackedChildren;
        private Transform trackedWrist;
        private Rigidbody rb;
        private bool mapped;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool warnedUnmapped;

        // Validates rather than just reporting the flag: this component rides on a
        // DontDestroyOnLoad object while the rig it reads lives in the scene, so `mapped` can
        // still be true while every transform behind it has already been destroyed by a scene
        // load. Callers deciding whether a rebind is needed have to see through that.
        public bool IsMapped { get { return mapped && handRoot != null && trackedWrist != null; } }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (ResolveRig())
            {
                TryMap();
            }
        }

        // Works out which skeleton this rig is actually on, and hands back to the legacy
        // component if it turns out to be the old one. Without this, a project still on the OVR
        // skeleton would end up with no hand driver at all: this component would find no
        // XRHand_* joints and the legacy one is switched off wherever this is installed.
        //
        // Returns true if the OpenXR skeleton is present and this component should run.
        public bool ResolveRig()
        {
            PhysicsHandTracking legacy = GetComponent<PhysicsHandTracking>();

            // handRoot may still be pointing at the legacy bone rig (OculusHand_L/R) from before
            // the SDK switch. The OpenXR root is its sibling under the same HandVisual, so look
            // there rather than by name across the scene -- there are several same-named
            // candidates and a global lookup picks one at random.
            if (handRoot != null && FindIn(handRoot, XRPrefix + "Wrist") == null)
            {
                Transform openXRRoot = FindOpenXRSibling(handRoot);
                if (openXRRoot != null)
                {
                    Debug.Log("[HexR] " + name + ": handRoot was on the legacy bone rig (" + handRoot.name
                        + "), repointing to " + openXRRoot.name + ".");
                    handRoot = openXRRoot;
                    // Corrections were baked against the old root; they mean nothing now.
                    ClearBindCorrections();
                }
            }

            bool hasOpenXR = handRoot != null && FindIn(handRoot, XRPrefix + "Wrist") != null;

            if (!hasOpenXR)
            {
                if (legacy != null)
                {
                    legacy.enabled = true;
                    Debug.Log("[HexR] " + name + ": no OpenXR (" + XRPrefix + "*) joints found -- this rig is on the "
                        + "legacy OVR skeleton, handing over to PhysicsHandTracking.");
                }
                else
                {
                    Debug.LogWarning("[HexR] " + name + ": no OpenXR joints found and no PhysicsHandTracking to fall "
                        + "back to -- this hand will not be driven.");
                }
                enabled = false;
                return false;
            }

            // Both components would drive the same ghost joints and fight each other.
            if (legacy != null && legacy.enabled)
            {
                legacy.enabled = false;
            }
            return true;
        }

        private Transform FindOpenXRSibling(Transform legacyRoot)
        {
            string wanted = "OpenXR" + (handType == HandType.Left ? "Left" : "Right") + "Hand";

            // Transform.Find sees inactive children, which matters: rigs authored before the SDK
            // switch keep the OpenXR root inactive until HandVisual.Awake enables it.
            if (legacyRoot.parent != null)
            {
                Transform sibling = legacyRoot.parent.Find(wanted);
                if (sibling != null)
                {
                    return sibling;
                }
            }
            return legacyRoot.Find(wanted);
        }

        // Cheap pre-check for callers that retry across frames (HexRManager's post-scene-load
        // rebind). Without it, handing Rebind() a rig whose OpenXR joints haven't been built yet
        // would run ResolveRig's hand-over-to-legacy path -- and log its message -- once per
        // attempt. Checks the sibling too, since a rig authored before the SDK switch hands out
        // the legacy root and keeps the OpenXR one next to it.
        public bool CanBindTo(Transform candidateRoot)
        {
            if (candidateRoot == null)
            {
                return false;
            }
            if (FindIn(candidateRoot, XRPrefix + "Wrist") != null)
            {
                return true;
            }

            Transform sibling = FindOpenXRSibling(candidateRoot);
            return sibling != null && FindIn(sibling, XRPrefix + "Wrist") != null;
        }

        // Re-points this component at a hand rig in a newly loaded scene, keeping the baked bind
        // corrections. They describe how the ghost rig relates to the tracked rig at ITS BIND
        // POSE, and the incoming scene's rig is another instance of the same prefab, so they
        // carry over unchanged. They're saved and restored around ResolveRig() on purpose: that
        // clears them whenever it re-points a legacy root, which would force a re-capture
        // against whatever pose the live hand happens to be in at that instant -- exactly the
        // silent mangling CaptureBindCorrections warns about.
        public bool Rebind(Transform newHandRoot)
        {
            if (newHandRoot == null)
            {
                return false;
            }

            Quaternion[] savedJointCorrections = jointCorrections;
            Quaternion savedWristCorrection = wristCorrection;
            bool savedBindCaptured = bindCaptured;

            handRoot = newHandRoot;
            mapped = false;
            warnedUnmapped = false;
            enabled = true;

            if (!ResolveRig())
            {
                return false;
            }

            if (savedBindCaptured && !bindCaptured)
            {
                jointCorrections = savedJointCorrections;
                wristCorrection = savedWristCorrection;
                bindCaptured = true;
            }

            return TryMap();
        }

        public bool TryMap()
        {
            mapped = false;

            if (handRoot == null || HexrRoot == null)
            {
                WarnOnce("[HexR] " + name + ": handRoot or HexrRoot is not assigned.");
                return false;
            }

            trackedWrist = FindIn(handRoot, XRPrefix + "Wrist");
            if (trackedWrist == null)
            {
                WarnOnce("[HexR] " + name + ": no " + XRPrefix + "Wrist under " + handRoot.name
                    + " -- is handRoot pointing at the OpenXR hand rather than the legacy OculusHand_L/R?");
                return false;
            }

            int count = JointPairs.GetLength(0);
            ghostJoints = new Transform[count];
            trackedJoints = new Transform[count];
            ghostChildren = new Transform[count];
            trackedChildren = new Transform[count];

            for (int i = 0; i < count; i++)
            {
                ghostJoints[i] = FindIn(HexrRoot, GhostName(JointPairs[i, 0]));
                trackedJoints[i] = FindIn(handRoot, XRPrefix + JointPairs[i, 1]);

                if (ghostJoints[i] == null || trackedJoints[i] == null)
                {
                    WarnOnce("[HexR] " + name + ": couldn't map "
                        + GhostName(JointPairs[i, 0]) + " <- " + XRPrefix + JointPairs[i, 1]
                        + " (ghost " + (ghostJoints[i] != null) + ", tracked " + (trackedJoints[i] != null) + ").");
                    return false;
                }

                // Leaf joints legitimately have no child; they inherit their parent's alignment.
                ghostChildren[i] = string.IsNullOrEmpty(JointPairs[i, 2]) ? null : FindIn(HexrRoot, GhostName(JointPairs[i, 2]));
                trackedChildren[i] = string.IsNullOrEmpty(JointPairs[i, 3]) ? null : FindIn(handRoot, XRPrefix + JointPairs[i, 3]);
            }

            if (!bindCaptured)
            {
                CaptureBindCorrections();
            }

            mapped = true;
            warnedUnmapped = false;
            return true;
        }

        // TryMap() runs from Update() every frame while unmapped, so an unrecoverable rig (a
        // scene with no hand tracking, say) would otherwise write the same warning to the log
        // 72 times a second. Reset once mapping succeeds, so a genuinely new failure is still
        // reported.
        private void WarnOnce(string message)
        {
            if (warnedUnmapped)
            {
                return;
            }
            warnedUnmapped = true;
            Debug.LogWarning(message);
        }

        // Records, per joint, the fixed rotation the driving step applies. Run it with both rigs
        // at their bind pose -- in the Editor, or in a build before the hands are being tracked.
        //
        // The correction has to align the two rigs' BONE DIRECTIONS, not their joint
        // orientations. A finger's visible bend comes from `joint.rotation * boneOffset`, and
        // the two rigs' bone offsets point different ways (their bind poses differ by a rigid
        // 180 degrees on the left, 90 on the right). Matching orientations alone therefore
        // reproduces curl but loses spread and lets twist leak into bend -- measured at 53 and
        // 58 degrees of error respectively, against 1.5 for curl.
        //
        // So each correction carries an extra `align`: the world rotation at bind that carries
        // the ghost's bone onto the tracked bone. Because it is applied on the far side of the
        // tracked rotation, it holds under motion too -- both bones then rotate together.
        public bool CaptureBindCorrections()
        {
            if (trackedJoints == null || ghostJoints == null || trackedWrist == null)
            {
                return false;
            }

            jointCorrections = new Quaternion[trackedJoints.Length];
            Quaternion inherited = Quaternion.identity;

            for (int i = 0; i < trackedJoints.Length; i++)
            {
                Quaternion align = inherited;

                if (ghostChildren[i] != null && trackedChildren[i] != null)
                {
                    Vector3 ghostBone = ghostChildren[i].position - ghostJoints[i].position;
                    Vector3 trackedBone = trackedChildren[i].position - trackedJoints[i].position;
                    if (ghostBone.sqrMagnitude > 1e-10f && trackedBone.sqrMagnitude > 1e-10f)
                    {
                        align = Quaternion.FromToRotation(ghostBone.normalized, trackedBone.normalized);
                    }
                }

                inherited = align;
                jointCorrections[i] = Quaternion.Inverse(trackedJoints[i].rotation) * align * ghostJoints[i].rotation;
            }

            // The wrist gets a full frame rather than a single bone direction, because the palm's
            // roll matters and aligning one axis would leave it free.
            Quaternion ghostFrame, trackedFrame;
            if (TryBuildWristFrame(HexrRoot, ghostJoints[7], ghostJoints[4], ghostJoints[14], out ghostFrame)
                && TryBuildWristFrame(trackedWrist, trackedJoints[7], trackedJoints[4], trackedJoints[14], out trackedFrame))
            {
                Quaternion alignWrist = trackedFrame * Quaternion.Inverse(ghostFrame);
                wristCorrection = Quaternion.Inverse(trackedWrist.rotation) * alignWrist * HexrRoot.rotation;
            }
            else
            {
                wristCorrection = Quaternion.Inverse(trackedWrist.rotation) * HexrRoot.rotation;
            }

            bindCaptured = true;
            return true;
        }

        // Wrist-to-middle-base for forward, index-base-to-little-base for sideways. Built the
        // same way on both rigs, so the cross-product handedness cancels between them.
        private static bool TryBuildWristFrame(Transform wrist, Transform middle, Transform index, Transform little, out Quaternion frame)
        {
            frame = Quaternion.identity;

            Vector3 forward = middle.position - wrist.position;
            Vector3 side = index.position - little.position;
            if (forward.sqrMagnitude < 1e-10f || side.sqrMagnitude < 1e-10f)
            {
                return false;
            }

            Vector3 up = Vector3.Cross(forward.normalized, side.normalized);
            if (up.sqrMagnitude < 1e-8f)
            {
                return false;
            }

            frame = Quaternion.LookRotation(forward.normalized, up.normalized);
            return true;
        }

        private string GhostName(string suffix)
        {
            if (suffix.StartsWith("@"))
            {
                // Marker naming is lowercase-prefixed: "l_index_finger_tip_marker".
                return (handType == HandType.Left ? "l" : "r") + suffix.Substring(1);
            }
            return (handType == HandType.Left ? "L" : "R") + suffix;
        }

        public void ClearBindCorrections()
        {
            bindCaptured = false;
            jointCorrections = null;
            wristCorrection = Quaternion.identity;
        }

        private void Update()
        {
            // The tracked rig lives in the scene while this component rides on a
            // DontDestroyOnLoad object, so a scene load destroys every joint out from under a
            // still-true `mapped`. Unity's destroyed references compare equal to null, which is
            // what catches it here -- without this the drive loop below throws
            // MissingReferenceException every frame instead of re-mapping.
            if (mapped && (handRoot == null || trackedWrist == null))
            {
                mapped = false;
            }

            if (!mapped)
            {
                // With no ghost rig assigned there is nothing to mirror onto and TryMap can
                // never succeed -- but it would still run a recursive joint search over the
                // whole hand hierarchy every frame, on both hands, forever. That is real time
                // on a Quest. Stand down instead; a rebind re-enables this component if a ghost
                // rig ever comes back.
                if (HexrRoot == null)
                {
                    WarnOnce("[HexR] " + name + ": no HexrRoot (ghost hand) assigned, so there is nothing to "
                        + "mirror the tracked hand onto -- disabling this component. Haptics on the tracked "
                        + "hand are unaffected.");
                    enabled = false;
                    return;
                }

                // Cheap to retry: the hand rig can be rebuilt underneath us on a skeleton change.
                TryMap();
                return;
            }

            if (jointCorrections == null || jointCorrections.Length != ghostJoints.Length)
            {
                return;
            }

            for (int i = 0; i < ghostJoints.Length; i++)
            {
                // World rotation, so the extra OpenXR metacarpal between wrist and proximal is
                // already accounted for and no parent-frame conversion is needed.
                ghostJoints[i].rotation = trackedJoints[i].rotation * jointCorrections[i];
            }

            targetPosition = trackedWrist.position;
            targetRotation = trackedWrist.rotation * wristCorrection;
        }

        // Same velocity-driven follow the legacy component used, so the physics hand keeps
        // behaving like a physics hand rather than teleporting through colliders.
        private void FixedUpdate()
        {
            if (!mapped || !driveRigidbody || rb == null)
            {
                return;
            }

            try
            {
                HexRCompat.SetLinearVelocity(rb, (targetPosition - transform.position) / Time.fixedDeltaTime);

                Quaternion delta = targetRotation * Quaternion.Inverse(rb.rotation);
                float angle;
                Vector3 axis;
                delta.ToAngleAxis(out angle, out axis);
                if (angle > 180f)
                {
                    angle -= 360f;
                }

                Vector3 angularVelocity = angle * axis * Mathf.Deg2Rad / Time.fixedDeltaTime;
                if (float.IsNaN(angularVelocity.x) || float.IsNaN(angularVelocity.y) || float.IsNaN(angularVelocity.z)
                    || float.IsInfinity(angularVelocity.x) || float.IsInfinity(angularVelocity.y) || float.IsInfinity(angularVelocity.z))
                {
                    return;
                }

                rb.angularVelocity = angularVelocity;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[HexR] " + name + ": " + e.Message);
            }
        }

        private static Transform FindIn(Transform root, string jointName)
        {
            if (root == null)
            {
                return null;
            }
            if (root.name == jointName)
            {
                return root;
            }
            return FindRecursive(root, jointName);
        }

        private static Transform FindRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
                Transform result = FindRecursive(child, childName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
