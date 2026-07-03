using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    public class TransformClip : PlayableAsset, ITimelineClipAsset
    {
        [HideInInspector]
        public ExposedReference<Transform> sourceTransform;

        public TransformMethod positionMethod = TransformMethod.Value;
        public Vector3 position = Vector3.zero;
        public bool applyPositionOffset;
        public Vector3 positionTransformLocalOffset = Vector3.zero;
        public Vector3 positionTransformWorldOffset = Vector3.zero;

        public TransformMethod rotationMethod = TransformMethod.Value;
        public Vector3 rotation = Vector3.zero;
        public bool applyRotationOffset;
        public Vector3 rotationTransformLocalOffset = Vector3.zero;
        public Vector3 rotationTransformWorldOffset = Vector3.zero;

        public TransformMethod scaleMethod = TransformMethod.Value;
        public Vector3 scale = Vector3.one;
        public bool applyScaleOffset;
        public Vector3 scaleTransformLocalOffset = Vector3.zero;
        public Vector3 scaleTransformWorldOffset = Vector3.zero;

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Extrapolation;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TransformBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.sourceTransform = sourceTransform.Resolve(graph.GetResolver());
            behaviour.positionMethod = positionMethod;
            behaviour.position = position;
            behaviour.applyPositionOffset = applyPositionOffset;
            behaviour.positionTransformLocalOffset = positionTransformLocalOffset;
            behaviour.positionTransformWorldOffset = positionTransformWorldOffset;
            behaviour.rotationMethod = rotationMethod;
            behaviour.rotation = rotation;
            behaviour.applyRotationOffset = applyRotationOffset;
            behaviour.rotationTransformLocalOffset = rotationTransformLocalOffset;
            behaviour.rotationTransformWorldOffset = rotationTransformWorldOffset;
            behaviour.scaleMethod = scaleMethod;
            behaviour.scale = scale;
            behaviour.applyScaleOffset = applyScaleOffset;
            behaviour.scaleTransformLocalOffset = scaleTransformLocalOffset;
            behaviour.scaleTransformWorldOffset = scaleTransformWorldOffset;

            return playable;
        }
    }
}
