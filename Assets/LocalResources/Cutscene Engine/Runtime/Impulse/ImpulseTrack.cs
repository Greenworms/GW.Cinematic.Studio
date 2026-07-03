#if CINEMACHINE_3_OR_NEWER
using Unity.Cinemachine;
#elif CINEMACHINE
using Cinemachine;
#endif
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    [TrackColor(0.2f, 0.8f, 0.2f)]
    [TrackClipType(typeof(ImpulseClip))]
    public class ImpulseTrack : TrackAsset
    {

        protected override void OnCreateClip(TimelineClip clip)
        {
#if CINEMACHINE_3_OR_NEWER
            var impulseClip = clip.asset as ImpulseClip;
            if(impulseClip.impulseDefinition.ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Legacy)
                clip.duration = impulseClip.impulseDefinition.TimeEnvelope.Duration;    
            else 
                clip.duration = impulseClip.impulseDefinition.ImpulseDuration;
            
#elif CINEMACHINE_2_8_OR_NEWER
            var impulseClip = clip.asset as ImpulseClip;
            if(impulseClip.impulseDefinition.m_ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Legacy)
                clip.duration = impulseClip.impulseDefinition.m_TimeEnvelope.Duration;    
            else 
                clip.duration = impulseClip.impulseDefinition.m_ImpulseDuration;
#elif CINEMACHINE
            var impulseClip = clip.asset as ImpulseClip;
            clip.duration = impulseClip.impulseDefinition.m_TimeEnvelope.Duration;
#endif
        }
    }
}