using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

// Utils to get hand-keyboard related data
public class HandUtil : MonoBehaviour
{

    [SerializeField]
    private OVRSkeleton.SkeletonType _skeletonType;

    private ConfigurePhysicalKeyboard _configScript;
    private ConfigurePhysicalKeyboard.Config _config;
    
    private HandSequence.SkeletonHandSequenceProvider _dataProvider;

    private Matrix4x4 _m;

    public float blackKeyOffset = 2.5f;
    public float blackKeyHeight = 0.01f;

    private static List<int> whiteKeys = new List<int> { 0, 2, 4, 5, 7, 9, 11 };
    private static List<int> blackKeys = new List<int> { 1, 3, 6, 8, 10 };

    [SerializeField]
    private bool _debugMode;
    


    void Start()
    {
        
    }
    void Awake()
    {
        if (_debugMode)
        {
            RunDebug();
            return;
        }

        _dataProvider = SearchSkeletonDataProvider();
        if(_dataProvider == null){ Debug.LogError("No data provider for util");
            return;
        }

        _configScript = SearchConfig();
        if(_configScript == null){ Debug.LogError("No config found for util");
            return;
        }

        _configScript.OnActiveConfigChanged += configUpdate;
    }

    void RunDebug()
    {
        Vector3[] fingertipPositions = new Vector3[5]
        {
            new Vector3(0.02f, 0.1f, 0.05f),
            new Vector3(-0.03f, 0.12f, 0.02f),
            new Vector3(0.01f, 0.15f, -0.04f),
            new Vector3(-0.02f, 0.13f, 0.06f),
            new Vector3(0.04f, 0.11f, -0.03f)
        };

        Vector3 targetPos = new Vector3(0.04f, 0.11f, -0.03f);
        Debug.Log(ClosestFinger3D(targetPos, fingertipPositions));
    }

    private void configUpdate(ConfigurePhysicalKeyboard.Config _){
        Debug.Log("Config update inside Util");
        _config = _configScript.activeConfig;
        _m = _configScript.getInverseSpaceMatrix();
    }

    //public int GetFingerFromKey(int key){
    //    return 50;
    //}

    public int GetFingerFromKey(int key){
        Vector3[] BoneTranslations = _dataProvider.GetHandFrameData().BoneTranslations;
        Debug.Log(BoneTranslations[(int)OVRHandData.ovrHandEnum.ThumbTip]);

        Vector3[] fingertipPositions = new Vector3[5]
        {
            BoneTranslations[(int)OVRHandData.ovrHandEnum.ThumbTip],
            BoneTranslations[(int)OVRHandData.ovrHandEnum.IndexTip],
            BoneTranslations[(int)OVRHandData.ovrHandEnum.MiddleTip],
            BoneTranslations[(int)OVRHandData.ovrHandEnum.RingTip],
            BoneTranslations[(int)OVRHandData.ovrHandEnum.LittleTip]
        };

        int anchorKey = _config.anchorKey;
        Vector3[] transformedFingerPositions = new Vector3[5];

        for (int i = 0; i < fingertipPositions.Length; i++) 
        {
            transformedFingerPositions[i] = _m.MultiplyPoint(fingertipPositions[i]);
        }

        Vector3 keyPos = getMidPositionFromKey(key, forwardMode:1);
        
        keyPos = _m.MultiplyPoint(keyPos);
        
        Debug.Log("keypos: " + keyPos);
        
        return ClosestFinger1D(keyPos, transformedFingerPositions);
    }

    // when we transform into keyboard space, the keyboard keys goes along the X axis, the forward vector will go into the Z axis, and up is Y.
    // When doing 1D check we don't care about Z and Y so we can ignore them. And only check which finger pos is closest to X.
    public static int ClosestFinger1D(Vector3 target, Vector3[] tipPositions)
    {
        return Array.IndexOf(tipPositions, tipPositions.OrderBy(n => Math.Abs(n.x - target.x)).First());
    }
    
    //using x and y, change to x and z ignore up dimension instead
    public static int ClosestFinger2D(Vector3 target, Vector3[] tipPositions)
    {
        Vector2 target2D = new Vector2(target.x, target.y);
        return Array.IndexOf(tipPositions, tipPositions.OrderBy(n => EuclideanDistance2D(new Vector2(n.x, n.y), target2D)).First());
    }
    
    public static int ClosestFinger3D(Vector3 target, Vector3[] tipPositions)
    {
        return Array.IndexOf(tipPositions, tipPositions.OrderBy(n => EuclideanDistance3D(n, target)).First());
    }

