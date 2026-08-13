using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightbulbLogic : MonoBehaviour
{
    public GameObject lightbulb, Secondbulb, thirdbulb, forthbulb, fifthbulb,Firework;
    public void LightBulbToggle()
    {
        if(lightbulb.activeInHierarchy)
        {
            lightbulb.SetActive(false);
        }
        else
        {
            lightbulb.SetActive(true);
            if(lightbulb.activeInHierarchy && Secondbulb.activeInHierarchy && thirdbulb.activeInHierarchy
                && forthbulb.activeInHierarchy && fifthbulb.activeInHierarchy)
            {
                Firework.SetActive(true);
                StartCoroutine(turnoffFirework());
            }
        }

    }

    public void RedButtonOne()
    {
        if (lightbulb.activeInHierarchy)
        {
            lightbulb.SetActive(false);
        }
        else
        {
            lightbulb.SetActive(true);
        }
        if (thirdbulb.activeInHierarchy)
        {
            thirdbulb.SetActive(false);
        }
        else
        {
            thirdbulb.SetActive(true);
        }
        if (fifthbulb.activeInHierarchy)
        {
            fifthbulb.SetActive(false);
        }
        else
        {
            fifthbulb.SetActive(true);
        }

        if (lightbulb.activeInHierarchy && Secondbulb.activeInHierarchy && thirdbulb.activeInHierarchy
            && forthbulb.activeInHierarchy && fifthbulb.activeInHierarchy)
        {
            Firework.SetActive(true);
            StartCoroutine(turnoffFirework());
        }
    }
    public void RedButtonTwo()
    {
        if (lightbulb.activeInHierarchy)
        {
            lightbulb.SetActive(false);
        }
        else
        {
            lightbulb.SetActive(true);
        }
        if (Secondbulb.activeInHierarchy)
        {
            Secondbulb.SetActive(false);
        }
        else
        {
            Secondbulb.SetActive(true);
        }

        if (fifthbulb.activeInHierarchy)
        {
            fifthbulb.SetActive(false);
        }
        else
        {
            fifthbulb.SetActive(true);
        }

        if (lightbulb.activeInHierarchy && Secondbulb.activeInHierarchy && thirdbulb.activeInHierarchy
            && forthbulb.activeInHierarchy && fifthbulb.activeInHierarchy)
        {
            Firework.SetActive(true);
            StartCoroutine(turnoffFirework());
        }
    }
    public void RedButtonThree()
    {
        if (thirdbulb.activeInHierarchy)
        {
            thirdbulb.SetActive(false);
        }
        else
        {
            thirdbulb.SetActive(true);
        }
        if (forthbulb.activeInHierarchy)
        {
            forthbulb.SetActive(false);
        }
        else
        {
            forthbulb.SetActive(true);
        }
        if (lightbulb.activeInHierarchy && Secondbulb.activeInHierarchy && thirdbulb.activeInHierarchy
            && forthbulb.activeInHierarchy && fifthbulb.activeInHierarchy)
        {
            Firework.SetActive(true);
            StartCoroutine(turnoffFirework());
        }
    }
    IEnumerator turnoffFirework()
    {
        // Wait for the specified delay time
        yield return new WaitForSeconds(5f);
        Firework.SetActive(false);

    }
    
}
