using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
#if HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

#if URP
using UnityEngine.Rendering.Universal;
#endif

using UnityEngine.Timeline;

namespace CutsceneEngine
{
    public class LightMixerBehaviour : PlayableBehaviour
    {
        internal LightTrack track;
        Light light;

        float _initialInnerSpotAngle;
        float _initialSpotAngle;
        
        Color _initialColor;
        float _initialColorTemperature;
        float _initialIntensity;
        float _initialBounceIntensity;
        float _initialRange;
        
        float _initialShadowStrength;
        
#if HDRP
        HDAdditionalLightData _lightData;

        Vector2 _initialAreaSize;

        float _initialAngularDiameter;
        float _initialRadius;
        float _initialIntensityMultiplier;
        float _initialVolumetricMultiplier;
        float _initialVolumetricShadowDimmer;

        Color _initialSurfaceColor;
        float _initialFlareSize;
        float _initialFlareFalloff;
        Color _initialFlareTint;
        float _initialFlareMultiplier;
        Color _initialShadowTint;
#elif URP
        UniversalAdditionalLightData _lightData;
#endif

        bool _initialized;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            light = playerData as Light;
            if (!light) return;
            Initialize();


            var inputCount = playable.GetInputCount();

            var totalWeight = 0f;

            var innerSpotAngle = 0f;
            var spotAngle = 0f;
            var areaSize = Vector2.zero;
            
            var color = Color.clear;
            var colorTemparature = 0f;
            var intensity = 0f;
            var bounceIntensity = 0f;
            var range = 0f;

            var shadowStrength = 0f;
#if HDRP
            var surfaceColor = Color.clear;
            var flareSize = 0f;
            var flareFalloff = 0f;
            var flareTint = Color.clear;
            var flareMultiplier = 0f;
            
            var angularDiameter = 0f;
            var radius = 0f;
            
            var intensityMultiplier = 0f;
            var vDimmer = 0f;
            var vShadowDimmer = 0f;
            var shadowTint = Color.clear;
#endif
            

            for (int i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<LightBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();
                
                totalWeight += inputWeight;

                innerSpotAngle += behaviour.innerSpotAngle * inputWeight;
                spotAngle += behaviour.spotAngle * inputWeight;
                areaSize += behaviour.areaSize * inputWeight;
                
                color += behaviour.color * inputWeight;
                colorTemparature += behaviour.colorTemperature * inputWeight;
                intensity += behaviour.intensity * inputWeight;
                
                if(behaviour.useIntensityNoise)
                {
                    var t = (float)inputPlayable.GetTime() * behaviour.intensityNoiseSpeed + behaviour.intensityNoiseOffset;
                    var noise = Mathf.Pow(Mathf.PerlinNoise1D(t), behaviour.intensityNoisePower) * inputWeight;
                    noise *= behaviour.intensityNoiseStrength;
                    intensity += noise;
                }
                
                bounceIntensity += behaviour.bounceIntensity * inputWeight;
                range += behaviour.range * inputWeight;
                
                shadowStrength += behaviour.shadowStrength * inputWeight;
#if HDRP
                surfaceColor += behaviour.surfaceColor * inputWeight;
                flareSize += behaviour.flareSize * inputWeight;
                flareFalloff += behaviour.flareFalloff * inputWeight;
                flareTint += behaviour.flareTint * inputWeight;
                flareMultiplier += behaviour.flareMultiplier * inputWeight;
                
                angularDiameter += behaviour.angularDiameter * inputWeight;
                radius += behaviour.radius * inputWeight;
                
                intensityMultiplier += behaviour.intensityMultiplier * inputWeight;
                vDimmer += behaviour.volumetricDimmer * inputWeight;
                vShadowDimmer += behaviour.volumetricShadowDimmer * inputWeight;
                shadowTint += behaviour.shadowTint * inputWeight;
#endif
            }
            
            light.innerSpotAngle = ControlValue(in track.innerSpotAngleControl, light.innerSpotAngle, _initialInnerSpotAngle, innerSpotAngle, totalWeight); 
            
            light.spotAngle = ControlValue(in track.spotAngleControl, light.spotAngle, _initialSpotAngle, spotAngle, totalWeight);
            

            light.color = ControlValue(in track.colorControl, light.color, _initialColor, color, totalWeight);
            light.colorTemperature = ControlValue(in track.colorTemperatureControl, light.colorTemperature, _initialColorTemperature, colorTemparature, totalWeight);
            intensity = ControlValue(in track.intensityControl, light.intensity, _initialIntensity, intensity, totalWeight);
            light.bounceIntensity = ControlValue(in track.bounceIntensityControl, light.bounceIntensity, _initialBounceIntensity, bounceIntensity, totalWeight);
            light.range = ControlValue(in track.rangeControl, light.range, _initialRange, range, totalWeight);
            
#if HDRP
            _lightData.shapeWidth = ControlValue(in track.areaSizeControl, _lightData.shapeWidth, _initialAreaSize.x, areaSize.x, totalWeight);
            _lightData.shapeHeight = ControlValue(in track.areaSizeControl, _lightData.shapeHeight, _initialAreaSize.y, areaSize.y, totalWeight);
            
