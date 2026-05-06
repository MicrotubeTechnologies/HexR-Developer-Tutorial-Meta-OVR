using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationOnAxis : MonoBehaviour
{
    [Header("Rotation Axes")]
    public bool rotateX = false;
    public bool rotateY = true;
    public bool rotateZ = false;

    [Header("Rotation Speed")]
    public float speedX = 90f;
    public float speedY = 90f;
    public float speedZ = 90f;

    [Header("Settings")]
    public Space rotationSpace = Space.Self;

    void Update()
    {
        float x = rotateX ? speedX * Time.deltaTime : 0f;
        float y = rotateY ? speedY * Time.deltaTime : 0f;
        float z = rotateZ ? speedZ * Time.deltaTime : 0f;

        transform.Rotate(x, y, z, rotationSpace);
    }
}
