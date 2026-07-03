using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
#if UNITY_POST_PROCESSING_STACK_V2 && !URP && !HDRP
using UnityEngine.Rendering.PostProcessing;
#endif
using UnityEngine.Timeline;

#if URP || HDRP
using VolumeProfileAsset = UnityEngine.Rendering.VolumeProfile;
#elif UNITY_POST_PROCESSING_STACK_V2
using VolumeProfileAsset = UnityEngine.Rendering.PostProcessing.PostProcessProfile;
#endif

namespace CutsceneEngine
{
    [Serializable]
    public class VolumeClip :  PlayableAsset, ITimelineClipAsset
    {
#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Extrapolation | ClipCaps.Looping;

        public double startTime;
        [Tooltip(VolumePriorityTooltip)]
        public float basePriority;
        [Tooltip(VolumeLayerTooltip)]
        public int volumeLayer;
        [HideInInspector]
        [Tooltip(VolumeProfileTooltip)]
        public VolumeProfileAsset volumeProfile;
        
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<VolumeBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.volumeProfile = volumeProfile;
            behaviour.volumeLayer = volumeLayer;
            behaviour.volumePriority = basePriority + (float)startTime;

            return playable;
        }

#if URP || HDRP
        const string VolumePriorityTooltip = "The base priority applied to the Volume component.";
        const string VolumeLayerTooltip = "The layer assigned to the GameObject that carries the Volume component.";
        const string VolumeProfileTooltip = "The Volume profile played during the clip.";
#else
        const string VolumePriorityTooltip = "The base priority applied to the PostProcessVolume.";
        const string VolumeLayerTooltip = "The layer assigned to the PostProcessVolume GameObject.";
        const string VolumeProfileTooltip = "The Post-processing profile that will be played.";
#endif
#else
        public ClipCaps clipCaps => ClipCaps.None;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<VolumeBehaviour>.Create(graph);
        }
#endif

    }
}
