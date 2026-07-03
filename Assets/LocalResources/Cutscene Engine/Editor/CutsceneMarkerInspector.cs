using System;
using CutsceneEngine;
using UnityEditor;
using UnityEngine;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(CutsceneMarker))]
    public class CutsceneMarkerInspector : Editor
    {
        static GUIStyle renameFieldStyle;
        static GUIStyle classLabelStyle;

        protected override void OnHeaderGUI()
        {
            if (renameFieldStyle == null)
            {
                renameFieldStyle = new GUIStyle(GUI.skin.textField);
                renameFieldStyle.fontStyle = FontStyle.Bold;
            }

            if (classLabelStyle == null)
            {
                classLabelStyle = new GUIStyle(GUI.skin.label);
                classLabelStyle.alignment = TextAnchor.LowerLeft;
                classLabelStyle.fontSize = 13;
                classLabelStyle.normal.textColor = Color.gray;
            }
            
            
            if(!target) return;
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            var icon = EditorGUIUtility.GetIconForObject(target);
            GUILayout.Box(icon, GUI.skin.label, GUILayout.Width(50), GUILayout.Height(50));

            EditorGUILayout.BeginVertical();
            var newName = EditorGUILayout.DelayedTextField(target.name, renameFieldStyle);
            if (newName != target.name)
            {
                Undo.RecordObject(target, "Rename");
                target.name = newName;
            }
            GUILayout.Label(nameof(CutsceneMarker), classLabelStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            
            base.OnInspectorGUI();
        }
    }
}