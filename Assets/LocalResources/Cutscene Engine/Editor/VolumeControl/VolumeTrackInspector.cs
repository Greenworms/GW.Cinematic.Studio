using CutsceneEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(VolumeTrack))]
    public class VolumeTrackInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
            AddVolumeControls(root);
#else
            root.Add(new HelpBox("Volume Control requires URP, HDRP, or the Post Processing package (com.unity.postprocessing).", HelpBoxMessageType.Warning));
#endif
            return root;
        }

#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
        void AddVolumeControls(VisualElement root)
        {
            var basePriorityField = new PropertyField { bindingPath = nameof(VolumeTrack.basePriority) };
            root.Add(basePriorityField);

            var volumeLayerProp = serializedObject.FindProperty(nameof(VolumeTrack.volumeLayer));
            var volumeLayerField = new LayerField(volumeLayerProp.displayName);
            volumeLayerField.BindProperty(volumeLayerProp);
            root.Add(volumeLayerField);
        }
#endif
    }
}
