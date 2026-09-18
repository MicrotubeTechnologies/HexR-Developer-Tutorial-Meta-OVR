using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HaptGlove
{
    public class CollisionDetection : MonoBehaviour
    {
        private HaptGloveHandler gloveHander;
        private Grasping graspLeftScript;

        Int64 enterTime = 0;
        Int64 exitTime = 0;
        private int delayColEnterTime = 100; //ms
        private int delayColExitTime = 100;  //ms
        private bool delayColExit = false;
        private bool delayColEnter = false;
        //private bool colEnterSend = false;
        //private bool colEntered = false;

        private Collider bufCol;

        public byte fingerTouchedColliders = 0;

        void Start()
        {
            gloveHander = GetComponentInParent<HaptGloveHandler>();
            graspLeftScript = GetComponentInParent<Grasping>();
        }

        void FixedUpdate()
        {
            if (delayColExit == true)
            {
                if (System.Environment.TickCount > enterTime + delayColExitTime)
                {
                    delayColExit = false;
                    if ((fingerTouchedColliders == 0))
                    {
                        graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Exit");
                        //Debug.Log("haha: Loop Exit Collider: " + bufCol.name);

                        exitTime = System.Environment.TickCount;
                        delayColEnter = true;
                        //colEnterSend = false;
                        //Debug.Log("haha: haptics off");
                    }
                }
            }

            if (delayColEnter == true)
            {
                if (Environment.TickCount > exitTime + delayColEnterTime)
                {
                    delayColEnter = false;
                    if ((fingerTouchedColliders == 1))
                    {
                        graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Enter");
                        //Debug.Log("haha: Loop Enter Collider: " + bufCol.name);

                        enterTime = System.Environment.TickCount;
                        //colEnterSend = true;
                        delayColExit = true;
                        //Debug.Log("haha: haptics on");
                    }
                }
            }

        }

        void OnTriggerEnter(Collider collider)
        {
            if (collider.GetComponent<HapMaterial>()==null)
            {
                return;
            }

            if (!gloveHander.hapticsInteratableLayers.Contains(collider.gameObject.layer))
            {
                return;
            }

            //if (collider.gameObject.layer != LayerMask.NameToLayer("HaptGloveInteractable"))
            //{
            //    return;
            //}

            //graspLeftScript.ChildColliderState(collider, gameObject.name, "Enter");

            fingerTouchedColliders++;

            //colEntered = true;
            bufCol = collider;
            // 加防抖
            if ((delayColEnter == false) & (fingerTouchedColliders == 1))
            {
                graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Enter");
                //Debug.Log("haha: Enter Collider: " + bufCol.name);

                enterTime = System.Environment.TickCount;
                //colEnterSend = true;
                delayColExit = true;
                //Debug.Log("haha: haptics on");
            }

            //if ((delayColEnter == false) & (colEnterSend == false))
            //{
            //    graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Enter");
            //    //Debug.Log("haha: Enter Collider: " + bufCol.name);

            //    enterTime = System.Environment.TickCount;
            //    colEnterSend = true;
            //    delayColExit = true;
            //    //Debug.Log("haha: haptics on");
            //}

        }

        void OnTriggerExit(Collider collider)
        {
            if (collider.GetComponent<HapMaterial>() == null)
            {
                return;
            }

            if (!gloveHander.hapticsInteratableLayers.Contains(collider.gameObject.layer))
            {
                return;
            }

            //if (collider.gameObject.layer != LayerMask.NameToLayer("HaptGloveInteractable"))
            //{
            //    return;
            //}

            //graspLeftScript.ChildColliderState(collider, gameObject.name, "Exit");

            fingerTouchedColliders--;

            //colEntered = false;
            bufCol = collider;

            //graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Exit");
            //Debug.Log("haha: Exit Collider: " + bufCol.name);

            // 加防抖
            if ((delayColExit == false) & (fingerTouchedColliders == 0))
            {
                graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Exit");
                exitTime = System.Environment.TickCount;
                delayColEnter = true;
                //colEnterSend = false;
                //Debug.Log("haha: haptics off");
                //Debug.Log("haha: Exit Collider: " + bufCol.name);
            }

        }


    }
}
