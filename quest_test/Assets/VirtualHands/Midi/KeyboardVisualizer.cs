using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  PimDeWitte.UnityMainThreadDispatcher;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using System;
using System.IO;


public class KeyboardVisualizer : MonoBehaviour
{
    public interface KeyboardDataProvider{
        public HashSet<int> GetNotesDown();
        public event Action<NoteEvent> OnNoteUpdate;
    }

    public Material planeMat;

    private static List<int> whiteKeys = new List<int> { 0, 2, 4, 5, 7, 9, 11 };
    private static List<int> blackKeys = new List<int> { 1, 3, 6, 8, 10 };

    private static List<KeyVisualization> keyVisualizations;

    int leftKey;
    int rightKey;
    Vector3 leftCornerPosition;
    Vector3 rightCornerPosition;
    Vector3 forwardVector;

    public float blackKeyOffset = 2.5f;
    public float blackKeyHeight = 0.01f;

    float octaveWidth;

    private bool _hasConfiguration;

    // must be at the start of an octave, between b and c key
    Vector3 leftAnchor;
    // always divisable by 12
    int anchorKey;

    // startkey in the scale 0-11, c, c#, d, d#, e etc..
    //private int _startKey;

    Vector3 deltaVec;
    Vector3 oneKeyVector;

    private GameObject _MIDIDeviceGO;

    private KeyboardDataProvider _dataProvider;
    private ConfigurePhysicalKeyboard _config;
    private HandUtil _handUtil;

    public bool logData;
    public string logFileName;
    
    [SerializeField]
    private bool _useActiveMIDI;
    
    void Start()
    {
        // config is searched for globally, Provider is searched for locally
        // there should only be one config in the project, there may be multiple providers
        SearchProvider();
        SearchConfig();
        _handUtil = GetComponent<HandUtil>();
        if(_handUtil == null)Debug.LogError("Hand util not found");

        _hasConfiguration = false;
        //_MIDIDeviceGO = GameObject.Find("MIDIDevice");
        //if(_MIDIDeviceGO == null) Debug.LogError("Provider needs a game object called MIDIDevice, and it has to contain MIDIDevice script");

        _dataProvider.OnNoteUpdate += NoteChanged;
        _config.OnActiveConfigChanged += configUpdate;
    }
    void NoteChanged(NoteEvent _){
        if(_hasConfiguration){
            UnityMainThreadDispatcher.Instance().Enqueue(UpdateKeyboard());
        }
    }
    // updates the visualizations when new keys are down
    public IEnumerator UpdateKeyboard()
    {
        for (int i = leftKey; i <= rightKey; i++) {
            int fingerThatPressed = -1;
            if(_dataProvider.GetNotesDown().Contains(i) && !keyVisualizations[i - leftKey].IsRendering()){
                fingerThatPressed = _handUtil.GetFingerFromKey2(i);
                keyVisualizations[i - leftKey].color = OVRHandData.GetColorFromFinger(fingerThatPressed);

                if(logData) LogFingerToCSV(fingerThatPressed);
            }
            keyVisualizations[i - leftKey].Update(_dataProvider.GetNotesDown().Contains(i));
        }
        yield return null;
    }

    private void LogFingerToCSV(int finger) {
        string filePath = "Assets/VirtualHands/fingerLogs/" + logFileName + ".csv";

        if (!File.Exists(filePath)) {
            using (StreamWriter writer = new StreamWriter(filePath, false)) {
                writer.Write(finger + ",");
            }
        } else {
            using (StreamWriter writer = new StreamWriter(filePath, true)) { 
                writer.Write(finger + ",");
            }
        }
    }

    public class KeyVisualization{
        private Vector3 _keyPosition;
        private GameObject _plane;
        public Color color;

        public bool IsRendering(){
            return _plane.activeSelf;
        }

        public KeyVisualization(int key, KeyboardVisualizer keyboardVisualizer){
            _keyPosition = keyboardVisualizer.getPositionFromKey(key);
            //Debug.Log("spawning at pos: " + _keyPosition);
            //Vector3 deltaVec = Vector3.Normalize(keyboardVisualizer.rightCornerPosition - keyboardVisualizer.leftCornerPosition);
            Vector3 keyVector = (keyboardVisualizer.octaveWidth * keyboardVisualizer.deltaVec) / 7.0f;
            _plane  = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _plane.GetComponent<Renderer>().material = keyboardVisualizer.planeMat;
            color = Color.red;
            _plane.GetComponent<Renderer>().material.color = color;
            float keyVisWidth = blackKeys.Contains(key%12) ?  keyVector.magnitude / 2.0f : keyVector.magnitude;
            _plane.transform.localScale = new Vector3(keyVisWidth, 1.0f, keyVector.magnitude*2) / 10.0f;
            _plane.transform.rotation = Quaternion.LookRotation(keyboardVisualizer.forwardVector);
            _plane.transform.position = _keyPosition + keyVector/2.0f + Vector3.Normalize(keyboardVisualizer.forwardVector) * keyVector.magnitude;

            Debug.Log("vis pos:" + _keyPosition);
        }

