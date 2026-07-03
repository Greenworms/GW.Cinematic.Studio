using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;

namespace CutsceneEngine
{
    [Serializable]
    public class LightClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("The inner spot angle.")]
        [Range(0, 179)]public float innerSpotAngle = 20;
        [Tooltip("The outer spot angle.")]
        [Range(0, 179)]public float outerSpotAngle = 30;
        [Tooltip("The size of the area light.")]
        public Vector2 areaSize = Vector2.one;
        
        [Tooltip("The color of the light.")]
        public Color color = Color.white;
        [Tooltip("The color temperature of the light.")]
        [Range(1500, 20000)]public float colorTemperature = 6570;
        [Tooltip("The intensity of the light.")]
        public float intensity = 1f;
        [Tooltip("The bounce intensity of the light.")]
        public float bounceIntensity = 1f;
        [Tooltip("The range of the light.")]
        public float range = 10;
        [Tooltip("The strength of the shadow.")]
        [Range(0, 1)] public float shadowStrength = 1;
        
#if HDRP
        [Tooltip("The angular diameter of the light. (HDRP only)")]
        public float angularDiameter = 0.5f;
        [Tooltip("The radius of the light. (HDRP only)")]
        public float radius = 0.025f;
        
        [Tooltip("The surface color of the light. (HDRP only)")]
        public Color surfaceColor = Color.white;
        [Tooltip("The size of the flare. (HDRP only)")]
        [Range(0, 90)]public float flareSize = 2;
        [Tooltip("The falloff of the flare. (HDRP only)")]
        public float flareFalloff = 4;
        [Tooltip("The tint of the flare. (HDRP only)")]
        public Color flareTint = Color.white;
        [Tooltip("The multiplier of the flare. (HDRP only)")]
        [Range(0, 1)]public float flareMultiplier = 1;
        
        
        [Tooltip("The intensity multiplier of the light. (HDRP only)")]
        [Range(0, 16)]public float intensityMultiplier = 1f;
        
        [Tooltip("The volumetric multiplier of the light. (HDRP only)")]
        [Range(0, 16)] public float volumetricMultiplier = 1;
        [Tooltip("The volumetric shadow dimmer of the light. (HDRP only)")]
        [Range(0, 1)] public float volumetricShadowDimmer = 1;
        
        [Tooltip("The tint of the shadow. (HDRP only)")]
        public Color shadowTint = Color.black;
#endif

        [Tooltip("This adds noise to the intensity of the light.\n" +
                 "It is \"added\" to the base Intensity of the light after the Intensity of the clip has been processed in the manner of the ValueControlOption. \n\n" +
                 "This noise is a simple 1D Perlin noise. Other types of noise may be added in the future.")]
        public bool useIntensityNoise;
        [Tooltip("The offset from where the noise starts. \n" +
                 "You can change this value to any value to make the noise random. If this value is the same, the same noise is generated")]
        [Range(0, 255)]public byte intensityNoiseOffset;
        [Tooltip("The speed of the noise. The higher this value, the faster the flickering occurs.")]
        public float intensityNoiseSpeed = 1;
        [Tooltip("This value is the square of the noise. As you can see from the clip's GUI, the higher this value, the more extreme the noise becomes. \n" +
                 "That is, a low value will result in a soft noise like a candle flame, while a high value will result in an extreme noise like a flickering light bulb.")]
        public float intensityNoisePower = 1;
        [Tooltip("This is the value that is multiplied by the noise. If a negative number is multiplied, the noise is \"subtracted\" from the Intensity.\n" +
                 "Also, if the absolute value of this value increases, the intensity of the noise increases. The difference from Power is that Power does not change the maximum intensity of the noise.")]
        public float intensityNoiseStrength = 1;

        [Tooltip("If this value is true, the final light intensity is multiplied by the value evaluated on the curve.")]
        public bool useIntensityCurve;
        [Tooltip("A curve that is multiplied to the final Intensity of the light. 0 is the value at the beginning of the clip, and 1 is the value at the end of the clip. \n" +
                 "This value also changes smoothly as the clips are blended. When blending with clips that do not use an Intensity Curve, it is blended based on 1.")]
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
            if (bounceIntensity < 0) bounceIntensity = 0;
            if (range < 0) range = 0;
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LightBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            
            behaviour.innerSpotAngle = innerSpotAngle;
            behaviour.spotAngle = outerSpotAngle;
            behaviour.areaSize = areaSize;
            
            behaviour.color = color;
            behaviour.colorTemperature = colorTemperature;
            behaviour.intensity = intensity;
            behaviour.bounceIntensity = bounceIntensity;
            behaviour.range = range;

            behaviour.shadowStrength = shadowStrength;
#if HDRP
            behaviour.angularDiameter = angularDiameter;
            behaviour.radius = radius;
            
            behaviour.surfaceColor = surfaceColor; 
            behaviour.flareSize = flareSize; 
            behaviour.flareFalloff = flareFalloff; 
            behaviour.flareTint = flareTint; 
            behaviour.flareMultiplier = flareMultiplier;
            
            behaviour.intensityMultiplier = intensityMultiplier;
            behaviour.volumetricDimmer = volumetricMultiplier;
            behaviour.volumetricShadowDimmer = volumetricShadowDimmer;

            behaviour.shadowTint = shadowTint;
#endif
            
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