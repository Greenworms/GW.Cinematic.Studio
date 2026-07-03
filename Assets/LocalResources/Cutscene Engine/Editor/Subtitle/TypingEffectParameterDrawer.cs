using CutsceneEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomPropertyDrawer(typeof(TypingEffectParameter))]
    public class TypingEffectParameterDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            root.Add(new Label(property.displayName));

            var timeMethodProp = property.FindPropertyRelative(nameof(TypingEffectParameter.timeMethod));
            var timeMethodField = new EnumField(timeMethodProp.displayName, (TypingEffectParameter.TimeMethod)timeMethodProp.enumValueIndex);
            timeMethodField.bindingPath = timeMethodProp.propertyPath;
            timeMethodField.style.Indent(1);
            root.Add(timeMethodField);


            var timePerCharField = new PropertyField(property.FindPropertyRelative(nameof(TypingEffectParameter.timePerChar)));
            timePerCharField.BindVisible(timeMethodField, TypingEffectParameter.TimeMethod.PerChar);
            timePerCharField.style.Indent(2);
            root.Add(timePerCharField);

            var totalDurationField = new PropertyField(property.FindPropertyRelative(nameof(TypingEffectParameter.totalDuration)));
            totalDurationField.BindVisible(timeMethodField, TypingEffectParameter.TimeMethod.Total);
            totalDurationField.style.Indent(2);
            root.Add(totalDurationField);


            var fadeOutTimeField = new PropertyField(property.FindPropertyRelative(nameof(TypingEffectParameter.fadeOutTime)));
            fadeOutTimeField.style.Indent(1);
            root.Add(fadeOutTimeField);

            var fadeOutCurveField = new PropertyField(property.FindPropertyRelative(nameof(TypingEffectParameter.fadeOutCurve)));
            fadeOutCurveField.style.Indent(1);
            root.Add(fadeOutCurveField);

            var additionalCharSettingsField = new PropertyField(property.FindPropertyRelative(nameof(TypingEffectParameter.additionalCharSettings)));
            additionalCharSettingsField.style.Indent(1);
            root.Add(additionalCharSettingsField);

            return root;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(line, label);
            line.y += line.height + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.indentLevel++;

            var timeMethodProp = property.FindPropertyRelative(nameof(TypingEffectParameter.timeMethod));
            DrawProperty(ref line, timeMethodProp, includeChildren: false);

            EditorGUI.indentLevel++;
            var timePerCharProp = property.FindPropertyRelative(nameof(TypingEffectParameter.timePerChar));
            var totalDurationProp = property.FindPropertyRelative(nameof(TypingEffectParameter.totalDuration));
            if ((TypingEffectParameter.TimeMethod)timeMethodProp.enumValueIndex == TypingEffectParameter.TimeMethod.PerChar)
            {
                DrawProperty(ref line, timePerCharProp);
            }
            else
            {
                DrawProperty(ref line, totalDurationProp);
            }
            EditorGUI.indentLevel--;

            DrawProperty(ref line, property.FindPropertyRelative(nameof(TypingEffectParameter.fadeOutTime)));
            DrawProperty(ref line, property.FindPropertyRelative(nameof(TypingEffectParameter.fadeOutCurve)));
            DrawProperty(ref line, property.FindPropertyRelative(nameof(TypingEffectParameter.additionalCharSettings)), includeChildren: true);

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var timeMethodProp = property.FindPropertyRelative(nameof(TypingEffectParameter.timeMethod));
            height += EditorGUI.GetPropertyHeight(timeMethodProp) + EditorGUIUtility.standardVerticalSpacing;

            var timePerCharProp = property.FindPropertyRelative(nameof(TypingEffectParameter.timePerChar));
            var totalDurationProp = property.FindPropertyRelative(nameof(TypingEffectParameter.totalDuration));
            if ((TypingEffectParameter.TimeMethod)timeMethodProp.enumValueIndex == TypingEffectParameter.TimeMethod.PerChar)
            {
                height += EditorGUI.GetPropertyHeight(timePerCharProp) + EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                height += EditorGUI.GetPropertyHeight(totalDurationProp) + EditorGUIUtility.standardVerticalSpacing;
            }

            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(TypingEffectParameter.fadeOutTime))) +
                      EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(TypingEffectParameter.fadeOutCurve))) +
                      EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(TypingEffectParameter.additionalCharSettings)), true) +
                      EditorGUIUtility.standardVerticalSpacing;

            return height;
        }

        static void DrawProperty(ref Rect line, SerializedProperty property, bool includeChildren = false)
        {
            var height = EditorGUI.GetPropertyHeight(property, includeChildren);
            line.height = height;
            EditorGUI.PropertyField(line, property, includeChildren);
            line.y += height + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
