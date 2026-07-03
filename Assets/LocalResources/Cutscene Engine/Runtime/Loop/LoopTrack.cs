using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    /// <summary>
    /// Timeline track for creating looping sections within cutscenes.
    /// Allows parts of the timeline to repeat based on LoopClip configurations.
    /// </summary>
    [TrackColor(1.0f, 1.0f, 1.0f)]
    [TrackClipType(typeof(LoopClip))]
    public class LoopTrack : TrackAsset
    {
        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.duration = 1;
        }

        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var loopClip = clip.asset as LoopClip;
            loopClip.start = clip.start;
            loopClip.end = clip.end;
            var director = gameObject.GetComponent<PlayableDirector>();
            loopClip.endAtTimelineEnd = Math.Abs(director.duration - clip.end) < 0.0001;
            return base.CreatePlayable(graph, gameObject, clip);
        }
    }
}