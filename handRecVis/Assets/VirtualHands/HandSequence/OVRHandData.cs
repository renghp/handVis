using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
public static class OVRHandData
{
    public enum ovrHandEnum {
        Palm, 
        Wrist, 
        ThumbMetacarpal, 
        ThumbProximal, 
        ThumbDistal, 
        ThumbTip,
        IndexMetacarpal, 
        IndexProximal, 
        IndexIntermediate, 
        IndexDistal, 
        IndexTip,
        MiddleMetacarpal, 
        MiddleProximal, 
        MiddleIntermediate, 
        MiddleDistal, 
        MiddleTip,
        RingMetacarpal, 
        RingProximal, 
        RingIntermediate, 
        RingDistal, 
        RingTip,
        LittleMetacarpal, 
        LittleProximal, 
        LittleIntermediate, 
        LittleDistal, 
        LittleTip,
        Invalid
    }

    public struct JointData
    {
        public ovrHandEnum Parent;
        public ovrHandEnum ID;
        public Color Color;

        public JointData(ovrHandEnum id, ovrHandEnum parent, Color color)
        {
            Parent = parent;
            ID = id;
            Color = color;
        }
    }
    public static Color GetColorFromFinger(int number)
    {
        return number switch
        {
            0 => Color.yellow,
            1 => Color.blue,
            2 => Color.green,
            3 => Color.red,
            4 => Color.magenta,
            _ => Color.white
        };
    }

    public static HashSet<int> hasParent = new HashSet<int> { 3, 4, 5, 7,8,9,10,12,13,14,15,17,18,19,20,22,23,24,25 };
    public static HashSet<int> hasParentWrist = new HashSet<int> {2,6,11,16,21};

    // Use this to go from OpenXR order to OVR order
    public static int GetJointIndex(ovrHandEnum jointID){
        //int i = jointID.ToIndex() - 1;
        int i = (int)jointID;
        return i;
    }
    // inverse of the above
    public static ovrHandEnum GetJointIDfromIndex(int index){
        return (ovrHandEnum)index;
    }

    // This should be the correct order in which the data comes in from OVR
    /*public static List<JointData> jointsOpenXr = new List<JointData>
    {
        new JointData(XRHandJointID.Palm, XRHandJointID.Invalid),
        new JointData(XRHandJointID.Wrist, XRHandJointID.Invalid),
        new JointData(XRHandJointID.ThumbMetacarpal, XRHandJointID.Wrist),
        new JointData(XRHandJointID.ThumbProximal, XRHandJointID.ThumbMetacarpal),
        new JointData(XRHandJointID.ThumbDistal, XRHandJointID.ThumbProximal),
        new JointData(XRHandJointID.ThumbTip, XRHandJointID.ThumbDistal),
        new JointData(XRHandJointID.IndexMetacarpal, XRHandJointID.Wrist),
        new JointData(XRHandJointID.IndexProximal, XRHandJointID.IndexMetacarpal),
        new JointData(XRHandJointID.IndexIntermediate, XRHandJointID.IndexProximal),
        new JointData(XRHandJointID.IndexDistal, XRHandJointID.IndexIntermediate),
        new JointData(XRHandJointID.IndexTip, XRHandJointID.IndexDistal),
        new JointData(XRHandJointID.MiddleMetacarpal, XRHandJointID.Wrist),
        new JointData(XRHandJointID.MiddleProximal, XRHandJointID.MiddleMetacarpal),
        new JointData(XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleProximal),
        new JointData(XRHandJointID.MiddleDistal, XRHandJointID.MiddleIntermediate),
        new JointData(XRHandJointID.MiddleTip, XRHandJointID.MiddleDistal),
        new JointData(XRHandJointID.RingMetacarpal, XRHandJointID.Wrist),
        new JointData(XRHandJointID.RingProximal, XRHandJointID.RingMetacarpal),
        new JointData(XRHandJointID.RingIntermediate, XRHandJointID.RingProximal),
        new JointData(XRHandJointID.RingDistal, XRHandJointID.RingIntermediate),
        new JointData(XRHandJointID.RingTip, XRHandJointID.RingDistal),
        new JointData(XRHandJointID.LittleMetacarpal, XRHandJointID.Wrist),
        new JointData(XRHandJointID.LittleProximal, XRHandJointID.LittleMetacarpal),
        new JointData(XRHandJointID.LittleIntermediate, XRHandJointID.LittleProximal),
        new JointData(XRHandJointID.LittleDistal, XRHandJointID.LittleIntermediate),
        new JointData(XRHandJointID.LittleTip, XRHandJointID.LittleDistal)
    };*/

    
    public static List<JointData> jointsCustom = new List<JointData>
    {
        new JointData(ovrHandEnum.Palm, ovrHandEnum.Invalid, Color.white),
        new JointData(ovrHandEnum.Wrist, ovrHandEnum.Invalid, Color.white),
        
        new JointData(ovrHandEnum.ThumbMetacarpal, ovrHandEnum.Wrist, Color.yellow),
        new JointData(ovrHandEnum.ThumbProximal, ovrHandEnum.ThumbMetacarpal, Color.yellow),
        new JointData(ovrHandEnum.ThumbDistal, ovrHandEnum.ThumbProximal, Color.yellow),
        new JointData(ovrHandEnum.ThumbTip, ovrHandEnum.ThumbDistal, Color.yellow),

        new JointData(ovrHandEnum.IndexMetacarpal, ovrHandEnum.Wrist, Color.blue),
        new JointData(ovrHandEnum.IndexProximal, ovrHandEnum.IndexMetacarpal, Color.blue),
        new JointData(ovrHandEnum.IndexIntermediate, ovrHandEnum.IndexProximal, Color.blue),
        new JointData(ovrHandEnum.IndexDistal, ovrHandEnum.IndexIntermediate, Color.blue),
        new JointData(ovrHandEnum.IndexTip, ovrHandEnum.IndexDistal, Color.blue),

        new JointData(ovrHandEnum.MiddleMetacarpal, ovrHandEnum.Wrist, Color.green),
        new JointData(ovrHandEnum.MiddleProximal, ovrHandEnum.MiddleMetacarpal, Color.green),
        new JointData(ovrHandEnum.MiddleIntermediate, ovrHandEnum.MiddleProximal, Color.green),
        new JointData(ovrHandEnum.MiddleDistal, ovrHandEnum.MiddleIntermediate, Color.green),
        new JointData(ovrHandEnum.MiddleTip, ovrHandEnum.MiddleDistal, Color.green),

        new JointData(ovrHandEnum.RingMetacarpal, ovrHandEnum.Wrist, Color.red),
        new JointData(ovrHandEnum.RingProximal, ovrHandEnum.RingMetacarpal, Color.red),
        new JointData(ovrHandEnum.RingIntermediate, ovrHandEnum.RingProximal, Color.red),
        new JointData(ovrHandEnum.RingDistal, ovrHandEnum.RingIntermediate, Color.red),
        new JointData(ovrHandEnum.RingTip, ovrHandEnum.RingDistal, Color.red),

        new JointData(ovrHandEnum.LittleMetacarpal, ovrHandEnum.Wrist, Color.magenta),
        new JointData(ovrHandEnum.LittleProximal, ovrHandEnum.LittleMetacarpal, Color.magenta),
        new JointData(ovrHandEnum.LittleIntermediate, ovrHandEnum.LittleProximal, Color.magenta),
        new JointData(ovrHandEnum.LittleDistal, ovrHandEnum.LittleIntermediate, Color.magenta),
        new JointData(ovrHandEnum.LittleTip, ovrHandEnum.LittleDistal, Color.magenta)
    };


    
}
