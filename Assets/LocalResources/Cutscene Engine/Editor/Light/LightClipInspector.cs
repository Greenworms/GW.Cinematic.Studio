using System;
using System.Collections.Generic;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(LightClip))]
    [CanEditMultipleObjects]
    public class LightClipInspector : Editor
    {
        internal static LightClipInspector active { get; private set; }
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
            
            var clip = (LightClip)target;
            var track = director.GetTrackOf<LightTrack>(clip);
            var light = director.GetGenericBinding(track) as Light;

            var box = new HelpBox("Set the ValueControlOptions in the track's inspector to the value you want to change.", 
                HelpBoxMessageType.Info);
            box.Q<Label>().style.fontSize = 12;
            root.Add(box);
            root.AddSpace();
            
            root.Add(new Label("Shape"));

            if (!light || light.type == LightType.Directional)
            {
#if HDRP
                var angularDiameterField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.angularDiameter)));
                if(track.angularDiameterControl == ValueControlOption.None) angularDiameterField.SetEnabled(false);
                root.Add(angularDiameterField);
#endif
            }
#if URP || HDRP
            if(!light || light.type is LightType.Spot or LightType.Pyramid)
#else
            if(!light || light.type is LightType.Spot)
#endif
            
            {
                if (!light || light.type == LightType.Spot)
                {
                    var innerSpotAngleField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.innerSpotAngle)));
                    if (track.innerSpotAngleControl == ValueControlOption.None) innerSpotAngleField.SetEnabled(false);
                    root.Add(innerSpotAngleField);
                }

                var outerSpotAngleField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.outerSpotAngle)));
                if (track.spotAngleControl == ValueControlOption.None) outerSpotAngleField.SetEnabled(false);
                root.Add(outerSpotAngleField);
            }

#if HDRP
            if(!light || light.type is LightType.Rectangle or LightType.Box)
            {
                var areaSizeField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.areaSize)));
                if (track.areaSizeControl == ValueControlOption.None) areaSizeField.SetEnabled(false);
                root.Add(areaSizeField);
            }

            if (!light || light.type == LightType.Tube)
            {
                var shapeWidthProp = serializedObject.FindProperty(nameof(LightClip.areaSize) + ".x");
                var lengthField = new PropertyField(shapeWidthProp);
                lengthField.label = "Length";
                if (track.areaSizeControl == ValueControlOption.None) lengthField.SetEnabled(false);
                root.Add(lengthField);
            }
            
            if (!light || light.type is LightType.Spot or LightType.Pyramid or LightType.Disc)
            {
                var radiusField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.radius)));
                if(track.radiusControl == ValueControlOption.None) radiusField.SetEnabled(false);
                root.Add(radiusField);    
            }
            
            root.AddSpace();
            
            root.Add(new Label("Celestial Body"));
            if (!light || light.type == LightType.Directional)
            {
                var surfaceColorField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.surfaceColor)));
                if(track.surfaceColorControl == ValueControlOption.None) surfaceColorField.SetEnabled(false);
                root.Add(surfaceColorField);
            
                var flareSizeField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.flareSize)));
                if(track.flareSizeControl == ValueControlOption.None) flareSizeField.SetEnabled(false);
                root.Add(flareSizeField);
            
                var flareFalloffField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.flareFalloff)));
                if(track.flareFalloffControl == ValueControlOption.None) flareFalloffField.SetEnabled(false);
                root.Add(flareFalloffField);
            
                var flareTintField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.flareTint)));
                if(track.flareTintControl == ValueControlOption.None) flareTintField.SetEnabled(false);
                root.Add(flareTintField);
            
                var flareMultiplierField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.flareMultiplier)));
                if(track.flareMultiplierControl == ValueControlOption.None) flareMultiplierField.SetEnabled(false);
                root.Add(flareMultiplierField);
            }
