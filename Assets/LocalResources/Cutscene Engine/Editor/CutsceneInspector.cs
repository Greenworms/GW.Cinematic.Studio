using System;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(Cutscene))]
    [CanEditMultipleObjects]
    public class CutsceneInspector : Editor
    {
        public VisualElement rootVisualElement;

        public Button playButton;
        public Button pauseButton;
        public Button stopButton;

        public Label stateLabel;
        public Label completionCountLabel;
        public Label loopCountLabel;
        public Label reachedTheEndLabel;

        void Awake()
        {
            var cutscene = (Cutscene)target;
            cutscene.onStateChanged += UpdatePlayButtons;
            EditorApplication.update += UpdateLabels;
            EditorApplication.update += UpdateTimelineWindow;
        }

        void OnDestroy()
        {
            var cutscene = (Cutscene)target;
            cutscene.onStateChanged -= UpdatePlayButtons;
            EditorApplication.update -= UpdateLabels;
            EditorApplication.update -= UpdateTimelineWindow;
        }

        public override VisualElement CreateInspectorGUI()
        {
            rootVisualElement = new VisualElement();
            var cutscene = (Cutscene)target;
            
            var directorField = new PropertyField();
            directorField.bindingPath = nameof(Cutscene.director);
            rootVisualElement.Add(directorField);
            
            var disableMainAudioListenerField = new PropertyField();
            disableMainAudioListenerField.bindingPath = nameof(Cutscene.disableMainAudioListener);
            disableMainAudioListenerField.tooltip = 
                "It Automatically disable the original AudioListener to prevent console messages\n" +
                "This option is recommanded if you use another camera for the cutscene.\n" +
                "But if you want to use the main camera as a cutscene camera, turn off this flag.";
            rootVisualElement.Add(disableMainAudioListenerField);
            
            
            var playButtonArea = EditorUtil.Horizontal();
            playButtonArea.name = "PlayButtonArea";
            playButtonArea.style.justifyContent = Justify.Center;

#if URP || HDRP
            playButton = EditorUtil.IconButton(EditorGUIUtility.IconContent("Play").image, Play, 32);
            pauseButton = EditorUtil.IconButton(EditorGUIUtility.IconContent("Pause").image, Pause, 32);
            stopButton = EditorUtil.IconButton(EditorGUIUtility.IconContent("Stop").image, Stop, 32);
#else
            playButton = EditorUtil.IconButton(EditorGUIUtility.IconContent("PlayButton").image, Play, 32);
            pauseButton = EditorUtil.IconButton(EditorGUIUtility.IconContent("PauseButton").image, Pause, 32);
            stopButton = EditorUtil.IconButton(EditorGUIUtility.IconContent("PreMatQuad").image, Stop, 32);
#endif
            
            playButtonArea.Add(playButton);
            playButtonArea.Add(pauseButton);
            playButtonArea.Add(stopButton);
            rootVisualElement.Add(playButtonArea);

            
            stateLabel = new Label($"State: {CutsceneState.None}");
            rootVisualElement.Add(stateLabel);

            var stateRow1 = EditorUtil.Horizontal();
            stateRow1.style.flexGrow = 1;
            rootVisualElement.Add(stateRow1);
            
            completionCountLabel = new Label($"Completion Count: {cutscene.completionCount:N0}");
            completionCountLabel.style.flexGrow = 1;
            stateRow1.Add(completionCountLabel);
            
            loopCountLabel = new Label($"Loop Count: {cutscene.loopCount:N0}");
            loopCountLabel.style.flexGrow = 1;
            stateRow1.Add(loopCountLabel);
            
            reachedTheEndLabel = new Label($"Reached The End: {cutscene.reachedTheEnd}");
            reachedTheEndLabel.style.flexGrow = 1;
            stateRow1.Add(reachedTheEndLabel);
            
            
            if (!EditorApplication.isPlaying) playButtonArea.SetEnabled(false);
            UpdatePlayButtons(cutscene.state);
            
            rootVisualElement.AddSpace();
            
            var onPlayedEventField = new PropertyField();
            onPlayedEventField.bindingPath = nameof(Cutscene.onPlayed);
            rootVisualElement.Add(onPlayedEventField);
            
            var onPausedEventField = new PropertyField();
            onPausedEventField.bindingPath = nameof(Cutscene.onPaused);
            rootVisualElement.Add(onPausedEventField);
            
            var onStoppedEventField = new PropertyField();
            onStoppedEventField.bindingPath = nameof(Cutscene.onStopped);
            rootVisualElement.Add(onStoppedEventField);
            

            return rootVisualElement;
        }


        void Play()
        {
            var cutscene = (Cutscene)target;
            cutscene.Play();
            UpdatePlayButtons(cutscene.state);
        }
        void Pause()
        {
            var cutscene = (Cutscene)target;
            cutscene.Pause();
            UpdatePlayButtons(cutscene.state);
        }

        void Stop()
        {
            var cutscene = (Cutscene)target;
            cutscene.Stop();
            UpdatePlayButtons(cutscene.state);
        }

        void UpdatePlayButtons(CutsceneState state)
        {
            switch (state)
            {
                case CutsceneState.None:
                    playButton?.SetEnabled(true);
                    pauseButton?.SetEnabled(false);
                    stopButton?.SetEnabled(false);
                    break;
                case CutsceneState.Playing:
                    playButton?.SetEnabled(false);
                    pauseButton?.SetEnabled(true);
                    stopButton?.SetEnabled(true);
                    break;
                case CutsceneState.Paused:
                    playButton?.SetEnabled(true);
                    pauseButton?.SetEnabled(false);
                    stopButton?.SetEnabled(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        void UpdateLabels()
        {
            var cutscene = (Cutscene)target;
            if(stateLabel == null) return; // 인스펙터가 잠긴 상태에서는 에디터가 켜져있어도 GUI가 생성되지 않아서 null일 수 있음.
            stateLabel.text = $"State: {cutscene.state}";
            completionCountLabel.text = $"Completion Count: {cutscene.completionCount:N0}";
            loopCountLabel.text = $"Loop Count: {cutscene.loopCount:N0}";
            reachedTheEndLabel.text = $"Reached The End: {cutscene.reachedTheEnd}";   
        }

        void UpdateTimelineWindow()
        {
            var window = TimelineEditor.GetWindow();
            if(window != null) window.Repaint();
        }
    }
}