using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    /// <summary>
    /// Timeline track for controlling Volume profiles in Render Pipelines.
    /// Manages post-processing effects during cutscenes with priority-based blending.
    /// </summary>
    [TrackColor(0.5f, 0.9f, 1f)]
    [TrackClipType(typeof(VolumeClip))]
    public class VolumeTrack : TrackAsset
    {
#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2

        [Tooltip(BasePriorityTooltip)]
        public float basePriority = 100;

        [Tooltip(VolumeLayerTooltip)]
        public int volumeLayer;

        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            if (clip.asset is VolumeClip c)
            {
                c.startTime = clip.start;
                c.basePriority = basePriority;
                c.volumeLayer = volumeLayer;
            }

            return base.CreatePlayable(graph, gameObject, clip);
        }

#if URP || HDRP
        const string BasePriorityTooltip = "Priority assigned to the Volume so it overrides gameplay volumes.";
        const string VolumeLayerTooltip = "Layer of the GameObject that holds the Volume component.";
#else
        const string BasePriorityTooltip = "Priority assigned to the PostProcessVolume so it overrides gameplay volumes.";
        const string VolumeLayerTooltip = "Layer of the GameObject that holds the PostProcessVolume component.";
#endif
#endif
    }
}
