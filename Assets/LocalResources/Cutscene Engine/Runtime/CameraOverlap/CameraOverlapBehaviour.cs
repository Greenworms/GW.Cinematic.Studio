#if UNITY_EDITOR
using UnityEditor;
#endif
#if CINEMACHINE_3_OR_NEWER
using Unity.Cinemachine;
#elif CINEMACHINE
using Cinemachine;
#endif
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace CutsceneEngine
{
    public class CameraOverlapBehaviour : PlayableBehaviour
    {
        static readonly AnimationCurve _defaultCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        public int sortingOrder;
        public bool followCam;
        public AnimationCurve opacityCurve;

        float blend;
        RenderTexture rt;
        Camera _cutsceneCamera;
        Camera _overlapCamera;
        Canvas _overlapCanvas;
        RawImage _overlay;
        
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (info.effectiveWeight <= 0)
            {
                Release();
            }
        }

        public override void OnGraphStop(Playable playable)
        {
            Release();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _cutsceneCamera = playerData as Camera;
            Initialize();
            
            if(!_cutsceneCamera) return;
            
            var normalizedTime = (float)(playable.GetTime() / playable.GetDuration());
            blend = GetCurve(this).Evaluate(normalizedTime);
            _overlay.color = new Color(1, 1, 1, blend);

            if (followCam)
            {
                _overlapCamera.transform.SetPositionAndRotation(_cutsceneCamera.transform.position, _cutsceneCamera.transform.rotation);
                _overlapCamera.fieldOfView = _cutsceneCamera.fieldOfView;
                
                _overlapCamera.sensorSize = _cutsceneCamera.sensorSize;
                _overlapCamera.iso = _cutsceneCamera.iso;
                _overlapCamera.shutterSpeed = _cutsceneCamera.shutterSpeed;
                _overlapCamera.gateFit = _cutsceneCamera.gateFit;
                
                _overlapCamera.focalLength = _cutsceneCamera.focalLength;
                _overlapCamera.lensShift = _cutsceneCamera.lensShift;
                _overlapCamera.aperture = _cutsceneCamera.aperture;
                _overlapCamera.focusDistance = _cutsceneCamera.focusDistance;
                
                _overlapCamera.bladeCount = _cutsceneCamera.bladeCount;
                _overlapCamera.curvature = _cutsceneCamera.curvature;
                _overlapCamera.barrelClipping = _cutsceneCamera.barrelClipping;
                _overlapCamera.anamorphism = _cutsceneCamera.anamorphism;
            }
        }

        void Initialize()
        {
            if (!_cutsceneCamera)
            {
                Debug.LogWarning("Cannot find current Camera");
                return;
            }
            
            if(!rt)
            {
                rt = new RenderTexture(_cutsceneCamera.pixelWidth, _cutsceneCamera.pixelHeight, 32);
                rt.hideFlags = HideFlags.HideAndDontSave;
            }
            
            if (!_overlapCamera)
            {
                _overlapCamera = Object.Instantiate(_cutsceneCamera);
                _overlapCamera.tag = "Untagged";
                _overlapCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
#if CINEMACHINE
                if(_overlapCamera.TryGetComponent(out CinemachineBrain brain)) CutsceneEngineUtility.SmartDestroy(brain); 
#endif
                if(_overlapCamera.TryGetComponent(out AudioListener listener)) CutsceneEngineUtility.SmartDestroy(listener);
                _overlapCamera.enabled = true;
                _overlapCamera.targetTexture = rt;
            }
            
            if (!_overlapCanvas)
            {
                var go = new GameObject("Camera Overlap");
                go.hideFlags = HideFlags.HideAndDontSave;
                _overlapCanvas = go.AddComponent<Canvas>();
                _overlapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                _overlay = go.AddComponent<RawImage>();
                _overlay.rectTransform.anchorMin = Vector2.zero;
                _overlay.rectTransform.anchorMax = Vector2.one;
                _overlay.rectTransform.sizeDelta = Vector2.zero;
                _overlay.color = new Color(1, 1, 1, 0);
            }
            
            _overlapCanvas.sortingOrder = sortingOrder;
            _overlay.texture = rt;
        }

        void Release()
        {
            if (_overlapCamera)
            {
                _overlapCamera.targetTexture = null;  // targetTexture를 먼저 해제하지 않으면 RenderTexture를 제거할때 오류가 발생함.
                CutsceneEngineUtility.SmartDestroy(_overlapCamera.gameObject);
            }
            
            if (rt)
            {
                rt.Release();
                CutsceneEngineUtility.SmartDestroy(rt);
            }
            if (_overlapCanvas)
            {
                CutsceneEngineUtility.SmartDestroy(_overlapCanvas.gameObject);
            }

        }
        
        static AnimationCurve GetCurve(CameraOverlapBehaviour behaviour)
        {
            if (behaviour?.opacityCurve == null) return _defaultCurve;
            return behaviour.opacityCurve;
        }
    }
}