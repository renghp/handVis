#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.Collections.Generic;
using UnityEditor;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;



/// <summary>
/// Importer for a OpenXR animated hand sequence,
/// 
/// file structure, one line correponds to one frame, all of the below will be on one line
/// rootpose.orientation.x, rootpose.orientation.y, rootpose.orientation.z, rootpose.orientation.w, 
/// rootpose.Position.x, rootpose.Position.y, rootpose.Position.z, 
/// rootScale(float), 
/// [bonerotations(quatf)*26], 
/// isdatavalid(bool), 
/// isdatahighconfidence(bool),
/// [bonetranslatations(quatf)*26],
/// skeletonchangedcount(int)
/// Time in seconds from start (float)
/// </summary>
[ScriptedImporter(1, new string[] { "hseq" })]
public class HandSequenceImporter : ScriptedImporter {
    public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx) {
        using (var file = new FileStream(ctx.assetPath, FileMode.Open, FileAccess.Read))
        using (var reader = new StreamReader(file)) {
            float fileLength = file.Length;

            HandSequence handSequence = ScriptableObject.CreateInstance<HandSequence>();
            
            string info = $"Reading particle set file {Path.GetFileName(ctx.assetPath)}";
            EditorUtility.DisplayProgressBar("Importing Hand Sequence", info, 0);
            int currentFrame = 0;
            while (true)
            {
                string line = reader.ReadLine(); // Read a new line
                if (line == null) break; // stop at end of file
                

                HandSequence.HandFrame frame = new HandSequence.HandFrame();

                var element = line.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray();
                HandSequence.posef tempRootPose = new HandSequence.posef();

                
                tempRootPose.Orientation.x = element[0];
                tempRootPose.Orientation.y = element[1];
                tempRootPose.Orientation.z = element[2];
                tempRootPose.Orientation.w = element[3];
                tempRootPose.Position.x = element[4];
                tempRootPose.Position.y = element[5];
                tempRootPose.Position.z = element[6];
                frame.RootPose = tempRootPose;

                frame.RootScale = element[7];
                
                int bones_amount = 26;
                int start_index = 8;
                frame.BoneRotations = new Quaternion[bones_amount]; 
                for(int i = 0; i < bones_amount; i++){
                    frame.BoneRotations[i].x = element[start_index+i*4];
                    frame.BoneRotations[i].y = element[start_index+i*4+1];
                    frame.BoneRotations[i].z = element[start_index+i*4+2];
                    frame.BoneRotations[i].w = element[start_index+i*4+3];
                }
                int next_element = start_index + bones_amount*4;

                frame.IsDataValid = element[next_element] == 1.0f;
                frame.IsDataHighConfidence = element[next_element+1] == 1.0f;

                start_index = next_element+2;
                frame.BoneTranslations = new Vector3[bones_amount]; 
                for(int i = 0; i < bones_amount; i++){
                    frame.BoneTranslations[i].x = element[start_index+i*3];
                    frame.BoneTranslations[i].y = element[start_index+i*3+1];
                    frame.BoneTranslations[i].z = element[start_index+i*3+2];
                }
                next_element = start_index + bones_amount*3;

                frame.SkeletonChangedCount = (int)element[next_element];

                frame.time = element[next_element + 1];

                frame.HasMidi = element[next_element + 2] != 0f;
                
                if(frame.HasMidi){
                    frame.MidiData = new List<HandSequence.SerializableNoteEvent>();

                    next_element = next_element + 3;

                    int noteOnCount = (int)element[next_element];
                    next_element = next_element + 1;
                    for(int i = 0; i < noteOnCount; i++){
                        int noteNumber = (int)element[next_element + i * 2];
                        int noteVelocity = (int)element[next_element + i * 2 + 1];
                        var n = new NoteOnEvent((SevenBitNumber)noteNumber, (SevenBitNumber)noteVelocity);
                        HandSequence.SerializableNoteEvent serializable_n = new HandSequence.SerializableNoteEvent(n);
                        frame.MidiData.Add(serializable_n);
                    }

                    int noteOffCount = (int)element[next_element + noteOnCount * 2 ];
                    next_element = next_element + noteOnCount * 2 + 1 ;
                    for(int i = 0; i < noteOffCount; i++){
                        int noteNumber = (int)element[next_element + i * 2];
                        int noteVelocity = (int)element[next_element + i * 2 + 1];
                        var n = new NoteOffEvent((SevenBitNumber)noteNumber, (SevenBitNumber)noteVelocity);
                        HandSequence.SerializableNoteEvent serializable_n = new HandSequence.SerializableNoteEvent(n);
                        frame.MidiData.Add(serializable_n);
                    }
                }

                
                //Debug.Log(currentFrame);

                handSequence.frames.Add(frame);
                //Debug.Log("new frame added");
                //Debug.Log("time: " + frame.time);
                //Debug.Log("example import: " + frame.BoneRotations[0].x);
                
                ++currentFrame;
                if (currentFrame % 20 == 0) {
                    EditorUtility.DisplayProgressBar("Hand sequence Importer", info, file.Position / fileLength);
                }
                
            }
            
            ctx.AddObjectToAsset("Hand.Sequence", handSequence);
            ctx.SetMainObject(handSequence);
            
            EditorUtility.ClearProgressBar();

        }
    }

}
#endif
