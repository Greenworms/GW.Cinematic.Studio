using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    public sealed class PhysicsRecordingTarget
    {
        public PlayableDirector Director { get; }
        public AnimationTrack Track { get; }
        public Transform BindingRoot { get; }
        public double StartDirectorTime { get; }

        public PhysicsRecordingTarget(PlayableDirector director, AnimationTrack track, Transform bindingRoot, double startDirectorTime)
        {
            Director = director;
            Track = track;
            BindingRoot = bindingRoot;
            StartDirectorTime = startDirectorTime;
        }
    }

    public sealed class PhysicsRecordedCurve
    {
        public string Path { get; }
        public string PropertyName { get; }
        public AnimationCurve Curve { get; }

        public PhysicsRecordedCurve(string path, string propertyName, AnimationCurve curve)
        {
            Path = path;
            PropertyName = propertyName;
            Curve = curve;
        }
    }

    public sealed class PhysicsRecordingResult
    {
        static readonly List<PhysicsRecordedCurve> EmptyCurves = new();

        public static PhysicsRecordingResult Empty { get; } = new(0f, 0f, EmptyCurves);

        public float StartTime { get; }
        public float EndTime { get; }
        public IReadOnlyList<PhysicsRecordedCurve> Curves { get; }
        public bool HasData => Curves.Count > 0;

        public PhysicsRecordingResult(float startTime, float endTime, IReadOnlyList<PhysicsRecordedCurve> curves)
        {
            StartTime = startTime;
            EndTime = endTime;
            Curves = curves ?? EmptyCurves;
        }
    }

    public sealed class PhysicsRigidbodyState
    {
        public Rigidbody Rigidbody3D { get; }
        public Rigidbody2D Rigidbody2D { get; }
        public Transform Transform { get; }
        public string RelativePath { get; }

        public Vector3 InitialPosition { get; set; }
        public Quaternion InitialRotation { get; set; }

        readonly Dictionary<string, AnimationCurve> _curves = new();
        public Dictionary<string, AnimationCurve> Curves => _curves;

        public PhysicsRigidbodyState(Transform root, Rigidbody rigidbody)
        {
            Rigidbody3D = rigidbody;
            Transform = rigidbody ? rigidbody.transform : null;

            if (Transform)
            {
                InitialPosition = Transform.position;
                InitialRotation = Transform.rotation;
                RelativePath = PhysicsRecorder.GetRelativePath(root, Transform);

                rigidbody.position = InitialPosition;
                rigidbody.rotation = InitialRotation;
            }
            else
            {
                RelativePath = string.Empty;
            }
        }

        public PhysicsRigidbodyState(Transform root, Rigidbody2D rigidbody)
        {
            Rigidbody2D = rigidbody;
            Transform = rigidbody ? rigidbody.transform : null;

            if (Transform)
            {
                InitialPosition = Transform.position;
                InitialRotation = Transform.rotation;
                RelativePath = PhysicsRecorder.GetRelativePath(root, Transform);

                rigidbody.position = InitialPosition;
                rigidbody.rotation = InitialRotation.eulerAngles.z;
            }
            else
            {
                RelativePath = string.Empty;
            }
        }
    }
}