using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    [Serializable]

    public class LightBehaviour : PlayableBehaviour
    {
        public float innerSpotAngle;
        public float spotAngle;
        public Vector2 areaSize;
        
        public Color color;
        public float colorTemperature;
        public float intensity;
        public float bounceIntensity;
        public float range;
        public float shadowStrength;

        public bool useIntensityNoise;
        public byte intensityNoiseOffset;
        public float intensityNoiseSpeed;
        public float intensityNoisePower;
        public float intensityNoiseStrength;

        public bool useIntensityCurve;
        public AnimationCurve intensityCurve;

#if HDRP
        public float angularDiameter;
        public float radius;
        
        public Color surfaceColor;
        public float flareSize;
        public float flareFalloff;
        public Color flareTint;
        public float flareMultiplier;
        
        public float intensityMultiplier;
        public float volumetricDimmer;
        public float volumetricShadowDimmer;
        
        public Color shadowTint;
#endif

        internal double duration { get; set; }
        
    }

}