using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Meta.WitAi;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

/// <summary>
/// Skeleton Renderer, made for rendering handframes. Can be used with OVRSkeleton, but it has to be converted to handframes first.
/// </summary>
public class SkeletonRenderer : MonoBehaviour
{
    private HandSequence.SkeletonHandSequenceProvider _dataProvider;
    
    [SerializeField]
    private OVRSkeleton.SkeletonType _skeletonType;
    
    private List<BoneVisualization> _boneVisualizations;

    private bool _isRendering = false;
    
    private static readonly Quaternion _capsuleRotationOffset = Quaternion.Euler(90, 0, 0);

    public static GameObject _handGO;
    
    void Start()
    {
        _dataProvider = SearchSkeletonDataProvider();
        if(_dataProvider == null){ Debug.Log("No data provider for renderer");
            return;
        }
    }

    private class BoneVisualization
    {
        private Vector3 _bonePosition;
        private BoneVisualization _parent;
        private Vector3 _delta;
        public OVRHandData.ovrHandEnum ID;
        
        public Vector3 BonePosition { 
            get => _bonePosition;
            set { _bonePosition = value; }
        }
        private Quaternion _boneRotation;
        public Quaternion BoneRotation { 
            get => _boneRotation;
            set { _boneRotation = value; }
        }
        
        private GameObject jointGO;
        private GameObject boneGO;

        private Material _material;

        public bool ShouldRender { get; set; }
        public BoneVisualization(int index, BoneVisualization parent, Vector3 refPosition, BoneVisualization wrist)
        {
            ID = OVRHandData.GetJointIDfromIndex(index);
            _parent = parent;
            _bonePosition = refPosition;
            
            jointGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jointGO.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
            jointGO.transform.SetParent(_handGO.transform, true);

            boneGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            boneGO.name = ("bone-" + ID.ToString());

            boneGO.transform.SetParent(_handGO.transform, true);

            _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));   
            _material.color = OVRHandData.jointsCustom[index].Color;
            boneGO.GetComponent<Renderer>().material = _material;

            float bone_length = 1.0f;
            if (parent != null) {
                _delta = _bonePosition - _parent.BonePosition;
                bone_length = _delta.magnitude;
            }
            
            boneGO.transform.localScale = new Vector3(0.002f, bone_length/2, 0.002f);
        }

        public void Update()
        {
            if (_parent != null)
            {
                _delta = _bonePosition - _parent.BonePosition;
                boneGO.transform.position = BonePosition - _delta/2;
                boneGO.transform.localRotation = BoneRotation * _capsuleRotationOffset;
                boneGO.SetActive(ShouldRender);
            }
            
            jointGO.transform.localPosition = BonePosition;
            
            jointGO.SetActive(ShouldRender);
            if (!ShouldRender) return;
        }

        public void DestroyVis()
        {
            Destroy(jointGO);
            if (_parent != null)
            {
                Destroy(boneGO);
            }
        }
    }

    private void Initialize()
    {
        _isRendering = true;
        _boneVisualizations = new List<BoneVisualization>();
        HandSequence.HandFrame data = _dataProvider.GetHandFrameData();
        _handGO = new GameObject();

        
        for (int i = 0; i < data.BoneTranslations.Length; i++)
        {
            OVRHandData.ovrHandEnum parent = OVRHandData.jointsCustom[i].Parent;

            _boneVisualizations.Add(new BoneVisualization(
            i, 
            parent!=OVRHandData.ovrHandEnum.Invalid?_boneVisualizations[OVRHandData.GetJointIndex(parent)]:null, 
            data.BoneTranslations[i],
            _boneVisualizations.Count>1?_boneVisualizations[1]:null
            ));
        }
        
    }

    private void clearVisualization()
    {
        foreach (var boneVis in _boneVisualizations)
        {
            boneVis.DestroyVis();
        }
        _boneVisualizations.Clear();
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
    
    void Update()
    {
        if (_dataProvider == null) return;
        HandSequence.HandFrame data = _dataProvider.GetHandFrameData();
        
        if (data == null || !data.IsDataValid || !data.IsDataHighConfidence)
        {
            if (_isRendering)
            {
                _isRendering = false;
                clearVisualization();
            }
            return;
        }

        if (!_isRendering)
        {
            Initialize();
        }

        OVRPlugin.Vector3f handPosition = new OVRPlugin.Vector3f{x = data.RootPose.Position.x, y = data.RootPose.Position.y, z = data.RootPose.Position.z};
        OVRPlugin.Quatf handRotation = new OVRPlugin.Quatf{x = data.RootPose.Orientation.x, y = data.RootPose.Orientation.y, z = data.RootPose.Orientation.z, w = data.RootPose.Orientation.w};
        //Quaternion rot = handRotation.FromFlippedZQuatf();
        //Vector3 pos = handPosition.FromFlippedZVector3f();

        //_handGO.transform.localRotation = rot;
        //_handGO.transform.localPosition = pos;
        //_handGO.transform.localScale = Vector3.one * data.RootScale;

        

        for (int i = 0; i < _boneVisualizations.Count; i++)
        {
            //int bone_i = i==0?1:(i==1?0:i);
            _boneVisualizations[i].BonePosition = data.BoneTranslations[i];
            _boneVisualizations[i].BoneRotation = data.BoneRotations[i];
            _boneVisualizations[i].ShouldRender = data.IsDataValid;
            _boneVisualizations[i].Update();
        }
    }
    
}
