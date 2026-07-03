#if URP
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    [TrackColor(0.5f, 1f, 0.5f)]
    [TrackClipType(typeof(Light2DClip))]
    [TrackBindingType(typeof(Light2D))]
    public class Light2DTrack : TrackAsset
    {
        [Tooltip("Control option for the color.")]
        public ValueControlOption colorControl = ValueControlOption.Replace;
        [Tooltip("Control option for the intensity.")]
        public ValueControlOption intensityControl = ValueControlOption.Multiply;
        
        [Tooltip("Control option for the inner radius.")]
        public ValueControlOption pointLightInnerRadiusControl = ValueControlOption.None;
        [Tooltip("Control option for the outer radius.")]
        public ValueControlOption pointLightOuterRadiusControl = ValueControlOption.None;
        [Tooltip("Control option for the inner angle.")]
        public ValueControlOption pointLightInnerAngleControl = ValueControlOption.None;
        [Tooltip("Control option for the outer angle.")]
        public ValueControlOption pointLightOuterAngleControl = ValueControlOption.None;
        
        [Tooltip("Control option for the shape falloff size.")]
        public ValueControlOption falloffControl = ValueControlOption.None;
        [Tooltip("Control option for the falloff strength.")]
        public ValueControlOption falloffStrengthControl = ValueControlOption.None;
        
        [Tooltip("Control option for the shadow intensity.")]
        public ValueControlOption shadowStrengthControl = ValueControlOption.None;
        [Tooltip("Control option for the shadow softness.")]
        public ValueControlOption shadowSoftnessControl = ValueControlOption.None;
        [Tooltip("Control option for the shadow falloff intensity.")]
        public ValueControlOption shadowFalloffStrengthControl = ValueControlOption.None;
        
        [Tooltip("Control option for the volumetric intensity.")]
        public ValueControlOption volumetricIntensityControl = ValueControlOption.None;
        [Tooltip("Control option for the volumetric shadow intensity.")]
        public ValueControlOption volumetricShadowIntensityControl = ValueControlOption.None;

        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = " ";
        }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<Light2DMixerBehaviour>.Create(graph, inputCount);
            var behaviour = playable.GetBehaviour();
            behaviour.track = this;

            return playable;
        }

        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var c = clip.asset as Light2DClip;
            c.start = clip.start;
            c.end = clip.end;
            
            return base.CreatePlayable(graph, gameObject, clip);
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var light = director.GetGenericBinding(this) as Light2D;
            if (!light) return;
            
            driver.AddFromName<Light2D>(light.gameObject, "m_Color");
            driver.AddFromName<Light2D>(light.gameObject, "m_Intensity");
            
            driver.AddFromName<Light2D>(light.gameObject, "m_PointLightInnerRadius");
            driver.AddFromName<Light2D>(light.gameObject, "m_PointLightOuterRadius");
            driver.AddFromName<Light2D>(light.gameObject, "m_PointLightInnerAngle");
            driver.AddFromName<Light2D>(light.gameObject, "m_PointLightOuterAngle");
            
            driver.AddFromName<Light2D>(light.gameObject, "m_ShapeLightFalloffSize");
            driver.AddFromName<Light2D>(light.gameObject, "m_FalloffIntensity");
            
            driver.AddFromName<Light2D>(light.gameObject, "m_ShadowIntensity");
            driver.AddFromName<Light2D>(light.gameObject, "m_ShadowSoftness");
            driver.AddFromName<Light2D>(light.gameObject, "m_ShadowSoftnessFalloffIntensity");
            
            driver.AddFromName<Light2D>(light.gameObject, "m_LightVolumeIntensity");
            driver.AddFromName<Light2D>(light.gameObject, "m_ShadowVolumeIntensity");

            base.GatherProperties(director, driver);
        }
    }
}
#endif