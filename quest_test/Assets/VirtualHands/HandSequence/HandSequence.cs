using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;

/// <summary>

/// </summary>
[Serializable]
public class HandSequence : ScriptableObject
{
     
     public interface SkeletonHandSequenceProvider
     {
          OVRSkeleton.SkeletonType GetSkeletonType();
          HandFrame GetHandFrameData();
          bool IsInitialized();
     }

     private OVRSkeleton.SkeletonType _skeletonType;
     public OVRSkeleton.SkeletonType SkeletonType { get; private set; }

     [SerializeField] public List<HandFrame> frames = new List<HandFrame>();
     public int Length { get { return frames.Count; } }


     public void applyTransformation(Matrix4x4 transformationMatrix){
          foreach(var frame in frames)
          {
               if(!frame.IsDataValid) continue;
               for (int i = 0; i < frame.BoneTranslations.Length; i++)
               {
                    frame.BoneTranslations[i] = transformationMatrix.MultiplyPoint(frame.BoneTranslations[i]);
               }
          }
          setRotationsFromTranslations();
     }

     public void setRotationsFromTranslations(){
          foreach(var frame in frames)
          {
               if (!frame.IsDataValid) continue;
               frame.BoneRotations = new Quaternion[frame.BoneTranslations.Length];
               for (int i = 0; i < frame.BoneTranslations.Length; i++)
               {
                    if(OVRHandData.hasParent.Contains(i)){
                         Vector3 thisPos = frame.BoneTranslations[i];
                         Vector3 parentPos = frame.BoneTranslations[(int)OVRHandData.jointsCustom[i].Parent];
                         frame.BoneRotations[i] = getBoneRotation(thisPos, parentPos);
                    }
                    else if(OVRHandData.hasParentWrist.Contains(i)){
                         Vector3 thisPos = frame.BoneTranslations[i];
                         Vector3 parentPos = frame.BoneTranslations[1];//1 -> index for wrist
                         frame.BoneRotations[i] = getBoneRotation(thisPos, parentPos);
                    }
               }
          }
     }

     [Serializable]
     public class HandFrame
     {
          
          public float time;

          public posef RootPose;
          
          public float RootScale;
          
          public Quaternion[] BoneRotations;
          
          public bool IsDataValid;
          
          public bool IsDataHighConfidence;
          
          public Vector3[] BoneTranslations;
          
          public int SkeletonChangedCount;

          public List<SerializableNoteEvent> MidiData;

          public bool HasMidi;

          //public Vector3 getTransformedTranslation(int i){
          //     return OVRExtensions.FromFlippedZVector3f(new OVRPlugin.Vector3f{x = BoneTranslations[i].x, y = BoneTranslations[i].y, z = BoneTranslations[i].z});
          //}
          
