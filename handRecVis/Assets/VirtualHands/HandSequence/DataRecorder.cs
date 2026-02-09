using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Reflection;
using UnityEngine.UI;
using Meta.XR.MRUtilityKit;
using UnityEngine.Android;
//using UnityEngine.AudioModule;

/// <summary>
/// Importer for a OpenXR animated hand sequence, recorded from 
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
///
/// </summary>

public class DataRecorder :  MonoBehaviour
{

    [SerializeField]
    private OVRSkeleton.SkeletonType _skeletonType;

    private HandSequence.SkeletonHandSequenceProvider _dataProvider;
    private MIDIDevice.MidiDataProvider _midiDataProvider;

    private string _saveLocation;

    [SerializeField]
    

    private bool _isRecording;

    private int _currentRecording;

    private bool _hasRecording;

    private bool _machineOn = true;

    public GameObject machine;

    private List<Vector3> _headPositions;

    private List<Vector3> _headRotations;

    private List<HandSequence> _handSequenceRecordings;

    public bool recordMidi;
    
    //Time in second where the last recording started
    private float _startTime;
    
    [SerializeField]
    private string _fileName;
    
    private ConfigurePhysicalKeyboard _config;
    
    public GameObject progressBarPrefab;
    private GameObject _progressBarGO;
    private progressbar _progressBar;
    
    [SerializeField]
    private GameObject _playbackGo;

    public Text debuggerLogger;

    public  AudioSource audioSource;

    public GameObject videoRecorder;

    public Transform proxyHeadPosition;

    int nr = 0;
    
    private void RecordCurrentFrame()
    {
        HandSequence.HandFrame data = _dataProvider.GetHandFrameData();
        data.time = Time.time - _startTime;
        data.HasMidi = recordMidi;
        if(recordMidi){
            data.MidiData = _midiDataProvider.GetMidiData();
        }
    
        _handSequenceRecordings[_currentRecording].frames.Add(data);

        _headPositions.Add(proxyHeadPosition.localPosition);
        _headRotations.Add(proxyHeadPosition.localEulerAngles);
    }


    public void StartRecording()
    {
         Debug.Log("poked record");
         
        

            if (!_isRecording)
            {
                //start new recording
                _startTime = Time.time;
                _hasRecording = true;
                _handSequenceRecordings.Add(ScriptableObject.CreateInstance<HandSequence>());
                Debug.Log(" * RECORDING STARTED *");
                debuggerLogger.text = "* RECORDING STARTED *";


                foreach (var device in Microphone.devices)
                {
                    Debug.Log("mic Name: " + device);
                }

                audioSource.clip = Microphone.Start(null, false, 180, 16000);     //length?
             Debug.Log("mic started recording");  

                videoRecorder.GetComponent<recordAsVideo>().startRecording();
                Debug.Log("calling video recorder from data recorder");  
                
            }
            else
            {
                //Stop recording
                Debug.Log(" * RECORDING STOPPED *");
                debuggerLogger.text = "* RECORDING STOPPED *";
                _currentRecording += 1;

                Microphone.End(null);
                 Debug.Log("mic stopped recording");  

                videoRecorder.GetComponent<recordAsVideo>().stopRecording();

                ExportFiles();
                StartCoroutine(WaitForExportSave());

               /* audioSource.Stop();
                audioSource.Play(); 
                Debug.Log("mic started playing from datarecoder");*/

                //Debug.Log("mic started playing");  

            }

            _isRecording = !_isRecording;

    }

    public void SavePlayback()
    {
         //   debuggerLogger.text += "\n * SAVING STARTED *";

            //if(_hasRecording) 
            ExportFiles();

         //   debuggerLogger.text += "\n * will do coroutine*";

            StartCoroutine(WaitForExportSave());

         //   debuggerLogger.text += "\n * coroutine STARTED *";

            //debuggerLogger.text = " * COROUTINE STARTED *";

    }

    public void TurnOnOffMachine()
    {
         Debug.Log("poked machine");

         machine.SetActive(!_machineOn);
         _machineOn = !_machineOn;

    }

    void Update()
    {


        if (Input.GetKeyDown(KeyCode.R) || OVRInput.GetDown(OVRInput.RawButton.X))
        {
             Debug.Log("pressed R or VR.A");

             //Microphone.End("Headset (WH-1000XM3)");



            if (!_isRecording)
            {
                //start new recording

                StartRecording();
                /*_startTime = Time.time;
                _hasRecording = true;
                _handSequenceRecordings.Add(ScriptableObject.CreateInstance<HandSequence>());
                Debug.Log(" * RECORDING STARTED *");*/
            }
            else
            {
                //Stop recording
                Debug.Log(" * RECORDING STOPPED *");
                _currentRecording += 1;

                if(_hasRecording) ExportFiles();
                StartCoroutine(WaitForExportSave());
            }

            _isRecording = !_isRecording;
        }  
        
        if (_isRecording) {
            RecordCurrentFrame();
        }
        if (Input.GetKeyDown(KeyCode.S) || OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            if(_hasRecording) ExportFiles();
            StartCoroutine(WaitForExportSave());

        }  
        
    }

