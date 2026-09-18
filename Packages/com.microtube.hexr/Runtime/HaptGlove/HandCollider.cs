using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;

namespace HaptGlove
{
    public class HandCollider : MonoBehaviour
    {
        public GameObject thisHand;
        private GameObject[] ghostFingertip;

        private byte targetPres = 30;

        private ParentConstraint parentConstraint;

        void Start()
        {
#if UNITY_ANDROID
        gameObject.SetActive(false);
        return;
#endif
            Transform[] allObjects = thisHand.transform.GetComponentsInChildren<Transform>().OrderBy(g => g.transform.GetSiblingIndex()).ToArray();
            List<GameObject> listGhostFinger = new List<GameObject>();

            foreach (var bufObject in allObjects)
            {
                if (bufObject.tag == "GhostHand")
                {
                    listGhostFinger.Add(bufObject.gameObject);
                }
            }

            ghostFingertip = listGhostFinger.ToArray();
        }

        void SetUpParentConstrain()
        {

        }

        void OnTriggerEnter(Collider col)
        {
            if (col.name == "HandCollider")
            {
                ShakeHand(col);

                Debug.Log("Shake Hands");
            }
        }

        void OnTriggerExit(Collider col)
        {
            if (col.name == "HandCollider")
            {
                byte[][] clutchStates =
                    {new byte[] {0, 2}, new byte[] {1, 2}, new byte[] {2, 2}, new byte[] {3, 2}, new byte[] {4, 2}};
                byte[] btData = thisHand.GetComponent<HaptGloveHandler>().haptics.ApplyHaptics(clutchStates, targetPres, false);
                thisHand.GetComponent<HaptGloveHandler>().BTSend(btData);
                for (int i = 0; i < ghostFingertip.Length; i++)
                {
                    ghostFingertip[i].GetComponent<CapsuleCollider>().enabled = true;
                }
            }

        }

        void ShakeHand(Collider col)
        {
            for (int i = 0; i < ghostFingertip.Length; i++)
            {
                ghostFingertip[i].GetComponent<CapsuleCollider>().enabled = false;
            }

            byte[][] clutchStates = new byte[5][];
            for (byte i = 0; i < 5; i++)
            {
                if (thisHand.GetComponent<Grasping>().collider[i] == null)
                {
                    clutchStates[i] = new byte[] { i, 0 };
                }
            }
            byte[] btData = thisHand.GetComponent<HaptGloveHandler>().haptics.ApplyHaptics(clutchStates, targetPres, false);
            thisHand.GetComponent<HaptGloveHandler>().BTSend(btData);


        }
    }
}
