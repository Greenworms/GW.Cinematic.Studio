#if URP
using CutsceneEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(Light2DTrack))]
    [CanEditMultipleObjects]
    public class Light2DTrackInspector: Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.Add(new Label("Shape & Emission"));
            var colorField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.colorControl)));
            root.Add(colorField);
            
            var intensityField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.intensityControl)));
            root.Add(intensityField);
            
            root.AddSpace();
            
            var innerRadiusField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.pointLightInnerRadiusControl)));
            root.Add(innerRadiusField);

            var outerRadiusField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.pointLightOuterRadiusControl)));
            root.Add(outerRadiusField);
            
            var innerAngleField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.pointLightInnerAngleControl)));
            root.Add(innerAngleField);

            var outerAngleField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.pointLightOuterAngleControl)));
            root.Add(outerAngleField);
            
            root.AddSpace();

            var falloffField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.falloffControl)));
            root.Add(falloffField);
            
            var falloffStrengthField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.falloffStrengthControl)));
            root.Add(falloffStrengthField);

            root.AddSpace();
            root.Add(new Label("Shadows"));
            var shadowIntensityField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.shadowStrengthControl)));
            root.Add(shadowIntensityField);

            var shadowSoftnessField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.shadowSoftnessControl)));
            root.Add(shadowSoftnessField);

            var shadowFalloffIntensityField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.shadowFalloffStrengthControl)));
            root.Add(shadowFalloffIntensityField);
            
            root.AddSpace();
            root.Add(new Label("Volumetrics"));
            var volumetricIntensityField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.volumetricIntensityControl)));
            root.Add(volumetricIntensityField);
            
            var volumetricShadowIntensityField = new PropertyField(serializedObject.FindProperty(nameof(Light2DTrack.volumetricShadowIntensityControl)));
            root.Add(volumetricShadowIntensityField);

            return root;
        }
    }
}
#endif