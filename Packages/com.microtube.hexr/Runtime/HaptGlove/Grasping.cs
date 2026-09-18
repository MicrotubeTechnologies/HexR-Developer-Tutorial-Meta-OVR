using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HaptGlove
{
    public class Grasping : MonoBehaviour
    {
        public HaptGloveHandler haptGloveHandler;

        private string secondHandLayer;
        private HapMaterial hapticMaterial = null;
        private List<String> colNameList = new List<String>();
        private Rigidbody targetRigidbody;
        [HideInInspector] public GameObject currentHapticObject;
        [HideInInspector] public GameObject[] realFingertip;
        [HideInInspector] public GameObject[] ghostFingertip;
        private DeformMesh[] deformer = new DeformMesh[5];
        [HideInInspector] public Collider[] collider = new Collider[5];
        [HideInInspector] public bool[] touchedFingers = new bool[5];
        [HideInInspector] public bool holdState = false;
        private bool startDeform = false;
        private Vector3[] distance = new Vector3[5];
        private bool[] firstContactDeform = new bool[5];

        private bool[] hapticsState = new bool[5];
        [HideInInspector] public float[] hapticStartPosition = new float[5];
        [HideInInspector] public float[] hapticMaxPosition = new float[5];
        private byte objectPressure = 0;
        private byte vibFrequency = 0;
        private byte vibIntensity = 0;
        [HideInInspector] public float[] normalizedMicrotubeData = new float[5];


        void Start()
        {
            Transform[] allObjects = transform.GetComponentsInChildren<Transform>().OrderBy(g => g.transform.GetSiblingIndex()).ToArray();
            List<GameObject> listGhostFinger = new List<GameObject>();
            List<GameObject> listRealFinger = new List<GameObject>();

            foreach (var bufObject in allObjects)
            {
                if (bufObject.tag == "GhostHand")
                {
                    listGhostFinger.Add(bufObject.gameObject);
                }

                if (bufObject.tag == "RealFingertip")
                {
                    listRealFinger.Add(bufObject.gameObject);
                }
            }

            ghostFingertip = listGhostFinger.ToArray();
            realFingertip = listRealFinger.ToArray();
            foreach (var bufObject in ghostFingertip)
            {
                bufObject.AddComponent<CollisionDetection>();
            }

            if (LayerMask.LayerToName(gameObject.layer) == "Hand1")
            {
                secondHandLayer = "Hand2";
            }
            else
            {
                secondHandLayer = "Hand1";
            }

            //textSliderOn = GameObject.Find("Text Slider On").GetComponent<Text>();
            //textSliderOff = GameObject.Find("Text Slider Off").GetComponent<Text>();
            //sliderOn = GameObject.Find("Slider On Duration").GetComponent<Slider>();
            //sliderOff = GameObject.Find("Slider Off Duration").GetComponent<Slider>();
        }


        void FixedUpdate()
        {
            //textSliderOn.text = "On Duration: " + sliderOn.value;
            //textSliderOff.text = "Off Duration: " + sliderOff.value;

            if ((colNameList.Count >= 2) & colNameList.Contains("GhostThumb"))
            {
                if (holdState == false)
                {
                    if (!hapticMaterial.isTouch)
                    {
                        if (hapticMaterial.iskinematicGrasp)
                        {
                            targetRigidbody.isKinematic = false;
                        }
                        gameObject.AddComponent<FixedJoint>();
                        gameObject.GetComponent<FixedJoint>().connectedBody = targetRigidbody;
                        Debug.Log("Create fixed joint");
                    }

                    targetRigidbody.GetComponent<HapMaterial>().isGrasped = true;
                    targetRigidbody.GetComponent<HapMaterial>().graspedHand = gameObject;
                    holdState = true;

                    for (int i = 0; i < realFingertip.Length; i++)
                    {
                        realFingertip[i].GetComponent<Collider>().isTrigger = true;
                    }

                    //foreach (var VARIABLE in colNameList)
                    //{
                    //    Debug.Log(VARIABLE);
                    //}
                }
            }
            else
            {
                if (holdState == true)
                {
                    if (!hapticMaterial.isTouch)
                    {
                        if (hapticMaterial.iskinematicGrasp)
                        {
                            targetRigidbody.isKinematic = true;
                        }
                        Destroy(gameObject.GetComponent<FixedJoint>());
                        targetRigidbody.GetComponent<HapMaterial>().isGrasped = false;
                        targetRigidbody.GetComponent<HapMaterial>().graspedHand = null;
                        Debug.Log("Destroy fixed joint");
                    }

                    holdState = false;

                    for (int i = 0; i < realFingertip.Length; i++)
                    {
                        realFingertip[i].GetComponent<Collider>().isTrigger = false;
                    }

                    hapticStartPosition = new float[5];
                    hapticGraspingIsStart = new bool[5];

                    //foreach (var VARIABLE in colNameList)
                    //{
                    //    Debug.Log(VARIABLE);
                    //}
                }
            }

            if (colNameList.Count == 0)
            {
                currentHapticObject = null;
            }

            if (startDeform)
            {
                for (byte fingerID = 0; fingerID < 5; fingerID++)
                {
                    if (collider[fingerID] != null)
                    {
                        UpdateDeform(collider[fingerID], fingerID);
                    }
                }

                if (colNameList.Count == 0)
                {
                    startDeform = false;
                }
            }

        }

        //void OnCollisionStay(Collision collisionInfo)
        //{
        //    // Debug-draw all contact points and normals
        //    foreach (ContactPoint contact in collisionInfo.contacts)
        //    {
        //        Debug.DrawRay(contact.point, contact.normal, Color.white);
        //    }
        //}

        [HideInInspector] public float removeHapitcsRatio = 1f;
        [HideInInspector] public float removeHapticsDifference = 0.5f;/////// not sure
        [HideInInspector] public bool[] hapticGraspingIsStart = new bool[5];
        public void GetCurrentMicrotubeData(float[] buf)
        {
            normalizedMicrotubeData = buf;

            for (byte i = 0; i < 5; i++)
            {
                if (hapticGraspingIsStart[i] == true)
                {
                    if (hapticMaxPosition[i] < normalizedMicrotubeData[i])
                    {
                        hapticMaxPosition[i] = normalizedMicrotubeData[i];
                    }

                    if (hapticStartPosition[i] == hapticMaxPosition[i])
                    {
                        continue;
                    }
                    if (((float)(normalizedMicrotubeData[i] - hapticMaxPosition[i]) / (hapticStartPosition[i] - hapticMaxPosition[i]) > removeHapitcsRatio) & (removeHapitcsRatio != 1))
                    {
                        //Remove haptics
                        colNameList.Remove(Haptics.GetGhostFingerName(i));
                        byte[] btData = haptGloveHandler.haptics.ApplyHaptics(new byte[] { i, 2 }, objectPressure, true);
                        haptGloveHandler.BTSend(btData);
                        hapticsState[i] = false;
                        hapticStartPosition[i] = 0;
                        hapticGraspingIsStart[i] = false;
                        collider[i] = null;
                        touchedFingers[i] = false;

                        if (deformer[i] != null)
                        {
                            deformer[i] = null;
                            Destroy(touchPointGameObject[i]);
                            Destroy(forcePointGameObject[i]);
                        }
                    }

                    if ((hapticMaxPosition[i] - normalizedMicrotubeData[i]) > removeHapticsDifference)
                    {
                        //Remove haptics
                        colNameList.Remove(Haptics.GetGhostFingerName(i));
                        byte[] btData = haptGloveHandler.haptics.ApplyHaptics(new byte[] { i, 2 }, objectPressure, true);
                        haptGloveHandler.BTSend(btData);
                        hapticsState[i] = false;
                        hapticStartPosition[i] = 0;
                        hapticGraspingIsStart[i] = false;
                        collider[i] = null;
                        touchedFingers[i] = false;

                        if (deformer[i] != null)
                        {
                            deformer[i] = null;
                            Destroy(touchPointGameObject[i]);
                            Destroy(forcePointGameObject[i]);
                        }
                    }

                }
                else
                {
                    hapticMaxPosition[i] = 0;
                }
            }

        }

        private void LeaveTrail(Vector3 point, float scale)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = Vector3.one * scale;
            sphere.transform.position = point;
            //sphere.transform.parent = transform.parent;
            sphere.GetComponent<Collider>().enabled = false;
            Destroy(sphere, 1);
        }

        Vector3[] closestPointOnObjectCollider = new Vector3[5];
        Vector3[] normalDir = new Vector3[5];
        GameObject[] touchPointGameObject = new GameObject[5];
        GameObject[] forcePointGameObject = new GameObject[5];
        private int slidingThreshold = 20;  //滑动偏移角度阈值
        Ray[] ray_UpdateTouchPoint = new Ray[5];

        void CreateTouchPointGameObject(byte fingerID, Vector3 position, float scale, Collider col)
        {
            touchPointGameObject[fingerID] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            touchPointGameObject[fingerID].transform.localScale = Vector3.one * scale;
            touchPointGameObject[fingerID].transform.position = position;
            touchPointGameObject[fingerID].transform.parent = col.transform;
            touchPointGameObject[fingerID].GetComponent<Collider>().enabled = false;
            touchPointGameObject[fingerID].GetComponent<MeshRenderer>().enabled = false;
        }

        void CreateForcePointGameObject(byte fingerID, Vector3 position, float scale)
        {
            forcePointGameObject[fingerID] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            forcePointGameObject[fingerID].transform.localScale = Vector3.one * scale;
            forcePointGameObject[fingerID].transform.position = position;
            forcePointGameObject[fingerID].transform.parent = ghostFingertip[fingerID].transform;
            forcePointGameObject[fingerID].GetComponent<Collider>().enabled = false;
            forcePointGameObject[fingerID].GetComponent<MeshRenderer>().enabled = false;
        }

        void UpdateDeform(Collider col, byte fingerID)
        {
            if (firstContactDeform[fingerID] == true)
            {
                closestPointOnObjectCollider[fingerID] = col.ClosestPoint(ghostFingertip[fingerID].transform.position);

                //在近似碰撞点创建小球，附着在手指和碰撞物体上，作为标记，计算手指移动距离和角度
                CreateTouchPointGameObject(fingerID, closestPointOnObjectCollider[fingerID], 0.003f, col);
                CreateForcePointGameObject(fingerID, closestPointOnObjectCollider[fingerID], 0.003f);

                //手指尖中心位置到接触点向量，作为初始滑动圆锥轴线
                normalDir[fingerID] =
                    -(forcePointGameObject[fingerID].transform.localPosition - ghostFingertip[fingerID].transform.localPosition).normalized;
                firstContactDeform[fingerID] = false;
                return;
            }

            distance[fingerID] = forcePointGameObject[fingerID].transform.position + 0.001f * normalDir[fingerID] -
                                 touchPointGameObject[fingerID].transform.position;
            deformer[fingerID].AddDeformingForce(touchPointGameObject[fingerID].transform.position, distance[fingerID], fingerID);

            //判断手指滑动太大后，更新接触点位置，并更新滑动圆锥轴线为新接触点法线
            float angle = Vector3.Angle(normalDir[fingerID], distance[fingerID]);
            if (angle > slidingThreshold)
            {
                //更新ClosestPointOnObjectCollider的位置和法线
                UpdateTouchPoint(fingerID);
            }

            Debug.DrawRay(touchPointGameObject[fingerID].transform.position, distance[fingerID].normalized, Color.red);
            Debug.DrawRay(touchPointGameObject[fingerID].transform.position, normalDir[fingerID].normalized, Color.blue);
            //Debug.Log("Angle: " + angle);
            //Debug.Log("Distance: " + distance[fingerID]);

        }

        //理解为：原滑动圆锥轴线在平面内，绕过原接触点的平面法线旋转α角，获得Ray角度向量。过力点沿Ray角度做偏移，使Ray其实点在物体外，做Ray，与物体交点作为新接触点。新轴线为接触点法线。
        void UpdateTouchPoint(byte fingerID)
        {
            Vector3 rotAxis = Vector3.Cross(normalDir[fingerID], distance[fingerID].normalized).normalized;
            Vector3 rayDir = normalDir[fingerID] * (float)Math.Cos((double)slidingThreshold / 180 * Math.PI) + Vector3.Cross(rotAxis, normalDir[fingerID]) * (float)Math.Sin((double)slidingThreshold / 180 * Math.PI) + rotAxis * Vector3.Dot(rotAxis, normalDir[fingerID]) * (1 - (float)Math.Cos((double)slidingThreshold / 180 * Math.PI));
            rayDir = rayDir.normalized;
            Debug.DrawRay(touchPointGameObject[fingerID].transform.position, rotAxis, Color.yellow);
            Debug.DrawRay(touchPointGameObject[fingerID].transform.position, rayDir, Color.black);

            ray_UpdateTouchPoint[fingerID] = new Ray(forcePointGameObject[fingerID].transform.position - 0.1f * rayDir, rayDir);
            RaycastHit hitInfo;
            int layerMask = currentHapticObject.layer;
            //int layerMask = LayerMask.GetMask("HaptGloveInteractable");
            if (Physics.Raycast(ray_UpdateTouchPoint[fingerID], out hitInfo, 100, layerMask, QueryTriggerInteraction.Collide))
            {
                //Debug.Log("击中");
                //LeaveTrail(hitInfo.point, 0.003f);
                Debug.DrawLine(ray_UpdateTouchPoint[fingerID].origin, hitInfo.point, Color.black);
                touchPointGameObject[fingerID].transform.position = hitInfo.point;
                normalDir[fingerID] = -hitInfo.normal;
            }
            else
            {
                Debug.Log("未击中");
            }
        }


        void CollideWithHand(Collider col, String bufName, String bufState)
        {
            byte[] clutchState = Haptics.SetClutchState(bufName, bufState);// clutchState[0] = fingerID, clutchState[1] = Enter or Stay or Exit
            byte fingerID = clutchState[0];

            byte targetPres = 30;

            if (bufState == "Enter")
            {
                if (colNameList.Contains(bufName))
                {
                    return;
                }

                //colNameList.Add(bufName);
                byte[] btData = haptGloveHandler.haptics.ApplyHaptics(clutchState, targetPres, false);
                haptGloveHandler.BTSend(btData);
                Debug.Log(bufName + "Hand Collider Enter");

                //collider[fingerID] = col;
                //deformer[fingerID] = col.gameObject.GetComponent<DeformMesh>();
                //if (deformer[fingerID] == null)
                //{
                //    Debug.Log("It's a Rigid Object");
                //}
                //else
                //{
                //    Debug.Log("It's a Soft Object");
                //    startDeform = true;
                //    firstContactDeform[fingerID] = true;
                //}
            }
            else if (bufState == "Stay")
            {
                Debug.Log(bufName + "Hand Collider Stay");
                return;
            }
            else if (bufState == "Exit")
            {
                //colNameList.Remove(bufName);
                byte[] btData = haptGloveHandler.haptics.ApplyHaptics(clutchState, targetPres, false);
                haptGloveHandler.BTSend(btData);
                //collider[fingerID] = null;
                //if (deformer[fingerID] != null)
                //{
                //    deformer[fingerID] = null;
                //    Destroy(touchPointGameObject[fingerID]);
                //    Destroy(forcePointGameObject[fingerID]);
                //}
                Debug.Log(bufName + "Hand Collider Exit");
            }
            else
            {
                return;
            }
        }

        //public byte[] fingerTouchedColliders = new byte[5];
        //public void ChildColliderState(Collider col, String bufName, String bufState)
        //{
        //    if (LayerMask.LayerToName(col.gameObject.layer) == secondHandLayer)
        //    {
        //        CollideWithHand(col, bufName, bufState);
        //        return;
        //    }

        //    if (col.GetComponent<Rigidbody>() == null)
        //    {
        //        return;
        //    }

        //    if (currentHapticObject == null)
        //    {
        //        currentHapticObject = col.gameObject;
        //    }
        //    else
        //    {
        //        //if (currentHapticObject.tag != col.gameObject.tag)
        //        //{
        //        //    return;
        //        //}
        //        if (currentHapticObject != col.gameObject)
        //        {
        //            return;
        //        }
        //    }

        //    targetRigidbody = col.GetComponent<Rigidbody>();
        //    Debug.Log("Target Rigidbody: " + col.gameObject.name);

        //    hapticMaterial = col.gameObject.GetComponent<HapMaterial>();

        //    if (hapticMaterial == null)
        //    {
        //        Debug.Log("No Haptic Material Assigned");
        //        return;
        //    }

        //    byte[] clutchState = Haptics.SetClutchState(bufName, bufState);// clutchState[0] = fingerID, clutchState[1] = Enter or Stay or Exit
        //    byte fingerID = clutchState[0];

        //    if (hapticMaterial.isOneObject)
        //    {


        //        switch (bufState)
        //        {
        //            case "Enter":
        //                if (fingerTouchedColliders[fingerID] == 0)
        //                {
        //                    fingerTouchedColliders[fingerID]++;
        //                }
        //                else if (fingerTouchedColliders[fingerID] > 0)
        //                {
        //                    fingerTouchedColliders[fingerID]++;
        //                    return;
        //                }
        //                else
        //                {
        //                    Debug.LogError("Invalid value (Enter): FingerTouchedColliders[" + fingerID + "] = " + fingerTouchedColliders[fingerID]);
        //                }
        //                break;
        //            case "Exit":
        //                if (fingerTouchedColliders[fingerID] == 1)
        //                {
        //                    fingerTouchedColliders[fingerID]--;
        //                }
        //                else if (fingerTouchedColliders[fingerID] > 1)
        //                {
        //                    fingerTouchedColliders[fingerID]--;
        //                    return;
        //                }
        //                else
        //                {
        //                    Debug.LogError("Invalid value (Exit): FingerTouchedColliders[" + fingerID + "] = " + fingerTouchedColliders[fingerID]);
        //                }
        //                break;
        //        }
        //    }

        //    byte targetPres = hapticMaterial.targetPressure;
        //    objectPressure = targetPres;

        //    if (bufState == "Enter")
        //    {
        //        if (colNameList.Contains(bufName))
        //        {
        //            return;
        //        }

        //        colNameList.Add(bufName);
        //        if ((col.tag == "Palpation") & (fingerID == 4))
        //        {
        //            //Do not apply haptics to Pinky when doing palpation
        //            Debug.Log("palpation");
        //        }
        //        else
        //        {
        //            Haptics.ApplyHaptics(clutchState, targetPres, whichHand, true);
        //        }

        //        hapticsState[fingerID] = true;
        //        //Debug.Log(bufName + "Enter");

        //        collider[fingerID] = col;
        //        deformer[fingerID] = col.gameObject.GetComponent<DeformMesh>();
        //        if (deformer[fingerID] == null)
        //        {
        //            Debug.Log("It's a Rigid Object");
        //        }
        //        else
        //        {
        //            Debug.Log("It's a Soft Object");
        //            startDeform = true;
        //            firstContactDeform[fingerID] = true;
        //        }
        //    }
        //    else if (bufState == "Stay")
        //    {
        //        Debug.Log(bufName + "Stay");
        //        return;
        //    }
        //    else if (bufState == "Exit")
        //    {
        //        if (hapticsState[fingerID] != false)
        //        {

        //            if ((col.tag == "Palpation") & (fingerID == 4))
        //            {
        //                //Do not apply haptics to pinky when doing palpation
        //            }
        //            else if ((col.tag == "Palpation") & (col.name == "Abdomen") & (fingerID == 0))
        //            {
        //                if (hapticMaterial.isGrasped)
        //                {
        //                    //When grasped, do not remove haptics to thumb when pressing abdomen. (Avoid hand passing through abdomen)
        //                    return;
        //                }
        //            }
        //            else
        //            {
        //                if (col.tag == "Tumor")     //Do not remove haptics when touching tumor
        //                {
        //                    Debug.Log("leave tumor");
        //                }
        //                else
        //                {
        //                    Haptics.ApplyHaptics(clutchState, targetPres, whichHand, true);
        //                }
        //            }
        //            colNameList.Remove(bufName);

        //            hapticsState[fingerID] = false;
        //            hapticStartPosition[fingerID] = 0;
        //            hapticGraspingIsStart[fingerID] = false;
        //            collider[fingerID] = null;

        //            if (deformer[fingerID] != null)
        //            {
        //                deformer[fingerID] = null;
        //                Destroy(touchPointGameObject[fingerID]);
        //                Destroy(forcePointGameObject[fingerID]);
        //            }
        //        }

        //        //Debug.Log(bufName + "Exit");
        //    }
        //    else
        //    {
        //        return;
        //    }

        //}

        //byte[] SetValveTimingFromSlider(byte targetPres)
        //{
        //    byte[] valveTiming = new byte[2] { 0xff, 0xff };
        //    //byte vOpen = 50;
        //    //byte vDelay = 50;
        //    byte vOpen = (byte)sliderOn.value;
        //    byte vDelay = (byte)sliderOff.value;
        //    valveTiming[0] = vOpen;
        //    valveTiming[1] = vDelay;
        //    return valveTiming;
        //}

        //public void DropObject()
        //{
        //    Debug.Log("Drop object");
        //    colNameList.Clear();
        //    for (int i = 0; i < collider.Length; i++)
        //    {
        //        collider[i] = null;
        //    }

        //    fingerTouchedColliders = new byte[5];
        //}





        public void ChildColliderState(Collider col, String bufName, String bufState)
        {
            if (LayerMask.LayerToName(col.gameObject.layer) == secondHandLayer)
            {
                CollideWithHand(col, bufName, bufState);
                return;
            }

            if (col.GetComponent<Rigidbody>() == null)
            {
                return;
            }

            if (currentHapticObject == null)
            {
                currentHapticObject = col.gameObject;
            }
            else
            {
                //if (currentHapticObject.tag != col.gameObject.tag)
                //{
                //    return;
                //}
                if (currentHapticObject != col.gameObject)
                {
                    return;
                }
            }

            targetRigidbody = col.GetComponent<Rigidbody>();
            Debug.Log("Target Rigidbody: " + col.gameObject.name);

            hapticMaterial = col.gameObject.GetComponent<HapMaterial>();

            if (hapticMaterial == null)
            {
                Debug.Log("No Haptic Material Assigned");
                return;
            }

            byte[] clutchState = Haptics.SetClutchState(bufName, bufState);// clutchState[0] = fingerID, clutchState[1] = Enter or Stay or Exit
            byte fingerID = clutchState[0];
            if (hapticMaterial.hapticsChannels[fingerID] == false)
            {
                Debug.Log("Do not apply haptics to this channel: " + fingerID); 
                return;
            }

            byte targetPres = 0;
            byte[] valveTiming = new byte[2];
            if (hapticMaterial.isVibration)
            {
                vibFrequency = hapticMaterial.vibFrequency;
                vibIntensity = hapticMaterial.vibIntensity;

                valveTiming = haptGloveHandler.haptics.GetValveTimingFromVibIntensity(vibFrequency, vibIntensity, fingerID);
            }
            else
            {
                targetPres = hapticMaterial.targetPressure;
            }
            
            objectPressure = targetPres;

            if (bufState == "Enter")
            {
                if (colNameList.Contains(bufName))
                {
                    return;
                }

                if (bufName != "GhostPalm")
                {
                    colNameList.Add(bufName);
                }
                
                if ((col.tag == "Palpation") & (fingerID == 4))
                {
                    // Do not apply haptics to Pinky when doing palpation
                    Debug.Log("palpation");
                }
                else
                {
                    if (hapticMaterial.isVibration)
                    {
                        byte[] btData = haptGloveHandler.haptics.ApplyHaptics(vibFrequency, clutchState, 20, false);
                        haptGloveHandler.BTSend(btData);
                    }
                    else
                    {
                        byte[] btData = haptGloveHandler.haptics.ApplyHaptics(clutchState, targetPres, true);
                        haptGloveHandler.BTSend(btData);
                    }
                    
                }

                if (fingerID < 5)
                {
                    hapticsState[fingerID] = true;
                    //Debug.Log(bufName + "Enter");

                    collider[fingerID] = col;
                    touchedFingers[fingerID] = true;
                    deformer[fingerID] = col.gameObject.GetComponent<DeformMesh>();
                    if (deformer[fingerID] == null)
                    {
                        Debug.Log("It's a Rigid Object");
                    }
                    else
                    {
                        Debug.Log("It's a Soft Object");
                        startDeform = true;
                        firstContactDeform[fingerID] = true;
                    }
                }
                
            }
            else if (bufState == "Stay")
            {
                Debug.Log(bufName + "Stay");
                return;
            }
            else if (bufState == "Exit")
            {
                if (fingerID == 5)
                {
                    if (hapticMaterial.isVibration)
                    {
                        byte[] btData = haptGloveHandler.haptics.ApplyHaptics(vibFrequency, clutchState, 20, false);
                        haptGloveHandler.BTSend(btData);

                        ////Ensure haptics removed completely.
                        //btData = haptGloveHandler.haptics.ApplyHaptics(clutchState, 30, false);
                        //haptGloveHandler.BTSend(btData);
                    }
                    else
                    {
                        byte[] btData = haptGloveHandler.haptics.ApplyHaptics(clutchState, targetPres, true);
                        haptGloveHandler.BTSend(btData);
                    }
                    return;
                }

                if (hapticsState[fingerID])
                {
                    if ((col.tag == "Palpation") & (fingerID == 4))
                    {
                        // Do not apply haptics to pinky when doing palpation
                    }
                    else if ((col.tag == "Palpation") & (col.name == "Abdomen") & (fingerID == 0))
                    {
                        if (hapticMaterial.isGrasped)
                        {
                            // When grasped, do not remove haptics to thumb when pressing abdomen. (Avoid hand passing through abdomen)
                            return;
                        }
                    }
                    else
                    {
                        if (col.tag == "Tumor")     //Do not remove haptics when touching tumor
                        {
                            Debug.Log("leave tumor");
                        }
                        else
                        {
                            if (hapticMaterial.isVibration)
                            {
                                byte[] btData = haptGloveHandler.haptics.ApplyHaptics(vibFrequency, clutchState, 20, false);
                                haptGloveHandler.BTSend(btData);

                                ////Ensure haptics removed completely.
                                //btData = haptGloveHandler.haptics.ApplyHaptics(clutchState, 30, false);
                                //haptGloveHandler.BTSend(btData);
                            }
                            else
                            {
                                byte[] btData = haptGloveHandler.haptics.ApplyHaptics(clutchState, targetPres, true);
                                haptGloveHandler.BTSend(btData);
                            }
                            
                        }
                    }

                    if (colNameList.Contains(bufName))
                    {
                        colNameList.Remove(bufName);
                    }
                    
                    hapticsState[fingerID] = false;
                    hapticStartPosition[fingerID] = 0;
                    hapticGraspingIsStart[fingerID] = false;
                    collider[fingerID] = null;
                    touchedFingers[fingerID] = false;

                    if (deformer[fingerID] != null)
                    {
                        deformer[fingerID] = null;
                        Destroy(touchPointGameObject[fingerID]);
                        Destroy(forcePointGameObject[fingerID]);
                    }
                }

                //Debug.Log(bufName + "Exit");
            }
            else
            {
                return;
            }

        }

        public void DropObject()
        {
            Debug.Log("Drop object");
            colNameList.Clear();
            for (int i = 0; i < collider.Length; i++)
            {
                collider[i] = null;
                touchedFingers[i] = false;
            }
        }
    }
}
