#if CINEMACHINE_3_OR_NEWER
using Unity.Cinemachine;
#elif CINEMACHINE
using Cinemachine;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Playables;

namespace CutsceneEngine
{
    public class ImpulseBehaviour : PlayableBehaviour
    {
#if CINEMACHINE_2_8_OR_NEWER
        public CinemachineImpulseDefinition impulseDefinition;
#elif CINEMACHINE
        public CinemachineImpulseDefinition impulseDefinition;
#endif
        public Vector3 velocity = Vector3.down;
        public Transform impulsePoint;
        
        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            ImpulseOnCinemachine();
        }

#if UNITY_EDITOR && CINEMACHINE && !CINEMACHINE_3_OR_NEWER && !CINEMACHINE_2_8_OR_NEWER
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if(!Application.isPlaying) ClearReactionNoise();
        }
#endif

        public override void OnGraphStop(Playable playable)
        {
#if UNITY_EDITOR
            if(!Application.isPlaying)ClearReactionNoise();
#endif
        }
        
        void ImpulseOnCinemachine()
        {
#if CINEMACHINE_3_OR_NEWER
            if (impulseDefinition == null) return;

            if (impulseDefinition.ImpulseType != CinemachineImpulseDefinition.ImpulseTypes.Uniform && impulsePoint)
            {
                impulseDefinition.CreateEvent(impulsePoint.position, velocity);
            }
            else
            {
                impulseDefinition.CreateEvent(Vector3.zero, velocity);
            }
#elif CINEMACHINE_2_8_OR_NEWER
            if (impulseDefinition == null) return;

            if (impulseDefinition.m_ImpulseType != CinemachineImpulseDefinition.ImpulseTypes.Uniform && impulsePoint)
            {
                impulseDefinition.CreateEvent(impulsePoint.position, velocity);
            }
            else
            {
                impulseDefinition.CreateEvent(Vector3.zero, velocity);
            }
#elif CINEMACHINE
            if (impulseDefinition == null) return;

            if (impulsePoint)
            {
                impulseDefinition.CreateEvent(impulsePoint.position, velocity);
            }
            else
            {
                impulseDefinition.CreateEvent(Vector3.zero, velocity);
            }
#endif
        }
        void ClearReactionNoise()
        {
#if CINEMACHINE_3_OR_NEWER
            if(!Application.isPlaying)
            {
                // When pausing the timeline preview, reset settings to disable playback of reactions to impulses.
                foreach (var listener in Object.FindObjectsByType<CinemachineImpulseListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    var settings = listener.ReactionSettings;
                    var newSettings = new CinemachineImpulseListener.ImpulseReaction();
                    newSettings.m_SecondaryNoise = settings.m_SecondaryNoise;
                    newSettings.AmplitudeGain = settings.AmplitudeGain;
                    newSettings.FrequencyGain = settings.FrequencyGain;
                    newSettings.Duration = settings.Duration;

                    listener.ReactionSettings = newSettings;
                }
            }
#elif CINEMACHINE
            if(!Application.isPlaying)
            {
                // When pausing the timeline preview, reset settings to disable playback of reactions to impulses.
                foreach (var listener in Object.FindObjectsByType<CinemachineImpulseListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    var brain = CinemachineCore.Instance.FindPotentialTargetBrain(listener.VirtualCamera);
                    var updateMode = brain.m_UpdateMethod;
                    brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.ManualUpdate;

                    CinemachineCore.CurrentTimeOverride = 1000;
                    CinemachineCore.UniformDeltaTimeOverride = 1000;
                    
                    brain.ManualUpdate();
                    brain.m_UpdateMethod = updateMode;
                    
                    CinemachineCore.CurrentTimeOverride = -1;
                    CinemachineCore.UniformDeltaTimeOverride = -1;
                }
            }
#endif
        }
    }
}