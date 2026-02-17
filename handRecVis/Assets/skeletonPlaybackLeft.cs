using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using JetBrains.Annotations;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using Unity.VisualScripting;
using System.IO;
using UnityEngine.UI;

using System.Linq;
//using UnityEditor.AssetImporters;
using UnityEditor;
//using UnityEditorInternal;




/// <summary>
/// Provides playback of handsequence.
/// Use by adding this together with a renderer.
/// </summary>
public class SkeletonPlaybackLeft : MonoBehaviour, 
HandSequence.SkeletonHandSequenceProvider, 
MIDIDevice.MidiDataProvider, 
KeyboardVisualizer.KeyboardDataProvider
{
    [SerializeField]
    private OVRSkeleton.SkeletonType _skeletonType;
    
    [SerializeField]
    private HandSequence _importSequence;
    // this is the one used, because playback modifies the data, it is copied over to _sequence from import sequence
    private HandSequence _sequence;

    [SerializeField]
    private bool _loop = true;
    
    private int _currentFrame = 0;

    private float _playbackTime;
    private float _startTime;
    private float _recordingLength;
    private float _lastUpdateTime;
    private float _progress;
    private int _framesAmount;

    public Text debuggerLogger; 

    private bool isPlaying = false;

    public AudioSource audioSource;

    public Transform frustumHead;


    private Texture2D texFromFile;

    public RawImage texTarget;

    private List<Vector3> _headPosFromFile;
    private List<Vector3> _headRotFromFile;
    //public Texture2D texTarget;

    //private bool isRecording;


    [Header("Recording Settings")]
    public int captureFPS;
    public string folderName = "ImageSequence";

    private int frameIndex = 0;
    private float frameTimer = 0f;

    private enum PlaybackState
    {
        Playing,
        Paused,
        FastForward,
        Rewind,
        SlowMo
    }
    private PlaybackState _activePlaybackState;
    private PlaybackState _lastState;
    
    private PlaybackState ActivePlaybackState
    {
        get => _activePlaybackState;
        set
        {
            if (_activePlaybackState != value)
            {
                _activePlaybackState = value;
                OnPlaybackStateChanged(value);
            }
        }
    }

    private void OnPlaybackStateChanged(PlaybackState newState)
    {
        switch(newState){
            case PlaybackState.Playing:
                _progressBar.SetTextLeft("Playing ▶");
                UpdatePlaybackSpeed(1.0f);
                break;
            case PlaybackState.Paused:
                //_progressBar.SetTextLeft(ActivePlaybackState == PlaybackState.Paused?"Paused \u25b6":"Playing ▶");
                _progressBar.SetTextLeft("Paused \u25b6");
                UpdatePlaybackSpeed(0.0f);
                break;
            case PlaybackState.Rewind:
                _progressBar.SetTextLeft("Rewinding ⏪");
                UpdatePlaybackSpeed(-1.0f);
                break;
            case PlaybackState.FastForward:
                _progressBar.SetTextLeft("Fast Forward ⏩");
                UpdatePlaybackSpeed(2.0f);
                break; 
            case PlaybackState.SlowMo:
                _progressBar.SetTextLeft("Slow Motion 🐢");
                UpdatePlaybackSpeed(0.3f);
                break;
            default:
                break;
        }

        return;
    }

    private float _lastMidiDataRead;

    public GameObject progressBarPrefab;
    private GameObject _progressBarGO;
    [CanBeNull] private progressbar _progressBar;

    public bool _isPlaybackActive = false;

    private float _playbackMultiplier;
    public bool IsPlaybackActive
    {
        get { return _isPlaybackActive; }
    }
    private HashSet<int> _notesDown;

    // for keyboard vis
    public HashSet<int> GetNotesDown(){
        return _notesDown;
    }
    public event Action<NoteEvent> OnNoteUpdate;
    
    public bool isInitialized { get; private set; }

    public bool PlayMidi;
    
    public List<HandSequence.SerializableNoteEvent> _midiEventBuffer;

    private Matrix4x4 _currentKeyboardSpaceMatrix;
    
    private ConfigurePhysicalKeyboard _config;

    private HandSequence.HandFrame _interpolatedFrame;

    private enum PlaybackInterpolationMode
    {
        noInterpolation,
        LinearInterpolation
    }
    [SerializeField]
    private PlaybackInterpolationMode _interpolationMode;

    /// <summary>
    /// Will be initialized if it has data
    /// </summary>
    /// <returns></returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    public OVRSkeleton.SkeletonType GetSkeletonType()
    {
        return _skeletonType;
    }
    
    public HandSequence.HandFrame GetHandFrameData()
    {
        if (!_isPlaybackActive) return null;
        
        if (_interpolationMode == PlaybackInterpolationMode.noInterpolation) return _sequence.frames[_currentFrame];
        
        return _interpolatedFrame;
    }

    public void UpdatePlaybackSpeed(float delta)
    {
        _playbackMultiplier = delta;
        _progressBar.SetTextRight(((float)Math.Round(_playbackMultiplier, 1)).ToString() + "X");
    }

    // MIDI data accumulates in midiEventBuffer, and gets consumed by this function
    public List<HandSequence.SerializableNoteEvent> GetMidiData(){
        if (!_isPlaybackActive) return null;
        // returning and reseting buffer
        var oldBuffer = _midiEventBuffer;
        _midiEventBuffer = new List<HandSequence.SerializableNoteEvent>();

        _lastMidiDataRead = _playbackTime;
            
        return oldBuffer;
    }

    private void StartPlayback()
    {
        
        _activePlaybackState = PlaybackState.Playing;
        
        _interpolatedFrame = _sequence.frames[0].DeepCopy();
        
        _playbackTime = 0.0f;
        _lastUpdateTime = Time.time;
        _playbackMultiplier = 1.0f;

        _lastMidiDataRead = 0.0f;
        
        _notesDown = new HashSet<int>();
        _currentKeyboardSpaceMatrix = _config.getSpaceMatrix();
        
        _sequence.applyTransformation(_currentKeyboardSpaceMatrix);
        Debug.Log("Applying transform on start playback");
        Debug.Log(_currentKeyboardSpaceMatrix);
        CreateProgressBar(_config.activeConfig);
   

        Debug.Log("is playing");
        _currentFrame = 0;
        _startTime = Time.time;
        _isPlaybackActive = true;
    }
    private void StopPlayback()
    {
        Debug.Log("Stopped playback");
        _isPlaybackActive = false;
        _sequence.applyTransformation(_currentKeyboardSpaceMatrix.inverse);
        DestroyProgressBar();
        Debug.Log("Applying transform on stop playback");
        Debug.Log(_currentKeyboardSpaceMatrix);
        isPlaying = false;
    }

    void CreateProgressBar(ConfigurePhysicalKeyboard.Config config)
    {
        //Vector3 position = config.anchor + config.deltaVec * config.keyboardSurfaceLength / 2.0f;
        Vector3 position = new Vector3(0f,0f,0f); // (config.anchor + config.deltaVec * config.keyboardSurfaceLength / 2.0f) +
                           //Vector3.Normalize(config.forwardVector) * (config.keyboardSurfaceLength / 3.0f) +
                           //Vector3.up * (config.keyboardSurfaceLength / 3.0f);
        _progressBarGO = Instantiate(progressBarPrefab, position, Quaternion.LookRotation(-config.deltaVec, Vector3.up), transform);

        _progressBarGO.transform.parent = debuggerLogger.transform.parent;

        _progressBarGO.transform.localEulerAngles = new Vector3(debuggerLogger.transform.localEulerAngles.x, debuggerLogger.transform.localEulerAngles.y-90f, debuggerLogger.transform.localEulerAngles.z);
        _progressBarGO.transform.localPosition = debuggerLogger.transform.localPosition;
        _progressBarGO.transform.localScale = new Vector3(_progressBarGO.transform.localScale.x/4,_progressBarGO.transform.localScale.y/4,_progressBarGO.transform.localScale.z/4);

        _progressBar = _progressBarGO.transform.Find("inner")?.gameObject.GetComponent<progressbar>();
        _progressBar.Inititalize();
    }

    void DestroyProgressBar()
    {
        Destroy(_progressBarGO);
    }

    void SetFrameFromTime2()
    { 
        Interpolate(Time.time, ref _interpolatedFrame);
    }

    private void ReadMidi()
    {
        float startTime = 0.0f;
        float endTime = 0.0f;
        
        if (ActivePlaybackState == PlaybackState.Paused) return;
        if (_playbackMultiplier < 0.0f)
        {
            startTime = _playbackTime;
            endTime = _lastMidiDataRead;
        }
        else
        {
            startTime = _lastMidiDataRead;
            endTime = _playbackTime;
        }

        List<HandSequence.SerializableNoteEvent> frameMidi = GetMidiFromRange(startTime, endTime);
        _midiEventBuffer.AddRange(frameMidi);

        //ReceiveMIDI(frameMidi);
        
        foreach(var e in frameMidi){
            OnEventReceived(e.ToNoteEvent());
        }
    }

    private void Interpolate(float time, ref HandSequence.HandFrame frame)
    {
        int left = BinarySearchSequence(time, 0, _framesAmount - 1);
        int right = left + 1;
        
        if (right >= _framesAmount)
        {
            isPlaying = false;
            StopPlayback();
            if (_loop) {
                StartPlayback();
                audioSource.Stop();
                audioSource.time = 0.0f;
                frameTimer = 0f;
                audioSource.Play(); 
                Debug.Log("mic started playing from skeletonpb 1");  
            }
            return;
        }

        HandSequence.HandFrame leftFrame = _sequence.frames[left];
        HandSequence.HandFrame rightFrame = _sequence.frames[right];
        
        LinearFrameInterpolation(leftFrame, rightFrame, ref frame);
        
        void LinearFrameInterpolation(HandSequence.HandFrame leftFrame, HandSequence.HandFrame rightFrame, ref HandSequence.HandFrame frame)
        {
            float factor = (time - leftFrame.time) / (rightFrame.time - leftFrame.time);
            
            //validity
            frame.IsDataValid = leftFrame.IsDataValid && rightFrame.IsDataValid;
            frame.IsDataHighConfidence = leftFrame.IsDataHighConfidence && rightFrame.IsDataHighConfidence;

            if (!frame.IsDataValid || !frame.IsDataHighConfidence) return;
            
            // time
            frame.time = LinearFloatInterpolation(leftFrame.time, rightFrame.time, factor);
            
            // root pose
            frame.RootPose.Position = LinearVector3Interpolation(leftFrame.RootPose.Position, rightFrame.RootPose.Position, factor);
            frame.RootPose.Orientation = Quaternion.Slerp(leftFrame.RootPose.Orientation, rightFrame.RootPose.Orientation, factor);
            
            // Root scale
            frame.RootScale = LinearFloatInterpolation(leftFrame.RootScale, rightFrame.RootScale, factor);

            //  rotations
            /*int boneCount = leftFrame.BoneRotations.Length;
            if (frame.BoneRotations == null || frame.BoneRotations.Length != boneCount)
                frame.BoneRotations = new Quaternion[boneCount];

            for (int i = 0; i < boneCount; i++)
                frame.BoneRotations[i] = Quaternion.Slerp(leftFrame.BoneRotations[i], rightFrame.BoneRotations[i], factor);
            */   
                
            // translations
            for (int i = 0; i < leftFrame.BoneTranslations.Length; i++)
                frame.BoneTranslations[i] = LinearVector3Interpolation(leftFrame.BoneTranslations[i], rightFrame.BoneTranslations[i], factor);
            
            //Applies the rotatations from the new translations
            frame.SetRotationFromTranslation();
            
            float LinearFloatInterpolation(float left, float right, float t)
            {
                return left * (1.0f-t) + right * (t);
            }
            Vector3 LinearVector3Interpolation(Vector3 left, Vector3 right, float t)
            {
                return left * (1.0f-t) + right * (t);
            }
        }
    }
    

    private List<HandSequence.SerializableNoteEvent> GetMidiFromRange(float startTime, float stopTime)
    {
        int start = BinarySearchSequence(startTime, 0, _framesAmount-1);
        int stop = BinarySearchSequence(stopTime, 0, _framesAmount-1);
        List<HandSequence.SerializableNoteEvent> acc = new List<HandSequence.SerializableNoteEvent>();
        for (int i = start; i <= stop; i++)
        {
            acc.AddRange(_sequence.frames[i].MidiData);
        }

        return acc;
    }
    // returns the first frame before the time
    private int BinarySearchSequence(float time, int start, int stop)
    {
        if (stop - start < 3) return start;
        
        int middleIndex = start + (stop - start / 2);
        float middleTime = _sequence.frames[middleIndex].time;
        
        if (time < middleTime) return BinarySearchSequence(time, start, middleIndex-1);
        else return BinarySearchSequence(time, middleIndex, stop);
        
    }


    // pretty bad but simple algorithm to choose a frame, 
    // just chooses the frame before in time.
    void SetFrameFromTime()
    {
        int offset = 1;
        while (true)
        {   
            // on end of recording
            if (_currentFrame + offset >= _sequence.Length)
            {
                if (_loop)
                {
                    // loop around
                    _startTime = Time.time;
                    _currentFrame = 0;
                    _playbackTime = 0.0f;
                    offset = 1;
                }
                else
                {
                    isPlaying = false;
                    StopPlayback();
                    break;
                }
            }
            
            HandSequence.HandFrame frame = _sequence.frames[_currentFrame + offset];
            List<HandSequence.SerializableNoteEvent> currentMidiData = new List<HandSequence.SerializableNoteEvent>();
            

            if (_playbackTime < frame.time)
            {
                _currentFrame = _currentFrame + (offset-1);
                return;
            }

            if(PlayMidi){
                if(frame.MidiData.Count != 0){
                    _midiEventBuffer.AddRange(frame.MidiData);

                    //Every frame midi event should just run once, 
                    // TODO Test this, im not sure this method is solid enough
                    foreach(var e in frame.MidiData){
                        OnEventReceived(e.ToNoteEvent());
                    }
                }
            }

            offset++;
        }
    }

    private void ReceiveMIDI(List<HandSequence.SerializableNoteEvent> frameMIDI)
    {
        
        foreach(var e in frameMIDI){
            var thisNoteEvent = e.ToNoteEvent();
            var number = (int)thisNoteEvent.NoteNumber;
            if(thisNoteEvent.EventType == MidiEventType.NoteOn){
                _notesDown.Add(number);
                OnNoteUpdate?.Invoke((NoteEvent)thisNoteEvent);
            }

            if(thisNoteEvent.EventType == MidiEventType.NoteOff){
                _notesDown.Remove(number);
                OnNoteUpdate?.Invoke((NoteEvent)thisNoteEvent);
            }
        }

        if (frameMIDI.Count != 0)
        {
            
        }
    }

    private void OnEventReceived(NoteEvent e)
    {
        //I don't know why this invoking for every note event?
        // shouldn't it wait for the full frame?
        // TODO
        var thisNoteEvent = e;
        var number = (int)thisNoteEvent.NoteNumber;
        if(thisNoteEvent.EventType == MidiEventType.NoteOn){
            Debug.Log("**NOTE DOWN _notesDown contains: ");
            Debug.Log(string.Join(", ", _notesDown));
            _notesDown.Add(number);
            OnNoteUpdate?.Invoke((NoteEvent)thisNoteEvent);
        }

        if(thisNoteEvent.EventType == MidiEventType.NoteOff){
            Debug.Log("**NOTE UP _notesDown contains: ");
            Debug.Log(string.Join(", ", _notesDown));
            _notesDown.Remove(number);
            OnNoteUpdate?.Invoke((NoteEvent)thisNoteEvent);
        }
    }
    


    void Start()
    {

        isPlaying = false;
        frameTimer = 0f;
        frameIndex = 0;

        _headPosFromFile = new List<Vector3>();
        _headRotFromFile = new List<Vector3>();

        SearchConfig();
        _notesDown = new HashSet<int>();
        _sequence = _importSequence.DeepCopy();
        _midiEventBuffer = new List<HandSequence.SerializableNoteEvent>();

        audioSource = GetComponent<AudioSource>();

        //gets the time of the last frame
        _recordingLength = _sequence.frames[_sequence.frames.Count - 1].time;

        



        _framesAmount = _sequence.frames.Count;

        isInitialized = _sequence.hasData();

        _config.OnKeyboardInputdeviceKeyPressed += KeyboardInput;

        StartPlayback();        //forcing playback to start without key input
        audioSource.Stop();
        audioSource.time = 0.0f;

        isPlaying = true;
        frameTimer = 0f;
        frameIndex = 0;
        audioSource.Play(0); 
        Debug.Log("mic started playing from skeletonpb 2");

        
    }

    public void PlayNewSequence(string fileName)
    {
        SearchConfig();
        _notesDown = new HashSet<int>();

        debuggerLogger.text += "\nreading handFiles";


        _importSequence = readFile(fileName);


        //debuggerLogger.text += "\ncalling headFiles";
        //readFileHead("recRnew");

        //debuggerLogger.text += "\nheadFiles called";



        _sequence = _importSequence.DeepCopy();
        _midiEventBuffer = new List<HandSequence.SerializableNoteEvent>();

        //gets the time of the last frame
        _recordingLength = _sequence.frames[_sequence.frames.Count - 1].time;
        _framesAmount = _sequence.frames.Count;

        isInitialized = _sequence.hasData();

        _config.OnKeyboardInputdeviceKeyPressed += KeyboardInput;

        StartPlayback();        //forcing playback to start without key input
        audioSource.time = 0.0f;
        isPlaying = true;
        frameTimer = 0f;
        frameIndex = 0;
        audioSource.Play(0);  
        Debug.Log("mic started playing from skeletonpb 3");
    }

    void readFileHead(string fileName)
    {
        var file = new FileStream(Application.persistentDataPath + "/" + fileName + "_HP", FileMode.Open, FileAccess.Read);
        var reader = new StreamReader(file);
        float fileLength = file.Length;

        debuggerLogger.text += "\nreading positions";

        while (true)
        {
            string line = reader.ReadLine(); // Read a new line
            if (line == null) break; // stop at end of file

            debuggerLogger.text += "\nline read: " +line;
            

            var element = line.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray();

            Vector3 read = new Vector3(element[0], element[1], element[2]);

            debuggerLogger.text += "\n x read: " +element[0];
            debuggerLogger.text += "\n z read: " +element[1];
            debuggerLogger.text += "\n z read: " +element[2];

            _headPosFromFile.Add(read);

            
        
        }

        debuggerLogger.text += "\nreading rotations";

        file = new FileStream(Application.persistentDataPath + "/" + fileName + "_HR", FileMode.Open, FileAccess.Read);
        reader = new StreamReader(file);
        fileLength = file.Length;

        while (true)
        {
            string line = reader.ReadLine(); // Read a new line
            if (line == null) break; // stop at end of file
            
            debuggerLogger.text += "\nline read: " +line;

            var element = line.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray();

            Vector3 read = new Vector3(element[0], element[1], element[2]);

            debuggerLogger.text += "\n x read: " +element[0];
            debuggerLogger.text += "\n z read: " +element[1];
            debuggerLogger.text += "\n z read: " +element[2];
            
            _headRotFromFile.Add(read);
        
        }

        
    }

    HandSequence readFile(string fileName)
    {


        var file = new FileStream(Application.persistentDataPath + "/" + fileName, FileMode.Open, FileAccess.Read);
        
       // debuggerLogger.text += "\n * READING * " + fileName + " from " + Application.persistentDataPath;
  
        
        var reader = new StreamReader(file);
        float fileLength = file.Length;

        Debug.Log("############importing now#################");

        _importSequence = ScriptableObject.CreateInstance<HandSequence>();
        
        //string info = $"Reading particle set file {Path.GetFileName(ctx.assetPath)}";
        //EditorUtility.DisplayProgressBar("Importing Hand Sequence", info, 0);
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

            
            Debug.Log(currentFrame);

            _importSequence.frames.Add(frame);
            //Debug.Log("new frame added");
            //Debug.Log("time: " + frame.time);
            //Debug.Log("example import: " + frame.BoneRotations[0].x);
            
            ++currentFrame;
           /* if (currentFrame % 20 == 0) {
                EditorUtility.DisplayProgressBar("Hand sequence Importer", info, file.Position / fileLength);
            }*/
            
        }

        return _importSequence;
            
           // ctx.AddObjectToAsset("Hand.Sequence", handSequence);
          //  ctx.SetMainObject(handSequence);
            
          //  EditorUtility.ClearProgressBar();

        
        
    }

    public void OverrideMainSequence(HandSequence s)
    {

        debuggerLogger.text += "\ncalling headFiles";
        readFileHead("recRnew.hseq");

        debuggerLogger.text += "\nheadFiles called";


        _sequence = s.DeepCopy();
        _midiEventBuffer = new List<HandSequence.SerializableNoteEvent>();

        //gets the time of the last frame
        _recordingLength = _sequence.frames[_sequence.frames.Count - 1].time;
        _framesAmount = _sequence.frames.Count;

        isInitialized = _sequence.hasData();
        Debug.Log("Override complete");

        StartPlayback();
        audioSource.time = 0.0f;
        isPlaying = true;
        frameTimer = 0f;
        frameIndex = 0;
        audioSource.Play(0);  
        Debug.Log("mic started playing from skeletonpb 4");

    }

    void KeyboardInput(List<int> inputList)
    {
        int input = inputList[0];
        
        switch (input){
            //Play and Stop Key
            case 0:
                // On not active: Play
                if(!_isPlaybackActive)
                {
                    StartPlayback();
                    audioSource.time = 0.0f;
                    isPlaying = true;
                    frameTimer = 0f;
                    frameIndex = 0;
                    audioSource.Play(0); 
                    Debug.Log("mic started playing from skeletonpb 5");
                }
                // On active
                else
                {
                    /*_isPaused = !_isPaused;
                    _progressBar.SetTextLeft(_isPaused?"Paused \u25b6":"Playing ▶");*/
                    isPlaying = false;
                    StopPlayback();
                }
                break;
            case 1:
                _lastState = ActivePlaybackState;
                ActivePlaybackState = PlaybackState.FastForward;
                break;
            case 2:
                _lastState = ActivePlaybackState;
                ActivePlaybackState = PlaybackState.Rewind;
                break;
            case 3: // Pause
                _lastState = ActivePlaybackState;
                if(ActivePlaybackState == PlaybackState.Paused) ActivePlaybackState = PlaybackState.Playing;
                else if (ActivePlaybackState == PlaybackState.Playing) ActivePlaybackState = PlaybackState.Paused;
                break;
            case 4: // SlowMo
                _lastState = ActivePlaybackState;
                ActivePlaybackState = PlaybackState.SlowMo;
                break;
            default: 
                break;
        }
    }

    private void TryUpdateProgressBar()
    {
        if (_progressBar == null) return;
        _progressBar.UpdateProgress(_progress);
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

    string GetReadPath()
    {
        
        #if UNITY_ANDROID && !UNITY_EDITOR
                //debuggerLogger.text+="\nsaving path is:" + Path.Combine(Application.persistentDataPath, folderName);
                return Path.Combine(Application.persistentDataPath, folderName);
        #else
                //debuggerLogger.text+="\nsaving path is:" + Path.Combine(Application.dataPath, folderName);
                return Path.Combine(Application.dataPath, folderName);
        #endif
    }

    Texture2D LoadTexture(string path, int currentFrame)
    {

        path = path + "/frame_"+currentFrame+".jpg";

        if (!File.Exists(path))
        {
            frameTimer = 0f;
            frameIndex = 0;

            path = path + "/frame_"+frameIndex+".jpg";

            byte[] imageData0 = File.ReadAllBytes(path);
            texFromFile = new Texture2D(2, 2, TextureFormat.RGB24, false);
            texFromFile.LoadImage(imageData0); // auto-resizes texture
            return texFromFile;


            //Debug.LogError("File not found: " + path);
            //return null;
        }

        byte[] imageData = File.ReadAllBytes(path);
        texFromFile = new Texture2D(2, 2, TextureFormat.RGB24, false);
        texFromFile.LoadImage(imageData); // auto-resizes texture
        return texFromFile;
    }




    void Update()
    {


        if (isPlaying)
        {
            frameTimer += Time.deltaTime;
            texTarget.enabled = true;
            

         if (frameTimer >= 1f / captureFPS)         //$"frame_{frameIndex}.jpg"
            {

                frustumHead.localPosition = _headPosFromFile[_currentFrame];
                frustumHead.localEulerAngles = _headRotFromFile[_currentFrame];

                frameTimer -= 1f / captureFPS;          

                texFromFile = LoadTexture(GetReadPath(), frameIndex);
                texTarget.texture = texFromFile;
                texTarget.SetNativeSize();

                
                /*RenderTexture.active = texFromFeed;
                uncompTex.ReadPixels(new Rect(0, 0, texFromFeed.width, texFromFeed.height), 0, 0);
                uncompTex.Apply();
                RenderTexture.active = null;*/


                frameIndex++;
            }

        }

        else
        {
            texTarget.enabled = false;
        }
        

        if (audioSource.time > _recordingLength)
        {
            isPlaying = true;
            frameTimer = 0f;
            frameIndex = 0;
            audioSource.Play(0);
            Debug.Log("time is: "+_playbackTime);
        }

        
        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayNewSequence("recR.hseq");
        }


        

        if (_isPlaybackActive)
        {
            switch(ActivePlaybackState){
                case PlaybackState.Rewind:
                    if (!_config.IsKeyDown(_config.decreaseSpeedKey))
                    {
                        ActivePlaybackState = _lastState;
                        _midiEventBuffer = new List<HandSequence.SerializableNoteEvent>();
                    }
                    break;
                case PlaybackState.FastForward:
                    if (!_config.IsKeyDown(_config.increaseSpeedKey))
                    {
                        ActivePlaybackState = _lastState;
                        _midiEventBuffer = new List<HandSequence.SerializableNoteEvent>();
                    }
                    break; 
                case PlaybackState.SlowMo:
                    if (!_config.IsKeyDown(_config.slowMoKey))
                    {
                        ActivePlaybackState = PlaybackState.Playing;
                    }
                    break;
            }
            
            if (ActivePlaybackState == PlaybackState.Paused) {}
            else {
                float deltaTime = Time.time - _lastUpdateTime;
                _playbackTime += deltaTime * _playbackMultiplier;
                _progress = _playbackTime / _recordingLength;
            }
            
            _lastUpdateTime = Time.time;

            if (_playbackMultiplier < 0.0f && _playbackTime < 0.0f)
            {
                isPlaying = false;
                StopPlayback();
            }


            TryUpdateProgressBar();
            if (_interpolationMode == PlaybackInterpolationMode.noInterpolation)
            {
                SetFrameFromTime();
            }

            if (_interpolationMode == PlaybackInterpolationMode.LinearInterpolation)
            {
                Interpolate(_playbackTime, ref _interpolatedFrame);
                if(PlayMidi && _playbackMultiplier > 0.0f) ReadMidi();
            }
        }
    }
}