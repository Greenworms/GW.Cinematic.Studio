#if URP
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(Light2DClip))]
    [CanEditMultipleObjects]
    public class Light2DClipInspector : Editor
    {
        internal static Light2DClipInspector active { get; private set; }
        internal bool wasNoisePropertyChanged { get; private set; }
        void OnEnable()
        {
            active = this;
        }

        void OnDisable()
        {
            if (active == this) active = null;
        }

#if UNITY_6000_0_OR_NEWER
        public override VisualElement CreateInspectorGUI()
        {
            var director = TimelineEditor.inspectedDirector;
            if (!director) return null;
            var root = new VisualElement();
            
            var clip = (Light2DClip)target;
            var track = director.GetTrackOf<Light2DTrack>(clip);
            var light = director.GetGenericBinding(track) as Light2D;

            var box = new HelpBox("Set the ValueControlOptions in the track's inspector to the value you want to change.", 
                HelpBoxMessageType.Info);
            box.Q<Label>().style.fontSize = 12;
            root.Add(box);
            root.AddSpace();
            
            root.Add(new Label("Shape & Emission"));
            
            var colorField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.color)));
            if(track.colorControl == ValueControlOption.None) colorField.SetEnabled(false);
            root.Add(colorField);
            
            var intensityField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.intensity)));
            if(track.intensityControl == ValueControlOption.None) intensityField.SetEnabled(false);
            root.Add(intensityField);
            
            if (!light || light.lightType == Light2D.LightType.Point)
            {
                root.AddSpace();
                var innerRadiusField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.pointLightInnerRadius)));
                if(track.pointLightInnerRadiusControl == ValueControlOption.None) innerRadiusField.SetEnabled(false);
                root.Add(innerRadiusField);

                var outerRadiusField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.pointLightOuterRadius)));
                if(track.pointLightOuterRadiusControl == ValueControlOption.None) outerRadiusField.SetEnabled(false);
                root.Add(outerRadiusField);
                
                var innerAngleField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.pointLightInnerAngle)));
                if(track.pointLightInnerAngleControl == ValueControlOption.None) innerAngleField.SetEnabled(false);
                root.Add(innerAngleField);

                var outerAngleField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.pointLightOuterAngle)));
                if(track.pointLightOuterAngleControl == ValueControlOption.None) outerAngleField.SetEnabled(false);
                root.Add(outerAngleField);
            }

            if (!light || light.lightType is Light2D.LightType.Freeform)
            {
                var falloffField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.falloff)));
                falloffField.label = "Falloff";
                if(track.falloffControl == ValueControlOption.None) falloffField.SetEnabled(false);
                root.Add(falloffField);
            }
            
            if (!light || light.lightType is Light2D.LightType.Point or Light2D.LightType.Freeform)
            {
                var falloffStrengthField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.falloffStrength)));
                falloffStrengthField.label = "Falloff Strength";
                if(track.falloffStrengthControl == ValueControlOption.None) falloffStrengthField.SetEnabled(false);
                root.Add(falloffStrengthField);
            }

            if (!light || light.lightType != Light2D.LightType.Global)
            {
                root.AddSpace();
                root.Add(new Label("Shadows"));
                var shadowIntensityField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.shadowStrength)));
                if(track.shadowStrengthControl == ValueControlOption.None) shadowIntensityField.SetEnabled(false);
                root.Add(shadowIntensityField);
                
                var shadowSoftnessField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.shadowSoftness)));
                if(track.shadowSoftnessControl == ValueControlOption.None) shadowSoftnessField.SetEnabled(false);
                root.Add(shadowSoftnessField);

                var shadowFalloffField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.shadowFalloffStrength)));
                if(track.shadowFalloffStrengthControl == ValueControlOption.None) shadowFalloffField.SetEnabled(false);
                root.Add(shadowFalloffField);
                
                root.AddSpace();
                root.Add(new Label("Volumetrics"));
                var volumetricIntensityField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.volumetricIntensity)));
                if(track.volumetricIntensityControl == ValueControlOption.None) volumetricIntensityField.SetEnabled(false);
                root.Add(volumetricIntensityField);
            
                var volumetricShadowIntensityField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.volumetricShadowIntensity)));
                if(track.volumetricShadowIntensityControl == ValueControlOption.None) volumetricShadowIntensityField.SetEnabled(false);
                root.Add(volumetricShadowIntensityField);

            }
            
            root.AddSpace();
            root.AddSpace();
            root.Add(new Label("Extra Features"));
            var useIntensityNoiseProp = serializedObject.FindProperty(nameof(Light2DClip.useIntensityNoise));
            var useIntensityNoiseField = new PropertyField(useIntensityNoiseProp);
            root.Add(useIntensityNoiseField);
            
            var intensityNoiseOffsetField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.intensityNoiseOffset)));
            intensityNoiseOffsetField.BindEnable(useIntensityNoiseField, x => useIntensityNoiseProp.boolValue);
            intensityNoiseOffsetField.style.Indent(1);
            intensityNoiseOffsetField.RegisterValueChangeCallback(evt => RepaintWindow());
            root.Add(intensityNoiseOffsetField);
            
            var intensityNoiseSpeedField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.intensityNoiseSpeed)));
            intensityNoiseSpeedField.BindEnable(useIntensityNoiseField, x => useIntensityNoiseProp.boolValue);
            intensityNoiseSpeedField.style.Indent(1);
            intensityNoiseSpeedField.RegisterValueChangeCallback(evt => RepaintWindow());
            root.Add(intensityNoiseSpeedField);
            
            var intensityNoisePowerField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.intensityNoisePower)));
            intensityNoisePowerField.BindEnable(useIntensityNoiseField, x => useIntensityNoiseProp.boolValue);
            intensityNoisePowerField.style.Indent(1);
            intensityNoisePowerField.RegisterValueChangeCallback(evt => RepaintWindow());
            root.Add(intensityNoisePowerField);
            
            var intensityNoiseStrengthField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.intensityNoiseStrength)));
            intensityNoiseStrengthField.BindEnable(useIntensityNoiseField, x => useIntensityNoiseProp.boolValue);
            intensityNoiseStrengthField.style.Indent(1);
            intensityNoiseStrengthField.RegisterValueChangeCallback(evt => RepaintWindow());
            root.Add(intensityNoiseStrengthField);
            
            root.AddSpace();
            
            var useIntensityCurveProp = serializedObject.FindProperty(nameof(Light2DClip.useIntensityCurve));
            var useIntensityCurveField = new PropertyField(useIntensityCurveProp);
            root.Add(useIntensityCurveField);
            
            var intensityCurveField = new PropertyField(serializedObject.FindProperty(nameof(Light2DClip.intensityCurve)));
            intensityCurveField.BindEnable(useIntensityCurveField, x => useIntensityCurveProp.boolValue);
            intensityCurveField.style.Indent(1);
            intensityCurveField.RegisterValueChangeCallback(evt => RepaintWindow());
            root.Add(intensityCurveField);
            
            return root;
        }