    IEnumerator WaitForExportSave()
    {
       // debuggerLogger.text += "\n * WAITFOREXPORT STARTED *";

        yield return new WaitForSeconds(3.0f);

       // debuggerLogger.text += "\n * YIELD STARTED *";

        _playbackGo.SetActive(true);

      //  debuggerLogger.text += "\n * SETACTIVE TRUE STARTED *";

        SkeletonPlayback pb = _playbackGo.GetComponent<SkeletonPlayback>();

      //  debuggerLogger.text += "\n * SKELETON STARTED *";


        pb.OverrideMainSequence(_handSequenceRecordings[0]);

       // debuggerLogger.text += "\n * OVERRIDE STARTED *";

        //StartCoroutine(WaitToChangeScene());
        gameObject.SetActive(false);

        /*audioSource.Stop();
                audioSource.Play(); 
                Debug.Log("mic started playing from datarecorder");*/

        //debuggerLogger.text += "\n * SAVISETACTIVE FALSE STARTED *";
    }

   /* IEnumerator WaitToChangeScene()
    {
        yield return new WaitForSeconds(3.0f);
        _playbackGo.SetActive(true);
        //SkeletonPlayback pb = _playbackGo.GetComponent<SkeletonPlayback>();
        //pb.OverrideMainSequence(_handSequenceRecordings[0]);
        gameObject.SetActive(false);
    }*/

