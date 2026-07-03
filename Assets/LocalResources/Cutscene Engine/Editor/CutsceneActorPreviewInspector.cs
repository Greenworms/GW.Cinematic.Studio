using CutsceneEngine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(CutsceneActorPreview))]
    public class CutsceneActorPreviewInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var preview = (CutsceneActorPreview)target;
            var animator = preview.GetComponent<Animator>();
            var director = preview.GetComponentInParent<PlayableDirector>();
            if (director && animator)
            {
                var animationTrack = director.GetTrack<AnimationTrack>(animator);
                if (animationTrack)
                {
                    if (animationTrack.trackOffset != TrackOffset.ApplySceneOffsets)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal();
                        var icon = EditorGUIUtility.IconContent("console.warnicon");
                        GUILayout.Label(icon, GUILayout.Width(32f), GUILayout.Height(32f));
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("For Cutscene Actor Preview to work correctly, the Animation Track's Track Offset must be set to 'Apply Scene Offsets'.", EditorStyles.wordWrappedLabel);
                        EditorGUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Fix", GUILayout.Width(80f)))
                        {
                            FixTrackOffset(animationTrack);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                }
            }
            base.OnInspectorGUI();
        }
        
        void FixTrackOffset(AnimationTrack track)
        {
            Undo.RecordObject(track, "Fix Track Offset");
            track.trackOffset = TrackOffset.ApplySceneOffsets;
            EditorUtility.SetDirty(track);
            Selection.activeObject = track;
        }
    }
}