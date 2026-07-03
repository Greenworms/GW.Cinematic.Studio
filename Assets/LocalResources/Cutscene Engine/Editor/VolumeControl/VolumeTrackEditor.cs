#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
#if URP || HDRP
using UnityEngine.Rendering;
#elif UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(VolumeTrack))]
    public class VolumeTrackEditor : TrackEditor
    {
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);
            options.icon = EditorGUIUtility.ObjectContent(null, VolumeType).image as Texture2D;
            return options;
        }

#if URP || HDRP
        static readonly System.Type VolumeType = typeof(Volume);
#elif UNITY_POST_PROCESSING_STACK_V2
        static readonly System.Type VolumeType = typeof(PostProcessVolume);
#endif
    }
}
#endif