    private static double EuclideanDistance2D(Vector2 point1, Vector2 point2)
    {
        return Math.Sqrt(Math.Pow(point1.x - point2.x, 2) + Math.Pow(point1.y - point2.y, 2));
    }
    private static double ManhattanDistance2D(Vector2 point1, Vector2 point2)
    {
        return Math.Abs(point1.x - point2.x) + Math.Abs(point1.y - point2.y);
    }
    
    private static double EuclideanDistance3D(Vector3 point1, Vector3 point2)
    {
        return Math.Sqrt(Math.Pow(point1.x - point2.x, 2) + 
                         Math.Pow(point1.y - point2.y, 2) + 
                         Math.Pow(point1.z - point2.z, 2));
    }
    private static double ManhattanDistance3D(Vector3 point1, Vector3 point2)
    {
        return Math.Abs(point1.x - point2.x) + 
               Math.Abs(point1.y - point2.y) + 
               Math.Abs(point1.z - point2.z);
    }

    private Vector3 getMidPositionFromKey(int Key, int heightMode = 0, int forwardMode = 0){
        int scaleKey = Key%12;
        //get point on scale, without taking into consideration octave and anchor point.
        Vector3 scalePos = getMidPositionFromScaleKey(scaleKey); 
        int octave = Key / 12;
        int anchorOctave = _config.anchorKey / 12;
        Vector3 octaveVector = Vector3.Normalize(_config.rightCornerPosition - _config.leftCornerPosition) * _config.octaveWidth;
        // If first key is not the same as the start of octave, this will add an offset.
        Vector3 octaveOffsetFromAnchor = (octave - anchorOctave) * octaveVector;

        // This forwardMode changes which point on the key will returned, 0 will be att the lowest point of the key,
        // then 1 will be small distance forward, 2 another step and so on. change pointMult to distance between points.
        float keyDist = _config.oneKeyVector.magnitude;
        float approxWhiteKeyLength = keyDist * 5.0f;
        float pointMult = whiteKeys.Contains(scaleKey) ? 0.2f : 0.15f;
        scalePos += (_config.forwardVector * approxWhiteKeyLength) * forwardMode * pointMult;
        
        // Offsets the point down a little but, it could simulate the key being pushed down
        if (heightMode == 1) scalePos += Vector3.down * approxWhiteKeyLength * 0.5f;

        return scalePos + octaveOffsetFromAnchor + _config.anchor;
    }

    private Vector3 getMidPositionFromScaleKey(int scaleKey){
        if(whiteKeys.Contains(scaleKey)){
            int i = whiteKeys.IndexOf(scaleKey);
            return (_config.deltaVec * (_config.octaveWidth/7.0f)) * i + _config.oneKeyVector/2;
        }else if(blackKeys.Contains(scaleKey)){
            Vector3 oneKeyVector = (_config.deltaVec * (_config.octaveWidth/7.0f));
            Vector3 midPos;
            switch(scaleKey){
                case 1:
                    midPos = _config.oneKeyVector * 1;
                    return midPos + (_config.forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight;
                case 3:
                    midPos = _config.oneKeyVector * 2;
                    return midPos + (_config.forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight;
                case 6:
                    midPos = _config.oneKeyVector * 4;
                    return midPos + (_config.forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight;
                case 8:
                    midPos = _config.oneKeyVector * 5;
                    return midPos + (_config.forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight;
                case 10:
                    midPos = _config.oneKeyVector * 6;
                    return midPos + (_config.forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight;
                default:
                return Vector3.zero;
            }
        }
        return Vector3.zero;
    }
    

    internal ConfigurePhysicalKeyboard SearchConfig(){
        if(_configScript == null) {
            var configGO = GameObject.Find("KeyboardConfiguration");
            var _configScript = configGO ? configGO.GetComponent<ConfigurePhysicalKeyboard>() : null;
            if(_configScript != null){
                return _configScript;
            }else{Debug.LogError("No config found");}
        }
        return null;
    }

    internal HandSequence.SkeletonHandSequenceProvider SearchSkeletonDataProvider()
    {
        var oldProviders = gameObject.GetComponentsInParent<HandSequence.SkeletonHandSequenceProvider>();
        foreach (var dataProvider in oldProviders)
        {
            if (dataProvider.GetSkeletonType() == _skeletonType)
            {
                return dataProvider;
            }
        }

        return null;
    }

}