          public void SetRotationFromTranslation(){
               
               this.BoneRotations = new Quaternion[this.BoneTranslations.Length];
               for (int i = 0; i < this.BoneTranslations.Length; i++)
               {
                    if(OVRHandData.hasParent.Contains(i)){
                         Vector3 thisPos = this.BoneTranslations[i];
                         Vector3 parentPos = this.BoneTranslations[(int)OVRHandData.jointsCustom[i].Parent];
                         this.BoneRotations[i] = getBoneRotation(thisPos, parentPos);
                    }
                    else if(OVRHandData.hasParentWrist.Contains(i)){
                         Vector3 thisPos = this.BoneTranslations[i];
                         Vector3 parentPos = this.BoneTranslations[1];//1 -> index for wrist
                         this.BoneRotations[i] = getBoneRotation(thisPos, parentPos);
                    }
               }
               
          }
          public static explicit operator OVRSkeleton.SkeletonPoseData(HandFrame obj)
          {
               OVRSkeleton.SkeletonPoseData output = new OVRSkeleton.SkeletonPoseData();

               output.RootPose = (OVRPlugin.Posef)obj.RootPose;
               output.RootScale = obj.RootScale;

               output.BoneRotations = new OVRPlugin.Quatf[obj.BoneRotations.Length];
               for (int i = 0; i < obj.BoneRotations.Length; i++)
               {
                    output.BoneRotations[i] = new OVRPlugin.Quatf
                    {
                         x = obj.BoneRotations[i].x, y = obj.BoneRotations[i].y, z = obj.BoneRotations[i].z,
                         w = obj.BoneRotations[i].w
                    };
               }

               output.IsDataValid = obj.IsDataValid;
               output.IsDataHighConfidence = obj.IsDataHighConfidence;

               output.BoneTranslations = new OVRPlugin.Vector3f[obj.BoneTranslations.Length];
               for (int i = 0; i < obj.BoneTranslations.Length; i++)
               {
                    output.BoneTranslations[i] = new OVRPlugin.Vector3f()
                    {
                         x = obj.BoneTranslations[i].x, y = obj.BoneTranslations[i].y, z = obj.BoneTranslations[i].z
                    };
               }

               output.SkeletonChangedCount = obj.SkeletonChangedCount;
               
               return output;
          }

          public static explicit operator HandFrame(OVRSkeleton.SkeletonPoseData obj)
          {
               HandFrame output = new HandFrame();

               if(!obj.IsDataValid || !obj.IsDataHighConfidence){
                    output.IsDataValid = false;
                    return output;
               }

               output.RootPose = (posef)obj.RootPose;
               output.RootScale = obj.RootScale;
               
               /*
               output.BoneRotations = new Quaternion[obj.BoneRotations.Length];
               for (int i = 0; i < obj.BoneRotations.Length; i++)
               {
                    output.BoneRotations[i] = new Quaternion()
                    {
                         x = obj.BoneRotations[i].x, y = obj.BoneRotations[i].y, z = obj.BoneRotations[i].z,
                         w = obj.BoneRotations[i].w
                    };
               }
               */

               
               output.IsDataValid = obj.IsDataValid;
               output.IsDataHighConfidence = obj.IsDataHighConfidence;
               
               output.BoneTranslations = new Vector3[obj.BoneTranslations.Length];

               for (int i = 0; i < obj.BoneTranslations.Length; i++)
               {
                    output.BoneTranslations[i] = OVRExtensions.FromFlippedZVector3f(obj.BoneTranslations[i]);
                    /*output.BoneTranslations[i] = new Vector3()
                    {
                         x = obj.BoneTranslations[i].x, y = obj.BoneTranslations[i].y, z = obj.BoneTranslations[i].z
                    };*/
               }

               // creating bone rotations, from the position of the joints, no need for flippedZ here because it's using the already transformed joints
               output.BoneRotations = new Quaternion[output.BoneTranslations.Length];
               for (int i = 0; i < output.BoneTranslations.Length; i++)
               {
                    if(OVRHandData.hasParent.Contains(i)){
                         Vector3 thisPos = output.BoneTranslations[i];
                         Vector3 parentPos = output.BoneTranslations[(int)OVRHandData.jointsCustom[i].Parent];
                         output.BoneRotations[i] = getBoneRotation(thisPos, parentPos);
                    }
                    else if(OVRHandData.hasParentWrist.Contains(i)){
                         Vector3 thisPos = output.BoneTranslations[i];
                         Vector3 parentPos = output.BoneTranslations[1];//1 -> index for wrist
                         output.BoneRotations[i] = getBoneRotation(thisPos, parentPos);
                    }
               }

               output.SkeletonChangedCount = obj.SkeletonChangedCount;
               
               return output;
          }

