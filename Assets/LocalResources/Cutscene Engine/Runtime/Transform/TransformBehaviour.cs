using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    [Serializable]
    public class TransformBehaviour : PlayableBehaviour
    {
        public Transform sourceTransform;

        public TransformMethod positionMethod;
        public Vector3 position;
        public bool applyPositionOffset;
        public Vector3 positionTransformLocalOffset;
        public Vector3 positionTransformWorldOffset;

        public TransformMethod rotationMethod;
        public Vector3 rotation;
        public bool applyRotationOffset;
        public Vector3 rotationTransformLocalOffset;
        public Vector3 rotationTransformWorldOffset;

        public TransformMethod scaleMethod;
        public Vector3 scale;
        public bool applyScaleOffset;
        public Vector3 scaleTransformLocalOffset;
        public Vector3 scaleTransformWorldOffset;
    }
}
