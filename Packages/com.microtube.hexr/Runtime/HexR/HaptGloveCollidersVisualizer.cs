using System.Collections.Generic;
using UnityEngine;

public class HaptGloveCollidersVisualizer : MonoBehaviour
{

    private List<GameObject> visualizers = new List<GameObject>();
    private Material redMaterial;
    private bool ColliderOn = false;
    void Start()
    {
        // Create a solid red material
        redMaterial = new Material(Shader.Find("Standard"));
        redMaterial.color = Color.red;
    }

    void Update()
    {

    }

    public void ColliderToggle()
    {
        if(!ColliderOn)
        {
            CreateVisualizer();
            ColliderOn = true;
        }
        else
        {
            DestroyVisualizer();
            ColliderOn = false;
        }
    }
    public void CreateVisualizer()
    {
        DestroyVisualizer(); // Clear old visualizers

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (!col.enabled) continue; // Skip disabled colliders

            GameObject visualizer = new GameObject("ColliderVisualizer");
            visualizer.transform.SetParent(col.transform, false);
            visualizer.transform.localPosition = GetColliderLocalPosition(col);
            visualizer.transform.localRotation = Quaternion.identity;

            MeshFilter mf = visualizer.AddComponent<MeshFilter>();
            MeshRenderer mr = visualizer.AddComponent<MeshRenderer>();

            mr.material = redMaterial; // Apply solid red material

            mf.mesh = CreateColliderMesh(col);

            // Apply exact scale based on collider type
            visualizer.transform.localScale = GetColliderLocalScale(col);

            visualizers.Add(visualizer);
        }
    }

    public void DestroyVisualizer()
    {
        foreach (var obj in visualizers)
        {
            if (obj != null)
                Destroy(obj);
        }
        visualizers.Clear();
    }

    private Mesh CreateColliderMesh(Collider col)
    {
        if (col is BoxCollider) return CreateBoxMesh();
        if (col is SphereCollider) return CreateSphereMesh();
        if (col is CapsuleCollider) return CreateCapsuleMesh();
        if (col is MeshCollider meshCol && meshCol.sharedMesh) return meshCol.sharedMesh;
        return null;
    }

    private Vector3 GetColliderLocalScale(Collider col)
    {
        if (col is BoxCollider box)
            return box.size; // Use exact box size
        if (col is SphereCollider sphere)
            return Vector3.one * (sphere.radius * 2f); // Match sphere radius
        if (col is CapsuleCollider capsule)
            return new Vector3(capsule.radius * 2f, capsule.height * 0.5f, capsule.radius * 2f); // Match capsule
        return Vector3.one;
    }

    private Vector3 GetColliderLocalPosition(Collider col)
    {
        if (col is BoxCollider box) return box.center;
        if (col is SphereCollider sphere) return sphere.center;
        if (col is CapsuleCollider capsule) return capsule.center;
        return Vector3.zero;
    }

    private Mesh CreateBoxMesh()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return mesh;
    }

    private Mesh CreateSphereMesh()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return mesh;
    }

    private Mesh CreateCapsuleMesh()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return mesh;
    }
}