#else
        public override void OnInspectorGUI()
        {
            var director = TimelineEditor.inspectedDirector;
            if (!director)
            {
                EditorGUILayout.HelpBox("Select this clip through a Timeline window to edit it.", MessageType.Info);
                return;
            }

            var clip = (Light2DClip)target;
            var track = director.GetTrackOf<Light2DTrack>(clip);
            if (!track)
            {
                EditorGUILayout.HelpBox("Could not locate the Light2DTrack for this clip.", MessageType.Warning);
                return;
            }

            var light = director.GetGenericBinding(track) as Light2D;

            serializedObject.Update();

            EditorGUILayout.HelpBox("Set the ValueControlOptions in the track's inspector to the value you want to change.",
                MessageType.Info);
            EditorGUILayout.Space();

            DrawShapeAndEmissionSection(track, light);

            if (!light || light.lightType != Light2D.LightType.Global)
            {
                EditorGUILayout.Space();
                DrawShadowSection(track);

                EditorGUILayout.Space();
                DrawVolumetricSection(track);
            }

            EditorGUILayout.Space();
            DrawExtraFeaturesSection();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawShapeAndEmissionSection(Light2DTrack track, Light2D light)
        {
            EditorGUILayout.LabelField("Shape & Emission", EditorStyles.boldLabel);

            DrawControlledProperty(nameof(Light2DClip.color), track.colorControl);
            DrawControlledProperty(nameof(Light2DClip.intensity), track.intensityControl);

            if (!light || light.lightType == Light2D.LightType.Point)
            {
                EditorGUILayout.Space();
                DrawControlledProperty(nameof(Light2DClip.pointLightInnerRadius), track.pointLightInnerRadiusControl);
                DrawControlledProperty(nameof(Light2DClip.pointLightOuterRadius), track.pointLightOuterRadiusControl);
                DrawControlledProperty(nameof(Light2DClip.pointLightInnerAngle), track.pointLightInnerAngleControl);
                DrawControlledProperty(nameof(Light2DClip.pointLightOuterAngle), track.pointLightOuterAngleControl);
            }

            if (!light || light.lightType == Light2D.LightType.Freeform)
            {
                DrawControlledProperty(nameof(Light2DClip.falloff), track.falloffControl, FalloffLabel);
            }

            if (!light || light.lightType is Light2D.LightType.Point or Light2D.LightType.Freeform)
            {
                DrawControlledProperty(nameof(Light2DClip.falloffStrength), track.falloffStrengthControl, FalloffStrengthLabel);
            }
        }

        void DrawShadowSection(Light2DTrack track)
        {
            EditorGUILayout.LabelField("Shadows", EditorStyles.boldLabel);
            DrawControlledProperty(nameof(Light2DClip.shadowStrength), track.shadowStrengthControl);
            DrawControlledProperty(nameof(Light2DClip.shadowSoftness), track.shadowSoftnessControl);
            DrawControlledProperty(nameof(Light2DClip.shadowFalloffStrength), track.shadowFalloffStrengthControl);
        }

        void DrawVolumetricSection(Light2DTrack track)
        {
            EditorGUILayout.LabelField("Volumetrics", EditorStyles.boldLabel);
            DrawControlledProperty(nameof(Light2DClip.volumetricIntensity), track.volumetricIntensityControl);
            DrawControlledProperty(nameof(Light2DClip.volumetricShadowIntensity), track.volumetricShadowIntensityControl);
        }

        void DrawExtraFeaturesSection()
        {
            EditorGUILayout.LabelField("Extra Features", EditorStyles.boldLabel);
            var useIntensityNoiseProp = serializedObject.FindProperty(nameof(Light2DClip.useIntensityNoise));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useIntensityNoiseProp);
            if (EditorGUI.EndChangeCheck())
            {
                RepaintWindow();
            }

            using (new EditorGUI.DisabledScope(!useIntensityNoiseProp.boolValue))
            {
                EditorGUI.indentLevel++;
                DrawRepaintProperty(nameof(Light2DClip.intensityNoiseOffset));
                DrawRepaintProperty(nameof(Light2DClip.intensityNoiseSpeed));
                DrawRepaintProperty(nameof(Light2DClip.intensityNoisePower));
                DrawRepaintProperty(nameof(Light2DClip.intensityNoiseStrength));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            var useIntensityCurveProp = serializedObject.FindProperty(nameof(Light2DClip.useIntensityCurve));
            EditorGUILayout.PropertyField(useIntensityCurveProp);
            using (new EditorGUI.DisabledScope(!useIntensityCurveProp.boolValue))
            {
                EditorGUI.indentLevel++;
                DrawRepaintProperty(nameof(Light2DClip.intensityCurve), includeChildren: true);
                EditorGUI.indentLevel--;
            }
        }

        void DrawControlledProperty(string propertyName, ValueControlOption option, GUIContent label = null)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null) return;

            using (new EditorGUI.DisabledScope(option == ValueControlOption.None))
            {
                if (label != null)
                {
                    EditorGUILayout.PropertyField(property, label);
                }
                else
                {
                    EditorGUILayout.PropertyField(property);
                }
            }
        }

        void DrawRepaintProperty(string propertyName, bool includeChildren = false)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null) return;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, includeChildren);
            if (EditorGUI.EndChangeCheck())
            {
                RepaintWindow();
            }
        }

        static readonly GUIContent FalloffLabel = new("Falloff");
        static readonly GUIContent FalloffStrengthLabel = new("Falloff Strength");
#endif

        internal void NoiseRepaintCallback()
        {
            wasNoisePropertyChanged = false;
        }

        void RepaintWindow()
        {
            wasNoisePropertyChanged = true;
        }
    }
}
#endif
