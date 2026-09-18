using System;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace HexR
{
    public class PhysicsHandTracking : MonoBehaviour
    {
#region General Field

        private HexRManager GloveManager;

        public enum HandType
        {
            Left,
            Right
        };

        public HandType handType;
        private string hand;
        private string hand_Short, Hexr_hand_Short;
        private Transform[] targetJoints = new Transform[26];
        [Tooltip("Vr hand root")]
        public Transform handRoot;
        [Tooltip("HexR hand root ")]
        public Transform HexrRoot;
        private Transform[] followingJoints = new Transform[26];
        private Vector3 targePosition = new Vector3();
        private Quaternion targeRotation = new Quaternion();
        private Rigidbody rb;
        private string handRootName;
        // Set by MetaOVRStart when the hand is on Meta's OpenXR (XRHand_*) skeleton rather
        // than the legacy OVR (b_l_/b_r_) one -- see TryMapOpenXRTargetJoints/CaptureRestPose.
        private bool usingOpenXRTargets;
        private Quaternion[] targetRestLocalRot = new Quaternion[26];
        private Quaternion[] followRestLocalRot = new Quaternion[26];
        //public bool leftHand;

        // Exposed because there is no other way to correct hand orientation: MetaOVRFixedUpdate
        // drives the rigidbody toward targeRotation every physics step and MetaOVRUpdate
        // overwrites every finger's localRotation every frame, so rotating the GameObject in the
        // Inspector is stomped immediately -- these offsets are the only rotation the update
        // loop leaves under your control. Applied after the tracked rotation, i.e. about the
        // hand's own axes, and read fresh each frame so they can be dialled in during Play mode.
        //
        // Expect to need these on Meta's OpenXR skeleton: its wrist/joint axis convention is not
        // the one the legacy OVR bones used, and the correction is mirrored between hands (a
        // +90 on one is typically -90 on the other), so each hand gets its own values.
        [Tooltip("Euler offset applied to the whole hand's tracked rotation. Use this when the hand is rotated as a unit (e.g. 90 degrees off).")]
        public Vector3 rotOffsetPalm = new Vector3(0, 0, 0);

        [Tooltip("Euler offset applied to every finger joint on top of the tracked rotation. Use this when the palm sits right but the fingers bend around the wrong axis.")]
        public Vector3 rotOffsetFinger = new Vector3(0, 0, 0);
#endregion

#region OpenXRField

#endregion

#region Meta OVR Field

#endregion

#region MRTK Field


#endregion


        void Start()
        {
            GloveManager = gameObject.GetComponentInParent<HexRManager>();
            handRootName = handRoot.name;
            if (GloveManager.XRFramework == HexRManager.Options.OpenXR)
            {
                OpenXRStart();
            }
            else if(GloveManager.XRFramework == HexRManager.Options.MetaOVR)
            {
                MetaOVRStart();
            }
/*            else if (GloveManager.XRFramework == HexRManager.Options.MRTK)
            {

            }*/

        }


        void FixedUpdate()
        {
            if (GloveManager.XRFramework == HexRManager.Options.OpenXR && handRoot != null)
            {
                OpenXRFixedUpdate();
            }
            else if (GloveManager.XRFramework == HexRManager.Options.MetaOVR && handRoot != null)
            {
                MetaOVRFixedUpdate();
            }

            if (handRoot == null)
            {

                handRoot = GameObject.Find(handRootName).transform;
                if (GloveManager.XRFramework == HexRManager.Options.OpenXR)
                {
                    OpenXRStart();
                }
                else if (GloveManager.XRFramework == HexRManager.Options.MetaOVR)
                {
                    MetaOVRStart();
                }

            }
        }


        void Update()
        {
            if (handRoot == null)
            {

                handRoot = GameObject.Find(handRootName).transform;
                if (GloveManager.XRFramework == HexRManager.Options.OpenXR)
                {
                    OpenXRStart();
                }
                else if (GloveManager.XRFramework == HexRManager.Options.MetaOVR)
                {
                    MetaOVRStart();
                }
            }
            else
            {
                if (GloveManager.XRFramework == HexRManager.Options.OpenXR)
                {
                    OpenXRUpdate();
                }
                else if (GloveManager.XRFramework == HexRManager.Options.MetaOVR)
                {
                    MetaOVRUpdate();
                }
                /*            else if (GloveManager.XRFramework == HexRManager.Options.MRTK)
            {

            }*/
            }

        }

#region MetaOVR
        private void MetaOVRStart()
        {
            // Ghost-rig mirroring is opt-in now -- HexrRoot only gets assigned if a ghost
            // rig actually exists in this prefab. handRoot itself stays required regardless
            // (ResolveRawFingerJoint/ResolveRawPalmJoint depend on it), this just skips the
            // part of Start() that maps onto a ghost rig that may no longer be there.
            if (HexrRoot == null) return;

            GameObject ParenHand = handRoot.gameObject;
            GameObject HexrHand = HexrRoot.gameObject;
            if (handType == HandType.Left)
            {
                hand = "Left";
                hand_Short = "b_l";
                Hexr_hand_Short = "L";
            }
            else
            {
                hand = "Right";
                hand_Short = "b_r";
                Hexr_hand_Short = "R";
            }


            //handRoot = GameObject.Find(hand + " Hand Tracking").GetComponent<Transform>();
            if (handRoot == null)
            {
                Debug.LogError("Hand root in " + gameObject.name + "is not assigned.");
                return;
            }
            rb = GetComponent<Rigidbody>();

            // Link Hexr hand position and rotation to Meta hand position and rotation
            #region Meta Hands Mapping
            usingOpenXRTargets = TryMapOpenXRTargetJoints(ParenHand);
            if (!usingOpenXRTargets)
            {
            targetJoints[0] = FindChildRecursive(ParenHand, hand_Short + "_thumb0").transform;
            targetJoints[1] = targetJoints[0].GetChild(0);
            targetJoints[2] = targetJoints[1].GetChild(0);
            targetJoints[3] = targetJoints[2].GetChild(0);

            targetJoints[4] = FindChildRecursive(ParenHand, hand_Short + "_index1").transform;
            targetJoints[5] = targetJoints[4].GetChild(0);
            targetJoints[6] = targetJoints[5].GetChild(0);
            targetJoints[7] = targetJoints[6].GetChild(2);

            targetJoints[8] = FindChildRecursive(ParenHand, hand_Short + "_middle1").transform;
            targetJoints[9] = targetJoints[8].GetChild(0);
            targetJoints[10] = targetJoints[9].GetChild(0);
            targetJoints[11] = targetJoints[10].GetChild(2);

            targetJoints[12] = FindChildRecursive(ParenHand, hand_Short + "_ring1").transform;
            targetJoints[13] = targetJoints[12].GetChild(0);
            targetJoints[14] = targetJoints[13].GetChild(0);
            targetJoints[15] = targetJoints[14].GetChild(2);

            targetJoints[16] = FindChildRecursive(ParenHand, hand_Short + "_pinky0").transform;
            targetJoints[17] = targetJoints[16].GetChild(0);
            targetJoints[18] = targetJoints[17].GetChild(0);
            targetJoints[19] = targetJoints[18].GetChild(0);
            targetJoints[20] = targetJoints[19].GetChild(0);

            if (hand_Short == "b_l")
            {
                targetJoints[21] = FindChildRecursive(ParenHand, "l_palm_center_marker").transform;
            }
            else
            {
                targetJoints[21] = FindChildRecursive(ParenHand, "r_palm_center_marker").transform;
            }
            targetJoints[22] = FindChildRecursive(ParenHand, hand_Short + "_wrist").transform;
            }
#endregion

#region HexR Hands Mapping
            followingJoints[0] = FindChildRecursive(HexrHand, Hexr_hand_Short + "_Thumb_0").transform;
            followingJoints[1] = followingJoints[0].GetChild(0);
            followingJoints[2] = followingJoints[1].GetChild(0);
            followingJoints[3] = followingJoints[2].GetChild(0);

            followingJoints[4] = FindChildRecursive(HexrHand, Hexr_hand_Short + "_Index_1").transform;
            followingJoints[5] = followingJoints[4].GetChild(0);
            followingJoints[6] = followingJoints[5].GetChild(0);
            followingJoints[7] = followingJoints[6].GetChild(2);

            followingJoints[8] = FindChildRecursive(HexrHand, Hexr_hand_Short + "_Middle_1").transform;
            followingJoints[9] = followingJoints[8].GetChild(0);
            followingJoints[10] = followingJoints[9].GetChild(0);
            followingJoints[11] = followingJoints[10].GetChild(2);

            followingJoints[12] = FindChildRecursive(HexrHand, Hexr_hand_Short + "_Ring_1").transform;
            followingJoints[13] = followingJoints[12].GetChild(0);
            followingJoints[14] = followingJoints[13].GetChild(0);
            followingJoints[15] = followingJoints[14].GetChild(2);

            followingJoints[16] = FindChildRecursive(HexrHand, Hexr_hand_Short + "_Pinky_0").transform;
            followingJoints[17] = followingJoints[16].GetChild(0);
            followingJoints[18] = followingJoints[17].GetChild(0);
            followingJoints[19] = followingJoints[18].GetChild(0);
            followingJoints[20] = followingJoints[19].GetChild(0);

            if (hand_Short == "b_l")
            {
                followingJoints[21] = FindChildRecursive(HexrHand, "l_palm_center_marker").transform;
            }
            else
            {
                followingJoints[21] = FindChildRecursive(HexrHand, "r_palm_center_marker").transform;
            }
            followingJoints[22] = HexrRoot;
#endregion
            if (usingOpenXRTargets)
            {
                CaptureRestPose();
            }
            Debug.Log("MetaOVR Hands are mapped" + (usingOpenXRTargets ? " (OpenXR skeleton)" : " (legacy OVR skeleton)"));
        }

        // com.meta.xr.sdk.interaction v201 compiles with ISDK_OPENXR_HAND unconditionally, so
        // HandVisual.Awake deactivates the legacy OculusHand_L/R bone rig and drives the
        // XRHand_* rig instead -- the b_l_/b_r_ bones are still in the hierarchy but nothing
        // poses them again. Map onto the XR joints when they're present, and only fall back to
        // the b_l_/b_r_ walk for older Meta rigs still on the legacy skeleton.
        //
        // Index mapping matches the legacy layout slot-for-slot so MetaOVRUpdate and the
        // HexR ghost rig (followingJoints) need no reindexing: the XR skeleton has one extra
        // metacarpal per finger, which the OVR skeleton only has for thumb and pinky, so
        // Index/Middle/Ring start at Proximal rather than Metacarpal.
        private bool TryMapOpenXRTargetJoints(GameObject parentHand)
        {
            GameObject wrist = FindChildRecursive(parentHand, MetaOpenXRPrefix + "Wrist");
            if (wrist == null) return false;

            string[] jointNames =
            {
                "ThumbMetacarpal", "ThumbProximal", "ThumbDistal", "ThumbTip",
                "IndexProximal", "IndexIntermediate", "IndexDistal", "IndexTip",
                "MiddleProximal", "MiddleIntermediate", "MiddleDistal", "MiddleTip",
                "RingProximal", "RingIntermediate", "RingDistal", "RingTip",
                "LittleMetacarpal", "LittleProximal", "LittleIntermediate", "LittleDistal", "LittleTip",
                "Palm", "Wrist"
            };

            for (int i = 0; i < jointNames.Length; i++)
            {
                GameObject joint = FindChildRecursive(parentHand, MetaOpenXRPrefix + jointNames[i]);
                if (joint == null)
                {
                    Debug.LogWarning("[HexR] " + handType + " hand: OpenXR joint " + MetaOpenXRPrefix + jointNames[i]
                        + " not found under " + parentHand.name + " -- falling back to the legacy OVR bone mapping.");
                    return false;
                }
                targetJoints[i] = joint.transform;
            }

            return true;
        }

        // The XR skeleton and the HexR ghost rig do not share a bind pose (their rest joint
        // orientations differ by up to ~130 degrees on the thumb), so MetaOVRUpdate's straight
        // localRotation copy -- correct while the ghost rig was mirroring the identically-posed
        // OVR rig -- would mangle the hand. Record both rest poses here so the update can apply
        // the tracked hand's rotation *relative to its own rest* onto the ghost rig's rest.
        private void CaptureRestPose()
        {
            for (int i = 0; i < targetRestLocalRot.Length; i++)
            {
                if (targetJoints[i] != null) targetRestLocalRot[i] = targetJoints[i].localRotation;
                if (followingJoints[i] != null) followRestLocalRot[i] = followingJoints[i].localRotation;
            }
        }
        private void MetaOVRFixedUpdate()
        {
            if (HexrRoot == null) return;

            try
            {
                // position
                HexRCompat.SetLinearVelocity(rb, (targePosition - transform.position) / Time.fixedDeltaTime);

                // rotation
                Quaternion deltaRotation = targeRotation * Quaternion.Inverse(rb.rotation);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                //if (float.IsNaN(axis.x)| float.IsNaN(axis.y)| float.IsNaN(axis.z)) { return; }
                //if (float.IsInfinity(axis.x) | float.IsInfinity(axis.y) | float.IsInfinity(axis.z)) { return; }
                if (angle > 180f) { angle -= 360f; };
                Vector3 angularVelocity = angle * axis * Mathf.Deg2Rad / Time.fixedDeltaTime;
                if (float.IsNaN(angularVelocity.x) | float.IsNaN(angularVelocity.y) | float.IsNaN(angularVelocity.z)) { return; }
                rb.angularVelocity = angle * axis * Mathf.Deg2Rad / Time.fixedDeltaTime;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                //logText2.text += "\n" + e.ToString();
            }
        }
        private void MetaOVRUpdate()
        {
            if (HexrRoot == null) return;

            try
            {
                targePosition = targetJoints[22].position;
                targeRotation = targetJoints[22].rotation * Quaternion.Euler(rotOffsetPalm);

                for (int i = 0; i < 20; i++)
                {
                    if (usingOpenXRTargets)
                    {
                        // Retarget rather than copy: apply how far the tracked joint has rotated
                        // away from its own rest pose onto the ghost rig's rest pose, since the
                        // two skeletons don't share one (see CaptureRestPose). Bone lengths differ
                        // too, so localPosition stays as authored rather than being copied --
                        // copying it would stretch the ghost mesh.
                        followingJoints[i].localRotation = followRestLocalRot[i]
                            * Quaternion.Inverse(targetRestLocalRot[i])
                            * targetJoints[i].localRotation
                            * Quaternion.Euler(rotOffsetFinger);

                        // The XR joints are the ones HandVisual actively poses, and the XR hand
                        // mesh is skinned to them, so unlike the legacy path they must not be
                        // deactivated here.
                        continue;
                    }

                    followingJoints[i].localPosition = targetJoints[i].localPosition;
                    followingJoints[i].localRotation = targetJoints[i].localRotation * Quaternion.Euler(rotOffsetFinger);
                    targetJoints[i].gameObject.SetActive(false);

                }

            }
            catch (Exception e)
            {
                MetaOVRStart();
                Debug.Log(e.ToString());
                //logText2.text += "\n" + e.ToString();
            }
        }
        public GameObject FindChildRecursive(GameObject parent, string childName)
        {
            foreach (Transform child in parent.transform)
            {
                if (child.gameObject.name == childName)
                {
                    return child.gameObject;
                }
                GameObject result = FindChildRecursive(child.gameObject, childName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

#endregion

#region OpenXR
        private void OpenXRStart()
        {
            // See MetaOVRStart -- ghost-rig mirroring is opt-in, handRoot stays required.
            if (HexrRoot == null) return;

            if (handType == HandType.Left)
            {
                hand = "Left";
                hand_Short = "L";
            }
            else
            {
                hand = "Right";
                hand_Short = "R";
            }

            //handRoot = GameObject.Find(hand + " Hand Tracking").GetComponent<Transform>();
            if (handRoot == null)
            {
                Debug.LogError("Hand root in " + gameObject.name + "is not assigned.");
                return;
            }
            rb = GetComponent<Rigidbody>();

            targetJoints[0] = handRoot.Find(hand_Short + "_ThumbMetacarpal");
            targetJoints[1] = targetJoints[0].GetChild(0);
            targetJoints[2] = targetJoints[1].GetChild(0);
            targetJoints[3] = targetJoints[2].GetChild(0);

            targetJoints[4] = handRoot.Find(hand_Short + "_IndexMetacarpal");
            targetJoints[5] = targetJoints[4].GetChild(0);
            targetJoints[6] = targetJoints[5].GetChild(0);
            targetJoints[7] = targetJoints[6].GetChild(0);
            targetJoints[8] = targetJoints[7].GetChild(0);

            targetJoints[9] = handRoot.Find(hand_Short + "_MiddleMetacarpal");
            targetJoints[10] = targetJoints[9].GetChild(0);
            targetJoints[11] = targetJoints[10].GetChild(0);
            targetJoints[12] = targetJoints[11].GetChild(0);
            targetJoints[13] = targetJoints[12].GetChild(0);

            targetJoints[14] = handRoot.Find(hand_Short + "_RingMetacarpal");
            targetJoints[15] = targetJoints[14].GetChild(0);
            targetJoints[16] = targetJoints[15].GetChild(0);
            targetJoints[17] = targetJoints[16].GetChild(0);
            targetJoints[18] = targetJoints[17].GetChild(0);

            targetJoints[19] = handRoot.Find(hand_Short + "_LittleMetacarpal");
            targetJoints[20] = targetJoints[19].GetChild(0);
            targetJoints[21] = targetJoints[20].GetChild(0);
            targetJoints[22] = targetJoints[21].GetChild(0);
            targetJoints[23] = targetJoints[22].GetChild(0);

            targetJoints[24] = handRoot.Find(hand_Short + "_Palm");
            targetJoints[25] = handRoot;




            followingJoints[0] = HexrRoot.Find(hand_Short + "_ThumbMetacarpal");
            followingJoints[1] = followingJoints[0].GetChild(0);
            followingJoints[2] = followingJoints[1].GetChild(0);
            followingJoints[3] = followingJoints[2].GetChild(0);

            followingJoints[4] = HexrRoot.Find(hand_Short + "_IndexMetacarpal");
            followingJoints[5] = followingJoints[4].GetChild(0);
            followingJoints[6] = followingJoints[5].GetChild(0);
            followingJoints[7] = followingJoints[6].GetChild(0);
            followingJoints[8] = followingJoints[7].GetChild(0);

            followingJoints[9] = HexrRoot.Find(hand_Short + "_MiddleMetacarpal");
            followingJoints[10] = followingJoints[9].GetChild(0);
            followingJoints[11] = followingJoints[10].GetChild(0);
            followingJoints[12] = followingJoints[11].GetChild(0);
            followingJoints[13] = followingJoints[12].GetChild(0);

            followingJoints[14] = HexrRoot.Find(hand_Short + "_RingMetacarpal");
            followingJoints[15] = followingJoints[14].GetChild(0);
            followingJoints[16] = followingJoints[15].GetChild(0);
            followingJoints[17] = followingJoints[16].GetChild(0);
            followingJoints[18] = followingJoints[17].GetChild(0);

            followingJoints[19] = HexrRoot.Find(hand_Short + "_LittleMetacarpal");
            followingJoints[20] = followingJoints[19].GetChild(0);
            followingJoints[21] = followingJoints[20].GetChild(0);
            followingJoints[22] = followingJoints[21].GetChild(0);
            followingJoints[23] = followingJoints[22].GetChild(0);

            followingJoints[24] = HexrRoot.Find(hand_Short + "_Palm");
            followingJoints[25] = HexrRoot;
        }
        private void OpenXRFixedUpdate()
        {
            if (HexrRoot == null) return;

            try
            {
                // position
                HexRCompat.SetLinearVelocity(rb, (targePosition - transform.position) / Time.fixedDeltaTime);

                // rotation
                Quaternion deltaRotation = targeRotation * Quaternion.Inverse(rb.rotation);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                //if (float.IsNaN(axis.x)| float.IsNaN(axis.y)| float.IsNaN(axis.z)) { return; }
                //if (float.IsInfinity(axis.x) | float.IsInfinity(axis.y) | float.IsInfinity(axis.z)) { return; }
                if (angle > 180f) { angle -= 360f; };
                Vector3 angularVelocity = angle * axis * Mathf.Deg2Rad / Time.fixedDeltaTime;
                if (float.IsNaN(angularVelocity.x) | float.IsNaN(angularVelocity.y) | float.IsNaN(angularVelocity.z)) { return; }
                rb.angularVelocity = angle * axis * Mathf.Deg2Rad / Time.fixedDeltaTime;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                //logText2.text += "\n" + e.ToString();
            }
        }
        private void OpenXRUpdate()
        {
            if (HexrRoot == null) return;

            try
            {
                targePosition = targetJoints[25].position;
                targeRotation = targetJoints[25].rotation * Quaternion.Euler(rotOffsetPalm);

                for (int i = 0; i < 23; i++)
                {
                    followingJoints[i].localPosition = targetJoints[i].localPosition;
                    followingJoints[i].localRotation = targetJoints[i].localRotation * Quaternion.Euler(rotOffsetFinger);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
#endregion

#region MRTK

#endregion


        public Transform GetDistal(int fingerID)
        {
            Transform distalTransform = null;
            switch (fingerID)
            {
                case 0:
                    distalTransform = GetThumbDistal();
                    break;
                case 1:
                    distalTransform = GetIndexDistal();
                    break;
                case 2:
                    distalTransform = GetMiddleDistal();
                    break;
                case 3:
                    distalTransform = GetRingDistal();
                    break;
                case 4:
                    distalTransform = GetPinkyDistal();
                    break;
            }

            return distalTransform;

        }

        public Transform GetThumbDistal()
        {
            return followingJoints[2];
        }

        public Transform GetIndexDistal()
        {
            return followingJoints[7];
        }

        public Transform GetMiddleDistal()
        {
            return followingJoints[12];
        }

        public Transform GetRingDistal()
        {
            return followingJoints[17];
        }

        public Transform GetPinkyDistal()
        {
            return followingJoints[22];
        }

        #region Raw Hand Joint Resolver
        // Resolves fingertip/palm transforms directly on the raw tracked hand (handRoot),
        // for placing collider + HapticFingerTrigger during Auto Setup. Unlike
        // targetJoints/followingJoints above, these don't require Start()/Handmap() to have
        // run, so they're safe to call from editor code.
        // Tries the OpenXR/XR-Hands joint name first (also matches modern Meta hands that
        // have opted into the OpenXR hand skeleton), falling back to the legacy b_l_/b_r_
        // OVR bone walk for older Meta scenes still on the legacy skeleton.
        public Transform ResolveRawFingerJoint(HapticFingerTrigger.FingerType finger)
        {
            if (handRoot == null || finger == HapticFingerTrigger.FingerType.Palm) return null;

            string openXRShort = handType == HandType.Left ? "L" : "R";
            string legacyShort = handType == HandType.Left ? "b_l" : "b_r";

            Transform openXRTip = ResolveOpenXRFingerTip(finger, openXRShort);
            if (openXRTip != null) return openXRTip;

            return ResolveLegacyFingerTip(finger, legacyShort);
        }

        // Two different OpenXR hand rigs turn up in practice and they name joints differently:
        // Unity's XR Hands rig prefixes with handedness ("L_ThumbMetacarpal"), while Meta's
        // OpenXR hand -- which com.meta.xr.sdk.interaction v201 switches every OVR rig onto,
        // see HandVisual.Awake -- uses a handedness-free "XRHand_ThumbMetacarpal" nested one
        // level down under XRHand_Wrist. Try both names, and fall back to a recursive search
        // so handRoot can be either the mesh root (OpenXRLeftHand) or the wrist itself.
        private const string MetaOpenXRPrefix = "XRHand_";

        private Transform FindJointByName(string jointName)
        {
            if (handRoot == null) return null;

            Transform direct = handRoot.Find(jointName);
            if (direct != null) return direct;

            GameObject nested = FindChildRecursive(handRoot.gameObject, jointName);
            return nested != null ? nested.transform : null;
        }

        // Below the metacarpal it's a plain parent-child bone chain, so -- unlike a flat
        // Find("..Tip") which only checks direct children and can never reach a joint this
        // deeply nested -- we have to walk down GetChild(0) the same number of steps
        // OpenXRStart does to land on the actual Tip joint (3 steps for Thumb's 4-joint chain,
        // 4 steps for the other fingers' 5-joint chains). Both rigs share that shape.
        private Transform ResolveOpenXRFingerTip(HapticFingerTrigger.FingerType finger, string openXRShort)
        {
            if (handRoot == null) return null;

            Transform metacarpal = FindJointByName(openXRShort + "_" + finger.ToString() + "Metacarpal")
                ?? FindJointByName(MetaOpenXRPrefix + finger.ToString() + "Metacarpal");
            if (metacarpal == null) return null;

            return finger == HapticFingerTrigger.FingerType.Thumb
                ? WalkChain(metacarpal, 0, 0, 0)
                : WalkChain(metacarpal, 0, 0, 0, 0);
        }

        // Thumb sits at a different orientation than the other four fingers, so a shared/
        // tuned center offset doesn't carry over the way it does for Index/Middle/Ring/
        // Little. Rigs that ship a "b_l_thumb_null"/"b_r_thumb_null" locator (a common
        // rigging convention for marking a specific point without a real bone) already have
        // the right answer authored -- use its position directly instead of guessing an
        // offset for the thumb.
        public Transform ResolveRawThumbCenterMarker()
        {
            if (handRoot == null) return null;
            string legacyShort = handType == HandType.Left ? "b_l" : "b_r";
            GameObject marker = FindChildRecursive(handRoot.gameObject, legacyShort + "_thumb_null");
            return marker != null ? marker.transform : null;
        }

        public Transform ResolveRawPalmJoint()
        {
            if (handRoot == null) return null;

            string openXRShort = handType == HandType.Left ? "L" : "R";
            Transform openXRPalm = FindJointByName(openXRShort + "_Palm")
                ?? FindJointByName(MetaOpenXRPrefix + "Palm");
            if (openXRPalm != null) return openXRPalm;

            string markerName = handType == HandType.Left ? "l_palm_center_marker" : "r_palm_center_marker";
            GameObject marker = FindChildRecursive(handRoot.gameObject, markerName);
            return marker != null ? marker.transform : null;
        }

        // Mirrors the exact chain-walk MetaOVRStart uses to build targetJoints, landing on
        // the same deepest joint in each finger's chain (targetJoints[3/7/11/15/20]) --
        // without needing the full targetJoints array to have been populated at runtime.
        private Transform ResolveLegacyFingerTip(HapticFingerTrigger.FingerType finger, string legacyShort)
        {
            if (handRoot == null) return null;
            GameObject root = handRoot.gameObject;

            switch (finger)
            {
                case HapticFingerTrigger.FingerType.Thumb:
                    return WalkChain(FindChildRecursive(root, legacyShort + "_thumb0")?.transform, 0, 0, 0);
                case HapticFingerTrigger.FingerType.Index:
                    return WalkChain(FindChildRecursive(root, legacyShort + "_index1")?.transform, 0, 0, 2);
                case HapticFingerTrigger.FingerType.Middle:
                    return WalkChain(FindChildRecursive(root, legacyShort + "_middle1")?.transform, 0, 0, 2);
                case HapticFingerTrigger.FingerType.Ring:
                    return WalkChain(FindChildRecursive(root, legacyShort + "_ring1")?.transform, 0, 0, 2);
                case HapticFingerTrigger.FingerType.Little:
                    return WalkChain(FindChildRecursive(root, legacyShort + "_pinky0")?.transform, 0, 0, 0, 0);
                default:
                    return null;
            }
        }

        // Returns the deepest transform actually reached rather than null on a short chain --
        // landing on a slightly-shallower-than-expected joint is a better fallback than no
        // joint at all.
        private static Transform WalkChain(Transform start, params int[] childSteps)
        {
            Transform current = start;
            foreach (int step in childSteps)
            {
                if (current == null || step >= current.childCount) break;
                current = current.GetChild(step);
            }
            return current;
        }
        #endregion

        #region Hand Orientation Solve
        // Rotating the hand GameObject can't correct a mis-oriented hand -- MetaOVRFixedUpdate
        // drives the rigidbody toward targeRotation every physics step and MetaOVRUpdate rewrites
        // every finger's localRotation every frame -- so rotOffsetPalm is the only hand rotation
        // left under anyone's control. This solves for it rather than making you scrub Euler
        // values, which matters most after the OpenXR skeleton switch: the tracked wrist no
        // longer uses the axis convention the legacy b_l_/b_r_ bones did, and the needed
        // correction is mirrored between hands.
        //
        // Lives in runtime (not the editor tool) so it can also be triggered from inside a
        // build, which is the only place hand tracking actually runs.
        //
        // Both frames are built from joint *positions*, never rotations, so the two skeletons'
        // disagreeing bone axes cannot bias the result.
        public bool TrySolvePalmOffset(out Vector3 solvedOffset, out string error)
        {
            solvedOffset = rotOffsetPalm;

            if (handRoot == null)
            {
                error = "handRoot is not assigned.";
                return false;
            }
            if (HexrRoot == null)
            {
                error = "HexrRoot (the HexR ghost hand) is not assigned, so there is nothing to orient.";
                return false;
            }

            string legacyShort = handType == HandType.Left ? "b_l" : "b_r";
            string ghostShort = handType == HandType.Left ? "L" : "R";

            Transform wrist = JointIn(handRoot, MetaOpenXRPrefix + "Wrist", legacyShort + "_wrist");
            Transform middle = JointIn(handRoot, MetaOpenXRPrefix + "MiddleProximal", legacyShort + "_middle1");
            Transform index = JointIn(handRoot, MetaOpenXRPrefix + "IndexProximal", legacyShort + "_index1");
            Transform little = JointIn(handRoot, MetaOpenXRPrefix + "LittleProximal", legacyShort + "_pinky1");
            if (wrist == null || middle == null || index == null || little == null)
            {
                error = "couldn't find the tracked hand's wrist/index/middle/little base joints under " + handRoot.name + ".";
                return false;
            }

            Transform ghostMiddle = JointIn(HexrRoot, ghostShort + "_Middle_1");
            Transform ghostIndex = JointIn(HexrRoot, ghostShort + "_Index_1");
            Transform ghostLittle = JointIn(HexrRoot, ghostShort + "_Pinky_00", ghostShort + "_Pinky_0");
            if (ghostMiddle == null || ghostIndex == null || ghostLittle == null)
            {
                error = "couldn't find the ghost hand's base finger joints under " + HexrRoot.name + ".";
                return false;
            }

            Quaternion trackedFrame;
            if (!TryBuildPalmFrame(wrist, middle, index, little, out trackedFrame))
            {
                error = "the tracked hand's joints are all but coincident -- is hand tracking actually running?";
                return false;
            }

            Quaternion ghostFrame;
            if (!TryBuildPalmFrame(HexrRoot, ghostMiddle, ghostIndex, ghostLittle, out ghostFrame))
            {
                error = "the ghost hand's joints are degenerate.";
                return false;
            }

            // MetaOVRUpdate drives the hand to `wrist.rotation * Euler(rotOffsetPalm)`, so a
            // world-space correction has to be folded back through that same wrist frame:
            //   delta * (W * Q_old) == W * Q_new   =>   Q_new = inverse(W) * delta * W * Q_old
            Quaternion delta = trackedFrame * Quaternion.Inverse(ghostFrame);
            Quaternion w = wrist.rotation;
            Quaternion solved = Quaternion.Inverse(w) * delta * w * Quaternion.Euler(rotOffsetPalm);

            solvedOffset = NormalizeEuler(solved.eulerAngles);
            error = null;
            return true;
        }

        // FindChildRecursive only walks children, so check the root itself too -- handRoot can
        // legitimately be the wrist (XRHand_Wrist) rather than the mesh root above it.
        private Transform JointIn(Transform root, params string[] names)
        {
            foreach (string name in names)
            {
                if (root.name == name)
                {
                    return root;
                }
                GameObject found = FindChildRecursive(root.gameObject, name);
                if (found != null)
                {
                    return found.transform;
                }
            }
            return null;
        }

        // Wrist-to-middle-base for forward, index-base-to-little-base for sideways. The cross
        // order is arbitrary but identical for both hands' frames, so the convention cancels
        // out of the delta between them.
        private static bool TryBuildPalmFrame(Transform wrist, Transform middle, Transform index, Transform little, out Quaternion frame)
        {
            frame = Quaternion.identity;

            Vector3 forward = middle.position - wrist.position;
            Vector3 side = index.position - little.position;
            if (forward.sqrMagnitude < 1e-8f || side.sqrMagnitude < 1e-8f)
            {
                return false;
            }

            Vector3 up = Vector3.Cross(forward.normalized, side.normalized);
            if (up.sqrMagnitude < 1e-6f)
            {
                return false;
            }

            frame = Quaternion.LookRotation(forward.normalized, up.normalized);
            return true;
        }

        public static Vector3 SnapTo90(Vector3 euler)
        {
            return new Vector3(
                Mathf.Round(euler.x / 90f) * 90f,
                Mathf.Round(euler.y / 90f) * 90f,
                Mathf.Round(euler.z / 90f) * 90f);
        }

        public static Vector3 NormalizeEuler(Vector3 euler)
        {
            return new Vector3(Wrap180(euler.x), Wrap180(euler.y), Wrap180(euler.z));
        }

        private static float Wrap180(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }
        #endregion
    }
}
