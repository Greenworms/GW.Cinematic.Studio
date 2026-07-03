#if URP
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;

namespace CutsceneEngine
{
    public class Light2DMixerBehaviour : PlayableBehaviour
    {
        internal Light2DTrack track;
        Light2D light;

        Color _initialColor;
        float _initialIntensity;
        
        float _initialPointLightInnerRadius;
        float _initialPointLightOuterRadius;
        float _initialPointLightInnerAngle;
        float _initialPointLightOuterAngle;
        
        float _initialShapeLightFalloffSize;
        float _initialFalloffStrength;
        
        float _initialShadowIntensity;
        float _initialShadowSoftness;
        float _initialShadowFalloffStrength;
        
        float _initialVolumetricIntensity;
        float _initialVolumetricShadowIntensity;


        bool _initialized;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            light = playerData as Light2D;
            if (!light) return;
            Initialize();

            var inputCount = playable.GetInputCount();
            var totalWeight = 0f;

            var blendedColor = Color.clear;
            var blendedIntensity = 0f;
            var blendedShapeLightFalloffSize = 0f;
            var blendedFalloffStrength = 0f;
            
            var blendedPointLightInnerRadius = 0f;
            var blendedPointLightOuterRadius = 0f;
            var blendedPointLightInnerAngle = 0f;
            var blendedPointLightOuterAngle = 0f;
            
            var blendedShadowIntensity = 0f;
            var blendedShadowSoftness = 0f;
            var blendedShadowFalloffStrength = 0f;
            
            var blendedVolumetricIntensity = 0f;
            var blendedVolumetricShadowIntensity = 0f;


            for (int i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<Light2DBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();
                
                totalWeight += inputWeight;

                blendedColor += behaviour.color * inputWeight;
                blendedIntensity += behaviour.intensity * inputWeight;
                
                blendedPointLightInnerRadius += behaviour.pointLightInnerRadius * inputWeight;
                blendedPointLightOuterRadius += behaviour.pointLightOuterRadius * inputWeight;
                blendedPointLightInnerAngle += behaviour.pointLightInnerAngle * inputWeight;
                blendedPointLightOuterAngle += behaviour.pointLightOuterAngle * inputWeight;
                
                blendedFalloffStrength += behaviour.falloffStrength * inputWeight;
                blendedShapeLightFalloffSize += behaviour.falloff * inputWeight;
                
                blendedShadowIntensity += behaviour.shadowIntensity * inputWeight;
                blendedShadowSoftness += behaviour.shadowSoftness * inputWeight;
                blendedShadowFalloffStrength += behaviour.shadowFalloffStrength * inputWeight;
                
                blendedVolumetricIntensity += behaviour.volumetricIntensity * inputWeight;
                blendedVolumetricShadowIntensity += behaviour.volumetricShadowIntensity * inputWeight;


                if(behaviour.useIntensityNoise)
                {
                    var t = (float)inputPlayable.GetTime() * behaviour.intensityNoiseSpeed + behaviour.intensityNoiseOffset;
                    var noise = Mathf.Pow(Mathf.PerlinNoise1D(t), behaviour.intensityNoisePower) * inputWeight;
                    noise *= behaviour.intensityNoiseStrength;
                    blendedIntensity += noise;
                }
            }
            
            light.color = ControlValue(in track.colorControl, light.color, _initialColor, blendedColor, totalWeight);
            blendedIntensity = ControlValue(in track.intensityControl, light.intensity, _initialIntensity, blendedIntensity, totalWeight);
            
            light.pointLightInnerRadius = ControlValue(in track.pointLightInnerRadiusControl, light.pointLightInnerRadius, _initialPointLightInnerRadius, blendedPointLightInnerRadius, totalWeight);
            light.pointLightOuterRadius = ControlValue(in track.pointLightOuterRadiusControl, light.pointLightOuterRadius, _initialPointLightOuterRadius, blendedPointLightOuterRadius, totalWeight);
            light.pointLightInnerAngle = ControlValue(in track.pointLightInnerAngleControl, light.pointLightInnerAngle, _initialPointLightInnerAngle, blendedPointLightInnerAngle, totalWeight);
            light.pointLightOuterAngle = ControlValue(in track.pointLightOuterAngleControl, light.pointLightOuterAngle, _initialPointLightOuterAngle, blendedPointLightOuterAngle, totalWeight);
            
            light.shapeLightFalloffSize = ControlValue(in track.falloffControl, light.shapeLightFalloffSize, _initialShapeLightFalloffSize, blendedShapeLightFalloffSize, totalWeight);
            light.falloffIntensity = ControlValue(in track.falloffStrengthControl, light.falloffIntensity, _initialFalloffStrength, blendedFalloffStrength, totalWeight);
            
            light.shadowIntensity = Mathf.Clamp01(ControlValue(in track.shadowStrengthControl, light.shadowIntensity, _initialShadowIntensity, blendedShadowIntensity, totalWeight));
#if UNITY_6000_0_OR_NEWER
            light.shadowSoftness = Mathf.Clamp01(ControlValue(in track.shadowSoftnessControl, light.shadowSoftness, _initialShadowSoftness, blendedShadowSoftness, totalWeight));
            light.shadowSoftnessFalloffIntensity = Mathf.Clamp01(ControlValue(in track.shadowFalloffStrengthControl, light.shadowSoftnessFalloffIntensity, _initialShadowFalloffStrength, blendedShadowFalloffStrength, totalWeight));
#endif
            

            light.volumeIntensity = ControlValue(in track.volumetricIntensityControl, light.volumeIntensity, _initialVolumetricIntensity, blendedVolumetricIntensity, totalWeight);
            light.shadowVolumeIntensity = ControlValue(in track.volumetricShadowIntensityControl, light.shadowVolumeIntensity, _initialVolumetricShadowIntensity, blendedVolumetricShadowIntensity, totalWeight);

            
            var curveMultiplier = 0f;
            var totalCurveWeight = 0f;
            for (int i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<Light2DBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();

                if (behaviour.useIntensityCurve)
                {
                    totalCurveWeight += inputWeight;
                    var t = (float)(inputPlayable.GetTime() / behaviour.duration);
                    t = Mathf.Repeat(t, (float)behaviour.duration);
                    
                    curveMultiplier += behaviour.intensityCurve.Evaluate(t) * inputWeight;
                }
            }
            light.intensity = blendedIntensity * (1 - totalCurveWeight) + blendedIntensity * curveMultiplier * totalCurveWeight;
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

            _initialColor = light.color;
            _initialIntensity = light.intensity;
            
            _initialPointLightInnerRadius = light.pointLightInnerRadius;
            _initialPointLightOuterRadius = light.pointLightOuterRadius;
            _initialPointLightInnerAngle = light.pointLightInnerAngle;
            _initialPointLightOuterAngle = light.pointLightOuterAngle;
            
            _initialShapeLightFalloffSize = light.shapeLightFalloffSize;
            _initialFalloffStrength = light.falloffIntensity;
            
            _initialShadowIntensity = light.shadowIntensity;
#if UNITY_6000_0_OR_NEWER
            _initialShadowSoftness = light.shadowSoftness;
            _initialShadowFalloffStrength = light.shadowSoftnessFalloffIntensity;
#endif
            
            _initialVolumetricIntensity = light.volumeIntensity;
            _initialVolumetricShadowIntensity = light.shadowVolumeIntensity;
            
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
#endif