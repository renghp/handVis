
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  PimDeWitte.UnityMainThreadDispatcher;
using System;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;

public class ConfigurePhysicalKeyboard : MonoBehaviour
{
    public enum ConfigMode{
        Inactive,
        Ready,
        ActiveLeft,
        ActiveRight
    }

    // 0 = No marker set, 1 = left marker set, 2 = both marker set
    private int _configStep;

    private ConfigMode _currentConfigMode;
    private HandSequence.SkeletonHandSequenceProvider _dataProvider;

    public GameObject LeftConfigSphere;
    public GameObject RightConfigSphere;

    private GameObject _MIDIDeviceGO;
    private MIDIDevice _device;

    Material yellowMaterial;
    Material redMaterial;

    private GameObject _threadManagerGO;
    private RenderGuideLine _guideLineRender;

    public int startKey = 48; // E4
    public int endKey = 76; // C2
    public int configKey = 28; // leftmost key

    private static List<int> whiteKeys = new List<int> { 0, 2, 4, 5, 7, 9, 11 };
    private static List<int> blackKeys = new List<int> { 1, 3, 6, 8, 10 };

    public event Action<Config> OnActiveConfigChanged;

    public int numWhiteKeys = 17;

    public struct Config{
        public int leftKey;
        public int rightKey;
        public Vector3 leftCornerPosition;
        public Vector3 rightCornerPosition;
        public Vector3 forwardVector;
        public int startKey;
        public Vector3 anchor;
        public Vector3 oneKeyVector;
        public Vector3 deltaVec;
        public int anchorKey;
        public float octaveWidth;
    }

    public Config activeConfig;

    

    // Matrix to transforms from keyboard space
    public Matrix4x4 getSpaceMatrix(){
        if(activeConfig.leftKey == 0 && activeConfig.rightKey == 0){
            Debug.LogWarning("No keyboard configuration has been done, returning identity");
            return Matrix4x4.identity;
        }
        Debug.Log("get regular");
        Debug.Log(activeConfig.anchor);
        return Matrix4x4.TRS(activeConfig.anchor, Quaternion.LookRotation(activeConfig.forwardVector, Vector3.up), Vector3.one);
    }

    // Matrix to transform to keyboard space, so anchor will become zero vector
    public Matrix4x4 getInverseSpaceMatrix(){
        if(activeConfig.leftKey == 0 && activeConfig.rightKey == 0){
            Debug.LogWarning("No keyboard configuration has been done, returning identity");
            return Matrix4x4.identity;
        }
        Debug.Log("get inverse");
        Debug.Log(activeConfig.anchor);
        return Matrix4x4.TRS(activeConfig.anchor, Quaternion.LookRotation(activeConfig.forwardVector, Vector3.up), Vector3.one).inverse;
    }
    
    
    
    void Start()
    {

        _guideLineRender = GetComponent<RenderGuideLine>();

        _configStep = 0;

        yellowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        yellowMaterial.SetColor("_BaseColor", Color.yellow);

        redMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        redMaterial.SetColor("_BaseColor", Color.red);

        _MIDIDeviceGO = GameObject.Find("MIDIDevice");
        if(_MIDIDeviceGO == null) Debug.LogError("Provider needs a game object called MIDIDevice, and it has to contain MIDIDevice script");
        _device = _MIDIDeviceGO.GetComponent<MIDIDevice>();
        _device.OnNoteUpdate += NoteChanged;

        if (_dataProvider == null)
        {
            var foundDataProvider = SearchSkeletonDataProvider();
            if (foundDataProvider != null)
            {
                _dataProvider = foundDataProvider;
                if (_dataProvider is MonoBehaviour mb)
                {
                    Debug.Log($"Recorder Found IOVRSkeletonDataProvider reference in {mb.name} due to unassigned field.");
                }
            }else{
                Debug.LogWarning("didn't find a data provider for recording" ); 
            }
        }
    }

    void NoteChanged(NoteEvent n){
        if(_device.notesDown.Contains(configKey)){
            Debug.Log("config key pressed");
            if(_currentConfigMode == ConfigMode.Inactive){
                _currentConfigMode = ConfigMode.Ready;
            }
        }else{
            //config stopped
            if((_currentConfigMode == ConfigMode.ActiveLeft || _currentConfigMode == ConfigMode.ActiveRight) && _configStep == 2){
                UnityMainThreadDispatcher.Instance().Enqueue(updateConfig());
            }
            _currentConfigMode = ConfigMode.Inactive;
        }

        if(_currentConfigMode == ConfigMode.Ready && _configStep == 0 && _device.notesDown.Contains(startKey)) {_currentConfigMode = ConfigMode.ActiveLeft; UnityMainThreadDispatcher.Instance().Enqueue(toggleConfigMode());}
        if(_currentConfigMode == ConfigMode.Ready && _configStep == 1 && _device.notesDown.Contains(endKey)) {_currentConfigMode = ConfigMode.ActiveRight; UnityMainThreadDispatcher.Instance().Enqueue(toggleConfigMode());}
        if(_currentConfigMode == ConfigMode.Ready && _configStep == 2 && _device.notesDown.Contains(startKey)) {_currentConfigMode = ConfigMode.ActiveLeft;}
        if(_currentConfigMode == ConfigMode.Ready && _configStep == 2 && _device.notesDown.Contains(endKey)) {_currentConfigMode = ConfigMode.ActiveRight;}
    }


