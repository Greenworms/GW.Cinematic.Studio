using System;
using UnityEngine;

namespace CutsceneEngine.Samples
{
    public class TimeScaleSample : MonoBehaviour
    {
        public Cutscene cutscene;
        public Rigidbody body;
        public Transform resetPoint;

        void Start()
        {
            cutscene.onReachedTheEnd += ResetTransform;
        }

        void ResetTransform()
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector3.zero;
#else
            body.velocity = Vector3.zero;
#endif
            
            body.angularVelocity = Vector3.zero;
            body.position = resetPoint.position;
            body.rotation = resetPoint.rotation;
        }
    }
}