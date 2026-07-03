#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    [TrackClipType(typeof(ParticleClip))]
    public class ParticleTrack : PlayableTrack
    {
        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var c = clip.asset as ParticleClip;
            c.start = clip.start;
            c.end = clip.end;
            var ps = c.particleSystem.Resolve(graph.GetResolver());
            if (ps && ps.main.playOnAwake)
            {
                var main = ps.main;
                main.playOnAwake = false;
                Debug.Log($"[Cutscene Engine] The playOnAwake property of the ParticleSystem({ps.name}) is automatically set to false. " +
                          $"If you want to turn on the particles from the beginning using playOnAwake, " +
                          $"please use SignalReciever or something similar to stop it at the desired point.");

#if UNITY_EDITOR
                EditorUtility.SetDirty(ps);
#endif
            }
            
            return base.CreatePlayable(graph, gameObject, clip);
        }
#if UNITY_EDITOR
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<ParticleMixerBehaviour>.Create(graph, inputCount);
            return playable;
        }
#endif
    }
}