    private IEnumerator updateConfig(){
        Vector3 a = LeftConfigSphere.transform.position;
        Vector3 b = RightConfigSphere.transform.position;
        Vector3 realDelta = (b - a);
        Vector3 delta = (b - a) *(1.0f-_guideLineRender.offset);
        float totalLength = delta.magnitude;
        Vector3 _forwardVector = Vector3.Normalize(-Vector3.Cross(Vector3.up, delta)) * delta.magnitude  / _guideLineRender.numKeys;
        //_lr.SetPosition(0, currentPos);
        Vector3 startPos = a + (realDelta * _guideLineRender.offset/2);


        int leftKey_temp = startKey;
        int rightKey_temp = endKey;
        Vector3 leftCornerPosition_temp = startPos;
        Vector3 rightCornerPosition_temp = startPos + delta;
        Vector3 forwardVector_temp = _forwardVector;
        int startKey_oneOctave = startKey % 12;

        float octaveWidth_temp = (((rightCornerPosition_temp - leftCornerPosition_temp) / numWhiteKeys) * 7.0f).magnitude;
        int anchorKey_temp = (startKey / 12) * 12;
        Vector3 deltaVec_temp = Vector3.Normalize(rightCornerPosition_temp - leftCornerPosition_temp);
        Vector3 oneKeyVector_temp = (deltaVec_temp * (octaveWidth_temp / 7.0f));
        Vector3 anchor_temp = leftCornerPosition_temp - oneKeyVector_temp * whiteKeys.IndexOf(startKey_oneOctave);
        Debug.Log("start key: " + startKey);

        activeConfig = new Config() {
            leftKey = leftKey_temp,
            rightKey = rightKey_temp,
            leftCornerPosition = leftCornerPosition_temp,
            rightCornerPosition = rightCornerPosition_temp,
            forwardVector = forwardVector_temp,
            octaveWidth = octaveWidth_temp,
            anchorKey = anchorKey_temp,
            deltaVec = deltaVec_temp,
            oneKeyVector = oneKeyVector_temp,
            anchor = anchor_temp
        };

        Debug.Log("anchor pos:" + anchor_temp);

        OnActiveConfigChanged?.Invoke(activeConfig);

        yield return null;
    }

    private Vector3 GetIndexRight(){
        if(_dataProvider != null){
            HandSequence.HandFrame data = _dataProvider.GetHandFrameData();
            //Vector3 indexPos = data.getTransformedTranslation(10);
            Vector3 indexPos = data.BoneTranslations[10];
            return indexPos;
        }
        return Vector3.zero;
    }

    internal HandSequence.SkeletonHandSequenceProvider SearchSkeletonDataProvider()
    {
        var oldProviders = gameObject.GetComponentsInParent<HandSequence.SkeletonHandSequenceProvider>();
        foreach (var dataProvider in oldProviders)
        {
            //if (dataProvider.GetSkeletonType() == _skeletonType)
            //{
                return dataProvider;
            //}
        }

        return null;
    }

    public IEnumerator toggleConfigMode(){
        
        if(_currentConfigMode == ConfigMode.ActiveLeft){
            Vector3 IndexPosition = GetIndexRight();
            LeftConfigSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            LeftConfigSphere.transform.position = IndexPosition + Vector3.down * 0.005f;
            LeftConfigSphere.transform.localScale = Vector3.one * 0.01f;
            LeftConfigSphere.GetComponent<Renderer>().material = yellowMaterial;
            LeftConfigSphere.name = "leftSphere";
            _configStep = 1;

        }

        if(_currentConfigMode == ConfigMode.ActiveRight){
            Vector3 IndexPosition = GetIndexRight();
            RightConfigSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            RightConfigSphere.transform.position = IndexPosition + Vector3.down * 0.005f;
            RightConfigSphere.transform.localScale = Vector3.one * 0.01f;
            RightConfigSphere.GetComponent<Renderer>().material = redMaterial;
            RightConfigSphere.name = "rightSphere";
            _configStep = 2;
            _guideLineRender.shouldRender = true;
        }
        yield return null;
    }
    /*public IEnumerator updateMarkerPosition(){
        
        if(_currentConfigMode == ConfigMode.ActiveLeft){
            Vector3 IndexPosition = GetIndexRight();
            LeftConfigSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            LeftConfigSphere.transform.position = IndexPosition;
            LeftConfigSphere.transform.localScale = Vector3.one * 0.01f;
            LeftConfigSphere.GetComponent<Renderer>().material = yellowMaterial;
            LeftConfigSphere.name = "leftSphere";
            _configStep = 1;
        }

        if(_currentConfigMode == ConfigMode.ActiveRight){
            _guideLineRender.shouldRender = true;
        }
        yield return null;
    }*/

    void Update()
    {
        if(_currentConfigMode == ConfigMode.ActiveLeft && LeftConfigSphere!= null){
            Vector3 IndexPosition = GetIndexRight();
            LeftConfigSphere.transform.position = IndexPosition + Vector3.down * 0.045f;
        }
        if(_currentConfigMode == ConfigMode.ActiveRight && RightConfigSphere!= null){
            Vector3 IndexPosition = GetIndexRight();
            RightConfigSphere.transform.position = IndexPosition + Vector3.down * 0.045f;
        }
    }
}