          public override string ToString()
          {
               string logLine = "";
               if(IsDataValid) logLine += RootPose.ToString("F10"); // 7
               else
               {
                    posef empty = new posef();
                    logLine += empty.ToString("F10");
               }

               if (IsDataValid) logLine += "," + RootScale.ToString("F6");
               else logLine += 0.0;

               
               if (IsDataValid)
               {
                    for(int i = 0; i < BoneRotations.Length; i++){ // 4*26
                         logLine += "," + BoneRotations[i].ToString("F10");
                    }  
               }
               else
               {
                    for (int i = 0; i < 4 * 26; i++) // Fill with 4 floats per bone
                    {
                         logLine += ",0.0";
                    }
               }

               

               logLine += "," + (IsDataValid ? "1" : "0");  // 1
               logLine += "," + (IsDataHighConfidence ? "1" : "0"); // 1

               if (IsDataValid) {
                    for (int i = 0; i < BoneTranslations.Length; i++) // 3*26
                    {
                         logLine += "," + BoneTranslations[i].ToString("F6");
                    }
               }
               else {
                    for (int i = 0; i < 3 * 26; i++) // Fill with 3 floats per bone
                    {
                         logLine += ",0.0";
                    }
               }

               if (IsDataValid)
               {
                    logLine += "," + SkeletonChangedCount.ToString();
               } // 1
               else { logLine += "," + 0;}


               logLine += "," + time.ToString(); // 1

               // MIDI part of data
               // The form is has midi - note one events - note off events,
               // before each section the number of events is specified, one event is two ints.
               string midiString = HasMidi ? ",1" : ",0";
               if(HasMidi){
                    if(MidiData.Count == 0){
                         midiString += ",0,0";
                    }
                    List<NoteEvent> NoteOnEvents = new List<NoteEvent>();
                    List<NoteEvent> NoteOffEvents = new List<NoteEvent>();

                    foreach(var mEvent in MidiData){
                         if(mEvent.EventType == MidiEventType.NoteOn){
                              NoteOnEvents.Add(mEvent.ToNoteEvent());
                         }
                         if(mEvent.EventType == MidiEventType.NoteOff){
                              NoteOffEvents.Add(mEvent.ToNoteEvent());
                         }
                    }
                    
                    midiString += "," + NoteOnEvents.Count.ToString();
                    foreach(var noteOnEvent in NoteOnEvents){
                         midiString += "," + noteOnEvent.NoteNumber.ToString() + "," + noteOnEvent.Velocity.ToString();
                    }
                    midiString += "," + NoteOffEvents.Count.ToString();
                    foreach(var noteOffEvent in NoteOffEvents){
                         midiString += "," + noteOffEvent.NoteNumber.ToString() + "," + noteOffEvent.Velocity.ToString();
                    }
               }
               logLine += midiString;
               logLine = logLine.Replace("(", "").Replace(")", "");
               return logLine.Replace(" ", "");

               
          }
          public HandFrame DeepCopy()
          {
               HandFrame copiedFrame = new HandFrame
               {
                    time = this.time,
                    RootPose = this.RootPose,
                    RootScale = this.RootScale,
                    IsDataValid = this.IsDataValid,
                    IsDataHighConfidence = this.IsDataHighConfidence,
                    SkeletonChangedCount = this.SkeletonChangedCount,
                    HasMidi = this.HasMidi
               };

               copiedFrame.BoneRotations = new Quaternion[this.BoneRotations.Length];
               this.BoneRotations.CopyTo(copiedFrame.BoneRotations, 0);

               copiedFrame.BoneTranslations = new Vector3[this.BoneTranslations.Length];
               this.BoneTranslations.CopyTo(copiedFrame.BoneTranslations, 0);

               copiedFrame.MidiData = new List<SerializableNoteEvent>();
               foreach (var midiEvent in this.MidiData)
               {
                    var copiedMidiEvent = new SerializableNoteEvent(midiEvent);
                    copiedFrame.MidiData.Add(copiedMidiEvent);
               }

               return copiedFrame;
          }
     }

