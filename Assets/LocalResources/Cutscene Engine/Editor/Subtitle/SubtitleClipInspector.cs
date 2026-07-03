using CutsceneEngine;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
#if LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
#endif
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(SubtitleClip))]
    [CanEditMultipleObjects]
    public class SubtitleClipInspector : Editor
    {
        const string VirtualToggleTooltip =
            "If this value is true, the text of the subtitle will not be updated with the clip text data and only the OnClipEnter callback will be called.\n" +
            "Fade in/out still occurs. Use this when driving subtitles from a third-party dialogue asset so the text field works as an ID and you inject your own copy.";

#if UNITY_6000_0_OR_NEWER
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var isVirtualProp = serializedObject.FindProperty(nameof(SubtitleClip.isVirtual));
            var isVirtualField = new Toggle(isVirtualProp.displayName);
            isVirtualField.BindProperty(isVirtualProp);
            isVirtualField.tooltip = VirtualToggleTooltip;
            root.Add(isVirtualField);
            
            var textProp = serializedObject.FindProperty(nameof(SubtitleClip.text));
            var textField = new TextField(textProp.displayName);
            textField.multiline = true;
            textField.style.whiteSpace = WhiteSpace.Normal;
            textField.style.overflow = Overflow.Visible;
            textField.isDelayed = true;
            EditorApplication.delayCall += () => textField.RegisterValueChangedCallback(OnTextChanged);
            textField.BindProperty(textProp);
            
            
#if LOCALIZATION
            var useLocalizedStringProp = serializedObject.FindProperty(nameof(SubtitleClip.useLocalizedString));
            var useLocalizedStringField = new Toggle(useLocalizedStringProp.displayName);
            useLocalizedStringField.BindProperty(useLocalizedStringProp);
            EditorApplication.delayCall += () => useLocalizedStringField.RegisterValueChangedCallback(OnUseLocalizedStringChanged);
            root.Add(useLocalizedStringField);

            var localizedStringField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleClip.localizedString)));
            localizedStringField.BindVisible(useLocalizedStringField, x => !isVirtualProp.boolValue && useLocalizedStringProp.boolValue);
            EditorApplication.delayCall += () => localizedStringField.RegisterValueChangeCallback(OnLocalizedStringChanged);
            root.Add(localizedStringField);
            
            textField.BindVisible(useLocalizedStringField, x => !isVirtualProp.boolValue && useLocalizedStringProp.boolValue, true);
#endif
            
            root.Add(textField);
            
            root.AddSpace();
            var overrideParamProp = serializedObject.FindProperty(nameof(SubtitleClip.overrideTypingEffectParameter));
            var overrideParamField = new Toggle(overrideParamProp.displayName);
            overrideParamField.BindProperty(overrideParamProp);
            root.Add(overrideParamField);
            
            var typeWiterParamField = new PropertyField(serializedObject.FindProperty(nameof(SubtitleClip.typingEffectParameter)));
            typeWiterParamField.BindVisible(overrideParamField, x => overrideParamProp.boolValue);
            root.Add(typeWiterParamField);
            return root;
        }
#else
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var isVirtualProp = serializedObject.FindProperty(nameof(SubtitleClip.isVirtual));
            EditorGUILayout.PropertyField(isVirtualProp, new GUIContent(isVirtualProp.displayName, VirtualToggleTooltip));

            var textProp = serializedObject.FindProperty(nameof(SubtitleClip.text));
            var useLocalizedString = false;
#if LOCALIZATION
            var useLocalizedStringProp = serializedObject.FindProperty(nameof(SubtitleClip.useLocalizedString));
            var localizedStringProp = serializedObject.FindProperty(nameof(SubtitleClip.localizedString));
            useLocalizedString = useLocalizedStringProp.boolValue;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useLocalizedStringProp);
            if (EditorGUI.EndChangeCheck())
            {
                SmartRename();
                useLocalizedString = useLocalizedStringProp.boolValue;
            }

            if (!isVirtualProp.boolValue && useLocalizedStringProp.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(localizedStringProp, true);
                if (EditorGUI.EndChangeCheck())
                {
                    SmartRename();
                }
            }
#endif

            var shouldShowTextField = isVirtualProp.boolValue || !useLocalizedString;
            if (shouldShowTextField)
            {
                var label = new GUIContent(textProp.displayName);
                EditorGUILayout.LabelField(label);
                EditorGUI.indentLevel++;
                var newValue = EditorGUILayout.TextArea(textProp.stringValue, GUILayout.MinHeight(60));
                EditorGUI.indentLevel--;
                if (newValue != textProp.stringValue)
                {
                    textProp.stringValue = newValue;
                    HandleTextChanged(newValue);
                }
            }

            EditorGUILayout.Space();

            var overrideParamProp = serializedObject.FindProperty(nameof(SubtitleClip.overrideTypingEffectParameter));
            EditorGUILayout.PropertyField(overrideParamProp);
            if (overrideParamProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SubtitleClip.typingEffectParameter)), true);
            }

            serializedObject.ApplyModifiedProperties();
        }
#endif


        void OnTextChanged(ChangeEvent<string> evt)
        {
            HandleTextChanged(evt.newValue);
        }

        void HandleTextChanged(string newValue)
        {
#if LOCALIZATION
            var useLocalizedStringProp = serializedObject.FindProperty(nameof(SubtitleClip.useLocalizedString));
            if (useLocalizedStringProp != null && useLocalizedStringProp.boolValue) return;
#endif
            Rename(string.IsNullOrWhiteSpace(newValue) ? nameof(SubtitleClip) : newValue);
        }
        
#if LOCALIZATION
        void OnLocalizedStringChanged(SerializedPropertyChangeEvent evt)
        {
            SmartRename();
        }
        
        void OnUseLocalizedStringChanged(ChangeEvent<bool> evt)
        {
            SmartRename();
        }

        void SmartRename()
        {
            var textProp = serializedObject.FindProperty(nameof(SubtitleClip.text));
            var useLocalizedStringProp = serializedObject.FindProperty(nameof(SubtitleClip.useLocalizedString));
            var localizedStringProp = serializedObject.FindProperty(nameof(SubtitleClip.localizedString));
            
            if(useLocalizedStringProp.boolValue)
            {
                var localizedString = localizedStringProp.boxedValue as LocalizedString;

                if (localizedString.IsEmpty || string.IsNullOrWhiteSpace(localizedString.TableEntryReference.ToString()) ||
                    LocalizationSettings.AssetDatabase == null)
                {
                    Rename(nameof(SubtitleClip));
                }
                else
                {
                    var tableCollection = LocalizationEditorSettings.GetStringTableCollection(localizedString.TableReference);

                    if (!tableCollection)
                    {
                        Rename(nameof(SubtitleClip));
                        return;
                    }

                    var entry = tableCollection.SharedData.GetEntry(localizedString.TableEntryReference.KeyId);

                    if (entry == null || string.IsNullOrWhiteSpace(entry.Key)) Rename(nameof(SubtitleClip));
                    else Rename(entry.Key);
                }
            }
            else
            {
                if(string.IsNullOrWhiteSpace(textProp.stringValue)) Rename(nameof(SubtitleClip));
                else Rename(textProp.stringValue);
            }
        }
#endif

        void Rename(string value)
        {
            foreach (var o in targets)
            {
                o.name = value;
            }
            foreach (var clipAsset in TimelineEditor.selectedClips)
            {
                clipAsset.displayName = target.name;
                clipAsset.asset.name = target.name; 
            }
        }
    }
}