#endif
            
            
            root.AddSpace();
            root.Add(new Label("Emission"));
            var colorField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.color)));
            if(track.colorControl == ValueControlOption.None) colorField.SetEnabled(false);
            root.Add(colorField);
            
            var temperatureField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.colorTemperature)));
            if(track.colorTemperatureControl == ValueControlOption.None) temperatureField.SetEnabled(false);
            root.Add(temperatureField);

            var intensityField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.intensity)));
            if(track.intensityControl == ValueControlOption.None) intensityField.SetEnabled(false);
            root.Add(intensityField);
            
            var bounceIntensityField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.bounceIntensity)));
            if(track.bounceIntensityControl == ValueControlOption.None) bounceIntensityField.SetEnabled(false);
            root.Add(bounceIntensityField);
            
            
#if HDRP
            bounceIntensityField.label = "Indirect Multiplier";
            var intensityMultiplierField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.intensityMultiplier)));
            if(track.intensityMultiplierControl == ValueControlOption.None) intensityMultiplierField.SetEnabled(false);
            root.Add(intensityMultiplierField);
#endif

            
            
            var rangeField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.range)));
            if(track.rangeControl == ValueControlOption.None) rangeField.SetEnabled(false);
            root.Add(rangeField);
            
#if HDRP
            root.AddSpace();
            root.Add(new Label("Volumetrics"));
            
            var vDimmerField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.volumetricMultiplier)));
            if(track.volumetricMultiplierControl == ValueControlOption.None) vDimmerField.SetEnabled(false);
            root.Add(vDimmerField);
            
            var vShadowDimmerField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.volumetricShadowDimmer)));
            if(track.volumetricShadowDimmerControl == ValueControlOption.None) vShadowDimmerField.SetEnabled(false);
            root.Add(vShadowDimmerField);
#endif
            
            root.AddSpace();
            root.Add(new Label("Shadows"));
            var shadowStrengthField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.shadowStrength)));
            if(track.shadowStrengthControl == ValueControlOption.None) shadowStrengthField.SetEnabled(false);
            root.Add(shadowStrengthField);

#if HDRP
            var shadowTintField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.shadowTint)));
            if(track.shadowTintControl == ValueControlOption.None) shadowTintField.SetEnabled(false);
            root.Add(shadowTintField);
#endif
            
            root.AddSpace();
            root.AddSpace();
            root.Add(new Label("Extra Features"));
            var useIntensityNoiseProp = serializedObject.FindProperty(nameof(LightClip.useIntensityNoise));
            var useIntensityNoiseField = new PropertyField(useIntensityNoiseProp);
            root.Add(useIntensityNoiseField);
            
            var intensityNoiseOffsetField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.intensityNoiseOffset)));
            intensityNoiseOffsetField.BindEnable(useIntensityNoiseField, x => useIntensityNoiseProp.boolValue);
            intensityNoiseOffsetField.style.Indent(1);
            intensityNoiseOffsetField.RegisterValueChangeCallback(evt => RepaintWindow());
            root.Add(intensityNoiseOffsetField);
            
            var intensityNoiseSpeedField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.intensityNoiseSpeed)));
            intensityNoiseSpeedField.BindEnable(useIntensityNoiseField, x => useIntensityNoiseProp.boolValue);
            intensityNoiseSpeedField.style.Indent(1);
            intensityNoiseSpeedField.RegisterValueChangeCallback(evt => RepaintWindow());
            root.Add(intensityNoiseSpeedField);
            
            var intensityNoisePowerField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.intensityNoisePower)));
            intensityNoisePowerField.BindEnable(useIntensityNoiseField, x => useIntensityNoiseProp.boolValue);
            intensityNoisePowerField.style.Indent(1);
            intensityNoisePowerField.RegisterValueChangeCallback(evt => RepaintWindow());
            root.Add(intensityNoisePowerField);
            
            var intensityNoiseStrengthField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.intensityNoiseStrength)));
            intensityNoiseStrengthField.BindEnable(useIntensityNoiseField, x => useIntensityNoiseProp.boolValue);
            intensityNoiseStrengthField.style.Indent(1);
            intensityNoiseStrengthField.RegisterValueChangeCallback(evt => RepaintWindow());
            root.Add(intensityNoiseStrengthField);
            
            root.AddSpace();
            
            var useIntensityCurveProp = serializedObject.FindProperty(nameof(LightClip.useIntensityCurve));
            var useIntensityCurveField = new PropertyField(useIntensityCurveProp);
            root.Add(useIntensityCurveField);
            
            var intensityCurveField = new PropertyField(serializedObject.FindProperty(nameof(LightClip.intensityCurve)));
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

            var clip = (LightClip)target;
            var track = director.GetTrackOf<LightTrack>(clip);
            if (!track)
            {
                EditorGUILayout.HelpBox("Could not locate the LightTrack for this clip.", MessageType.Warning);
                return;
            }

            var light = director.GetGenericBinding(track) as Light;

            serializedObject.Update();

            EditorGUILayout.HelpBox("Set the ValueControlOptions in the track's inspector to the value you want to change.",
                MessageType.Info);
            EditorGUILayout.Space();

            DrawShapeSection(track, light);
