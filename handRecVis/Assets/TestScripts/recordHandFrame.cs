using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class recordHandFrame : MonoBehaviour
{
    OVRSkeleton leftSkeleton;

    void Start()
    {
        leftSkeleton = GetComponent<OVRSkeleton>();
    
    }
    void logBones(OVRSkeleton skel){
        Debug.Log(skel.Bones.Count);

        string[] logLines = new string[27];

        logLines[0] = "id,x,y,z";

        for (int i = 0; i < skel.Bones.Count ; i++)
        {
            logLines[i+1] = (int)skel.Bones[i].Id +", "+ skel.Bones[i].Transform.position.x + 
                ", " + skel.Bones[i].Transform.position.y + 
                ", " + skel.Bones[i].Transform.position.z;
        }
        

        File.WriteAllLines("output.csv", logLines);

    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) {
            
            
            logBones(leftSkeleton);
            
        }
    }
}
