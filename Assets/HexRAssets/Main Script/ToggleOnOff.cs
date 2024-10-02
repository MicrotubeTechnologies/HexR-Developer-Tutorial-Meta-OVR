using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleOnOff : MonoBehaviour
{
    public GameObject ObjecttoToggle;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ToggleOnOrOff()
    {
        if(ObjecttoToggle.activeInHierarchy)
        {
            ObjecttoToggle.SetActive(false);
        }
        else
        {
            ObjecttoToggle.SetActive(true);
        }
    }
}
