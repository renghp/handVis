using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;


public class recordAsVideo : MonoBehaviour
{
    // Start is called before the first frame update

    //public RawImage ri;

    public Text debuggerLogger;
    private RenderTexture texFromFeed;

    Texture2D uncompTex;

    private bool isRecording;


    [Header("Recording Settings")]
    public int captureFPS;
    public string folderName = "ImageSequence";

    private int frameIndex;
    private float frameTimer;


    void Start()
    {
        texFromFeed = gameObject.GetComponent<RawImage>().texture as RenderTexture;

        isRecording = false;


    }

    // Update is called once per frame
    void Update()
    {
        if (isRecording)
        {
           // Debug.Log("recording");
           // debuggerLogger.text += "\nrecording";

            frameTimer += Time.deltaTime;

            if (frameTimer >= 1f / captureFPS)
            {
                frameTimer -= 1f / captureFPS;

                //this def needs compressing
                RenderTexture.active = texFromFeed;
                uncompTex.ReadPixels(new Rect(0, 0, texFromFeed.width, texFromFeed.height), 0, 0);
                uncompTex.Apply();
                RenderTexture.active = null;


                CaptureFrame();
            }
        }
    }

    public void startRecording()
    {

        //Debug.Log("videoRecorder called ");
      debuggerLogger.text = "STARTED Recording";


        texFromFeed = gameObject.GetComponent<RawImage>().texture as RenderTexture;

        uncompTex = new Texture2D(texFromFeed.width, texFromFeed.height, TextureFormat.RGB24, false); 


      // debuggerLogger.text += "\ntexture gotten";

        frameIndex = 0;
        isRecording = true;
        frameTimer = 0f;

        string path = GetSavePath();
        Directory.CreateDirectory(path);

       // debuggerLogger.text += "\n Recording image sequence to: " + path;

        //Debug.Log("Recording image sequence to: " + path);
    }

    public void stopRecording()
    {
         isRecording = false;
         debuggerLogger.text = "STOPPED Recording";
    }

    void CaptureFrame()
    {

        //debuggerLogger.text+="\nsaving frame";
        //Debug.Log("saving frame");
        //byte[] png = ImageConversion.EncodeToPNG(uncompTex);        //would another format be less hassle?
        byte[] jpg = ImageConversion.EncodeToJPG(uncompTex, 50);         //50% quality jpg

        //debuggerLogger.text+="\nframe encoded";

        string filePath = Path.Combine(
            GetSavePath(),
           // $"frame_{frameIndex:D05}.png"
           // $"frame_{frameIndex}.png"
            $"frame_{frameIndex}.jpg"
        );

        //debuggerLogger.text+="\nwriting frame";
        //Debug.Log("writing frame");

        File.WriteAllBytes(filePath, jpg);
        frameIndex++;
        //debuggerLogger.text+="\nframe written";
        //Debug.Log("frame written");
    }

    string GetSavePath()
    {
        
        #if UNITY_ANDROID && !UNITY_EDITOR
                //debuggerLogger.text+="\nsaving path is:" + Path.Combine(Application.persistentDataPath, folderName);
                return Path.Combine(Application.persistentDataPath, folderName);
        #else
                //debuggerLogger.text+="\nsaving path is:" + Path.Combine(Application.dataPath, folderName);
                return Path.Combine(Application.dataPath, folderName);
        #endif
    }

}