        public void destroy(){
            Destroy(_plane);
        }
        public void Update(bool shouldRender){
            _plane.GetComponent<Renderer>().material.color = color;
            _plane.SetActive(shouldRender);
        }
    }

    Vector3 getPositionFromKey(int Key){
        int scaleKey = Key%12;
        Vector3 scalePos = getPositionFromScaleKey(scaleKey); 
        int octave = Key / 12;
        int anchorOctave = anchorKey / 12;
        Vector3 octaveVector = Vector3.Normalize(rightCornerPosition - leftCornerPosition) * octaveWidth;
        Vector3 octaveOffsetFromAnchor = (octave - anchorOctave) * octaveVector;
        return scalePos + octaveOffsetFromAnchor + leftAnchor;
    }

    Vector3 getPositionFromScaleKey(int scaleKey){
        Vector3 deltaVec = Vector3.Normalize(rightCornerPosition - leftCornerPosition);
        if(whiteKeys.Contains(scaleKey)){
            int i = whiteKeys.IndexOf(scaleKey);
            return (deltaVec * (octaveWidth/7.0f)) * i;
        }else if(blackKeys.Contains(scaleKey)){
            Vector3 oneKeyVector = (deltaVec * (octaveWidth/7.0f));
            Vector3 midPos;
            switch(scaleKey){
                case 1:
                    midPos = oneKeyVector * 1;
                    return midPos + (forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight - oneKeyVector * 0.75f;
                case 3:
                    midPos = oneKeyVector * 2;
                    return midPos + (forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight - oneKeyVector * 0.5f;
                case 6:
                    midPos = oneKeyVector * 4;
                    return midPos + (forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight - oneKeyVector * 0.5f;
                case 8:
                    midPos = oneKeyVector * 5;
                    return midPos + (forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight - oneKeyVector * 0.5f;
                case 10:
                    midPos = oneKeyVector * 6;
                    return midPos + (forwardVector * blackKeyOffset) + Vector3.up * blackKeyHeight - oneKeyVector * 0.5f;
                default:
                return Vector3.zero;
            }
        }
        return Vector3.zero;
    }

    void configUpdate(ConfigurePhysicalKeyboard.Config config){

        leftKey = config.leftKey;
        rightKey = config.rightKey;
        leftCornerPosition = config.leftCornerPosition;
        rightCornerPosition = config.rightCornerPosition;
        forwardVector = config.forwardVector;
        octaveWidth = config.octaveWidth;
        anchorKey = config.anchorKey;
        deltaVec = config.deltaVec;
        oneKeyVector = config.oneKeyVector;
        leftAnchor = config.anchor;
        

        if(keyVisualizations != null){
            keyVisualizations.ForEach(keyVis => keyVis.destroy());
            keyVisualizations.Clear();
        }else{
            keyVisualizations = new List<KeyVisualization>();
        }

        int amountOfKeys = (rightKey - leftKey) + 1;
        for(int j = 0; j < amountOfKeys; j++){
            keyVisualizations.Add(
                new KeyVisualization(leftKey+j, this)
            );
        }
        _hasConfiguration = true;
        Debug.Log("config updated");
        StartCoroutine(UpdateKeyboard());
    }

    void SearchProvider(){
        if(_dataProvider == null) {
            if (_useActiveMIDI)
            {
                GameObject deviceGO = GameObject.Find("MIDIDevice");
                var providerGlobal =  deviceGO.GetComponent<KeyboardDataProvider>();
                if(providerGlobal != null){
                    _dataProvider = providerGlobal;
                    return;
                }else{Debug.LogError("No global provider found");}
            }

            var provider = GetComponent<KeyboardDataProvider>();
            if(provider != null){
                _dataProvider = provider;
            }else{Debug.LogError("No provider found");}
        }
    }
    void SearchConfig(){
        if(_config == null) {
            var configGO = GameObject.Find("KeyboardConfiguration");
            var config = configGO ? configGO.GetComponent<ConfigurePhysicalKeyboard>() : null;
            if(config != null){
                _config = config;
            }else{Debug.LogError("No config found");}
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
