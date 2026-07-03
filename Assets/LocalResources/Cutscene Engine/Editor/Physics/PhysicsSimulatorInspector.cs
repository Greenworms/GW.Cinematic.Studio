using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using CutsceneEngine;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(PhysicsSimulator))]
    public class PhysicsSimulatorInspector : Editor
    {
        double _lastUpdateTime;
        int _handledRecordingResultVersion = -1;

        bool _hasRecordingTrack;
        PlayableDirector _recordingDirector;
        AnimationTrack _recordingTrack;
        Transform _recordingBinding;

        PhysicsSimulator Simulator => target as PhysicsSimulator;

        void OnEnable()
        {
            var simulator = Simulator;
            if (simulator != null)
            {
                _handledRecordingResultVersion = simulator.RecordingResultVersion;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UpdateRecordingTrackInfo();
            DrawSimulationControls();
            DrawTimedSimulationControls();

            serializedObject.ApplyModifiedProperties();
            ApplyPendingRecordingIfNeeded();
        }

        void DrawSimulationControls()
        {
            var simulator = Simulator;
            if (simulator == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Simulation Controls", EditorStyles.boldLabel);

            if (_hasRecordingTrack)
            {
                EditorGUILayout.HelpBox("Cannot edit properties during recording or previewing.", MessageType.Info, true);
                GUI.enabled = false;
            }

            var simulationStepProp = serializedObject.FindProperty(nameof(PhysicsSimulator.simulationStep));
            EditorGUILayout.PropertyField(simulationStepProp, new GUIContent("Simulation Step (seconds)"));

            var preDelayProp = serializedObject.FindProperty(nameof(PhysicsSimulator.preDelay));
            EditorGUILayout.PropertyField(preDelayProp, new GUIContent("Pre Delay (seconds)"));

            var curveOptimizationProp = serializedObject.FindProperty(nameof(PhysicsSimulator.curveOptimizationValue));
            EditorGUILayout.PropertyField(curveOptimizationProp, new GUIContent("Curve Optimization Value"), true);

            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !simulator.IsSimulating || simulator.IsPaused;
            if (GUILayout.Button(simulator.IsPaused ? "Resume" : "Play"))
            {
                if (!simulator.IsSimulating)
                {
                    StartSimulation(false);
                }
                else
                {
                    ResumeSimulation();
                }
            }

            GUI.enabled = simulator.IsSimulating && !simulator.IsPaused;
            if (GUILayout.Button("Pause"))
            {
                PauseSimulation();
            }

            GUI.enabled = simulator.IsSimulating;
            if (GUILayout.Button("Stop"))
            {
                StopSimulation(false);
            }

            GUI.enabled = simulator.IsPaused;
            if (GUILayout.Button("Apply Paused State"))
            {
                StopSimulation(true);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        void DrawTimedSimulationControls()
        {
            var simulator = Simulator;
            if (simulator == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Timed Simulation", EditorStyles.boldLabel);

            if (_hasRecordingTrack)
            {
                GUI.enabled = false;
            }

            var durationProp = serializedObject.FindProperty(nameof(PhysicsSimulator.simulationDuration));
            EditorGUILayout.PropertyField(durationProp, new GUIContent("Duration (seconds)"));

            GUI.enabled = true;

            GUI.enabled = !simulator.IsSimulating;
            if (GUILayout.Button("Run Timed Simulation"))
            {
                StartSimulation(true);
            }

            GUI.enabled = true;
        }

        void StartSimulation(bool timed)
        {
            var simulator = Simulator;
            if (simulator == null)
            {
                return;
            }

            ConfigureRecordingTargetFromTimeline(simulator);

            if (!simulator.StartSimulation(timed))
            {
                return;
            }

            _lastUpdateTime = EditorApplication.timeSinceStartup;
            _handledRecordingResultVersion = simulator.RecordingResultVersion;

            EditorApplication.update -= SimulationUpdateLoop;
            EditorApplication.update += SimulationUpdateLoop;

            EditorApplication.delayCall += () =>
            {
                foreach (var view in SceneView.sceneViews)
                {
                    if (view is SceneView sceneView)
                    {
                        EditorUtility.SetDirty(sceneView);
                    }
                }

                EditorApplication.Step();
            };
        }

        void PauseSimulation()
        {
            var simulator = Simulator;
            if (simulator == null)
            {
                return;
            }

            simulator.PauseSimulation();
        }

        void ResumeSimulation()
        {
            var simulator = Simulator;
            if (simulator == null)
            {
                return;
            }

            simulator.ResumeSimulation();
            _lastUpdateTime = EditorApplication.timeSinceStartup;
        }

        void StopSimulation(bool applyPausedState)
        {
            var simulator = Simulator;
            if (simulator == null)
            {
                return;
            }

            var result = simulator.StopSimulation(applyPausedState);
            HandleRecordingResult(simulator, result);
            _handledRecordingResultVersion = simulator.RecordingResultVersion;

            EditorApplication.update -= SimulationUpdateLoop;
            SceneView.RepaintAll();
        }

        void ConfigureRecordingTargetFromTimeline(PhysicsSimulator simulator)
        {
            UpdateRecordingTrackInfo();

            if (!_hasRecordingTrack)
            {
                simulator.ClearRecordingTarget();
                return;
            }

            if (!simulator.TryConfigureRecordingTarget(_recordingDirector, _recordingTrack, _recordingBinding, _recordingDirector.time, out var error))
            {
                Debug.LogWarning(error);
                simulator.ClearRecordingTarget();
                return;
            }

            PhysicsRecordingEditorBridge.PrepareTrackForRecording(_recordingDirector, _recordingTrack);
        }

        void SimulationUpdateLoop()
        {
            var simulator = Simulator;
            if (simulator == null)
            {
                EditorApplication.update -= SimulationUpdateLoop;
                return;
            }

            if (!simulator.IsSimulating)
            {
                ApplyPendingRecordingIfNeeded();
                EditorApplication.update -= SimulationUpdateLoop;
                SceneView.RepaintAll();
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            var delta = now - _lastUpdateTime;
            _lastUpdateTime = now;

            simulator.TickSimulation(delta);

            if (simulator.IsSimulating && simulator.HasRecordingTarget)
            {
                var targetInfo = simulator.RecordingTarget;
                if (targetInfo != null && targetInfo.Director)
                {
                    targetInfo.Director.time = targetInfo.StartDirectorTime + simulator.CurrentSimulationTime;
                }
            }

            ApplyPendingRecordingIfNeeded();
            SceneView.RepaintAll();
            EditorApplication.Step();
        }

        void ApplyPendingRecordingIfNeeded()
        {
            var simulator = Simulator;
            if (simulator == null)
            {
                return;
            }

            if (simulator.RecordingResultVersion == _handledRecordingResultVersion)
            {
                return;
            }

            _handledRecordingResultVersion = simulator.RecordingResultVersion;
            HandleRecordingResult(simulator, simulator.LastRecordingResult);
        }

        void HandleRecordingResult(PhysicsSimulator simulator, PhysicsRecordingResult result)
        {
            if (simulator == null)
            {
                PhysicsRecordingEditorBridge.RefreshTimelineAfterRecording();
                return;
            }

            var targetInfo = simulator.RecordingTarget;
            if (targetInfo == null || !targetInfo.Director || targetInfo.Track == null)
            {
                PhysicsRecordingEditorBridge.RefreshTimelineAfterRecording();
                return;
            }

            if (result != null && result.HasData)
            {
                PhysicsRecordingEditorBridge.ApplyRecordingResultToTrack(targetInfo.Director, targetInfo.Track, result);
            }

            PhysicsRecordingEditorBridge.FinalizeEditorRecording(targetInfo.Track);
            PhysicsRecordingEditorBridge.RefreshTimelineAfterRecording();
        }

        void UpdateRecordingTrackInfo()
        {
            _hasRecordingTrack = PhysicsRecordingEditorBridge.TryFindRecordingTarget(out _recordingDirector, out _recordingTrack, out _recordingBinding);
        }

        void OnDisable()
        {
            EditorApplication.update -= SimulationUpdateLoop;

            var simulator = Simulator;
            if (simulator != null && simulator.IsSimulating)
            {
                var result = simulator.StopSimulation();
                HandleRecordingResult(simulator, result);
                _handledRecordingResultVersion = simulator.RecordingResultVersion;
            }
        }
    }
}
