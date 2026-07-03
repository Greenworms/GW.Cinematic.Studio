using CutsceneEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(VideoPlayClip))]
    public class VideoPlayClipInspector : Editor
    {
#if UNITY_6000_0_OR_NEWER
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var videoField = new PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.video)));
            root.Add(videoField);
            
            var loopField = new PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.loop)));
            root.Add(loopField);
            
            root.AddSpace();
            
            var renderTargetProp = serializedObject.FindProperty(nameof(VideoPlayClip.renderTarget));
            var renderTargetField = new EnumField(renderTargetProp.displayName, (VideoRenderTarget)renderTargetProp.intValue);
            renderTargetField.labelElement.style.width = Length.Percent(38);
            renderTargetField.BindProperty(renderTargetProp);
            root.Add(renderTargetField);
            
            var aspectRatioField = new PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.aspectRatio)));
            aspectRatioField.style.Indent(1);
            root.Add(aspectRatioField);
            
            var sortingOrderField = new PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.sortingOrder)));
            sortingOrderField.BindVisible(renderTargetField, VideoRenderTarget.Screen);
            sortingOrderField.style.Indent(1);
            root.Add(sortingOrderField);
            
            var renderTextureField = new PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.renderTexture)));
            renderTextureField.BindVisible(renderTargetField, VideoRenderTarget.RenderTexture);
            renderTextureField.style.Indent(1);
            root.Add(renderTextureField);
            
            root.AddSpace();
            
            var audioOutputTargetProp = serializedObject.FindProperty(nameof(VideoPlayClip.audioOutputTarget));
            var audioOutputTargetField = new EnumField(audioOutputTargetProp.displayName, (VideoAudioOutputTarget)audioOutputTargetProp.intValue);
            audioOutputTargetField.labelElement.style.width = Length.Percent(38);
            audioOutputTargetField.BindProperty(audioOutputTargetProp);
            root.Add(audioOutputTargetField);
            
            var audioSourceField = new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.audioSource)), new GUIContent("Audio Source"));
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }); 
            audioSourceField.BindVisible(audioOutputTargetField, VideoAudioOutputTarget.AudioSource);
            audioSourceField.style.Indent(1);
            root.Add(audioSourceField);
            
            var audioVolumeField = new PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.audioVolume)));
            audioVolumeField.BindVisible(audioOutputTargetField, VideoAudioOutputTarget.Mute, true);
            audioVolumeField.style.Indent(1);
            root.Add(audioVolumeField);

            return root;
        }
#else
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.video)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.loop)));

            EditorGUILayout.Space();

            var renderTargetProp = serializedObject.FindProperty(nameof(VideoPlayClip.renderTarget));
            EditorGUILayout.PropertyField(renderTargetProp);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.aspectRatio)));

                var renderTarget = (VideoRenderTarget)renderTargetProp.intValue;
                if (renderTarget == VideoRenderTarget.Screen)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.sortingOrder)));
                }
                else if (renderTarget == VideoRenderTarget.RenderTexture)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.renderTexture)));
                }
            }

            EditorGUILayout.Space();

            var audioOutputProp = serializedObject.FindProperty(nameof(VideoPlayClip.audioOutputTarget));
            EditorGUILayout.PropertyField(audioOutputProp);

            using (new EditorGUI.IndentLevelScope())
            {
                var audioOutput = (VideoAudioOutputTarget)audioOutputProp.intValue;
                if (audioOutput == VideoAudioOutputTarget.AudioSource)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.audioSource)),
                        AudioSourceLabel);
                }

                if (audioOutput != VideoAudioOutputTarget.Mute)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VideoPlayClip.audioVolume)));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        static readonly GUIContent AudioSourceLabel = new("Audio Source");
#endif
    }
}
