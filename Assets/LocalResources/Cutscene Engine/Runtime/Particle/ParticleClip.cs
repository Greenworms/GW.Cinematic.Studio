using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;

namespace CutsceneEngine
{
    [Serializable]
    public class ParticleClip : PlayableAsset, ITimelineClipAsset, IPropertyPreview
    {
        public ClipCaps clipCaps => ClipCaps.None;
        public ExposedReference<ParticleSystem> particleSystem;
        public bool stopOnEnd;
        public bool controlChildren = true;
        public bool connectName = true;
        
        public double start { get; internal set; }
        public double end { get; internal set; }
        

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ParticleBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            
            behaviour.start = start;
            behaviour.end = end;
            
            behaviour.stopOnEnd = stopOnEnd;
            behaviour.controlChildren = controlChildren;
            var binding = particleSystem.Resolve(graph.GetResolver());
            behaviour.particleSystem = binding;
            
            return playable;
        }
        
        

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var ps = particleSystem.Resolve(director);
            
            if(ps)
            {
                foreach (var child in ps.GetComponentsInChildren<ParticleSystem>())
                {
                    driver.AddFromName<ParticleSystem>(child.gameObject, "autoRandomSeed");
                }
            }
        }
        
    }
}