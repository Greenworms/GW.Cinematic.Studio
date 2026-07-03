#if CINEMACHINE_3_OR_NEWER
using Unity.Cinemachine;
#elif CINEMACHINE
using Cinemachine;
#endif
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(ImpulseClip))]
    [CanEditMultipleObjects]
    public class ImpulseClipInspector : Editor
    {

#if UNITY_6000_0_OR_NEWER
        public override VisualElement CreateInspectorGUI()
        {
            return new IMGUIContainer(DrawInspectorContent);
        }
#else
        public override void OnInspectorGUI()
        {
            DrawInspectorContent();
        }
#endif

        void DrawInspectorContent()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ImpulseClip.impulseDefinition)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ImpulseClip.velocity)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ImpulseClip.impulsePoint)));
            serializedObject.ApplyModifiedProperties();

            var clip = (ImpulseClip)target;
            var director = TimelineEditor.inspectedDirector;
            var impulsePoint = clip.impulsePoint.Resolve(director);

#if CINEMACHINE_3_OR_NEWER
            if (!impulsePoint)
            {
                var type = clip.impulseDefinition.ImpulseType;
                if (type is CinemachineImpulseDefinition.ImpulseTypes.Legacy or 
                    not CinemachineImpulseDefinition.ImpulseTypes.Uniform )
                {
                    EditorGUILayout.HelpBox("This impulse will be happened at (0, 0, 0). \n" +
                                            "If you don't want this, set a transform to the impulsePoint.", 
                        MessageType.Warning);    
                }
            }
#elif CINEMACHINE_2_8_OR_NEWER
            if (!impulsePoint)
            {
                var type = clip.impulseDefinition.m_ImpulseType;
                if (type is CinemachineImpulseDefinition.ImpulseTypes.Legacy or 
                    not CinemachineImpulseDefinition.ImpulseTypes.Uniform )
                {
                    EditorGUILayout.HelpBox("This impulse will be happened at (0, 0, 0). \n" +
                                            "If you don't want this, set a transform to the impulsePoint.", 
                        MessageType.Warning);    
                }
            }

#elif CINEMACHINE
            if (!impulsePoint)
            {
                EditorGUILayout.HelpBox("This impulse will be happened at (0, 0, 0). \n" +
                                        "If you don't want this, set a transform to the impulsePoint.", 
                    MessageType.Warning);
            }
#endif
        }
    }
}
