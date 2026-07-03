using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    /// <summary>
    /// Timeline track for controlling time scale during cutscenes.
    /// Allows speeding up, slowing down, or pausing time at specific moments.
    /// </summary>
    [TrackColor(0.6f, 0.6f, 0f)]
    [TrackClipType(typeof(TimeScaleClip))]
    public class TimeScaleTrack : TrackAsset
    {
        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = " ";
        }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<TimeScaleMixerBehaviour>.Create(graph, inputCount);
            return playable;
        }
    }
}
