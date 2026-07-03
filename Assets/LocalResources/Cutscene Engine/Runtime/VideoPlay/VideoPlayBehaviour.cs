using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CutsceneEngine
{
    public class VideoPlayBehaviour : PlayableBehaviour
    {
        public VideoPlayer videoPlayer => _videoPlayer;

        public GameObject owner;
        public VideoClip video;
        public bool loop;
        public VideoAspectRatio aspectRatio;

        public VideoRenderTarget renderTarget;
        public int sortingOrder;
        public RenderTexture renderTexture;

        public VideoAudioOutputTarget audioOutputTarget;
        public AudioSource audioSource;

        public float audioVolume;

        bool _initialized;
        double _lastTime;
        Camera _cam;
        VideoPlayer _videoPlayer;
        Canvas _canvas;
        RawImage _rawImage;
        AspectRatioFitter _aspectRatioFitter;


        public override void OnGraphStart(Playable playable)
        {
            Initialize();
            if (videoPlayer && !_videoPlayer.isPrepared) _videoPlayer.Prepare();
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if(!_videoPlayer) return;
            if (info.effectiveWeight > 0)
            {
                _videoPlayer.timeReference = VideoTimeReference.ExternalTime;
                _videoPlayer.playbackSpeed = Mathf.Clamp(info.effectiveSpeed, 1 / 10f, 10f);
                SetVideoPlayerTime(playable.GetTime());

                if (!_videoPlayer.isPlaying)
                {
                    _videoPlayer.Play();
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_videoPlayer)
            {
                if (info.timeLooped) return;
                _videoPlayer.timeReference = VideoTimeReference.Freerun;
                SetVideoPlayerTime(playable.GetTime());
                if (!_videoPlayer.isPaused)
                {
                    _videoPlayer.Pause();
                }

                _videoPlayer.timeReference = VideoTimeReference.ExternalTime;

                if (info.weight <= 0)
                {
                    _videoPlayer.targetCameraAlpha = 0;
                    if (_rawImage) _rawImage.color = new Color(1, 1, 1, 0);

                    SetVideoPlayerTime(0);
                }
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            _initialized = false;
            if (_videoPlayer)
            {
                _videoPlayer.Stop();
                CutsceneEngineUtility.SmartDestroy(_videoPlayer.gameObject);
            }

            if (_canvas) CutsceneEngineUtility.SmartDestroy(_canvas.gameObject);
            if (renderTexture && renderTexture.hideFlags == HideFlags.HideAndDontSave) CutsceneEngineUtility.SmartDestroy(renderTexture);
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if(!_videoPlayer || !video) return;
            var shouldPlay = info.evaluationType == FrameData.EvaluationType.Playback;
            var time = playable.GetTime();
            _videoPlayer.targetCameraAlpha = info.effectiveWeight;
            if (_rawImage) _rawImage.color = new Color(1, 1, 1, info.effectiveWeight);

            if (!_videoPlayer.isLooping && time >= _videoPlayer.clip.length && info.effectiveWeight > 0)
                shouldPlay = false;

            // pause if the director's wrap mode is Hold, and it reaches to the end.
            if (info.effectivePlayState == PlayState.Playing && shouldPlay && time > playable.GetDuration() * info.effectiveSpeed)
            {
                if (!_videoPlayer.isPaused)
                {
                    _videoPlayer.Pause();
                }

                return;
            }

            if (shouldPlay)
            {
                _videoPlayer.externalReferenceTime = time / _videoPlayer.playbackSpeed;
                _videoPlayer.timeReference = VideoTimeReference.ExternalTime;
                if (!_videoPlayer.isPlaying)
                {
                    _videoPlayer.Play();
                }
            }
            else
            {
                _videoPlayer.timeReference = VideoTimeReference.Freerun;
                SetVideoPlayerTime(time);
                if (!_videoPlayer.isPaused)
                {
                    _videoPlayer.Pause();
                }

                if (info.effectiveWeight > 0) _videoPlayer.timeReference = VideoTimeReference.ExternalTime;
            }

            SetAudioVolume(audioVolume * info.effectiveWeight);
            _lastTime = time;
        }

        void Initialize()
        {
            if (_initialized) return;

            if (renderTarget != VideoRenderTarget.RenderTexture && renderTexture) renderTexture = null;
            InitializeRenderTexture();
            InitializeVideoPlayer(renderTexture);
            switch (renderTarget)
            {
                case VideoRenderTarget.Screen:
                    InitializeCanvas(renderTexture);
                    break;
                case VideoRenderTarget.Background:
                    InitializeCamera(videoPlayer);
                    break;
                case VideoRenderTarget.RenderTexture:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _initialized = true;
        }

        void InitializeRenderTexture()
        {
            if (!video) return;
            if (!renderTexture)
            {
                renderTexture = new RenderTexture((int)video.width, (int)video.height, 0);
                renderTexture.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        void InitializeVideoPlayer(RenderTexture rt)
        {
            if (!video) return;
            if (_videoPlayer) return;

            var go = new GameObject(video.name);
            go.hideFlags = HideFlags.HideAndDontSave;
            var vp = go.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.source = VideoSource.VideoClip;
            vp.timeReference = VideoTimeReference.ExternalTime;
            vp.clip = video;
            vp.waitForFirstFrame = false;
            vp.skipOnDrop = true;


            switch (renderTarget)
            {
                case VideoRenderTarget.Background:
                    vp.renderMode = VideoRenderMode.CameraFarPlane;
                    break;
                default:
                    vp.renderMode = VideoRenderMode.RenderTexture;
                    break;
            }

            vp.targetTexture = rt;

            vp.aspectRatio = aspectRatio;
            vp.isLooping = loop;

            switch (audioOutputTarget)
            {
                case VideoAudioOutputTarget.Mute:
                    vp.audioOutputMode = VideoAudioOutputMode.None;
                    break;
                case VideoAudioOutputTarget.Direct:
                    vp.audioOutputMode = VideoAudioOutputMode.Direct;
                    break;
                case VideoAudioOutputTarget.AudioSource:
                    if (!audioSource)
                    {
                        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
                        for (ushort i = 0; i < vp.clip.audioTrackCount; ++i)
                            vp.SetTargetAudioSource(i, audioSource);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (vp.targetCamera) vp.targetCameraAlpha = 0;
            _videoPlayer = vp;
        }


        void InitializeCanvas(RenderTexture rt)
        {
            if (!_canvas)
            {
                var go = new GameObject("Video Render Canvas");
                go.hideFlags = HideFlags.HideAndDontSave;
                _canvas = go.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            _canvas.sortingOrder = sortingOrder;

            if (!_rawImage)
            {
                var rawImageGO = new GameObject("RawImage");
                rawImageGO.transform.SetParent(_canvas.transform);
                rawImageGO.transform.localPosition = Vector3.zero;

                _rawImage = rawImageGO.gameObject.AddComponent<RawImage>();

                _rawImage.texture = rt;
                _rawImage.color = new Color(1, 1, 1, 0);

                _aspectRatioFitter = _rawImage.gameObject.AddComponent<AspectRatioFitter>();
                _rawImage.SetNativeSize();

                switch (aspectRatio)
                {
                    case VideoAspectRatio.NoScaling:
                        _rawImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                        _rawImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                        _rawImage.rectTransform.position = ((RectTransform)_canvas.transform).sizeDelta * 0.5f;
                        break;
                    case VideoAspectRatio.FitInside:
                        _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                        break;
                    case VideoAspectRatio.FitOutside:
                        _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                        break;
                    case VideoAspectRatio.Stretch:
                        _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.None;
                        _rawImage.rectTransform.anchorMin = Vector2.zero;
                        _rawImage.rectTransform.anchorMax = Vector2.one;

                        _rawImage.rectTransform.sizeDelta = Vector2.zero;
                        break;
                    case VideoAspectRatio.FitVertically:
                        _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                        _rawImage.rectTransform.anchorMin = new Vector2(0.5f, 0);
                        _rawImage.rectTransform.anchorMax = new Vector2(0.5f, 1);
                        _rawImage.rectTransform.sizeDelta = Vector2.zero;
                        break;
                    case VideoAspectRatio.FitHorizontally:
                        _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                        _rawImage.rectTransform.anchorMin = new Vector2(0, 0.5f);
                        _rawImage.rectTransform.anchorMax = new Vector2(1, 0.5f);
                        _rawImage.rectTransform.sizeDelta = Vector2.zero;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }



        void InitializeCamera(VideoPlayer player)
        {
            if (!_cam)
            {
                _cam = Camera.current;
#if UNITY_EDITOR
                foreach (SceneView sceneView in SceneView.sceneViews)
                {
                    if (_cam == sceneView.camera)
                    {
                        _cam = null;
                        break;
                    }    
                }
                
#endif
                if (!_cam) _cam = owner.GetComponentInChildren<Camera>();
                if (!_cam) _cam = Camera.main;
                player.targetCamera = _cam;
            }
        }


        void SetVideoPlayerTime(double time)
        {
            if (!_videoPlayer || !_videoPlayer.clip) return;

            if (_videoPlayer.isLooping) _videoPlayer.time = time % _videoPlayer.clip.length;
            else _videoPlayer.time = Math.Min(time, _videoPlayer.clip.length);
        }


        void SetAudioVolume(float value)
        {
            if (audioSource) audioSource.volume = value;

            if (_videoPlayer)
            {
                for (ushort i = 0; i < _videoPlayer.audioTrackCount; i++)
                {
                    _videoPlayer.SetDirectAudioVolume(i, value);
                }
            }
        }
    }
}