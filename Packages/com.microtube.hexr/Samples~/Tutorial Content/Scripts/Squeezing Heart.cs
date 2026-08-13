using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexR;

public class SqueezingHeart : MonoBehaviour
{
    public SpecialHaptics TargetSpecialHaptics;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void whensqueeze()
    {
        TargetSpecialHaptics.InTimer = 0.2f;
        TargetSpecialHaptics.OutTimer = 0.2f;
        TargetSpecialHaptics.HapticStrenngthValue = 0.5f;
    }
    public void whenrelease()
    {
        TargetSpecialHaptics.InTimer = 0.5f;
        TargetSpecialHaptics.OutTimer = 0.5f;
        TargetSpecialHaptics.HapticStrenngthValue = 0.2f;
    }
}
