using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    public class TimeScaleMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var inputCount = playable.GetInputCount();
            var timeScale = 0f;
            var totalWeight = 0f;

            
            for (int i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);

                var playableInput = (ScriptPlayable<TimeScaleBehaviour>)playable.GetInput(i);
                var behaviour = playableInput.GetBehaviour();

                var duration = playableInput.GetDuration();
                var normalizedTime = duration > double.Epsilon ? (float)(playableInput.GetTime() / duration) : 0f;
                var curveValue = behaviour.multiplier != null
                    ? behaviour.multiplier.Evaluate(Mathf.Clamp01(normalizedTime))
                    : 1f;

                timeScale += inputWeight * behaviour.timeScale * curveValue;
                totalWeight += inputWeight;
            }

            var scale = Mathf.Max(TimeScaleClip.MinTimeScale, Mathf.Lerp(1, timeScale, Mathf.Clamp01(totalWeight)));
            Time.timeScale = scale;
        }


        public override void OnPlayableDestroy(Playable playable)
        {
            Time.timeScale = 1;
        }
    }
}
