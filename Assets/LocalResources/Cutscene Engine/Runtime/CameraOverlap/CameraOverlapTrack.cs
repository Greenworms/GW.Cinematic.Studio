using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    [TrackColor(1.0f, 0.5f, 1.0f)]
    [TrackBindingType(typeof(Camera))]
    [TrackClipType(typeof(CameraOverlapClip))]
    public class CameraOverlapTrack : TrackAsset
    {
    }
}