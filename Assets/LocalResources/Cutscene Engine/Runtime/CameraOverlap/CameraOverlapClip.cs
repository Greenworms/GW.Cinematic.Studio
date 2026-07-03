using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    [Serializable]
    public class CameraOverlapClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        [Tooltip("The sorting order of the canvas on which the camera’s overlap is drawn. " +
                 "If there are skip buttons or subtitles, use a lower value than the sorting order of other canvases.")]
        public int sortingOrder;
        [Tooltip("If the camera that needs to overlap is moving, set this value to true.\nThen it will follow the movement of the camera that needs to overlap.\n")]
        public bool followCam;

        [Tooltip("A curve showing the change in transparency of the camera overlap. [0..1]")]
        public AnimationCurve opacityCurve = new AnimationCurve()
        {
            keys = new[]
            {
                new Keyframe(0,1),
                new Keyframe(0.25f,0.95f, -0.5f, -0.5f),
                new Keyframe(1,0),
            }
        };
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CameraOverlapBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.sortingOrder = sortingOrder;
            behaviour.followCam = followCam;
            behaviour.opacityCurve = opacityCurve;

            return playable;
        }
    }
}