using System;
#if CINEMACHINE_3_OR_NEWER
using Unity.Cinemachine;
#elif CINEMACHINE
using Cinemachine;
#endif
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    public class ImpulseClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

#if CINEMACHINE_2_8_OR_NEWER
        [HideInInspector]
        [Tooltip("The definition of the Cinemachine Impulse.")]
        public CinemachineImpulseDefinition impulseDefinition = new CinemachineImpulseDefinition();
#elif CINEMACHINE
        [HideInInspector]
        public CinemachineImpulseDefinition impulseDefinition = new CinemachineImpulseDefinition();
#endif
        [Tooltip("The velocity of the impulse.")]
        public Vector3 velocity = Vector3.down;
        [HideInInspector]
        [Tooltip("The point where the impulse will occur.")]
        public ExposedReference<Transform> impulsePoint;

        void Reset()
        {
#if CINEMACHINE_3_OR_NEWER
            impulseDefinition = new CinemachineImpulseDefinition
            {
                ImpulseChannel = 1,
                ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
                CustomImpulseShape = new AnimationCurve(),
                ImpulseDuration = 0.2f,
                ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                DissipationDistance = 100,
                DissipationRate = 0.25f,
                PropagationSpeed = 343
            };
#elif CINEMACHINE_2_8_OR_NEWER
            impulseDefinition = new CinemachineImpulseDefinition
            {
                m_ImpulseChannel = 1,
                m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
                m_CustomImpulseShape = new AnimationCurve(),
                m_ImpulseDuration = 0.2f,
                m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                m_DissipationDistance = 100,
                m_DissipationRate = 0.25f,
                m_PropagationSpeed = 343
            };
#elif CINEMACHINE
            impulseDefinition = new CinemachineImpulseDefinition();
            impulseDefinition.OnValidate();
#endif

        }


        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ImpulseBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
#if CINEMACHINE
            behaviour.impulseDefinition = impulseDefinition;
            behaviour.velocity = velocity;
            behaviour.impulsePoint = impulsePoint.Resolve(graph.GetResolver());
#endif
            return playable;
        }
    }
}
