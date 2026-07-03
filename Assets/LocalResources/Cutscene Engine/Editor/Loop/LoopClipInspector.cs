using CutsceneEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(LoopClip))]
    [CanEditMultipleObjects]
    public class LoopClipInspector : Editor
    {
#if UNITY_6000_0_OR_NEWER
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var deactivateAfterEscapeField = new PropertyField(serializedObject.FindProperty(nameof(LoopClip.resetAfterEscape)));
            root.Add(deactivateAfterEscapeField);
            
            var escapeMethodProp = serializedObject.FindProperty(nameof(LoopClip.escapeMethod));
            var escapeMethodField = new EnumField(escapeMethodProp.displayName, (LoopClip.EscapeMethod)escapeMethodProp.enumValueIndex);
            escapeMethodField.tooltip = "Determines under what conditions the loop will escape.\n\n" +
                                        "- Manual: You must call EscapeCurrentLoop(bool) directly in your Cutscene.\n" +
                                        "- LoopCount: The loop will end after the given number of loops has been performed.\n" +
                                        "- Elapsed: The loop will end if you have stayed in the loop for more than the given amount of time.";
            escapeMethodField.BindProperty(escapeMethodProp);
            root.Add(escapeMethodField);

            var targetLoopCountField = new PropertyField(serializedObject.FindProperty(nameof(LoopClip.targetLoopCount)));
            targetLoopCountField.BindVisible(escapeMethodField, LoopClip.EscapeMethod.LoopCount);
            targetLoopCountField.style.Indent(1);
            root.Add(targetLoopCountField);
            
            var minElapseTime = new PropertyField(serializedObject.FindProperty(nameof(LoopClip.minElapseTime)));
            minElapseTime.BindVisible(escapeMethodField, LoopClip.EscapeMethod.Elapsed);
            minElapseTime.style.Indent(1);
            root.Add(minElapseTime);
            
            root.AddSpace();

            var descriptionField = new PropertyField(serializedObject.FindProperty(nameof(LoopClip.description)));
            root.Add(descriptionField);
            
            return root;
        }
#else
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LoopClip.resetAfterEscape)));

            var escapeMethodProp = serializedObject.FindProperty(nameof(LoopClip.escapeMethod));
            EditorGUILayout.PropertyField(escapeMethodProp);

            using (new EditorGUI.IndentLevelScope())
            {
                switch ((LoopClip.EscapeMethod)escapeMethodProp.enumValueIndex)
                {
                    case LoopClip.EscapeMethod.LoopCount:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LoopClip.targetLoopCount)));
                        break;
                    case LoopClip.EscapeMethod.Elapsed:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LoopClip.minElapseTime)));
                        break;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LoopClip.description)), true);

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
