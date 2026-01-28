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

using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using System.IO;
using UnityEngine.UI;

using System;

using UnityEngine;


namespace Meta.XR.MRUtilityKitSamples.QRCodeDetection
{
    [MetaCodeSample("MRUKSample-QRCodeDetection")]
    public class QRCodeManager : MonoBehaviour
    {
        //
        // Static interface

        public const string ScenePermission = OVRPermissionsRequester.ScenePermission;

        public static bool IsSupported
            => OVRAnchor.TrackerConfiguration.QRCodeTrackingSupported;

        public static bool HasPermissions
#if UNITY_EDITOR
            => true;
#else
            => UnityEngine.Android.Permission.HasUserAuthorizedPermission(ScenePermission);
#endif

        public static int ActiveTrackedCount
            => s_instance ? s_instance._activeCount : 0;

        public static bool TrackingEnabled
        {
            get => s_instance && s_instance._mrukInstance && s_instance._mrukInstance.SceneSettings.TrackerConfiguration.QRCodeTrackingEnabled;
            set
            {
                if (!s_instance || !s_instance._mrukInstance)
                {
                    return;
                }
                var config = s_instance._mrukInstance.SceneSettings.TrackerConfiguration;
                config.QRCodeTrackingEnabled = value;
                s_instance._mrukInstance.SceneSettings.TrackerConfiguration = config;
            }
        }


        public static void RequestRequiredPermissions(Action<bool> onRequestComplete)
        {
            if (!s_instance)
            {
                Debug.LogError($"{nameof(RequestRequiredPermissions)} failed; no QRCodeManager instance.");
                return;
            }

#if UNITY_EDITOR
            const string kCantRequestMsg =
                "Cannot request Android permission when using Link or XR Sim. " +
                "For Link, enable the spatial data permission from the Link app under Settings > Beta > Spatial Data over Meta Quest Link. " +
                "For XR Sim, no permission is necessary.";

            Log(kCantRequestMsg, LogType.Warning);

            onRequestComplete?.Invoke(HasPermissions);
#else
            Log($"Requesting {ScenePermission} ... (currently: {HasPermissions})");

            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += perm => Log($"{perm} granted");

            var msgDenied = $"{ScenePermission} denied. Please press the 'Request Permission' button again.";
            var msgDeniedPermanently = $"{ScenePermission} permanently denied. To enable:\n" +
                                       $"    1. Uninstall and reinstall the app, OR\n" +
                                       $"    2. Manually grant permission in device Settings > Privacy & Safety > App Permissions.";

#if !UNITY_6000_0_OR_NEWER
            callbacks.PermissionDenied += _ => Log(msgDenied, LogType.Error);
            callbacks.PermissionDeniedAndDontAskAgain += _ => Log(msgDeniedPermanently, LogType.Error);
#else
            callbacks.PermissionDenied += perm =>
            {
                // ShouldShowRequestPermissionRationale returns false only if
                // the user selected 'Never ask again' or if the user has never
                // been asked for the permission (which can't be the case here).
                Log(
                    UnityEngine.Android.Permission.ShouldShowRequestPermissionRationale(perm)
                        ? msgDenied
                        : msgDeniedPermanently,
                    LogType.Error);
            };
#endif // UNITY_6000_0_OR_NEWER

            if (onRequestComplete is not null)
            {
                callbacks.PermissionGranted += _ => onRequestComplete(HasPermissions);
                callbacks.PermissionDenied += _ => onRequestComplete(HasPermissions);
#if !UNITY_6000_0_OR_NEWER
                callbacks.PermissionDeniedAndDontAskAgain += _ => onRequestComplete(HasPermissions);
#endif // UNITY_6000_0_OR_NEWER
            }

            UnityEngine.Android.Permission.RequestUserPermission(ScenePermission, callbacks);
#endif // UNITY_EDITOR
        }


        //
        // Serialized fields

        [SerializeField]
        GameObject _qrCodeInstruction;

        [SerializeField]
        GameObject _rightHandClone;

        [SerializeField]
        Text _debugTextField;

        [SerializeField]
        QRCode _qrCodePrefab;

        [SerializeField]
        QRCodeSampleUI _uiInstance;

        [SerializeField]
        MRUK _mrukInstance;

        // non-serialized fields

        int _activeCount;

        static QRCodeManager s_instance;


        //
        // MonoBehaviour messages

