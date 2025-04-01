using System.Collections;
using System.Collections.Generic;
using Unity.XRTools.Rendering;
using UnityEngine;

public class RenderGuideLine : MonoBehaviour
{

    public bool shouldRender;

    private GameObject sphere1;
    private GameObject sphere2;

    private Vector3 sphere1pos = Vector3.zero;
    private Vector3 sphere2pos = new Vector3(10.0f,0.01f,10.0f);

    private XRLineRenderer _lr;
    public int numKeys = 17;
    private Vector3 _forwardVector;

    public float insetLength = 0.2f;
    public float offset = -0.001f;

    private ConfigurePhysicalKeyboard _configScript;
    private Material _defaultMaterial;
    private bool _useXRLineRenderer;

    // Start is called before the first frame update
    void Start()
    {

        _defaultMaterial = new Material(Shader.Find("Diffuse"));
        _configScript = GetComponent<ConfigurePhysicalKeyboard>();

        //sphere2 = _configScript.RightConfigSphere;
        //sphere1 = _configScript.LeftConfigSphere;

        _forwardVector = Vector3.Normalize(-Vector3.Cross(Vector3.up, sphere1pos - sphere2pos)) * insetLength;

        _lr = GetComponent<XRLineRenderer>();

        if (_lr == null)
        {
            Debug.LogError("No line renderer found");
        }

        //_lr.positionCount = (numKeys * 3)+1;
        _lr.SetVertexCount((numKeys * 3)+1);
        _lr.sharedMaterial = _defaultMaterial;

        /*

        Material redMaterial = new Material(Shader.Find("Standard")) { color = Color.red };
        Material yellowMaterial = new Material(Shader.Find("Standard")) { color = Color.yellow };


       sphere1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        sphere1.transform.position = sphere1pos;
        sphere2.transform.position = sphere2pos;

        sphere1.transform.localScale = Vector3.one * 0.05f;
        sphere2.transform.localScale = Vector3.one * 0.05f;

        sphere1.GetComponent<Renderer>().material = yellowMaterial;
        sphere2.GetComponent<Renderer>().material = redMaterial;

        sphere2.name = "right";
        sphere1.name = "left";
        */

    }

    public void reRender(){
        Vector3 a = _configScript.LeftConfigSphere.transform.position;
        Vector3 b = _configScript.RightConfigSphere.transform.position;
        Vector3 realDelta = (b - a);
        Vector3 delta = (b - a) *(1.0f-offset);
        float totalLength = delta.magnitude;
        _forwardVector = Vector3.Normalize(-Vector3.Cross(Vector3.up, delta)) * delta.magnitude  / numKeys;


        //_lr.SetPosition(0, currentPos);
        Vector3 startPos = a + (realDelta * offset/2);
        for(int i = 0; i < numKeys; i++){
            
            Vector3 currentPos = startPos + (i / (float)numKeys) * delta;
            Vector3 nextPos = startPos + ((i+1) / (float)numKeys) * delta;

            
            _lr.SetPosition((i*3), currentPos + _forwardVector);
            _lr.SetPosition((i*3)+1, currentPos);
            _lr.SetPosition((i*3)+2, nextPos);
        }
        _lr.SetPosition(numKeys*3, startPos + delta+ _forwardVector);
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldRender) {
            _lr.enabled = true;
            reRender();
        }else{
            _lr.enabled = false;
        }
        
    }
}