    private IEnumerator SlowUpdate()
    {
        while (true)
        {
            if (_isRecording) {
                UpdateProgressBar();
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    void Start()
    {

        if(!Permission.HasUserAuthorizedPermission(Permission.Microphone)){
     Permission.RequestUserPermission(Permission.Microphone);
        }
        _isRecording = false;
        _currentRecording = 0;
        _handSequenceRecordings = new List<HandSequence>();

        _headPositions = new List<Vector3>();
        _headRotations = new List<Vector3>();
        
        audioSource = audioSource.GetComponent<AudioSource>();

        SearchConfig();
        _config.OnKeyboardInputdeviceKeyPressed += KeyboardInput;

        StartCoroutine(SlowUpdate());
        
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
        if(_midiDataProvider == null && recordMidi){
            var foundDataProvider = SearchMidiDataProvider();
            if (foundDataProvider != null)
            {
                _midiDataProvider = foundDataProvider;
                if (_midiDataProvider is MonoBehaviour mb)
                {
                    Debug.Log($"found mididataprovider");
                }
            }else{
                Debug.LogWarning("didn't find a midi data provider for recording, uncheck recordMidi or add provider"); 
                recordMidi = false;
            }
        }
    }

    private void KeyboardInput(List<int> inputList)
    {
        int input = inputList[0];

        switch (input)
        {
            case 0: // play/stop key pressed
                if (!_isRecording)
                {
                    //start new recording
                    _startTime = Time.time;
                    _hasRecording = true;
                    _handSequenceRecordings.Add(ScriptableObject.CreateInstance<HandSequence>());
                    Debug.Log(" * RECORDING STARTED *");
                    CreateProgressBar(_config.activeConfig);

                }
                else
                {
                    //Stop recording
                    Debug.Log(" * RECORDING STOPPED *");
                    DestroyProgressBar();
                    _currentRecording += 1;
                }

                _isRecording = !_isRecording;
                
                break;
            case 1:
                break;
            default:
                break;
        }
    }
    
    void CreateProgressBar(ConfigurePhysicalKeyboard.Config config)
    {
        //Vector3 position = config.anchor + config.deltaVec * config.keyboardSurfaceLength / 2.0f;
        Vector3 position = (config.anchor + config.deltaVec * config.keyboardSurfaceLength / 2.0f) +
                           Vector3.Normalize(config.forwardVector) * (config.keyboardSurfaceLength / 3.0f) +
                           Vector3.up * (config.keyboardSurfaceLength / 3.0f);
        _progressBarGO = Instantiate(progressBarPrefab, position, Quaternion.LookRotation(-config.deltaVec, Vector3.up), transform);
        _progressBar = _progressBarGO.transform.Find("inner")?.gameObject.GetComponent<progressbar>();
        _progressBar.Inititalize();
        
        _progressBar.SetTextLeft("Recording \ud83d\udd34");
        _progressBar.SetTextRight("00:00:00");
    }
    private void DestroyProgressBar()
    {
        Destroy(_progressBarGO);
    }

    private void UpdateProgressBar()
    {
        float elapsed = Time.time - _startTime; 

        int minutes = (int)(elapsed / 60);
        int seconds = (int)(elapsed % 60);
        int milliseconds = (int)((elapsed * 100) % 100);

        string timeFormatted = $"{minutes:00}:{seconds:00}:{milliseconds:00}";
        _progressBar.SetTextRight(timeFormatted);
    }

    void OnApplicationQuit()
    {
        Debug.Log("application quit");
        if(_hasRecording) ExportFiles();
    }

    private void ExportFiles()
    {
        debuggerLogger.text = "* SAVING RECORDING *";

       /* GameObject keyboardConfig = GameObject.Find("KeyboardConfiguration");
        if(keyboardConfig != null){
            Debug.Log("found config. now transforming into keyboard space");
            ConfigurePhysicalKeyboard config = keyboardConfig.GetComponent<ConfigurePhysicalKeyboard>();
            Matrix4x4 inverseKeyboardSpaceMatrix = config.getInverseSpaceMatrix();
            foreach (var handSequence in _handSequenceRecordings)
            {
                Debug.Log("applying");
                Debug.Log(inverseKeyboardSpaceMatrix);
                handSequence.applyTransformation(inverseKeyboardSpaceMatrix);
            }
        }*/

        //debuggerLogger.text += "\n * FIRSTIF STARTED *";

        
        //foreach (var handSequence in _handSequenceRecordings)
        //{
            //debuggerLogger.text += "\n * foreach STARTED *";

            string timestamp = DateTime.Now.ToString("dd-MM-yy");

            string filename = "recRnew";  //_fileName + timestamp;

            //debuggerLogger.text += "\n * filename will be * " + filename;
            


            _saveLocation = Application.persistentDataPath; //"/mnt/sdcard/Android/data";


            List<string> lines = new List<string>();

            //debuggerLogger.text += "\n * getting into for *";
        
        
            for (int i = 0; i < _handSequenceRecordings[nr].frames.Count; i++)
            {
                //debuggerLogger.text += "\n * inside for *";
                lines.Add(_handSequenceRecordings[nr].frames[i].ToString());
                //debuggerLogger.text += "\n * line added *";
            }
            //debuggerLogger.text += "\n * all lines added *";

            File.WriteAllLines(_saveLocation+"/"+filename+".hseq", lines);



            lines = new List<string>();



            filename = "recRnew.hseq_HP";  //_fileName + timestamp;

            foreach (Vector3 hp in _headPositions)
            {
                lines.Add(hp.x.ToString()+","+hp.y.ToString()+","+hp.z.ToString());
            }
        

            File.WriteAllLines(_saveLocation+"/"+filename, lines);


            lines = new List<string>();



            filename = "recRnew.hseq_HR";  //_fileName + timestamp;

            foreach (Vector3 hr in _headRotations)
            {
                lines.Add(hr.x.ToString()+","+hr.y.ToString()+","+hr.z.ToString());
            }
        

            File.WriteAllLines(_saveLocation+"/"+filename, lines);

         



            debuggerLogger.text = "* EXPORTING FINISHED*";// + filename + " to " + _saveLocation + "\n not to " + Application.persistentDataPath;
  
            nr++;
        //}

        //debuggerLogger.text += "\n * SECOND IF STARTED *";
    }

    private void SwitchToPlaybackMode()
    {
    }

    internal HandSequence.SkeletonHandSequenceProvider SearchSkeletonDataProvider()
    {
        var oldProviders = gameObject.GetComponentsInParent<HandSequence.SkeletonHandSequenceProvider>();
        foreach (var dataProvider in oldProviders)
        {
            if (dataProvider.GetSkeletonType() == _skeletonType)
            {
                Debug.Log("Data provider found for Recorder");
                return dataProvider;
            }
        }

        return null;
    }
    internal MIDIDevice.MidiDataProvider SearchMidiDataProvider()
    {
        var oldProviders = gameObject.GetComponentsInParent<MIDIDevice.MidiDataProvider>();
        foreach (var dataProvider in oldProviders)
        {
            return dataProvider;
        }

        return null;
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

    // public void OnValidate()
    // {
    //     var skeleton = GetComponent<OVRSkeleton>();
    //     if (skeleton != null)
    //     {
    //         if (skeleton.GetSkeletonType() != _skeletonType)
    //         {
    //             MethodInfo setSkeletonTypeMethod = typeof(OVRSkeleton).GetMethod("SetSkeletonType",
    //                 BindingFlags.Instance | BindingFlags.NonPublic); // Access protected method
    //
    //             if (setSkeletonTypeMethod != null)
    //             {
    //                 setSkeletonTypeMethod.Invoke(skeleton, new object[] { _skeletonType });
    //             }
    //             else
    //             {
    //                 Debug.LogError("SetSkeletonType() method not found");
    //             }
    //         }
    //     }
    // }
}
