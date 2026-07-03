using CutsceneEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(LightTrack))]
    [CanEditMultipleObjects]
    public class LightTrackInspector: Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.Add(new Label("Shape"));
            var innerSpotAngleField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.innerSpotAngleControl)));
            root.Add(innerSpotAngleField);
            
            var spotAngleField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.spotAngleControl)));
            root.Add(spotAngleField);

#if HDRP
            var areaSizeField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.areaSizeControl)));
            root.Add(areaSizeField);
            
            var angularDiameterField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.angularDiameterControl)));
            root.Add(angularDiameterField);

            root.AddSpace();
            root.Add(new Label("Celestial Body"));
            
            var surfaceColorField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.surfaceColorControl)));
            root.Add(surfaceColorField);
            
            var flareSizeField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.flareSizeControl)));
            root.Add(flareSizeField);
            
            var flareFalloffField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.flareFalloffControl)));
            root.Add(flareFalloffField);
            
            var flareTintField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.flareTintControl)));
            root.Add(flareTintField);
            
            var flareMultiplierField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.flareMultiplierControl)));
            root.Add(flareMultiplierField);
#endif
            
            root.AddSpace();
            root.Add(new Label("Emission"));
            
            var colorField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.colorControl)));
            root.Add(colorField);
            
            var colorTemeratureField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.colorTemperatureControl)));
            root.Add(colorTemeratureField);
            
            var intensityField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.intensityControl)));
            root.Add(intensityField);

            var bounceIntensityField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.bounceIntensityControl)));
            root.Add(bounceIntensityField);
            
            var rangeField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.rangeControl)));
            root.Add(rangeField);
            
#if HDRP
            bounceIntensityField.label = "Indirect Multiplier";
            var intensityMultiplierField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.intensityMultiplierControl)));
            root.Add(intensityMultiplierField);
            
            root.AddSpace();
            root.Add(new Label("Volumetrics"));
            
            var vMultiplierField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.volumetricMultiplierControl)));
            root.Add(vMultiplierField);
            
            var vShadowDimmerField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.volumetricShadowDimmerControl)));
            root.Add(vShadowDimmerField);
#endif

            root.AddSpace();
            root.Add(new Label("Shadows"));
            
            var shadowStrengthField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.shadowStrengthControl)));
#if HDRP
            shadowStrengthField.label = "Shadow Dimmer";
#endif
            root.Add(shadowStrengthField);

#if HDRP
            var shadowTintField = new PropertyField(serializedObject.FindProperty(nameof(LightTrack.shadowTintControl)));
            root.Add(shadowTintField);
#endif
            return root;
        }
    }
}