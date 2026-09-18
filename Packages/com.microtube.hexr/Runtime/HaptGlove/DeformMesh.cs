using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HaptGlove
{
    public class DeformMesh : MonoBehaviour
    {
        private Mesh deformingMesh;
        private Vector3[] originalVertices, displacedVertices;
        private Vector3[] vertexVelocities;
        public float springForce = 100f;
        public float damping = 30f;
        private float uniformScale = 1f;
        //private float[] iniDistance = new float[5];
        private Vector3[] distance = new Vector3[5];

        void Start()
        {
            deformingMesh = GetComponent<MeshFilter>().mesh;
            originalVertices = deformingMesh.vertices;
            displacedVertices = new Vector3[originalVertices.Length];
            for (int i = 0; i < originalVertices.Length; i++)
            {
                displacedVertices[i] = originalVertices[i];
            }
            vertexVelocities = new Vector3[originalVertices.Length];
        }

        void FixedUpdate()
        {
            uniformScale = transform.localScale.x;
            for (int i = 0; i < displacedVertices.Length; i++)
            {
                UpdataVertex(i);
            }

            deformingMesh.vertices = displacedVertices;
            //deformingMesh.RecalculateNormals();
        }

        //public void SetIniDistance(byte fingerID, float dist)
        //{
        //    iniDistance[fingerID] = dist;
        //}

        void UpdataVertex(int i)
        {
            Vector3 velocity = vertexVelocities[i];
            Vector3 displacement = displacedVertices[i] - originalVertices[i];
            velocity -= displacement * springForce * Time.deltaTime;
            velocity *= 1f - damping * Time.deltaTime;
            vertexVelocities[i] = velocity;
            displacedVertices[i] += velocity * (Time.deltaTime / uniformScale);
        }

        public void AddDeformingForce(Vector3 point, Vector3 dist, byte fingerID)
        {
            distance[fingerID] = transform.InverseTransformDirection(dist);
            //distance[fingerID] = dist;
            point = transform.InverseTransformPoint(point);
            //float force = dist.magnitude * 60;
            float force = dist.magnitude * 60;
            //Debug.Log("Force: " + force);
            for (int i = 0; i < displacedVertices.Length; i++)
            {
                AddForceToVertex(i, point, force, fingerID, distance[fingerID]);
            }
        }


        void AddForceToVertex(int i, Vector3 point, float force, byte fingerID, Vector3 dir)
        {
            Vector3 pointToVertex = displacedVertices[i] - (point - dir.normalized * 0.5f);

            pointToVertex *= uniformScale;
            //float attenuatedForce = force / (1f + 3000f * pointToVertex.sqrMagnitude);
            float attenuatedForce = force / (1f + 10f * pointToVertex.sqrMagnitude);
            float velocity = attenuatedForce * Time.deltaTime;
            vertexVelocities[i] += pointToVertex.normalized * velocity;
            //vertexVelocities[i] += dir.normalized * velocity;
        }

    }
}
