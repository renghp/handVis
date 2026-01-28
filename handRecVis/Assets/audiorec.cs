using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System;
using System.Reflection;
//using UnityEngine.AudioModule;

public class audiorec : MonoBehaviour
{
    // Start is called before the first frame update

     AudioSource audioSource;

    void Start()
    {
        foreach (var device in Microphone.devices)
                {
                    Debug.Log("mic Name: " + device);       //Headset (WH-1000XM3)
                }

        audioSource = GetComponent<AudioSource>();

                     //length?
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("recording1");
            audioSource.clip = Microphone.Start("Headset (WH-1000XM3)", true, 3000, 44100);
            Debug.Log("recording2");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
             Debug.Log("stopping1");

             Microphone.End("Headset (WH-1000XM3)");
            Debug.Log("stopping2");
        }

        
        if (Input.GetKeyDown(KeyCode.P))
        {
            
             Debug.Log("playing1");

            audioSource.Play(); 
            Debug.Log("playing2");

        }
        
    }
}
