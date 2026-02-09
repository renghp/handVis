using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followHead : MonoBehaviour
{
    // Start is called before the first frame update

    public Transform headToFollow;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        transform.position = headToFollow.transform.position;
        transform.eulerAngles = headToFollow.transform.eulerAngles;
        
    }
}