            _lightData.surfaceTint = ControlValue(in track.surfaceColorControl, _lightData.surfaceTint, _initialSurfaceColor, surfaceColor, totalWeight);
            _lightData.flareSize = ControlValue(in track.flareSizeControl, _lightData.flareSize, _initialFlareSize, flareSize, totalWeight);
            _lightData.flareFalloff = ControlValue(in track.flareSizeControl, _lightData.flareFalloff, _initialFlareFalloff, flareFalloff, totalWeight);
            _lightData.flareTint = ControlValue(in track.flareSizeControl, _lightData.flareTint, _initialFlareTint, flareTint, totalWeight);
            _lightData.flareMultiplier = ControlValue(in track.flareSizeControl, _lightData.flareMultiplier, _initialFlareMultiplier, flareMultiplier, totalWeight);
            
            _lightData.angularDiameter = ControlValue(in track.angularDiameterControl, _lightData.angularDiameter, _initialAngularDiameter, angularDiameter, totalWeight);
            _lightData.shapeRadius = ControlValue(in track.radiusControl, _lightData.shapeRadius, _initialRadius, radius, totalWeight);

            _lightData.lightDimmer = ControlValue(in track.intensityMultiplierControl, _lightData.lightDimmer, _initialIntensityMultiplier, intensityMultiplier, totalWeight);
            _lightData.shadowDimmer = ControlValue(in track.shadowStrengthControl, _lightData.shadowDimmer, _initialShadowStrength, shadowStrength, totalWeight);
            _lightData.volumetricDimmer = ControlValue(in track.volumetricMultiplierControl, _lightData.volumetricDimmer, _initialVolumetricMultiplier, vDimmer, totalWeight);
            _lightData.volumetricShadowDimmer = ControlValue(in track.volumetricShadowDimmerControl, _lightData.volumetricShadowDimmer, _initialVolumetricShadowDimmer, vShadowDimmer, totalWeight);
            _lightData.shadowTint = ControlValue(in track.shadowTintControl, _lightData.shadowTint, _initialShadowTint, shadowTint, totalWeight);
#else
            light.shadowStrength = ControlValue(in track.shadowStrengthControl, light.shadowStrength, _initialShadowStrength, shadowStrength, totalWeight);
#endif
            
            
            
            var curveMultiplier = 0f;
            var totalCurveWeight = 0f;
            for (int i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<LightBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();

                if (behaviour.useIntensityCurve)
                {
                    totalCurveWeight += inputWeight;
                    var t = (float)(inputPlayable.GetTime() / behaviour.duration);
                    t = Mathf.Repeat(t, (float)behaviour.duration);
                    
                    curveMultiplier += behaviour.intensityCurve.Evaluate(t) * inputWeight;
                }
            }
            light.intensity = intensity * (1 - totalCurveWeight) + intensity * curveMultiplier * totalCurveWeight;
        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            var inputCount = playable.GetInputCount();
            var weightSum = 0f;
            for (int i = 0; i < inputCount; i++)
            {
                weightSum += playable.GetInputWeight(i);
            }
            if (weightSum <= 0)
            {
                UnInitialize();
            }
        }
        public override void OnPlayableDestroy(Playable playable)
        {
            UnInitialize();
        }

        void Initialize()
        {
            if (_initialized) return;

            _initialInnerSpotAngle = light.innerSpotAngle;
            _initialSpotAngle = light.spotAngle;
            
            _initialColor = light.color;
            _initialColorTemperature = light.colorTemperature;
            _initialIntensity = light.intensity;
            _initialBounceIntensity = light.bounceIntensity;
            _initialRange = light.range;
            _initialShadowStrength = light.shadowStrength;
            
#if HDRP
            _lightData = light.GetComponent<HDAdditionalLightData>();

            _initialAreaSize = light.areaSize;

            _initialAngularDiameter = _lightData.angularDiameter;
            _initialRadius = _lightData.shapeRadius;
            _initialIntensityMultiplier = _lightData.lightDimmer;
            _initialShadowStrength = _lightData.shadowDimmer;
            _initialVolumetricMultiplier = _lightData.volumetricDimmer;
            _initialVolumetricShadowDimmer = _lightData.volumetricShadowDimmer;
            
            _initialSurfaceColor = _lightData.surfaceTint;
            _initialFlareSize = _lightData.flareSize;
            _initialFlareFalloff = _lightData.flareFalloff;
            _initialFlareTint = _lightData.flareTint;
            _initialFlareMultiplier = _lightData.flareMultiplier;

            _initialShadowTint = _lightData.shadowTint;
#endif
            
            
            _initialized = true;
        }

        void UnInitialize()
        {
            _initialized = false;
        }

        static float ControlValue(in ValueControlOption option, float bindingValue, float initialValue, float inputValue, float weight)
        {
            return option switch
            {
                ValueControlOption.None => bindingValue,
                ValueControlOption.Replace => Mathf.Lerp(initialValue, inputValue, weight),
                ValueControlOption.Add => initialValue + inputValue,
                ValueControlOption.Multiply => initialValue * inputValue,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        static Color ControlValue(in ValueControlOption option, Color bindingValue, Color initialValue, Color inputValue, float weight)
        {
            return option switch
            {
                ValueControlOption.None => bindingValue,
                ValueControlOption.Replace => Color.Lerp(initialValue, inputValue, weight),
                ValueControlOption.Add => initialValue + inputValue,
                ValueControlOption.Multiply => initialValue * inputValue,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

}