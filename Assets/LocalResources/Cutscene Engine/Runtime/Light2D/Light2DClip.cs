#if URP
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;

namespace CutsceneEngine
{
    [Serializable]
    public class Light2DClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("The color of the light.")]
        public Color color = Color.white;
        [Tooltip("The intensity of the light.")]
        public float intensity = 1f;

        [Tooltip("The inner radius of the point light.")]
        public float pointLightInnerRadius = 0f;
        [Tooltip("The outer radius of the point light.")]
        public float pointLightOuterRadius = 5f;

        [Tooltip("The inner angle of the spot light.")]
        [Range(0, 179)] public float pointLightInnerAngle = 20f;
        [Tooltip("The outer angle of the spot light.")]
        [Range(0, 179)] public float pointLightOuterAngle = 45f;
        
        [Tooltip("The shape falloff size of the light.")]
        public float falloff = 1f;
        [Tooltip("The falloff strength of the light.")]
        public float falloffStrength = 1f;

        [Tooltip("The strength of the shadow.")]
        public float shadowStrength = 1;
        [Tooltip("The softness of the shadow.")]
        public float shadowSoftness = 0.5f;
        [Tooltip("The falloff strength of the shadow.")]
        public float shadowFalloffStrength = 1f;
        
        [Tooltip("The intensity of the volumetric light.")]
        public float volumetricIntensity = 1;
        [Tooltip("The intensity of the volumetric shadow.")]
        public float volumetricShadowIntensity = 0.75f;

        [Tooltip("This adds noise to the intensity of the light.")]
        public bool useIntensityNoise;
        [Tooltip("The offset from where the noise starts.")]
        [Range(0, 255)]public byte intensityNoiseOffset;
        [Tooltip("The speed of the noise.")]
        public float intensityNoiseSpeed = 1;
        [Tooltip("This value is the square of the noise.")]
        public float intensityNoisePower = 1;
        [Tooltip("This is the value that is multiplied by the noise.")]
        public float intensityNoiseStrength = 1;

        [Tooltip("If this value is true, the final light intensity is multiplied by the value evaluated on the curve.")]
        public bool useIntensityCurve;
        [Tooltip("A curve that is multiplied to the final Intensity of the light.")]
        public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0,1,1,1);
        
        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Extrapolation;
        public double start;
        public double end;
        void Reset()
        {
            intensityNoiseOffset = (byte)Random.Range(byte.MinValue, byte.MaxValue);
        }

        void OnValidate()
        {
            if (intensity < 0) intensity = 0;
            if (pointLightInnerRadius < 0) pointLightInnerRadius = 0;
            if (pointLightOuterRadius < 0) pointLightOuterRadius = 0;
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<Light2DBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            
            behaviour.color = color;
            behaviour.intensity = intensity;
            
            behaviour.pointLightInnerRadius = pointLightInnerRadius;
            behaviour.pointLightOuterRadius = pointLightOuterRadius;
            behaviour.pointLightInnerAngle = pointLightInnerAngle;
            behaviour.pointLightOuterAngle = pointLightOuterAngle;
            
            behaviour.falloff = falloff;
            behaviour.falloffStrength = falloffStrength;
            
            behaviour.shadowIntensity = shadowStrength;
            behaviour.shadowSoftness = shadowSoftness;
            behaviour.shadowFalloffStrength = shadowFalloffStrength;
            
            behaviour.volumetricIntensity = volumetricIntensity;
            behaviour.volumetricShadowIntensity = volumetricShadowIntensity;
            
            behaviour.useIntensityNoise = useIntensityNoise;
            behaviour.intensityNoiseOffset = intensityNoiseOffset;
            behaviour.intensityNoiseSpeed = intensityNoiseSpeed;
            behaviour.intensityNoisePower = intensityNoisePower;
            behaviour.intensityNoiseStrength = intensityNoiseStrength;

            behaviour.useIntensityCurve = useIntensityCurve;
            behaviour.intensityCurve = intensityCurve;

            behaviour.duration = end - start;

            return playable;
        }
    }
}
#endif