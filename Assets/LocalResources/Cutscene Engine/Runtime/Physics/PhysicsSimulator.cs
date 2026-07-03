using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    /// <summary>
    /// Component that simulates physics for recording and playback in cutscenes.
    /// Allows recording physics movements and forces to recreate them during timeline playback.
    /// </summary>
    [AddComponentMenu("Cutscene Engine/Physics Simulator (Cutscene Engine)")]
    public class PhysicsSimulator : MonoBehaviour
    {
        const float MinSimulationStep = 0.0001f;

        [Tooltip("This adds a slight delay when starting the simulation. The simulation will continue during this delay, but recording will not occur.\n\n" +
                 "Some objects may have a collider partially overlapping the ground or slightly off the ground. " +
                 "In these cases, starting the simulation will take some time for the object to find a stable position. \n" +
                 "This is a useful option to allow time for physics objects to stabilize." )]
        public float preDelay;

        [Tooltip("This is the delta time used in the simulation. When recording key frames, recording is also based on this time. \n\n" +
                 "If you are concerned about performance, it is recommended to increase this value. " +
                 "You should adjust it by testing the quality and performance of the simulation yourself.")]
        public float simulationStep = 0.02f;

        [Tooltip("The time to apply the simulation. This is the value used when running a simulation with the Run Timed Simulation button in the Inspector.\n\n" +
                 "When this time is up, the simulation will automatically end, and if there is a track being recorded, that will also end.")]
        public float simulationDuration = 2.0f;

        [Tooltip("Determines the degree to which keyframes in the recorded curve are optimized. If this value is 0, no optimization is applied, and all keyframes for each step are recorded.")]
        [Range(0, 1f)] public float curveOptimizationValue = 0.33f;

        public bool IsSimulating => _isSimulating;
        public bool IsPaused => _isPaused;
        public float CurrentSimulationTime => _currentSimulationTime;
        public bool HasRecordingTarget => _recordingTarget != null;
        public PhysicsRecordingTarget RecordingTarget => _recordingTarget;
        public PhysicsRecordingResult LastRecordingResult => _lastRecordingResult;
        public int RecordingResultVersion => _recordingResultVersion;

        bool _isSimulating;
        bool _isPaused;
        bool _isTimedSimulation;

        float _currentSimulationTime;
        float _preDelayElapsed;
        double _timeAccumulator;

        int _recordingResultVersion;
        PhysicsRecordingResult _lastRecordingResult;
        PhysicsRecordingTarget _recordingTarget;
        PhysicsRecorder _recorder;

        SimulationMode _previousSimulationMode;
        SimulationMode2D _previousSimulationMode2D;

        readonly List<Rigidbody> _rigidbodies = new();
        readonly List<Rigidbody2D> _rigidbodies2D = new();
        readonly List<PhysicsRigidbodyState> _allRigidbodyStates = new();
        readonly List<ForceSettings> _forceSettings = new();
        readonly List<ForceFieldSettings> _forceFieldSettings = new();

        public bool TryConfigureRecordingTarget(PlayableDirector director, AnimationTrack track, Transform bindingRoot, double? startDirectorTime, out string error)
        {
            if (director == null)
            {
                error = "PlayableDirector is null.";
                return false;
            }

            if (track == null)
            {
                error = "AnimationTrack is null.";
                return false;
            }

            if (bindingRoot == null)
            {
                error = "Binding root is null.";
                return false;
            }

            bool trackExistsOnDirector = false;
            foreach (var existingTrack in director.GetTracks<AnimationTrack>())
            {
                if (existingTrack == track)
                {
                    trackExistsOnDirector = true;
                    break;
                }
            }

            if (!trackExistsOnDirector)
            {
                error = "The specified AnimationTrack does not belong to the provided PlayableDirector.";
                return false;
            }

            var configuredStartTime = startDirectorTime ?? director.time;
            _recordingTarget = new PhysicsRecordingTarget(director, track, bindingRoot, configuredStartTime);
            error = null;
            return true;
        }

        public void ClearRecordingTarget()
        {
            _recordingTarget = null;
            _recorder = null;
        }

        public bool StartSimulation(bool timed)
        {
            if (_isSimulating)
            {
                return false;
            }

            _isTimedSimulation = timed;
            _isSimulating = true;
            _isPaused = false;
            _currentSimulationTime = 0f;
            _preDelayElapsed = 0f;
            _timeAccumulator = 0d;
            _lastRecordingResult = null;

            _previousSimulationMode = Physics.simulationMode;
            _previousSimulationMode2D = Physics2D.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;

            InitializeRigidbodies();
            FindForceSettings();
            FindForceFieldSettings();
            InitializeForces();
            InitializeRecorder();

            return true;
        }

        public void PauseSimulation()
        {
            if (!_isSimulating)
            {
                return;
            }

            _isPaused = true;
            _timeAccumulator = 0d;
        }

        public void ResumeSimulation()
        {
            if (!_isSimulating || !_isPaused)
            {
                return;
            }

            _isPaused = false;
        }

        public void TickSimulation(double deltaTime)
        {
            if (!_isSimulating || _isPaused)
            {
                return;
            }

            if (deltaTime <= 0d)
            {
                return;
            }

            var step = Mathf.Max(MinSimulationStep, simulationStep);
            _timeAccumulator += deltaTime;

            while (_timeAccumulator >= step)
            {
                _timeAccumulator -= step;
                SimulateSingleStep(step);

                if (_isTimedSimulation)
                {
                    var duration = Mathf.Max(0f, simulationDuration);
                    if (_currentSimulationTime >= duration)
                    {
                        StopSimulation();
                        break;
                    }
                }
            }
        }

        public PhysicsRecordingResult StopSimulation(bool applyPausedState = false)
        {
            if (!_isSimulating)
            {
                return _lastRecordingResult ?? PhysicsRecordingResult.Empty;
            }

            if (applyPausedState)
            {
                CapturePausedStateAsResetState();
            }

            _isSimulating = false;
            _isPaused = false;
            _isTimedSimulation = false;

            Physics.simulationMode = _previousSimulationMode;
            Physics2D.simulationMode = _previousSimulationMode2D;

            var result = _recorder != null
                ? _recorder.CompleteRecording(curveOptimizationValue)
                : PhysicsRecordingResult.Empty;

            _lastRecordingResult = result;
            _recordingResultVersion++;

            ResetRigidbodyStates();
            _forceSettings.Clear();
            _forceFieldSettings.Clear();
            _recorder = null;

            return result;
        }

        void Update()
        {
            if (!Application.isPlaying || !_isSimulating || _isPaused)
            {
                return;
            }

            TickSimulation(Time.unscaledDeltaTime);
        }

        void OnDisable()
        {
            if (_isSimulating)
            {
                StopSimulation();
            }
        }

        void SimulateSingleStep(float step)
        {
            if (_preDelayElapsed < Mathf.Max(0f, preDelay))
            {
                Physics.Simulate(step);
                Physics2D.Simulate(step);
                _preDelayElapsed += step;
                return;
            }

            if (_recorder != null && _recordingTarget != null)
            {
                var timelineTime = (float)(_recordingTarget.StartDirectorTime + _currentSimulationTime);
                _recorder.RecordKeyframes(timelineTime);
            }

            foreach (var forceSetting in _forceSettings)
            {
                if (forceSetting != null)
                {
                    forceSetting.ApplyForces(_currentSimulationTime);
                }
            }

            foreach (var forceField in _forceFieldSettings)
            {
                if (forceField != null)
                {
                    forceField.ApplyForce(_currentSimulationTime);
                }
            }

            Physics.Simulate(step);
            Physics2D.Simulate(step);
            _currentSimulationTime += step;
        }

        void InitializeRecorder()
        {
            if (_recordingTarget == null || _recordingTarget.BindingRoot == null)
            {
                _recorder = null;
                return;
            }

            _recorder = new PhysicsRecorder(_recordingTarget.BindingRoot, _allRigidbodyStates, (float)_recordingTarget.StartDirectorTime);
        }

        void InitializeRigidbodies()
        {
            _rigidbodies.Clear();
            _rigidbodies2D.Clear();
            _allRigidbodyStates.Clear();

            GetComponentsInChildren(false, _rigidbodies);
            GetComponentsInChildren(false, _rigidbodies2D);

            var root = _recordingTarget != null && _recordingTarget.BindingRoot
                ? _recordingTarget.BindingRoot
                : transform;

            foreach (var rigidbody in _rigidbodies)
            {
                if (rigidbody == null)
                {
                    continue;
                }

                _allRigidbodyStates.Add(new PhysicsRigidbodyState(root, rigidbody));
            }

            foreach (var rigidbody2D in _rigidbodies2D)
            {
                if (rigidbody2D == null)
                {
                    continue;
                }

                _allRigidbodyStates.Add(new PhysicsRigidbodyState(root, rigidbody2D));
            }
        }

        void FindForceSettings()
        {
            _forceSettings.Clear();
            GetComponentsInChildren(false, _forceSettings);
        }

        void FindForceFieldSettings()
        {
            _forceFieldSettings.Clear();
            GetComponentsInChildren(false, _forceFieldSettings);
        }

        void InitializeForces()
        {
            foreach (var forceSetting in _forceSettings)
            {
                if (forceSetting != null)
                {
                    forceSetting.Initialize();
                }
            }

            foreach (var forceField in _forceFieldSettings)
            {
                if (forceField != null)
                {
                    forceField.Initialize();
                }
            }
        }

        void CapturePausedStateAsResetState()
        {
            foreach (var state in _allRigidbodyStates)
            {
                if (state == null || state.Transform == null)
                {
                    continue;
                }

                state.InitialPosition = state.Transform.position;
                state.InitialRotation = state.Transform.rotation;
            }
        }

        void ResetRigidbodyStates()
        {
            foreach (var state in _allRigidbodyStates)
            {
                if (state == null || state.Transform == null)
                {
                    continue;
                }

                if (state.Rigidbody2D)
                {
#if UNITY_6000_0_OR_NEWER
                    state.Rigidbody2D.linearVelocity = Vector2.zero;
#else
                    state.Rigidbody2D.velocity = Vector2.zero;
#endif
                    state.Rigidbody2D.angularVelocity = 0f;
                }

                if (state.Rigidbody3D)
                {
#if UNITY_6000_0_OR_NEWER
                    state.Rigidbody3D.linearVelocity = Vector3.zero;
#else
                    state.Rigidbody3D.velocity = Vector3.zero;
#endif
                    state.Rigidbody3D.angularVelocity = Vector3.zero;
                }

                state.Transform.position = state.InitialPosition;
                state.Transform.rotation = state.InitialRotation;
            }

            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            _allRigidbodyStates.Clear();
        }
    }
}