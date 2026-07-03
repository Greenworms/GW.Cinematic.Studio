using System;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(SubtitleText))]
    [CanEditMultipleObjects]
    public class SubtitleTextInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var subtitleText = (SubtitleText)target;
            if (subtitleText.TryGetComponent(out UIDocument doc))
            {
                var uiDocumentField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleText.uiDocument)));
                uiDocumentField.SetEnabled(false);
                root.Add(uiDocumentField);
                
                var elementNameField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleText.elementName)));
                root.Add(elementNameField);
            }
            else
            {
                switch (subtitleText.textType)
                {
                    case SubtitleTextType.None:
                        var label = new Label("Could not find any text component.");
                        label.style.color = Color.red;
                        root.Add(label);
                        break;
                    case SubtitleTextType.Legacy:
                    {
                        var textField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleText.text_legacy)));
                        textField.SetEnabled(false);
                        root.Add(textField);
                        break;
                    }
                    case SubtitleTextType.TextMesh:
                    {
                        var textField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleText.textMesh)));
                        textField.SetEnabled(false);
                        root.Add(textField);
                        break;
                    }
                    case SubtitleTextType.UIElements:
                        break;
                    case SubtitleTextType.TMP:
                    {
                        var textField = new PropertyField(serializedObject.FindProperty("text_tmp")); // TMP 어셈블리가 에디터에서 참조되지 않은 상태라서 그냥 텍스트를 직접 입력함.
                        textField.SetEnabled(false);
                        root.Add(textField);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            var ignoreTimeScaleField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleText.ignoreTimeScale)));
            root.Add(ignoreTimeScaleField);
            
            var deactivateIfEmptyField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleText.deactivateIfEmpty)));
            root.Add(deactivateIfEmptyField);
            
            var prefixField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleText.prefix)));
            root.Add(prefixField);
            
            var textDisplayEffectProp = serializedObject.FindProperty(nameof(SubtitleText.textDisplayEffect));
            var textDisplayEffectField = new EnumField(textDisplayEffectProp.displayName, (Enum)Enum.ToObject(typeof(TextDisplayEffect), textDisplayEffectProp.intValue));
            textDisplayEffectField.BindProperty(textDisplayEffectProp);
            textDisplayEffectField.labelElement.style.width = Length.Percent(40);
            root.Add(textDisplayEffectField);
            
            root.AddSpace();
            
            var fadeParamField = new PropertyField();
            fadeParamField.bindingPath = nameof(SubtitleText.subtitleFadeParameter);
            fadeParamField.BindVisible(textDisplayEffectField, TextDisplayEffect.Fade);
            
            root.Add(fadeParamField);
            
            var typeWriterParamField = new PropertyField();
            typeWriterParamField.bindingPath = nameof(SubtitleText.typingEffectParameter);
            typeWriterParamField.BindVisible(textDisplayEffectField, TextDisplayEffect.Typing);
            root.Add(typeWriterParamField);

            root.AddSpace();

            var onChangeEventField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleText.onChanged)));
            root.Add(onChangeEventField);
            
            return root;
        }
    }
}