#if HDRP
            EditorGUILayout.Space();
            DrawCelestialBodySection(track, light);
#endif
            EditorGUILayout.Space();
            DrawEmissionSection(track);
#if HDRP
            EditorGUILayout.Space();
            DrawVolumetricSection(track);
#endif
            EditorGUILayout.Space();
            DrawShadowSection(track);
            EditorGUILayout.Space();
            DrawExtraFeaturesSection();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawShapeSection(LightTrack track, Light light)
        {
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);

#if HDRP
            if (!light || light.type == LightType.Directional)
            {
                DrawControlledProperty(nameof(LightClip.angularDiameter), track.angularDiameterControl);
            }
#endif

#if (URP || HDRP) && UNITY_6000_0_OR_NEWER
            var showSpotOrPyramid = !light || light.type is LightType.Spot or LightType.Pyramid;
#else
            var showSpotOrPyramid = !light || light.type is LightType.Spot;
#endif
            if (showSpotOrPyramid)
            {
                if (!light || light.type == LightType.Spot)
                {
                    DrawControlledProperty(nameof(LightClip.innerSpotAngle), track.innerSpotAngleControl);
                }

                DrawControlledProperty(nameof(LightClip.outerSpotAngle), track.spotAngleControl);
            }

#if HDRP
            if (!light || light.type is LightType.Rectangle or LightType.Box)
            {
                DrawControlledProperty(nameof(LightClip.areaSize), track.areaSizeControl);
            }

            if (!light || light.type == LightType.Tube)
            {
                DrawControlledProperty(nameof(LightClip.areaSize) + ".x", track.areaSizeControl, LengthLabel);
            }

            if (!light || light.type is LightType.Spot or LightType.Pyramid or LightType.Disc)
            {
                DrawControlledProperty(nameof(LightClip.radius), track.radiusControl);
            }
#endif
        }

#if HDRP
        void DrawCelestialBodySection(LightTrack track, Light light)
        {
            EditorGUILayout.LabelField("Celestial Body", EditorStyles.boldLabel);
            if (light && light.type != LightType.Directional) return;

            DrawControlledProperty(nameof(LightClip.surfaceColor), track.surfaceColorControl);
            DrawControlledProperty(nameof(LightClip.flareSize), track.flareSizeControl);
            DrawControlledProperty(nameof(LightClip.flareFalloff), track.flareFalloffControl);
            DrawControlledProperty(nameof(LightClip.flareTint), track.flareTintControl);
            DrawControlledProperty(nameof(LightClip.flareMultiplier), track.flareMultiplierControl);
        }
