using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Greenworms.Cinematics.CharacterStory.Production
{
    public sealed class CharacterStoryLookAtClip : PlayableAsset, ITimelineClipAsset
    {
        public ExposedReference<Transform> Target;
        [Range(0f, 1f)] public float Weight = 1f;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<CharacterStoryLookAtBehaviour> playable =
                ScriptPlayable<CharacterStoryLookAtBehaviour>.Create(graph);
            CharacterStoryLookAtBehaviour behaviour = playable.GetBehaviour();
            behaviour.Target = Target.Resolve(graph.GetResolver());
            behaviour.Weight = Weight;
            return playable;
        }
    }
}
