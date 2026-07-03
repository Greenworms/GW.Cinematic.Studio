using System;
using System.Collections.Generic;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if URP || HDRP
using VolumeProfileAsset = UnityEngine.Rendering.VolumeProfile;
#elif UNITY_POST_PROCESSING_STACK_V2
using UnityEditor.Rendering.PostProcessing;
using VolumeProfileAsset = UnityEngine.Rendering.PostProcessing.PostProcessProfile;
#else
using VolumeProfileAsset = UnityEngine.Object;
#endif

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(VolumeClip))]
    public class VolumeClipInspector : Editor
    {
        VisualElement rootVisualElement;
#if UNITY_POST_PROCESSING_STACK_V2 && !UNITY_6000_0_OR_NEWER
        EffectListEditor _effectListEditor;

        void OnEnable()
        {
            var clip = target as VolumeClip;
            _effectListEditor = new EffectListEditor(this);
            _effectListEditor.Init(clip.volumeProfile, new SerializedObject(clip.volumeProfile));
        }
#endif

#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
        void OnDestroy()
        {
            var asset = TimelineEditor.inspectedAsset;
            if (!asset) return;

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));
            var profiles = new List<VolumeProfileAsset>();

            foreach (var track in asset.GetOutputTracks())
            {
                foreach (var clip in track.GetClips())
                {
                    if (clip?.asset is VolumeClip c && c.volumeProfile)
                    {
                        profiles.Add(c.volumeProfile);
                    }
                }
            }

            foreach (var subAsset in subAssets)
            {
                if (subAsset is VolumeProfileAsset v && !profiles.Contains(v))
                {
                    AssetDatabase.RemoveObjectFromAsset(v);
                    EditorUtility.SetDirty(asset);
                }
            }

            AssetDatabase.SaveAssetIfDirty(asset);
        }
#endif

#if UNITY_6000_0_OR_NEWER
        public override VisualElement CreateInspectorGUI()
        {
            if (!IsVolumeControlSupported) return BuildWarningInspector();
#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
            var root = new VisualElement();
            var clip = (VolumeClip)target;

            var scriptField = new PropertyField { bindingPath = "m_Script" };
            scriptField.SetEnabled(false);
            root.Add(scriptField);

            if (!clip.volumeProfile)
            {
                var createButton = new Button(() =>
                {
                    CreateProfile();
                    Refresh();
                })
                {
                    name = "CreateButton",
                    text = CreateButtonLabel
                };
                root.Add(createButton);
            }
            else
            {
                var profileField = new PropertyField(serializedObject.FindProperty(nameof(VolumeClip.volumeProfile)));
                profileField.SetEnabled(false);
                root.Add(profileField);

                root.AddSpace();

                var profileInspector = new InspectorElement(clip.volumeProfile);
                root.Add(profileInspector);
            }

            rootVisualElement = root;
            return root;
#else
            return BuildWarningInspector();
#endif
        }
#else
        public override void OnInspectorGUI()
        {
            if (!IsVolumeControlSupported)
            {
                DrawWarningInspector();
                return;
            }

#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                var scriptProperty = serializedObject.FindProperty("m_Script");
                if (scriptProperty != null)
                {
                    EditorGUILayout.PropertyField(scriptProperty);
                }
            }

            var clip = (VolumeClip)target;

            if (!clip.volumeProfile)
            {
                if (GUILayout.Button(CreateButtonLabel))
                {
                    CreateProfile();
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VolumeClip.volumeProfile)));
                }

                EditorGUILayout.Space();
                
#if UNITY_POST_PROCESSING_STACK_V2 && !UNITY_6000_0_OR_NEWER
                _effectListEditor.OnGUI();
#endif

            }

            serializedObject.ApplyModifiedProperties();
#else
            DrawWarningInspector();
#endif
        }
#endif

#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
        void CreateProfile()
        {
            var clip = TimelineEditor.selectedClip;
            var director = TimelineEditor.inspectedDirector;
            if (clip == null || !director || target != clip.asset) return;

            var profile = CreateInstance<VolumeProfileAsset>();
            profile.name = clip.displayName;

            AssetDatabase.AddObjectToAsset(profile, director.playableAsset);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);

            ((VolumeClip)target).volumeProfile = profile;
        }

        void Refresh()
        {
            rootVisualElement?.parent?.Clear();
            CreateInspectorGUI();
        }

#if URP || HDRP
        const string CreateButtonLabel = "Create Volume Profile";
#else
        const string CreateButtonLabel = "Create Post Process Profile";
#endif
#endif

        static VisualElement BuildWarningInspector()
        {
            var root = new VisualElement();
            root.Add(new HelpBox(VolumeControlWarningMessage, HelpBoxMessageType.Warning));
            return root;
        }

        static void DrawWarningInspector()
        {
            EditorGUILayout.HelpBox(VolumeControlWarningMessage, MessageType.Warning);
        }

        const string VolumeControlWarningMessage = "Volume Control requires URP, HDRP, or the Post Processing package (com.unity.postprocessing).";

        static bool IsVolumeControlSupported =>
#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
            true;
#else
            false;
#endif
    }
}
