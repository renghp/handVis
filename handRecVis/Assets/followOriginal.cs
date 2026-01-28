using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class followOriginal : MonoBehaviour
{
    // Start is called before the first frame update

    GameObject[] originalRight;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    

        try 
        {
            //Debug.Log("achou");
            GameObject[] originalRight = GameObject.FindGameObjectsWithTag ("originalRight");
            //original = GameObject.FindWithTag("original");

            foreach (GameObject or in originalRight)
            {
                if (or.name==gameObject.name)
                {
                    
                    Debug.Log(or.name);
                    transform.localPosition = or.transform.position;
                    transform.localEulerAngles = or.transform.eulerAngles;
                    break;            
                }
            }
            
        }
        catch(Exception e){
            Debug.Log("not found");
        }

        
    }
}
