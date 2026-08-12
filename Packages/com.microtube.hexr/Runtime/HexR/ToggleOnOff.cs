using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleOnOff : MonoBehaviour
{
    [Tooltip("Select the GameObject to toggle on and off state")]
    public GameObject ObjecttToToggle;
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
        if(ObjecttToToggle.activeInHierarchy)
        {
            ObjecttToToggle.SetActive(false);
        }
        else
        {
            ObjecttToToggle.SetActive(true);
        }
    }
}
