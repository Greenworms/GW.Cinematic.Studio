using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    public class TimeScaleClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("The target time scale. \n" +
                 "The minimum value is 0.001, as the timeline cannot progress when the time scale becomes 0.")]
        public float timeScale = 1;
        [Tooltip("Multiplier curve applied over the clip's normalized time.")]
        public AnimationCurve multiplier = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        public ClipCaps clipCaps => ClipCaps.Extrapolation | ClipCaps.Blending;
        public const float MinTimeScale = 0.001f;
        void OnValidate()
        {
            if (timeScale < MinTimeScale) timeScale = MinTimeScale;
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimeScaleBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.timeScale = timeScale;
            behaviour.multiplier = multiplier;
            
            return playable;
        }
    }
}
