using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#if HDRP
using UnityEngine.Rendering.HighDefinition;
#endif


namespace CutsceneEngine
{
    [TrackColor(1f, 1f, 0.5f)]
    [TrackClipType(typeof(LightClip))]
    [TrackBindingType(typeof(Light))]
    public class LightTrack : TrackAsset
    {
        [Tooltip("Control option for the inner spot angle.")]
        public ValueControlOption innerSpotAngleControl = ValueControlOption.None;
        [Tooltip("Control option for the spot angle.")]
        public ValueControlOption spotAngleControl = ValueControlOption.None;
        
        [Space]
        [Tooltip("Control option for the color.")]
        public ValueControlOption colorControl = ValueControlOption.Replace;
        [Tooltip("Control option for the color temperature.")]
        public ValueControlOption colorTemperatureControl = ValueControlOption.None;
        [Space]
        [Tooltip("Control option for the intensity.")]
        public ValueControlOption intensityControl = ValueControlOption.Multiply;
        [Tooltip("Control option for the bounce intensity.")]
        public ValueControlOption bounceIntensityControl = ValueControlOption.None;
        [Tooltip("Control option for the range.")]
        public ValueControlOption rangeControl = ValueControlOption.None;

        [Tooltip("Control option for the shadow strength.")]
        public ValueControlOption shadowStrengthControl = ValueControlOption.None;
        
#if HDRP
        [Tooltip("Control option for the area size. (HDRP only)")]
        public ValueControlOption areaSizeControl = ValueControlOption.None;
        [Space]
        [Tooltip("Control option for the angular diameter. (HDRP only)")]
        public ValueControlOption angularDiameterControl = ValueControlOption.None;
        [Tooltip("Control option for the radius. (HDRP only)")]
        public ValueControlOption radiusControl = ValueControlOption.None;

        [Tooltip("Control option for the surface color. (HDRP only)")]
        public ValueControlOption surfaceColorControl = ValueControlOption.None;
        [Tooltip("Control option for the flare size. (HDRP only)")]
        public ValueControlOption flareSizeControl = ValueControlOption.None;
        [Tooltip("Control option for the flare falloff. (HDRP only)")]
        public ValueControlOption flareFalloffControl = ValueControlOption.None;
        [Tooltip("Control option for the flare tint. (HDRP only)")]
        public ValueControlOption flareTintControl = ValueControlOption.None;
        [Tooltip("Control option for the flare multiplier. (HDRP only)")]
        public ValueControlOption flareMultiplierControl = ValueControlOption.None;
        
        [Tooltip("Control option for the intensity multiplier. (HDRP only)")]
        public ValueControlOption intensityMultiplierControl = ValueControlOption.None;
        [Tooltip("Control option for the volumetric multiplier. (HDRP only)")]
        public ValueControlOption volumetricMultiplierControl = ValueControlOption.None;
        [Tooltip("Control option for the volumetric shadow dimmer. (HDRP only)")]
        public ValueControlOption volumetricShadowDimmerControl = ValueControlOption.None;
        
        [Tooltip("Control option for the shadow tint. (HDRP only)")]
        public ValueControlOption shadowTintControl = ValueControlOption.None;
#endif
        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = " ";
        }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<LightMixerBehaviour>.Create(graph, inputCount);
            var behaviour = playable.GetBehaviour();
            behaviour.track = this;

            return playable;
        }

        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var c = clip.asset as LightClip;
            c.start = clip.start;
            c.end = clip.end;
            
            return base.CreatePlayable(graph, gameObject, clip);
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var light = director.GetGenericBinding(this) as Light;
            if (!light) return;
            

            driver.AddFromName<Light>(light.gameObject, "m_Color");
            driver.AddFromName<Light>(light.gameObject, "m_ColorTemperature");
            driver.AddFromName<Light>(light.gameObject, "m_Intensity");
            driver.AddFromName<Light>(light.gameObject, "m_Range");
            driver.AddFromName<Light>(light.gameObject, "m_BounceIntensity");
            
            driver.AddFromName<Light>(light.gameObject, "m_InnerSpotAngle");
            driver.AddFromName<Light>(light.gameObject, "m_SpotAngle");
#if HDRP
            
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "m_AngularDiameter");
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "m_ShapeRadius");
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "m_ShapeWidth");
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "m_ShapeHeight");
            
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "surfaceTint");
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "flareSize");
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "flareFalloff");
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "flareTint");
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "flareMultiplier");
            
            
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "m_LightDimmer"); // IntensityMultiplier
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "m_ShadowDimmer");
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "m_VolumetricDimmer");
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "m_VolumetricShadowDimmer");
            
            driver.AddFromName<HDAdditionalLightData>(light.gameObject, "m_ShadowTint");
            
#else
            driver.AddFromName<Light>(light.gameObject, "m_Shadows.m_Strength");
            
#endif
            base.GatherProperties(director, driver);
        }
    }
}