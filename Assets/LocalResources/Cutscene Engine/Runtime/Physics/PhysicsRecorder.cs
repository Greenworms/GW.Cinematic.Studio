using System;
using System.Collections.Generic;
using UnityEngine;

namespace CutsceneEngine
{
    public class PhysicsRecorder
    {
        readonly struct CurveId : IEquatable<CurveId>
        {
            public readonly string Path;
            public readonly string Property;

            public CurveId(string path, string property)
            {
                Path = path;
                Property = property;
            }

            public bool Equals(CurveId other)
            {
                return string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                       string.Equals(Property, other.Property, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is CurveId other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + (Path != null ? Path.GetHashCode() : 0);
                    hash = hash * 31 + (Property != null ? Property.GetHashCode() : 0);
                    return hash;
                }
            }
        }

        readonly Transform _bindingRoot;
        readonly float _startTime;
        readonly List<PhysicsRigidbodyState> _states;
        readonly Dictionary<CurveId, List<Vector2>> _createdKeyframes = new();

        float _lastTime;

        public PhysicsRecorder(Transform bindingRoot, List<PhysicsRigidbodyState> states, float startTime)
        {
            _bindingRoot = bindingRoot;
            _states = states;
            _startTime = startTime;
            _lastTime = startTime;
        }

        public void RecordKeyframes(float time)
        {
            if (_states == null)
            {
                return;
            }

            foreach (var state in _states)
            {
                if (state == null || state.Transform == null)
                {
                    continue;
                }

                if (!IsRelevantObject(state))
                {
                    continue;
                }

                string path = state.RelativePath;
                var targetTransform = state.Transform;

                SetKeyframe(path, state, "m_LocalPosition", targetTransform.localPosition, time);
                SetKeyframe(path, state, "m_LocalRotation", targetTransform.localRotation, time);
            }

            _lastTime = time;
        }

        public PhysicsRecordingResult CompleteRecording(float optimizationValue)
        {
            var curves = new List<PhysicsRecordedCurve>(_createdKeyframes.Count);
            float minTime = float.PositiveInfinity;
            float maxTime = float.NegativeInfinity;

            foreach (var keyValue in _createdKeyframes)
            {
                if (keyValue.Value == null || keyValue.Value.Count == 0)
                {
                    continue;
                }

                keyValue.Value.Sort((a, b) => a.x.CompareTo(b.x));

                var curve = new AnimationCurve();
                foreach (var sample in keyValue.Value)
                {
                    curve.AddKey(sample.x, sample.y);
                }

                curve = AnimationCurveOptimizer.Optimize(curve, optimizationValue);

                curves.Add(new PhysicsRecordedCurve(keyValue.Key.Path, keyValue.Key.Property, curve));

                var firstKey = curve.keys[0].time;
                var lastKey = curve.keys[curve.length - 1].time;
                minTime = Mathf.Min(minTime, firstKey);
                maxTime = Mathf.Max(maxTime, lastKey);
            }

            if (curves.Count == 0)
            {
                return new PhysicsRecordingResult(_startTime, _lastTime, curves);
            }

            return new PhysicsRecordingResult(minTime, maxTime, curves);
        }

        bool IsRelevantObject(PhysicsRigidbodyState state)
        {
            if (_bindingRoot == null)
            {
                return true;
            }

            return state.Transform == _bindingRoot || state.Transform.IsChildOf(_bindingRoot);
        }

        void SetKeyframe(string path, PhysicsRigidbodyState state, string propertyName, Vector3 value, float time)
        {
            if (!IsValidVector3(value))
            {
                Debug.LogWarning($"Invalid Vector3 value for {propertyName} at time {time}: {value}. Skipping keyframe.");
                return;
            }

            SetCurveKey(path, state, propertyName + ".x", value.x, time);
            SetCurveKey(path, state, propertyName + ".y", value.y, time);
            SetCurveKey(path, state, propertyName + ".z", value.z, time);
        }

        void SetKeyframe(string path, PhysicsRigidbodyState state, string propertyName, Quaternion value, float time)
        {
            if (!IsValidQuaternion(value))
            {
                Debug.LogWarning($"Invalid Quaternion value for {propertyName} at time {time}: {value}. Using identity quaternion.");
                value = Quaternion.identity;
            }
            else
            {
                value = NormalizeQuaternion(value);
            }

            SetCurveKey(path, state, propertyName + ".x", value.x, time);
            SetCurveKey(path, state, propertyName + ".y", value.y, time);
            SetCurveKey(path, state, propertyName + ".z", value.z, time);
            SetCurveKey(path, state, propertyName + ".w", value.w, time);
        }

        static bool IsValidVector3(Vector3 vector)
        {
            return !float.IsNaN(vector.x) && !float.IsNaN(vector.y) && !float.IsNaN(vector.z) &&
                   !float.IsInfinity(vector.x) && !float.IsInfinity(vector.y) && !float.IsInfinity(vector.z);
        }

        static bool IsValidQuaternion(Quaternion quaternion)
        {
            return !float.IsNaN(quaternion.x) && !float.IsNaN(quaternion.y) && !float.IsNaN(quaternion.z) && !float.IsNaN(quaternion.w) &&
                   !float.IsInfinity(quaternion.x) && !float.IsInfinity(quaternion.y) && !float.IsInfinity(quaternion.z) && !float.IsInfinity(quaternion.w) &&
                   (quaternion.x != 0f || quaternion.y != 0f || quaternion.z != 0f || quaternion.w != 0f);
        }

        static Quaternion NormalizeQuaternion(Quaternion quaternion)
        {
            float magnitude = Mathf.Sqrt(quaternion.x * quaternion.x + quaternion.y * quaternion.y + quaternion.z * quaternion.z + quaternion.w * quaternion.w);
            if (magnitude < 1e-6f)
            {
                return Quaternion.identity;
            }

            return new Quaternion(
                quaternion.x / magnitude,
                quaternion.y / magnitude,
                quaternion.z / magnitude,
                quaternion.w / magnitude
            );
        }

        static bool IsValidFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        void SetCurveKey(string path, PhysicsRigidbodyState state, string property, float value, float time)
        {
            if (!IsValidFloat(value))
            {
                Debug.LogWarning($"Invalid float value for {property} at time {time}: {value}. Skipping keyframe.");
                return;
            }

            if (!IsValidFloat(time) || time < 0f)
            {
                Debug.LogWarning($"Invalid time value for {property}: {time}. Skipping keyframe.");
                return;
            }

            var curveId = new CurveId(path, property);

            state.Curves.TryAdd(property, new AnimationCurve());

            if (!_createdKeyframes.TryGetValue(curveId, out var createdKeyframes))
            {
                createdKeyframes = new List<Vector2>();
                _createdKeyframes[curveId] = createdKeyframes;
            }

            createdKeyframes.Add(new Vector2(time, value));
        }

        public static string GetRelativePath(Transform root, Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            if (root == target)
            {
                return string.Empty;
            }

            var path = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                path.Add(current.name);
                current = current.parent;
            }

            path.Reverse();
            return string.Join("/", path);
        }
    }
}