using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HaptGlove;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

namespace HexR
{
    public class ProximityCheck : MonoBehaviour
    {
        public PressureTrackerMain pressureTrackerMain;
        private bool restart = false;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
        private void OnTriggerEnter(Collider other)
        {
            pressureTrackerMain.IsPhysicsCollisionNear(true);
            StartCoroutine(removeCollisiontrue());
        }
        private void OnTriggerStay(Collider other)
        {
            pressureTrackerMain.IsPhysicsCollisionNear(true);
            restart = false;
        }
        IEnumerator removeCollisiontrue()
        {
            // Wait for the specified delay time
            yield return new WaitForSeconds(1f);
            if(restart == true)
            {
                pressureTrackerMain.IsPhysicsCollisionNear(false);
            }
            else
            {
                restart = true;
                StartCoroutine(removeCollisiontrue());
            }


        }
        private void OnTriggerExit(Collider other)
        {
            pressureTrackerMain.IsPhysicsCollisionNear(false);
            restart = true;
        }
    }
}
