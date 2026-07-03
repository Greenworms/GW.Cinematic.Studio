using System;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace CutsceneEngine
{
    public class ParticleBehaviour : PlayableBehaviour
    {
        public bool stopOnEnd;
        public bool controlChildren;
        public ParticleSystem particleSystem;

        public double start;
        public double end;
        
        bool _seedInitialized;
        bool _shouldReplay;
        public override void OnGraphStart(Playable playable)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (particleSystem && !_seedInitialized && particleSystem.useAutoRandomSeed)
                {
                    InitializeSeed(particleSystem);
                    _seedInitialized = true;
                }
            }
#endif
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        { 
            PlayParticle();
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if(playable.GetTime() > 0 && info.weight <= 0 && stopOnEnd)
            {
                StopParticle();
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (_shouldReplay)
            {
                particleSystem.Play(controlChildren);
                _shouldReplay = false;
            }
        }

        void PlayParticle()
        {
            if(!Application.isPlaying) return;
            if(particleSystem == null) return;
            
            particleSystem.Play(controlChildren);

            // Activation트랙과 파티클의 시작이 같은 경우, 게임 오브젝트가 아직 활성화가 안되어있을 수 있으므로
            // 이때는 한 프레임 기다렸다가 파티클을 재생함. 
            if(!particleSystem.isPlaying)
            {
#if UNITY_2023_2_OR_NEWER
                var wait = Awaitable.NextFrameAsync();
                wait.GetAwaiter().OnCompleted(() =>
                {
                    particleSystem.Play();    
                });
#else
                _shouldReplay = true;
#endif
            }
        }
        

        void StopParticle()
        {
            if(!Application.isPlaying) return;
            if(particleSystem == null) return;
            // _played나 particleSystem.isPlaying으로 필터링하는 조건문 넣지 말 것. 그거 넣으면 프리뷰때 파티클이 안멈춤.
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        static void InitializeSeed(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
                return;

            var seed = (uint)Random.Range(uint.MinValue, uint.MaxValue);

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.randomSeed = seed;

            foreach (var child in particleSystem.GetComponentsInChildren<ParticleSystem>())
            {
                child.randomSeed = ++seed;
            }

            for (int i = 0; i < particleSystem.subEmitters.subEmittersCount; i++)
            {
                InitializeSeed(particleSystem.subEmitters.GetSubEmitterSystem(i));
            }
        }
    }
}