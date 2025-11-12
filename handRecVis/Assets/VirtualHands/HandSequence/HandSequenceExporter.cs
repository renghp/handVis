using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class HandSequenceExporter
{
    public static void Export(HandSequence obj, string filename, string location)
    {
        List<string> lines = new List<string>();

        location = Application.persistentDataPath;      //now writing to: \AppData\LocalLow\kth\handRecVis_rec
                                                        //need to read from "Application.persistentDataPath" as well
        
        for (int i = 0; i < obj.frames.Count; i++)
        {
            lines.Add(obj.frames[i].ToString());
        }
        File.WriteAllLines(location+"/"+filename+".hseq", lines);
    }
}