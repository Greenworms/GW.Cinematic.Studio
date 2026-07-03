using CutsceneEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(ColorClip))]
    [CanEditMultipleObjects]
    public class ColorClipInspector : Editor
    {
#if UNITY_6000_0_OR_NEWER
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var colorTypeProp = serializedObject.FindProperty(nameof(ColorClip.colorType));
            var colorTypeField = new EnumField(colorTypeProp.displayName, (ColorType)colorTypeProp.enumValueIndex);
            colorTypeField.BindProperty(colorTypeProp);
            root.Add(colorTypeField);
            
            var colorField = new PropertyField(serializedObject.FindProperty(nameof(ColorClip.color)));
            colorField.BindVisible(colorTypeField, ColorType.Default);
            root.Add(colorField);
            
            var gradientGammaField = new PropertyField(serializedObject.FindProperty(nameof(ColorClip.gradient)));
            gradientGammaField.BindVisible(colorTypeField, ColorType.Gradient);
            root.Add(gradientGammaField);
            
            return root;
        }
#else
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var colorTypeProp = serializedObject.FindProperty(nameof(ColorClip.colorType));
            EditorGUILayout.PropertyField(colorTypeProp);

            EditorGUI.indentLevel++;
            switch ((ColorType)colorTypeProp.enumValueIndex)
            {
                case ColorType.Default:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ColorClip.color)));
                    break;
                case ColorType.Gradient:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ColorClip.gradient)));
                    break;
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