     [Serializable]
     public class SerializableNoteEvent
     {
          public int note;
          public int velocity;
          public MidiEventType EventType;
          public SerializableNoteEvent(SerializableNoteEvent copy)
          {
               note = copy.note;
               velocity = copy.velocity;
               EventType = copy.EventType;
          }

          public SerializableNoteEvent(NoteEvent original)
          {
               note = original.NoteNumber;
               velocity = original.Velocity;
               EventType = original.EventType;
          }

          public NoteEvent ToNoteEvent()
          {
               if(EventType == MidiEventType.NoteOn) return new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)velocity);
               else return new NoteOffEvent((SevenBitNumber)note, (SevenBitNumber)velocity);
               
          }
     }

     public static Quaternion getBoneRotation(Vector3 a, Vector3 b){
        Vector3 directionVector = b-a;
        return Quaternion.LookRotation(directionVector, Vector3.right);
     }
     public static Vector3 ToVector3(OVRPlugin.Vector3f ovrvec3){
          return new Vector3()
          {
               x = ovrvec3.x, y = ovrvec3.y, z = ovrvec3.z
          };
     }

     public bool hasData()
     {
          return frames.Count != 0;
     }

     public int getNumBones()
     {
          return frames[0].BoneTranslations.Length;
     }

     public string test_print(){
          return frames.Count.ToString();
          //return frames[0].IsDataValid.ToString();
     }
     // REMOVE WHEN POSSIBLE
     // [Serializable]
     // public struct quatf{
     //      public float x;
     //      public float y; 
     //      public float z;
     //      public float w;
     //      public static explicit operator OVRPlugin.Quatf(quatf obj)
     //      {
     //           return new OVRPlugin.Quatf{x = obj.x, y = obj.y, z = obj.z, w = obj.w};
     //      }
     // }
     // [Serializable]
     // public struct vec3f{
     //      public float x;
     //      public float y; 
     //      public float z;
     //      public static explicit operator OVRPlugin.Vector3f(vec3f obj)
     //      {
     //           return new OVRPlugin.Vector3f{x = obj.x, y = obj.y, z = obj.z};
     //      }
     //      public static explicit operator Vector3(vec3f obj)
     //      {
     //           return new Vector3(obj.x, obj.y, obj.z);
     //      }
     // }
     
     [Serializable]
     public struct posef
	{
		public Quaternion Orientation;
		public Vector3 Position;

          public string ToString(string decimals)
          {
               return Position.ToString(decimals) + ","+ Orientation.ToString(decimals);
          }

          public static explicit operator OVRPlugin.Posef(posef obj)
          {
               return new OVRPlugin.Posef
               {
                    Orientation = new OVRPlugin.Quatf
                    {
                         x = obj.Orientation.x,
                         y = obj.Orientation.y,
                         z = obj.Orientation.z,
                         w = obj.Orientation.w
                         
                    }, 
                    Position = new OVRPlugin.Vector3f
                    {
                         x = obj.Position.x,
                         y = obj.Position.y,
                         z = obj.Position.z
                    }
               };
          }
          public static explicit operator posef(OVRPlugin.Posef obj)
          {
               return new posef
               {
                    Orientation = new Quaternion
                    {
                         x = obj.Orientation.x,
                         y = obj.Orientation.y,
                         z = obj.Orientation.z,
                         w = obj.Orientation.w
                    },
                    Position = new Vector3
                    {
                         x = obj.Position.x,
                         y = obj.Position.y,
                         z = obj.Position.z
                    }
               };
          }
	}

     public HandSequence DeepCopy()
     {
          HandSequence copiedSequence = ScriptableObject.CreateInstance<HandSequence>();

          copiedSequence._skeletonType = this._skeletonType;
          copiedSequence.SkeletonType = this.SkeletonType;

          copiedSequence.frames = new List<HandFrame>();
          foreach (var frame in this.frames)
          {
               copiedSequence.frames.Add(frame.DeepCopy());
          }

          return copiedSequence;
     }
     
}
