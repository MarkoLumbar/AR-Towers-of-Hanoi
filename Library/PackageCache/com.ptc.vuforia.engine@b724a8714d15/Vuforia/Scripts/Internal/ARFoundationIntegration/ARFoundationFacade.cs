/*===============================================================================
Copyright (c) 2024 PTC Inc. and/or Its Subsidiary Companies. All Rights Reserved.

Confidential and Proprietary - Protected under copyright and other laws.
Vuforia is a trademark of PTC Inc., registered in the United States and other 
countries.
===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Vuforia.ARFoundation;
using Vuforia.Internal.VuforiaDriver;

#if UNITY_XR_ARFOUNDATION
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
#endif

namespace Vuforia.UnityRuntimeCompiled
{
    public static class ARFoundationInitializer
    {
        static OpenSourceARFoundationFacade sFacade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void OnAfterAssembliesLoaded()
        {
            InitializeFacade();
        }

        public static void InitializeFacade()
        {
            if (sFacade != null) return;

            sFacade = new OpenSourceARFoundationFacade();
            ARFoundationFacade.Instance = sFacade;
        }
    }

    class OpenSourceARFoundationFacade : IARFoundationFacade
    {
        bool mIsAnchorSupported;
#if UNITY_XR_ARFOUNDATION
        ARCameraManager   mCameraManager;
        ARAnchorManager   mAnchorManager;
        ARSession         mSession;
#if UNITY_2022_2_OR_NEWER
        XROrigin mSessionOrigin;
#else
        ARSessionOrigin   mSessionOrigin;
#endif
        ARRaycastManager  mRaycastManager;

        Dictionary<string, ARAnchor> mAnchors = new Dictionary<string, ARAnchor>();
#endif
        public event Action<ARFoundationImage> ARFoundationImageEvent = image => { };
        public event Action<Transform, long> ARFoundationPoseEvent = (pose, timestamp) => { };

        public event Action<List<Tuple<string, Transform>>, List<Tuple<string, Transform>>> AnchorsChangedEvent = (removed, updated) => {};

        public bool IsAnchorSupported => mIsAnchorSupported;

        public bool IsARFoundationScene()
        {
#if UNITY_XR_ARFOUNDATION
            var arSession = GameObject.FindObjectOfType<ARSession>(true);
            return arSession != null;
#else
            return false;
#endif
        }

        public IEnumerator CheckAvailability()
        {
#if UNITY_XR_ARFOUNDATION
            var anchorDescriptors = new List<XRAnchorSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(anchorDescriptors);
            mIsAnchorSupported = anchorDescriptors.Count > 0;

            yield return ARSession.CheckAvailability();
#else
            yield break;
#endif
        }

        public void Init()
        {
#if UNITY_XR_ARFOUNDATION
            mCameraManager = GameObject.FindObjectOfType<ARCameraManager>(true);
            mSession = GameObject.FindObjectOfType<ARSession>(true);
#if UNITY_2022_2_OR_NEWER
            mSessionOrigin = GameObject.FindObjectOfType<XROrigin>(true);
#else
            mSessionOrigin = GameObject.FindObjectOfType<ARSessionOrigin>(true);
#endif
            mRaycastManager = GameObject.FindObjectOfType<ARRaycastManager>(true);

            if (mIsAnchorSupported)
            {
                mAnchorManager = mSessionOrigin.GetComponent<ARAnchorManager>();
                if (mAnchorManager == null)
                    mAnchorManager = mSessionOrigin.gameObject.AddComponent<ARAnchorManager>();
                mAnchorManager.anchorsChanged += OnAnchorsChanged;
            }
            UnityEngine.Application.onBeforeRender += UpdateStateFromARFoundationFrame;
#endif
        }

        public void Deinit()
        {
            // ClearAnchors();
#if UNITY_XR_ARFOUNDATION
            mSession.Reset();
            if (mIsAnchorSupported)
                mAnchorManager.anchorsChanged -= OnAnchorsChanged;
            UnityEngine.Application.onBeforeRender -= UpdateStateFromARFoundationFrame;
#endif
        }

        public IEnumerator WaitForCameraReady()
        {
#if UNITY_XR_ARFOUNDATION
            var waitForEndOfFrame = new WaitForEndOfFrame();
            while (mCameraManager == null || mCameraManager.subsystem == null || !mCameraManager.subsystem.running ||
                !mCameraManager.permissionGranted)
            {
                yield return waitForEndOfFrame;
            }
#else
            yield break;
#endif
        }

        public bool IsARFoundationReady()
        {
#if UNITY_XR_ARFOUNDATION
            return ARSession.state >= ARSessionState.Ready;
#else
            return false;
#endif
        }

        public Transform GetCameraTransform()
        {
#if UNITY_XR_ARFOUNDATION
            if (!mCameraManager)
                mCameraManager = GameObject.FindObjectOfType<ARCameraManager>(true);
            return mCameraManager.transform;
#else
            return null;
#endif
        }

        public List<DriverCameraMode> GetProfiles()
        {
            var profiles = new List<DriverCameraMode>();
#if UNITY_XR_ARFOUNDATION
            using (var configurations = mCameraManager.GetConfigurations(Allocator.Temp))
            {
                if (!configurations.IsCreated || configurations.Length <= 0)
                    return profiles;

                foreach (var configuration in configurations)
                {
                    profiles.Add(new DriverCameraMode
                    (
                        configuration.width,
                        configuration.height,
                        configuration.framerate ?? 30,
#if UNITY_IOS
                        DriverPixelFormat.NV12
#elif UNITY_ANDROID
                        DriverPixelFormat.NV21
#else
                        DriverPixelFormat.UNKNOWN
#endif
                    ));
                }
            }
#endif
            return profiles;
        }

        public bool SelectProfile(DriverCameraMode profile)
        {
#if UNITY_XR_ARFOUNDATION
            using (var configurations = mCameraManager.GetConfigurations(Allocator.Temp))
            {
                if (!configurations.IsCreated || configurations.Length <= 0)
                    return false;

                var configs = new SortedDictionary<int, List<XRCameraConfiguration>>();
                foreach (var configuration in configurations)
                {
                    var framerate = configuration.framerate ?? 30;
                    if (!configs.ContainsKey(framerate))
                        configs.Add(framerate, new List<XRCameraConfiguration>());
                    configs[framerate].Add(configuration);
                }

                var selectedConfiguration = configs[profile.Fps]
                    .First(x => x.width == profile.Width && x.height == profile.Height);

                if (mCameraManager.currentConfiguration != selectedConfiguration)
                    mCameraManager.currentConfiguration = selectedConfiguration;
            }
            return true;
#else
            return false;
#endif
        }

#if UNITY_XR_ARFOUNDATION
        private void UpdateStateFromARFoundationFrame()
        {
            if (!mCameraManager.TryGetIntrinsics(out var cameraIntrinsics))
                return;
            if (!mCameraManager.TryAcquireLatestCpuImage(out var cameraImage))
                return;

            var timestamp = (long)(cameraImage.timestamp * 1000000000);
            ARFoundationPoseEvent(mCameraManager.transform, timestamp);

            var image = new ARFoundationImage(
                cameraImage.dimensions,
                cameraImage.GetPlane(0).data,
                cameraImage.GetPlane(1).data,
#if UNITY_ANDROID
                cameraImage.GetPlane(2).data,
#else
                new NativeArray<byte>(new byte[0], Allocator.None),
#endif
                cameraImage.GetPlane(0).rowStride,
                cameraImage.GetPlane(1).rowStride,
                cameraImage.GetPlane(1).pixelStride,
                timestamp,
                cameraIntrinsics.principalPoint,
                cameraIntrinsics.focalLength
            );

            ARFoundationImageEvent.Invoke(image);
            cameraImage.Dispose();
        }

        void OnAnchorsChanged(ARAnchorsChangedEventArgs eventArgs)
        {
            var removed = new List<Tuple<string, Transform>>();
            foreach (var anchor in eventArgs.removed)
            {
                var uuid = anchor.trackableId.ToString();
                if (mAnchors.ContainsKey(uuid))
                {
                    removed.Add(Tuple.Create(uuid,anchor.transform));
                    mAnchors.Remove(uuid);
                }
            }
            var updated = new List<Tuple<string, Transform>>();
            foreach (var anchor in eventArgs.updated)
            {
                var uuid = anchor.trackableId.ToString();
                if (mAnchors.ContainsKey(uuid))
                {
                    updated.Add(Tuple.Create(uuid,anchor.transform));
                }
            }
            AnchorsChangedEvent.Invoke(removed, updated);
        }
#endif

        public string AddAnchor(Pose pose)
        {
#if UNITY_XR_ARFOUNDATION
            var anchorGameObject = new GameObject("VuforiaAnchor");
            anchorGameObject.transform.position = pose.position;
            anchorGameObject.transform.rotation = pose.rotation;
            var anchor = anchorGameObject.AddComponent<ARAnchor>();
            if (anchor == null) return null;

            var id = anchor.trackableId.ToString();
            mAnchors[id] = anchor;
            return id;
#else
            return null;
#endif
        }

        public bool RemoveAnchor(string uuid)
        {
#if UNITY_XR_ARFOUNDATION
            if (mAnchors.ContainsKey(uuid))
            {
                if (mAnchors[uuid] != null && mAnchorManager.subsystem != null && mAnchorManager.subsystem.running)
                    UnityEngine.Object.Destroy(mAnchors[uuid]);
                mAnchors.Remove(uuid);
            }
            return true;
#else
            return false;
#endif
        }

        public void ClearAnchors()
        {
#if UNITY_XR_ARFOUNDATION
            foreach (var anchor in mAnchors)
                UnityEngine.Object.Destroy(anchor.Value);
            mAnchors.Clear();
#endif
        }

        public bool HitTest(Vector2 screenPoint, out List<Pose> hitPoses)
        {
#if UNITY_XR_ARFOUNDATION
            var hits = new List<ARRaycastHit>();
            var hitSuccess = mRaycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon);
            hitPoses = hits.ConvertAll(hit => hit.pose);
            return hitSuccess;
#else
            hitPoses = new List<Pose>();
            return false;
#endif
        }
    }
}

