using UnityEngine;
using UnityEngine.Playables;

namespace CutsceneEngine
{
    public class TransformMixerBehaviour : PlayableBehaviour
    {
        public bool controlActivation;
        Vector3 _initialPos;
        Quaternion _initialRot;
        Vector3 _initialScale;

        Transform transform;

        bool _initialized;

        public override void OnPlayableDestroy(Playable playable)
        {
            _initialized = false;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            transform = playerData as Transform;

            if (transform == null)
                return;

            if (!_initialized)
            {
                _initialPos = transform.localPosition;
                _initialRot = transform.localRotation;
                _initialScale = transform.localScale;
                _initialized = true;
            }

            var blendedPosition = Vector3.zero;
            var blendedEulerAngles = Vector3.zero;
            var blendedScale = Vector3.zero;

            var inputCount = playable.GetInputCount();

            var totalWeight = 0f;
            for (int i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                if (inputWeight <= 0) continue;
                totalWeight += inputWeight;

                var inputPlayable = (ScriptPlayable<TransformBehaviour>)playable.GetInput(i);
                var input = inputPlayable.GetBehaviour();

                var refTransform = input.sourceTransform;

                Vector3 targetPosition;
                if (input.positionMethod == TransformMethod.Value)
                {
                    targetPosition = input.position;
                    if (input.applyPositionOffset)
                    {
                        targetPosition += _initialPos;
                    }
                }
                else
                {
                    if (refTransform)
                    {
                        var worldPosition = refTransform.position;
                        if (input.positionTransformLocalOffset != Vector3.zero)
                        {
                            worldPosition += refTransform.TransformVector(input.positionTransformLocalOffset);
                        }

                        worldPosition += input.positionTransformWorldOffset;

                        targetPosition = transform.parent != null
                            ? transform.parent.InverseTransformPoint(worldPosition)
                            : worldPosition;
                    }
                    else
                    {
                        targetPosition = _initialPos;
                    }
                }

                blendedPosition += targetPosition * inputWeight;
                
                Vector3 targetEuler;
                if (input.rotationMethod == TransformMethod.Value)
                {
                    targetEuler = input.rotation;
                    if (input.applyRotationOffset)
                    {
                        targetEuler += _initialRot.eulerAngles;
                    }
                }
                else
                {
                    if (refTransform)
                    {
                        Quaternion targetWorldRotation = refTransform.rotation;
                        if (input.rotationTransformWorldOffset != Vector3.zero)
                        {
                            targetWorldRotation = Quaternion.Euler(input.rotationTransformWorldOffset) * targetWorldRotation;
                        }

                        if (input.rotationTransformLocalOffset != Vector3.zero)
                        {
                            targetWorldRotation *= Quaternion.Euler(input.rotationTransformLocalOffset);
                        }

                        Quaternion targetLocalRotation = transform.parent != null
                            ? Quaternion.Inverse(transform.parent.rotation) * targetWorldRotation
                            : targetWorldRotation;
                        targetEuler = targetLocalRotation.eulerAngles;
                    }
                    else
                    {
                        targetEuler = _initialRot.eulerAngles;
                    }
                }

                blendedEulerAngles += targetEuler * inputWeight;
                
                Vector3 targetScale;
                if (input.scaleMethod == TransformMethod.Value)
                {
                    targetScale = input.scale;
                    if (input.applyScaleOffset)
                    {
                        targetScale += _initialScale;
                    }
                }
                else
                {
                    if (refTransform)
                    {
                        targetScale = refTransform.localScale;

                        if (input.scaleTransformWorldOffset != Vector3.zero)
                        {
                            var parentLossyScale = transform.parent ? transform.parent.lossyScale : Vector3.one;
                            Vector3 localWorldOffset = new Vector3(
                                parentLossyScale.x != 0 ? input.scaleTransformWorldOffset.x / parentLossyScale.x : input.scaleTransformWorldOffset.x,
                                parentLossyScale.y != 0 ? input.scaleTransformWorldOffset.y / parentLossyScale.y : input.scaleTransformWorldOffset.y,
                                parentLossyScale.z != 0 ? input.scaleTransformWorldOffset.z / parentLossyScale.z : input.scaleTransformWorldOffset.z
                            );
                            targetScale += localWorldOffset;
                        }

                        if (input.scaleTransformLocalOffset != Vector3.zero)
                        {
                            targetScale += input.scaleTransformLocalOffset;
                        }
                    }
                    else
                    {
                        targetScale = _initialScale;
                    }
                }

                blendedScale += targetScale * inputWeight;
            }

            transform.localPosition = _initialPos * (1 - totalWeight) + blendedPosition;
            transform.localRotation = Quaternion.Euler(blendedEulerAngles + _initialRot.eulerAngles * (1f - totalWeight));
            transform.localScale = _initialScale * (1f - totalWeight) + blendedScale;
            
            if (controlActivation)
            {
                if(totalWeight > 0 && !transform.gameObject.activeSelf) transform.gameObject.SetActive(true);
                else if (totalWeight <= 0 && transform.gameObject.activeSelf) transform.gameObject.SetActive(false);
            }
        }
    }
}
