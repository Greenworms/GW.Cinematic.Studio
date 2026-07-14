using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Greenworms.Cinematics.CharacterStory.Production
{
    public sealed class CharacterStoryLookAtBehaviour : PlayableBehaviour
    {
        public Transform Target;
        public float Weight;
    }

    public sealed class CharacterStoryLookAtMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            CharacterStoryLookAtPresenter presenter = playerData as CharacterStoryLookAtPresenter;
            if (presenter == null)
                return;

            Transform selectedTarget = null;
            float selectedWeight = 0f;
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight <= 0f)
                    continue;
                ScriptPlayable<CharacterStoryLookAtBehaviour> input =
                    (ScriptPlayable<CharacterStoryLookAtBehaviour>)playable.GetInput(i);
                CharacterStoryLookAtBehaviour behaviour = input.GetBehaviour();
                float weighted = inputWeight * behaviour.Weight;
                if (behaviour.Target == null || weighted <= selectedWeight)
                    continue;
                selectedTarget = behaviour.Target;
                selectedWeight = weighted;
            }
            presenter.EvaluateTarget(selectedTarget, selectedWeight);
        }
    }

    [TrackClipType(typeof(CharacterStoryLookAtClip))]
    [TrackBindingType(typeof(CharacterStoryLookAtPresenter))]
    public sealed class CharacterStoryLookAtTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<CharacterStoryLookAtMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
