using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class followOriginalLeft : MonoBehaviour
{
    // Start is called before the first frame update

    GameObject[] originalLeft;

    void Start()
    {
        gameObject.GetComponent<Renderer>().enabled = false;
        
    }

    // Update is called once per frame
    void Update()
    {
    

        try 
        {
            //Debug.Log("achou");
            GameObject[] originalLeft = GameObject.FindGameObjectsWithTag ("originalLeft");
            //original = GameObject.FindWithTag("original");

            foreach (GameObject or in originalLeft)
            {
                if (or.name==gameObject.name)
                {
                    
                    Debug.Log(or.name);
                    transform.localPosition = or.transform.position;
                    transform.localEulerAngles = or.transform.eulerAngles;

                    or.GetComponent<Renderer>().enabled = false;
                    gameObject.GetComponent<Renderer>().enabled = true;

                    break;            
                }
            }
            
        }
        catch(Exception e){
            Debug.Log("not found");
            gameObject.GetComponent<Renderer>().enabled = false;
        }

        
    }
}
