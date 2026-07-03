using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace CutsceneEngine
{
    [DisallowMultipleComponent]
    public class RuntimePhysicsRecordingPlayback : MonoBehaviour
    {
        class ChannelSet
        {
            public Transform target;
            public AnimationCurve posX;
            public AnimationCurve posY;
            public AnimationCurve posZ;
            public AnimationCurve rotX;
            public AnimationCurve rotY;
            public AnimationCurve rotZ;
            public AnimationCurve rotW;

            public bool HasPosition => posX != null || posY != null || posZ != null;
            public bool HasRotation => rotX != null || rotY != null || rotZ != null || rotW != null;
        }

        [SerializeField] bool holdLastPose = true;
        [SerializeField] bool useUnscaledTime = true;

        PlayableDirector _director;
        Transform _bindingRoot;
        PhysicsRecordingResult _result;
        bool _manualPlaying;
        float _manualTime;
        float _manualEndTime;

        readonly Dictionary<string, ChannelSet> _channelsByPath = new();

        public bool IsPlaying => _manualPlaying;

        public void Configure(PlayableDirector director, Transform bindingRoot, PhysicsRecordingResult result)
        {
            _director = director;
            _bindingRoot = bindingRoot;
            _result = result;

            RebuildChannels();
            enabled = _channelsByPath.Count > 0;
        }

        public void Play(float startTime, float endTime)
        {
            if (_result == null || !_result.HasData || _channelsByPath.Count == 0)
            {
                return;
            }

            var clampedStart = Mathf.Clamp(startTime, _result.StartTime, _result.EndTime);
            _manualEndTime = Mathf.Clamp(endTime, clampedStart, _result.EndTime);
            _manualTime = clampedStart;
            _manualPlaying = true;
            ApplyAtTime(_manualTime);
        }

        public void Stop()
        {
            _manualPlaying = false;
        }

        public void Clear()
        {
            _director = null;
            _bindingRoot = null;
            _result = null;
            _manualPlaying = false;
            _channelsByPath.Clear();
            enabled = false;
        }

        void LateUpdate()
        {
            if (_director == null || _bindingRoot == null || _result == null || !_result.HasData)
            {
                return;
            }

            float time;

            if (_manualPlaying)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                _manualTime += Mathf.Max(0f, deltaTime);

                if (_manualTime >= _manualEndTime)
                {
                    _manualTime = _manualEndTime;
                    _manualPlaying = false;
                }

                time = _manualTime;
            }
            else if (_director != null)
            {
                time = (float)_director.time;
            }
            else
            {
                return;
            }

            if (time < _result.StartTime && !_manualPlaying)
            {
                return;
            }

            if (time > _result.EndTime)
            {
                if (!holdLastPose)
                {
                    return;
                }

                time = _result.EndTime;
            }

            ApplyAtTime(time);
        }

        void ApplyAtTime(float time)
        {
            foreach (var channels in _channelsByPath.Values)
            {
                if (channels.target == null)
                {
                    continue;
                }

                if (channels.HasPosition)
                {
                    var localPosition = channels.target.localPosition;
                    if (channels.posX != null) localPosition.x = channels.posX.Evaluate(time);
                    if (channels.posY != null) localPosition.y = channels.posY.Evaluate(time);
                    if (channels.posZ != null) localPosition.z = channels.posZ.Evaluate(time);
                    channels.target.localPosition = localPosition;
                }

                if (channels.HasRotation)
                {
                    var localRotation = channels.target.localRotation;
                    var x = channels.rotX != null ? channels.rotX.Evaluate(time) : localRotation.x;
                    var y = channels.rotY != null ? channels.rotY.Evaluate(time) : localRotation.y;
                    var z = channels.rotZ != null ? channels.rotZ.Evaluate(time) : localRotation.z;
                    var w = channels.rotW != null ? channels.rotW.Evaluate(time) : localRotation.w;

                    var normalized = NormalizeQuaternion(new Quaternion(x, y, z, w));
                    channels.target.localRotation = normalized;
                }
            }
        }

        void RebuildChannels()
        {
            _channelsByPath.Clear();

            if (_result == null || !_result.HasData || _bindingRoot == null)
            {
                return;
            }

            foreach (var recordedCurve in _result.Curves)
            {
                if (recordedCurve == null)
                {
                    continue;
                }

                var path = recordedCurve.Path ?? string.Empty;
                if (!_channelsByPath.TryGetValue(path, out var channels))
                {
                    channels = new ChannelSet
                    {
                        target = ResolveTransform(path)
                    };
                    _channelsByPath[path] = channels;
                }

                if (channels.target == null)
                {
                    continue;
                }

                switch (recordedCurve.PropertyName)
                {
                    case "m_LocalPosition.x": channels.posX = recordedCurve.Curve; break;
                    case "m_LocalPosition.y": channels.posY = recordedCurve.Curve; break;
                    case "m_LocalPosition.z": channels.posZ = recordedCurve.Curve; break;
                    case "m_LocalRotation.x": channels.rotX = recordedCurve.Curve; break;
                    case "m_LocalRotation.y": channels.rotY = recordedCurve.Curve; break;
                    case "m_LocalRotation.z": channels.rotZ = recordedCurve.Curve; break;
                    case "m_LocalRotation.w": channels.rotW = recordedCurve.Curve; break;
                }
            }
        }

        Transform ResolveTransform(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return _bindingRoot;
            }

            return _bindingRoot.Find(path);
        }

        static Quaternion NormalizeQuaternion(Quaternion quaternion)
        {
            float magnitude = Mathf.Sqrt(
                quaternion.x * quaternion.x +
                quaternion.y * quaternion.y +
                quaternion.z * quaternion.z +
                quaternion.w * quaternion.w);

            if (magnitude < 1e-6f)
            {
                return Quaternion.identity;
            }

            return new Quaternion(
                quaternion.x / magnitude,
                quaternion.y / magnitude,
                quaternion.z / magnitude,
                quaternion.w / magnitude);
        }
    }
}