#endif

        void DrawEmissionSection(LightTrack track)
        {
            EditorGUILayout.LabelField("Emission", EditorStyles.boldLabel);
            DrawControlledProperty(nameof(LightClip.color), track.colorControl);
            DrawControlledProperty(nameof(LightClip.colorTemperature), track.colorTemperatureControl);
            DrawControlledProperty(nameof(LightClip.intensity), track.intensityControl);

#if HDRP
            DrawControlledProperty(nameof(LightClip.bounceIntensity), track.bounceIntensityControl, IndirectMultiplierLabel);
#else
            DrawControlledProperty(nameof(LightClip.bounceIntensity), track.bounceIntensityControl);
#endif

#if HDRP
            DrawControlledProperty(nameof(LightClip.intensityMultiplier), track.intensityMultiplierControl);
#endif

            DrawControlledProperty(nameof(LightClip.range), track.rangeControl);
        }

#if HDRP
        void DrawVolumetricSection(LightTrack track)
        {
            EditorGUILayout.LabelField("Volumetrics", EditorStyles.boldLabel);
            DrawControlledProperty(nameof(LightClip.volumetricMultiplier), track.volumetricMultiplierControl);
            DrawControlledProperty(nameof(LightClip.volumetricShadowDimmer), track.volumetricShadowDimmerControl);
        }
#endif

        void DrawShadowSection(LightTrack track)
        {
            EditorGUILayout.LabelField("Shadows", EditorStyles.boldLabel);
            DrawControlledProperty(nameof(LightClip.shadowStrength), track.shadowStrengthControl);
#if HDRP
            DrawControlledProperty(nameof(LightClip.shadowTint), track.shadowTintControl);
#endif
        }

        void DrawExtraFeaturesSection()
        {
            EditorGUILayout.LabelField("Extra Features", EditorStyles.boldLabel);
            var useIntensityNoiseProp = serializedObject.FindProperty(nameof(LightClip.useIntensityNoise));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useIntensityNoiseProp);
            if (EditorGUI.EndChangeCheck())
            {
                RepaintWindow();
            }

            using (new EditorGUI.DisabledScope(!useIntensityNoiseProp.boolValue))
            {
                EditorGUI.indentLevel++;
                DrawRepaintProperty(nameof(LightClip.intensityNoiseOffset));
                DrawRepaintProperty(nameof(LightClip.intensityNoiseSpeed));
                DrawRepaintProperty(nameof(LightClip.intensityNoisePower));
                DrawRepaintProperty(nameof(LightClip.intensityNoiseStrength));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            var useIntensityCurveProp = serializedObject.FindProperty(nameof(LightClip.useIntensityCurve));
            EditorGUILayout.PropertyField(useIntensityCurveProp);
            using (new EditorGUI.DisabledScope(!useIntensityCurveProp.boolValue))
            {
                EditorGUI.indentLevel++;
                DrawRepaintProperty(nameof(LightClip.intensityCurve), includeChildren: true);
                EditorGUI.indentLevel--;
            }
        }

        void DrawControlledProperty(string propertyName, ValueControlOption option, GUIContent label = null, bool includeChildren = false)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null) return;

            using (new EditorGUI.DisabledScope(option == ValueControlOption.None))
            {
                if (label != null)
                {
                    EditorGUILayout.PropertyField(property, label, includeChildren);
                }
                else
                {
                    EditorGUILayout.PropertyField(property, includeChildren);
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

        static readonly GUIContent LengthLabel = new("Length");
#if HDRP
        static readonly GUIContent IndirectMultiplierLabel = new("Indirect Multiplier");
#endif
#endif

        internal void NoiseRepaintCallback()
        {
            wasNoisePropertyChanged = false;
        }

        void RepaintWindow()
        {
            wasNoisePropertyChanged = true;
            // var window = TimelineEditor.GetWindow();
            // if (window)
            // {
            //     Debug.Log($"repaint window");
            //     window.Repaint();
            // }
        }
    }
}
