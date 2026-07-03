#if URP
using System;
using UnityEngine;
using UnityEngine.Playables;

namespace CutsceneEngine
{
    [Serializable]
    public class Light2DBehaviour : PlayableBehaviour
    {
        public Color color;
        public float intensity;
        
        public float pointLightInnerRadius;
        public float pointLightOuterRadius;
        public float pointLightInnerAngle;
        public float pointLightOuterAngle;
        
        public float falloff;
        public float falloffStrength;
        
        public float shadowIntensity;
        public float shadowSoftness;
        public float shadowFalloffStrength;
        
        public float volumetricIntensity;
        public float volumetricShadowIntensity;

        public bool useIntensityNoise;
        public byte intensityNoiseOffset;
        public float intensityNoiseSpeed;
        public float intensityNoisePower;
        public float intensityNoiseStrength;

        public bool useIntensityCurve;
        public AnimationCurve intensityCurve;

        internal double duration { get; set; }
    }
}
#endif