        void OnValidate()
        {
            if (!_uiInstance && FindAnyObjectByType<QRCodeSampleUI>() is { } ui && ui.gameObject.scene == gameObject.scene)
            {
                _uiInstance = ui;
            }
            if (!_mrukInstance && FindAnyObjectByType<MRUK>() is { } mruk && mruk.gameObject.scene == gameObject.scene)
            {
                _mrukInstance = mruk;
            }
        }

        void OnEnable()
        {
            s_instance = this;

            if (!_mrukInstance)
            {
                Log($"{nameof(QRCodeManager)} requires an MRUK object in the scene!", LogType.Error);
                return;
            }

            _mrukInstance.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
            _mrukInstance.SceneSettings.TrackableRemoved.AddListener(OnTrackableRemoved);
        }

        void OnDestroy()
            => s_instance = null;


        //
        // UnityEvent listeners

        public void OnTrackableAdded(MRUKTrackable trackable)
        {
            if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            {
                return;
            }

            var log = $"{nameof(OnTrackableAdded)}: QRCode tracked!\nUUID={trackable.Anchor.Uuid}";

            



            //var instance2 = Instantiate(_qrCodeInstruction, trackable.transform);
            var instance = Instantiate(_qrCodePrefab, trackable.transform);

            

            var qrCode = instance.GetComponent<QRCode>();
            qrCode.Initialize(trackable);
            instance.GetComponent<Bounded2DVisualizer>().Initialize(trackable);

            ++_activeCount;

            Log($"{log}\nPayload={qrCode.PayloadText}");

            //string machineName = qrCode.PayloadText;

            _debugTextField.text = " QRCode:" + qrCode.PayloadText;    /// qr code text 
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// rendering and positioning hand instructions:

            if (qrCode.PayloadText=="\"Neri Marchesini BL400\"")   //quotes \" \" are mandatory!!
            {
                //_debugTextField.text = "are equal";

                _qrCodeInstruction.transform.parent = instance.transform;

                _qrCodeInstruction.transform.localPosition = _qrCodePrefab.transform.localPosition;
                _qrCodeInstruction.transform.localPosition = new Vector3(_qrCodeInstruction.transform.localPosition.x, _qrCodePrefab.transform.localPosition.y + 0.3f, _qrCodeInstruction.transform.localPosition.z);

                _qrCodeInstruction.transform.localEulerAngles = _qrCodePrefab.transform.localEulerAngles;
                //_qrCodeInstruction.transform.localEulerAngles = new Vector3(_qrCodePrefab.transform.localEulerAngles.x+90f, _qrCodeInstruction.transform.localEulerAngles.y, _qrCodeInstruction.transform.localEulerAngles.z);    //slight offset in yaw
                _qrCodeInstruction.transform.localEulerAngles = new Vector3(86.2526779f,75.8418579f,76.0955505f);

                _qrCodeInstruction.gameObject.SetActive(true);


                

                _rightHandClone.transform.localPosition = _qrCodePrefab.transform.localPosition;
                //_rightHandClone.transform.localPosition = new Vector3(_rightHandClone.transform.localPosition.x + , _qrCodePrefab.transform.localPosition.y + 0.3f, _rightHandClone.transform.localPosition.z);

                _rightHandClone.transform.localEulerAngles = _qrCodePrefab.transform.localEulerAngles;

                _rightHandClone.transform.parent = instance.transform;
                
                _rightHandClone.gameObject.SetActive(true);

            }

            //_debugTextField.text = " QRCode:" + qrCode.PayloadText;     /// qr code text

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        }

        public void OnTrackableRemoved(MRUKTrackable trackable)
        {
            if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            {
                return;
            }

            Log($"{nameof(OnTrackableRemoved)}: {trackable.Anchor.Uuid.ToString("N").Remove(8)}[..]");

            --_activeCount;

            Destroy(trackable.gameObject);
        }


        //
        // private impl.

        static void Log(object msg, LogType type = LogType.Log)
        {
            if (s_instance && s_instance._uiInstance)
            {
                s_instance._uiInstance.Log(msg, type);
            }
            else
            {
                Debug.LogFormat(
                    logType: type,
                    logOptions: LogOption.None,
                    context: s_instance,
                    format: "{0}(noinst): {1}", nameof(QRCodeManager), msg
                );
            }
        }

    }
}
