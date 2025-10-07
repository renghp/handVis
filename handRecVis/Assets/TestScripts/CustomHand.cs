/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;


public class CustomHand : MonoBehaviour,
    /*OVRInputModule.InputSource,*/
    OVRSkeleton.IOVRSkeletonDataProvider,
    OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider,
    OVRMesh.IOVRMeshDataProvider,
    OVRMeshRenderer.IOVRMeshRendererDataProvider
{

    OVRSkeleton.SkeletonPoseData recordedPoseData;

    /// <summary>
    /// This enum dictates if a hand is a left or right hand. It's used in many scenarios such as choosing which hand
    /// mesh to return to <see cref="OVRMesh"/>, which skeleton to return, etc.
    /// </summary>
    public enum Hand
    {
        None = OVRPlugin.Hand.None,
        HandLeft = OVRPlugin.Hand.HandLeft,
        HandRight = OVRPlugin.Hand.HandRight,
    }

    /// <summary>
    /// This enum is used for clarifying which finger you are currently working with or need data on.
    /// For example, you can pass "HandFinger.Ring" to <see cref="GetFingerIsPinching(HandFinger)"/> to check if the
    /// ring finger is pinching.
    /// </summary>
    public enum HandFinger
    {
        Thumb = OVRPlugin.HandFinger.Thumb,
        Index = OVRPlugin.HandFinger.Index,
        Middle = OVRPlugin.HandFinger.Middle,
        Ring = OVRPlugin.HandFinger.Ring,
        Pinky = OVRPlugin.HandFinger.Pinky,
        Max = OVRPlugin.HandFinger.Max,
    }


    [SerializeField]
    internal Hand HandType = Hand.None;

    private static OVRHandSkeletonVersion GlobalHandSkeletonVersion =>
            OVRRuntimeSettings.Instance.HandSkeletonVersion;

    OVRSkeleton.SkeletonType OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonType()
    {
        switch (HandType)
        {
            case Hand.HandLeft:
                return GlobalHandSkeletonVersion switch
                {
                    OVRHandSkeletonVersion.OVR => OVRSkeleton.SkeletonType.HandLeft,
                    OVRHandSkeletonVersion.OpenXR => OVRSkeleton.SkeletonType.XRHandLeft,
                    _ => OVRSkeleton.SkeletonType.None
                };
            case Hand.HandRight:
                return GlobalHandSkeletonVersion switch
                {
                    OVRHandSkeletonVersion.OVR => OVRSkeleton.SkeletonType.HandRight,
                    OVRHandSkeletonVersion.OpenXR => OVRSkeleton.SkeletonType.XRHandRight,
                    _ => OVRSkeleton.SkeletonType.None
                };
            case Hand.None:
            default:
                return OVRSkeleton.SkeletonType.None;
        }
    }

    OVRSkeleton.SkeletonPoseData OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonPoseData()
    {
        var data = new OVRSkeleton.SkeletonPoseData();
        /*data.IsDataValid = IsDataValid;
        if (IsDataValid)
        {
            data.RootPose = _handState.RootPose;
            data.RootScale = _handState.HandScale;
            data.BoneRotations = _handState.BoneRotations;
            data.BoneTranslations = _handState.BonePositions;
            data.IsDataHighConfidence = IsTracked && HandConfidence == TrackingConfidence.High;
        }*/

        return data;
    }

    /// <summary>
    /// Returns a <see cref="OVRSkeletonRenderer.SkeletonRendererData"/> associated with this hand's state.
    /// You can use the SkeletonRendererData to verify the validity/confidence/scale of the data you are receiving from the hand.
    /// </summary>
    OVRSkeletonRenderer.SkeletonRendererData OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider.
        GetSkeletonRendererData()
    {
        var data = new OVRSkeletonRenderer.SkeletonRendererData();

        /*data.IsDataValid = IsDataValid;
        if (IsDataValid)
        {
            data.RootScale = _handState.HandScale;
            data.IsDataHighConfidence = IsTracked && HandConfidence == TrackingConfidence.High;
            data.ShouldUseSystemGestureMaterial = IsSystemGestureInProgress;
        }*/

        return data;
    }




    /// <summary>
    /// Returns the mesh type associated with this Hand's type.
    /// For example, if the hand type is "HandLeft", the mesh type returned would be "HandLeft".
    /// </summary>
    OVRMesh.MeshType OVRMesh.IOVRMeshDataProvider.GetMeshType()
    {
        switch (HandType)
        {
            case Hand.None:
                return OVRMesh.MeshType.None;
            case Hand.HandLeft:
                return GlobalHandSkeletonVersion switch
                {
                    OVRHandSkeletonVersion.OVR => OVRMesh.MeshType.HandLeft,
                    OVRHandSkeletonVersion.OpenXR => OVRMesh.MeshType.XRHandLeft,
                    _ => OVRMesh.MeshType.None
                };
            case Hand.HandRight:
                return GlobalHandSkeletonVersion switch
                {
                    OVRHandSkeletonVersion.OVR => OVRMesh.MeshType.HandRight,
                    OVRHandSkeletonVersion.OpenXR => OVRMesh.MeshType.XRHandRight,
                    _ => OVRMesh.MeshType.None
                };
            default:
                return OVRMesh.MeshType.None;
        }
    }

    /// <summary>
    /// Returns a <see cref="OVRMeshRenderer.MeshRendererData"/> associated with this hand's state. You can
    /// use MeshRendererData to verify the validity/confidence of the data you are receiving from the hand.
    /// </summary>
    OVRMeshRenderer.MeshRendererData OVRMeshRenderer.IOVRMeshRendererDataProvider.GetMeshRendererData()
    {
        var data = new OVRMeshRenderer.MeshRendererData();

        /*data.IsDataValid = IsDataValid;
        if (IsDataValid)
        {
            data.IsDataHighConfidence = IsTracked && HandConfidence == TrackingConfidence.High;
            data.ShouldUseSystemGestureMaterial = IsSystemGestureInProgress;
        }*/

        return data;
    }

    public void OnEnable()
    {
        /*OVRInputModule.TrackInputSource(this);
        SceneManager.activeSceneChanged += OnSceneChanged;
        if (RayHelper && ShouldShowHandUIRay())
        {
            RayHelper.gameObject.SetActive(true);
        }*/
    }

    public void OnDisable()
    {
        /*OVRInputModule.UntrackInputSource(this);
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (RayHelper)
        {
            RayHelper.gameObject.SetActive(false);
        }*/
    }


    /*public void OnValidate()
    {
#if UNITY_EDITOR
        if (!Meta.XR.Editor.Callbacks.InitializeOnLoad.EditorReady)
        {
            return;
        }
#endif
        // Verify that all hand side based components on this object are using the same hand side.
        var skeleton = GetComponent<OVRSkeleton>();
        if (skeleton != null)
        {
            if (skeleton.GetSkeletonType() != HandType.AsSkeletonType(GlobalHandSkeletonVersion))
            {
                skeleton.SetSkeletonType(HandType.AsSkeletonType(GlobalHandSkeletonVersion));
            }
        }

        var mesh = GetComponent<OVRMesh>();
        if (mesh != null)
        {
            if (mesh.GetMeshType() != HandType.AsMeshType(GlobalHandSkeletonVersion))
            {
                mesh.SetMeshType(HandType.AsMeshType(GlobalHandSkeletonVersion));
            }
        }
    }
    */



    /// <summary>
    /// True when this object is not null. This is different from <see cref="IsDataValid"/>, which refers to the validity
    /// of the data itself.
    /// </summary>
    public bool IsValid()
    {
        return this != null;
    }

    /// <summary>
    /// Returns the type of this Hand (left or right), see <see cref="Hand"/> for more info.
    /// </summary>
    /// <returns></returns>
    public OVRPlugin.Hand GetHand()
    {
        return (OVRPlugin.Hand)HandType;
    }


}
