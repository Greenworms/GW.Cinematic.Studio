using CutsceneEngine;
using UnityEditor;
using UnityEngine;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(ParticleClip))]
    [CanEditMultipleObjects]
    public class ParticleClipInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ParticleClip.particleSystem)));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ParticleClip.stopOnEnd)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ParticleClip.controlChildren)));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ParticleClip.connectName)));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
