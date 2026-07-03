using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if TMP
using TMPro;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace CutsceneEngine
{
    [RequireComponent(typeof(PlayableDirector))]
    [AddComponentMenu("Cutscene Engine/Cutscene (Cutscene Engine)")]
    public class Cutscene : MonoBehaviour, IPropertyPreview
    {
        /// <summary>
        /// Gets the current state of the cutscene (Playing, Paused, or None).
        /// </summary>
        public CutsceneState state
        {
            get
            {
                if (!director) return CutsceneState.None;
                return director.state switch
                {
                    PlayState.Playing => CutsceneState.Playing,
                    PlayState.Paused => _paused ? CutsceneState.Paused : CutsceneState.None,
                    _ => CutsceneState.None
                };
            }
        }

        /// <summary>
        /// Gets the current playback time of the cutscene.
        /// </summary>
        public double time => !director ? 0 : director.time;
        
        /// <summary>
        /// Gets the total duration of the cutscene.
        /// </summary>
        public double duration => !director ? 0 : director.duration;
        
        /// <summary>
        /// Gets the number of times the cutscene has been completed.
        /// </summary>
        public int completionCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times the cutscene has looped.
        /// This value represents loops when PlayableDirector's WrapMode is set to Loop, not LoopClip.
        /// </summary>
        public int loopCount { get; private set; }
        
        /// <summary>
        /// Gets whether the cutscene has reached the end.
        /// This is useful for checking if the end of the timeline has been reached when the PlayableDirector's WrapMode is Hold.
        /// </summary>
        public bool reachedTheEnd => state == CutsceneState.Playing && time.IsGreaterThanOrEqual(duration);
        
        /// <summary>
        /// The PlayableDirector that controls the cutscene playback.
        /// </summary>
        [Tooltip("The PlayableDirector that controls the cutscene.")]
        public PlayableDirector director;

        /// <summary>
        /// Determines whether to automatically disable the main camera's AudioListener during cutscene playback.
        /// When the cutscene ends, the AudioListener will be re-enabled.
        /// </summary>
        public bool disableMainAudioListener = true;

        /// <summary>
        /// A list of currently active cutscenes.
        /// </summary>
        public static List<Cutscene> activeCutscenes = new List<Cutscene>();

        /// <summary>
        /// Event called when a cutscene starts playing.
        /// </summary>
        public static Action<Cutscene> onCutsceneStarted;

        /// <summary>
        /// Event called when a cutscene is paused.
        /// </summary>
        public static Action<Cutscene> onCutscenePaused;

        /// <summary>
        /// Event called when a cutscene is stopped.
        /// </summary>
        public static Action<Cutscene> onCutsceneStopped;
        
        /// <summary>
        /// Event called when the state of the cutscene changes.
        /// </summary>
        public event Action<CutsceneState> onStateChanged;
        
        /// <summary>
        /// Event called when the cutscene reaches the end.
        /// </summary>
        public event Action onReachedTheEnd;
        /// <summary>
        /// Event called when the cutscene reaches a marker.
        /// </summary>
        public event Action<IMarker> onReachedMarker; 

        /// <summary>
        /// UnityEvent called when the cutscene is played.
        /// </summary>
        public UnityEvent onPlayed;
        
        /// <summary>
        /// UnityEvent called when the cutscene is paused.
        /// </summary>
        public UnityEvent onPaused;
        
        /// <summary>
        /// UnityEvent called when the cutscene is stopped.
        /// </summary>
        public UnityEvent onStopped;
        
        Coroutine _readingProcess;
        AudioListener _mainAudioListener;
        LoopTrack _loopTrack;
        List<LoopBehaviour> _loopBehaviours;
        readonly List<IMarker> _passedMarkers = new List<IMarker>();
        readonly HashSet<Marker> _exitMarkers = new HashSet<Marker>();
        readonly Dictionary<Marker, Marker> _jumpMarkers = new Dictionary<Marker, Marker>();

        CutsceneState _lastState;
        bool _played;
        bool _paused;
        bool _stopped;
        bool _reachedTheEndInvoked;
        bool _loopedByClipThisFrame;
        bool _stoppedByFinished;
        double _lastTime;
        double _deltaTime;
        double _loopClipDepartureTime;
        double _loopClipArrivalTime;
        void Reset()
        {
            director = GetComponent<PlayableDirector>();
        }

        void Awake()
        {
            if(!director) director = GetComponent<PlayableDirector>();
            PrepareCallbacks();
            GetLoopTrackAndBehaviours();
        }

        
        void PrepareCallbacks()
        {
            if (director)
            {
                director.played += OnPlayed;
                director.paused += OnPaused;
                director.stopped += OnStopped;
                
                if(director.playOnAwake && director.state == PlayState.Playing && !_played) OnPlayed(director);
            }
        }

        void OnPlayed(PlayableDirector playableDirector)
        {
            if (_lastState == CutsceneState.Playing)return;
            if (_lastState == CutsceneState.Paused)
            {
                _stopped = false;
                _paused = false;
                _stoppedByFinished = false;
                onStateChanged?.Invoke(state);
                return;
            }
            _played = true;
            _stopped = false;
            _stoppedByFinished = false;
            _reachedTheEndInvoked = false;
            _deltaTime = 0;
            _lastTime = 0;

            if (disableMainAudioListener)
            {
                if(!_mainAudioListener)
                {
                    var mainCam = Camera.main;
                    if (mainCam)
                    {
                        _mainAudioListener = mainCam.GetComponent<AudioListener>();
                    }

                    if (!_mainAudioListener)
                    {
                        _mainAudioListener = FindAnyObjectByType<AudioListener>();
                    }
                }
                
                if (_mainAudioListener && _mainAudioListener.transform.IsChildOf(transform)) _mainAudioListener = null;
                if (_mainAudioListener) _mainAudioListener.enabled = false;
            }
            
            _passedMarkers.Clear();

            if(_readingProcess != null) StopCoroutine(_readingProcess);
            _readingProcess = StartCoroutine(ReadTimeline());
            if (!activeCutscenes.Contains(this)) activeCutscenes.Add(this);
            onPlayed?.Invoke();
            onCutsceneStarted?.Invoke(this);
            onStateChanged?.Invoke(state);
        }

        void OnPaused(PlayableDirector playableDirector)
        {
            if(_lastState != CutsceneState.Playing) return;
            _paused = true;
            onPaused?.Invoke();
            onCutscenePaused?.Invoke(this);
            onStateChanged?.Invoke(state);
        }

        void OnStopped(PlayableDirector playableDirector)
        {
            if(_lastState == CutsceneState.None) return;
            if(director.extrapolationMode == DirectorWrapMode.None && 
               state == CutsceneState.None && _lastState == CutsceneState.Playing &&
               time == director.initialTime && (_lastTime + _deltaTime).IsGreaterThanOrEqual(duration))
            {
                _stoppedByFinished = true;
            }
            _stopped = true;
            _paused = false;
            _played = false;
            if (disableMainAudioListener && _mainAudioListener)
            {
                _mainAudioListener.enabled = true;
            }
            

#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if(this) // play mode only
                {
                    var audioListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    if (audioListeners.Length > 1)
                    {
                        Debug.LogWarning($"[Cutscene({name})] There are more than 2 audio listeners. \n" +
                                         "If the camera used in the cutscene is not deactivated, select the ActivationTrack and check if the Post-playback state is set to Inactive or Revert in the Inspector.");
                    }
                }
            };
#endif
            activeCutscenes.Remove(this);
            onStopped?.Invoke();
            onCutsceneStopped?.Invoke(this);
            onStateChanged?.Invoke(state);
            _lastState = CutsceneState.None;
        }

        IEnumerator ReadTimeline()
        {
            while (state != CutsceneState.None)
            {
                yield return new WaitForEndOfFrame();
                UpdatePropertiesPhase1();
                CheckPassedMarkers();
                CheckReachedTheEnd();
                PostProcess();
                UpdatePropertiesPhase2();
            }
        }

        void CheckReachedTheEnd()
        {
            if(_stopped && director.extrapolationMode != DirectorWrapMode.None) return;
            var passedTheEnd = false;
            
            if (time < _lastTime)
            {
                if(_loopedByClipThisFrame)
                {
                    if (_loopClipDepartureTime.IsGreaterThanOrEqual(duration)) passedTheEnd = true;
                }
                else if(!_stopped)
                {
                    loopCount++;
                    passedTheEnd = true;
                }

                _reachedTheEndInvoked = false;
            }

            if((reachedTheEnd || passedTheEnd || _stoppedByFinished) && !_reachedTheEndInvoked)
            {
                onReachedTheEnd?.Invoke();
                _reachedTheEndInvoked = true;
            }
        }
        void CheckPassedMarkers()
        {
            if(_stopped && director.extrapolationMode != DirectorWrapMode.None) return;
            var timelineAsset = director.playableAsset as TimelineAsset;

            var loopedByWrapMode = !_stopped && time < _lastTime && !_loopedByClipThisFrame;
            
            if (time < _lastTime)
            {
                // Time has moved backwards
                foreach (var track in timelineAsset.GetOutputTracks())
                {
                    foreach (var marker in track.GetMarkers())
                    {
                        if(_passedMarkers.Contains(marker)) continue;
                        if(Math.Abs(_lastTime - marker.time) > _deltaTime * 2) continue;
                        if (_loopedByClipThisFrame)
                        {
                            if (marker.time.IsGreaterThanOrEqual(_lastTime) && marker.time.IsLessThanOrEqual(_loopClipDepartureTime))
                            {
                                ProcessMarker(marker);
                            }   
                        }
                        else
                        {
                            if((loopedByWrapMode || _stoppedByFinished) && marker.time.IsGreaterThanOrEqual(_lastTime) && marker.time.IsLessThanOrEqual(duration))
                            {
                                ProcessMarker(marker);
                            }
                        }
                    }
                }
            }
            
            if(!_stopped)
            {
                foreach (var track in timelineAsset.GetOutputTracks())
                {
                    foreach (var marker in track.GetMarkers())
                    {
                        if (time.IsGreaterThanOrEqual(marker.time) && !_passedMarkers.Contains(marker))
                        {
                            if(Math.Abs(time - marker.time) > _deltaTime * 2) continue;
                            ProcessMarker(marker);
                        }
                    }
                }
            }
        }
        void PostProcess()
        {
            if(director.timeUpdateMode == DirectorUpdateMode.Manual &&
               director.extrapolationMode == DirectorWrapMode.None &&
               director.time >= director.duration)
            {
                director.Stop();
            }
        }

        void UpdatePropertiesPhase1()
        {
            if (time < _lastTime)
            {
                if (_loopedByClipThisFrame)
                {
                    _deltaTime = (_loopClipDepartureTime - _lastTime) + (time - _loopClipArrivalTime);
                }
                else
                {
                    _deltaTime = (duration - _lastTime) + (time);
                    if(!_stopped) OnLooped1ByWrapMode();
                }
            }
            else
            {
                _deltaTime = time - _lastTime;
            }
        }

        void UpdatePropertiesPhase2()
        {
            if (_loopedByClipThisFrame)
            {
                var timelineAsset = director.playableAsset as TimelineAsset;
                foreach (var track in timelineAsset.GetOutputTracks())
                {
                    foreach (var marker in track.GetMarkers())
                    {
                        if (marker.time.IsGreaterThanOrEqual(_loopClipArrivalTime) && marker.time.IsLessThanOrEqual(_loopClipDepartureTime))
                        {
                            _passedMarkers.Remove(marker);
                        }
                    }
                }
            }
            
            _lastTime = time;
            _loopedByClipThisFrame = false;
            _lastState = state;
        }
        void ProcessMarker(IMarker marker)
        {
            onReachedMarker?.Invoke(marker);
            _passedMarkers.Add(marker);

            if (marker is Marker m)
            {
                if(_loopedByClipThisFrame) return;
                if(_jumpMarkers.TryGetValue(m, out var destination)) GoToMarker(destination.name);
                else if(_exitMarkers.Contains(m)) Stop();
            }
        }

        void GetLoopTrackAndBehaviours()
        {
            _loopTrack = director.GetTrack<LoopTrack>();
            _loopBehaviours = new List<LoopBehaviour>();
            if(_loopTrack == null) return;
            foreach (var timelineClip in _loopTrack.GetClips())
            {
                var loopClip = timelineClip.asset as LoopClip;
                _loopBehaviours.Add(loopClip.behaviour);
            }
        }
        
        void OnLooped1ByWrapMode()
        {
            _passedMarkers.Clear();
        }
        
        internal void OnLoopedByClip(double from, double to)
        {
            _loopedByClipThisFrame = true;
            _loopClipDepartureTime = from;
            _loopClipArrivalTime = to;
        }

        internal void PrepareFrame(in Playable playable, in FrameData info)
        {
            // _deltaTime = info.deltaTime;
        }

        internal void OnBehaviourPause(in Playable playable, in FrameData info)
        {
            // _deltaTime = info.deltaTime;
            // _stoppedByFinished = director.time.IsGreaterThanOrEqual(duration);
            // if(_stoppedByFinished) completionCount++;
        }

        /// <summary> Plays the timeline.
        /// This is a wrapper method for use as a button in the inspector,
        /// but it's also fine to play directly from the PlayableDirector. </summary>
        public void Play()
        {
            if (state == CutsceneState.Playing)
            {
                Debug.LogWarning("The cutscene is already playing.");
                return;
            }
            if(state == CutsceneState.Paused) director.Resume();
            else director.Play();
        }

        /// <summary> Plays the timeline from the time of the specified marker. </summary>
        /// <param name="markerName"> The name of the marker to start playing from. </param>
        public void PlayAt(string markerName)
        {
            var marker = GetMarker(markerName);
            if (!marker)
            {
                Debug.LogWarning($"Cannot find the marker({markerName})");
                return;
            }

            director.initialTime = marker.time;
            Play();
        }

        /// <summary> Pauses the timeline playback. </summary>
        public void Pause()
        {
            director.Pause();
        }

        /// <summary> Stops the timeline playback. </summary>
        public void Stop()
        {
            director.Stop();
        }

        /// <summary> Sets the current time of the timeline. </summary>
        /// <param name="time"> The time to set. </param>
        public void SetTime(float time)
        {
            director.time = time;
        }
        
        /// <summary> Finds a marker by name. </summary>
        /// <param name="markerName"> The name of the marker to find. </param>
        /// <returns> Returns the found marker. Returns null if not found. </returns>
        public Marker GetMarker(string markerName)
        {
            var timelineAsset = director.playableAsset as TimelineAsset;
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                foreach (var marker in track.GetMarkers())
                {
                    if (marker is Marker m && m.name == markerName) return m;
                }
            }

            Debug.LogWarning($"Cannot find the marker({markerName})");
            return null;
        }

        /// <summary> Moves the timeline to the time of the specified marker. </summary>
        /// <param name="markerName"> The name of the marker to move to. </param>
        public void GoToMarker(string markerName)
        {
            var marker = GetMarker(markerName);
            if(!marker) return;
            director.time = marker.time;
        }

        /// <summary> Registers an exit marker that stops the timeline when reached. </summary>
        /// <param name="markerName"> The name of the marker to register. </param>
        public void RegisterExitMarker(string markerName)
        {
            var marker = GetMarker(markerName);
            if(!marker) return;
            RegisterExitMarker(marker);
        }

        /// <summary> Registers an exit marker that stops the timeline when reached. </summary>
        /// <param name="marker"> The marker to register. </param>
        public void RegisterExitMarker(Marker marker)
        {
            if (!_exitMarkers.Add(marker))
            {
                Debug.LogWarning($"{marker.name} is an already registered marker.");
            }
        }

        /// <summary> Registers a jump from a start marker to an end marker. </summary>
        /// <param name="startMarkerName"> The name of the start marker. </param>
        /// <param name="endMarkerName"> The name of the end marker. </param>
        public void RegisterJumpMarkers(string startMarkerName, string endMarkerName)
        {
            var startMarker = GetMarker(startMarkerName);
            var endMarker = GetMarker(endMarkerName);

            if (startMarker && endMarker) RegisterJumpMarkers(startMarker, endMarker);
        }

        /// <summary> Registers a jump from a start marker to an end marker. </summary>
        /// <param name="startMarker"> The start marker. </param>
        /// <param name="endMarker"> The end marker. </param>
        public void RegisterJumpMarkers(Marker startMarker, Marker endMarker)
        {
            _jumpMarkers[startMarker] = endMarker;
        }

        /// <summary> Compares a specific time with the time of a marker. </summary>
        /// <param name="time"> The time to compare. </param>
        /// <param name="markerName"> The name of the marker to compare. </param>
        /// <returns> Returns -1 if the marker is before the time, 1 if after, and 0 if they are almost the same. </returns>
        public int CompareMarkerTiming(string markerName, double time)
        {
            var marker = GetMarker(markerName);
            if (marker == null) return 1;
            
            if(marker.time < time) return -1;
            if(marker.time > time) return 1;
            
            var timelineAsset = director.playableAsset as TimelineAsset;
            if(Math.Abs(marker.time - time) < 1/timelineAsset.editorSettings.frameRate) return 0;

            return 1;
        }

        /// <summary> Checks if the current time is before the specified marker. </summary>
        /// <param name="markerName"> The name of the marker to check. </param>
        /// <returns> Returns true if the current time is before the marker. </returns>
        public bool IsBefore(string markerName)
        {
            return CompareMarkerTiming(markerName, director.time) == -1;
        }
        
        /// <summary> Checks if the current time is after the specified marker. </summary>
        /// <param name="markerName"> The name of the marker to check. </param>
        /// <returns> Returns true if the current time is after the marker. </returns>
        public bool IsAfter(string markerName)
        {
            return CompareMarkerTiming(markerName, director.time) == 1;
        }


        /// <summary> Escapes from the currently playing loop clip. </summary>
        /// <param name="toEnd"> If true, jumps to the end of the loop. If false, disables the current loop without changing the time. </param>
        public void EscapeCurrentLoop(bool toEnd)
        {
            if(!IsInLoopClip(out var loop)) return;
            loop.Escape(toEnd);
        }
        
        /// <summary> Checks if the current time is within a loop clip. 
        /// This is regardless of whether the loop is disabled, it compares the start and end of the loop clip. </summary>
        /// <param name="loop"> Returns the LoopBehaviour corresponding to the current time. </param>
        /// <returns> Returns true if within a loop clip. </returns>
        public bool IsInLoopClip(out LoopBehaviour loop)
        {
            var time = director.time;
            foreach (var loopBehaviour in _loopBehaviours)
            {
                if ((float)loopBehaviour.start <= time && time <= (float)loopBehaviour.end)
                {
                    loop = loopBehaviour;
                    return true;
                }
            }

            loop = null;
            return false;
        }

        /// <summary> Checks if the current time is within an active loop clip. </summary>
        /// <param name="loop"> Returns the LoopBehaviour corresponding to the current time. </param>
        /// <returns> Returns true if within a loop clip and the loop is active. </returns>
        public bool IsInActiveLoopClip(out LoopBehaviour loop)
        {
            if(!IsInLoopClip(out loop))
            {
                loop = null;
                return false;
            }

            return !loop.isFinished;
        }


        /// <summary> Removes the binding from the track with the specified name. If there are multiple tracks with the same name, the binding is removed from all of them.</summary>
        /// <param name="trackName"> The name of the track to remove the binding from. </param>
        public void RemoveBindingFrom(string trackName)
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if (playableBinding.sourceObject.name == trackName)
                {
                    var track = playableBinding.sourceObject as TrackAsset;
                    director.SetGenericBinding(track, null);
                }
            }
        }

        /// <summary> Removes the binding from the track of the specified type and name. If there are multiple tracks with the same name, the binding is removed from all of them. </summary>
        /// <typeparam name="T"> The type of the track. </typeparam>
        /// <param name="trackName"> The name of the track to remove the binding from. If null or whitespace, the binding is removed from all tracks of type T. </param>
        public void RemoveBindingFrom<T>(string trackName = null) where T : TrackAsset
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if(playableBinding.outputTargetType != typeof(T)) continue;
                if (!string.IsNullOrWhiteSpace(trackName) || playableBinding.sourceObject.name == trackName)
                {
                    var track = playableBinding.sourceObject as TrackAsset;
                    director.SetGenericBinding(track, null);
                }
            }
        }
        
        /// <summary> Adds a binding to the track with the specified name. If there are multiple tracks with the same name, the binding is added to all of them.
        /// If there is already a bound object, the object will be replaced. </summary>
        /// <param name="trackName"> The name of the track to add the binding to. </param>
        /// <param name="bindingObject"> The object to bind. </param>
        public void AddBindingTo(string trackName, Object bindingObject)
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if (playableBinding.sourceObject.name == trackName)
                {
                    var track = playableBinding.sourceObject as TrackAsset;
                    director.SetGenericBinding(track, bindingObject);
                }
            }
        }

        /// <summary> Adds a binding to the track of the specified type and name. If there are multiple tracks with the same name, the binding is added to all of them.
        /// If there is already a bound object, the object will be replaced. </summary>
        /// <typeparam name="T"> The type of the track. </typeparam>
        /// <param name="trackName"> The name of the track to add the binding to. If null or whitespace, the binding is added to all tracks of type T. </param>
        /// <param name="bindingObject"> The object to bind. </param>
        public void AddBindingTo<T>(string trackName, Object bindingObject) where T : TrackAsset
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if(playableBinding.outputTargetType != typeof(T)) continue;
                if (!string.IsNullOrWhiteSpace(trackName) || playableBinding.sourceObject.name == trackName)
                {
                    var track = playableBinding.sourceObject as TrackAsset;
                    director.SetGenericBinding(track, bindingObject);
                }
            }
        }

        public void ReplaceBindings(GameObject original, GameObject target)
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                var track = playableBinding.sourceObject as TrackAsset;
                var boundObject = director.GetGenericBinding(track);
                if (boundObject is Component c)
                {
                    if (c.gameObject == original) director.SetGenericBinding(track, target);
                }
            }
        }

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            driver.AddFromName<Cutscene>(gameObject, nameof(disableMainAudioListener));
        }
    }
}
