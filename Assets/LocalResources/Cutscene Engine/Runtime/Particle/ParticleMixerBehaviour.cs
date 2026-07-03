using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace CutsceneEngine
{
#if UNITY_EDITOR
    public class ParticleMixerBehaviour : PlayableBehaviour
    {
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!Application.isPlaying)
            {
                if(info.effectiveWeight <= 0)
                {
                    var inputCount = playable.GetInputCount();
                    for (int i = 0; i < inputCount; i++)
                    {
                        var inputPlayable = (ScriptPlayable<ParticleBehaviour>)playable.GetInput(i);
                        var behaviour = inputPlayable.GetBehaviour();

                        if (behaviour.particleSystem) behaviour.particleSystem.Clear();
                    }
                }
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!Application.isPlaying)
            {
                var inputCount = playable.GetInputCount();
                for (int i = 0; i < inputCount; i++)
                {
                    var inputPlayable = (ScriptPlayable<ParticleBehaviour>)playable.GetInput(i);
                    var behaviour = inputPlayable.GetBehaviour();
                    
                    Simulate(behaviour, playable);
                }
            }
        }
        
        void Simulate(ParticleBehaviour behaviour, Playable playable)
        {
            if(Application.isPlaying) return;
            var ps = behaviour.particleSystem;
            var playableTime = playable.GetTime();
            if(!ps) return;
            if (playableTime < behaviour.start)
            {
                if(ps.particleCount > 0) ps.Clear(true);
            }
            else if (behaviour.start <= playableTime && playableTime < behaviour.end)
            {
                ps.Simulate((float)(playableTime - behaviour.start), behaviour.controlChildren, true);
            }
            else
            {
                if(ps.particleCount > 0)
                {
                    if(behaviour.stopOnEnd)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
                ps.Clear(true);
                ps.Simulate((float)(behaviour.end - behaviour.start), behaviour.controlChildren, true);
                ps.Stop(behaviour.controlChildren, ParticleSystemStopBehavior.StopEmitting);
                ps.Simulate((float)(playableTime - behaviour.end), behaviour.controlChildren, false);
            }
        }
    }
#endif
}