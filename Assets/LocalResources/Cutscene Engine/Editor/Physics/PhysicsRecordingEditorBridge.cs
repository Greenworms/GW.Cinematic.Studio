using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    internal static class PhysicsRecordingEditorBridge
    {
        const double MinClipDuration = 0.0001d;

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

        internal static bool TryFindRecordingTarget(out PlayableDirector director, out AnimationTrack recordingTrack, out Transform bindingRoot)
        {
            director = TimelineEditor.inspectedDirector;
            recordingTrack = null;
            bindingRoot = null;

            if (!director)
            {
                return false;
            }

            foreach (var track in director.GetTracks<AnimationTrack>())
            {
                if (!track.IsRecording())
                {
                    continue;
                }

                var animator = director.GetGenericBinding(track) as Animator;
                if (!animator)
                {
                    continue;
                }

                recordingTrack = track;
                bindingRoot = animator.transform;
                return true;
            }

            return false;
        }

        internal static void PrepareTrackForRecording(PlayableDirector director, AnimationTrack track)
        {
            if (!director || track == null)
            {
                return;
            }

            Undo.RecordObject(track, "Prepare Physics Recording");
            if (track.infiniteClip != null)
            {
                Undo.RecordObject(track.infiniteClip, "Prepare Physics Recording");
            }

            RefreshTimeline(RefreshReason.SceneNeedsUpdate);
        }

        internal static void ApplyRecordingResultToTrack(PlayableDirector director, AnimationTrack track, PhysicsRecordingResult result)
        {
            if (!director || track == null || result == null || !result.HasData)
            {
                return;
            }

            Undo.RecordObject(track, "Apply Physics Recording");

            if (track.infiniteClip != null || !track.hasClips)
            {
                ApplyToInfiniteClip(track, result);
            }
            else
            {
                ApplyToClipTrack(track, result);
            }

            director.time = result.EndTime;
            RefreshTimeline(RefreshReason.ContentsModified | RefreshReason.SceneNeedsUpdate);
        }

        internal static void FinalizeEditorRecording(AnimationTrack track)
        {
            if (track == null)
            {
                return;
            }

            track.StopRecording();
        }

        internal static void RefreshTimeline(RefreshReason reason)
        {
            var window = TimelineEditor.GetWindow();
            if (!window)
            {
                return;
            }

            TimelineEditor.Refresh(reason);
        }

        internal static void RefreshTimelineAfterRecording()
        {
            const RefreshReason reason =
                RefreshReason.ContentsModified |
                RefreshReason.ContentsAddedOrRemoved |
                RefreshReason.SceneNeedsUpdate;

            RefreshTimeline(reason);
            EditorApplication.delayCall += () => RefreshTimeline(reason);
        }

        static void ApplyToInfiniteClip(AnimationTrack track, PhysicsRecordingResult result)
        {
            if (track.infiniteClip == null)
            {
                track.CreateInfiniteClip("Physics Record");
            }

            var infiniteClip = track.infiniteClip;
            if (infiniteClip == null)
            {
                return;
            }

            Undo.RecordObject(infiniteClip, "Apply Physics Recording");

            foreach (var recordedCurve in result.Curves)
            {
                var binding = EditorCurveBinding.FloatCurve(recordedCurve.Path, typeof(Transform), recordedCurve.PropertyName);
                var existingCurve = AnimationUtility.GetEditorCurve(infiniteClip, binding);

                var mergedCurve = MergeAbsoluteCurves(existingCurve, recordedCurve.Curve, result.StartTime, result.EndTime);
                AnimationUtility.SetEditorCurve(infiniteClip, binding, mergedCurve);
            }
        }

        static void ApplyToClipTrack(AnimationTrack track, PhysicsRecordingResult result)
        {
            var recordStart = (double)result.StartTime;
            var recordEnd = (double)result.EndTime;

            var overlaps = track.GetClips()
                .Where(clip => RangesOverlap(clip.start, clip.end, recordStart, recordEnd))
                .OrderBy(clip => clip.start)
                .ToList();

            var sourceCurveKeys = CaptureSourceCurveKeys(overlaps, result);

            TimelineClip targetClip;

            if (overlaps.Count == 0)
            {
                targetClip = CreateRecordableClip(track, recordStart, recordEnd);
            }
            else if (overlaps.Count == 1)
            {
                targetClip = overlaps[0];
                ExpandClipToInclude(targetClip, recordStart, recordEnd);
            }
            else
            {
                targetClip = overlaps[0];
                MergeClipShape(targetClip, overlaps, recordStart, recordEnd);
            }

            if (targetClip == null)
            {
                return;
            }

            ApplyCurvesToTargetClip(targetClip, sourceCurveKeys, result);

            if (overlaps.Count > 1)
            {
                foreach (var clip in overlaps)
                {
                    if (clip == targetClip)
                    {
                        continue;
                    }

                    track.DeleteClip(clip);
                }
            }
        }

        static Dictionary<CurveId, List<Keyframe>> CaptureSourceCurveKeys(List<TimelineClip> sourceClips, PhysicsRecordingResult result)
        {
            var resultMap = new Dictionary<CurveId, List<Keyframe>>();
            if (sourceClips == null || sourceClips.Count == 0)
            {
                return resultMap;
            }

            foreach (var sourceClip in sourceClips)
            {
                var sourceAnimationClip = GetAnimationClip(sourceClip);
                if (sourceAnimationClip == null)
                {
                    continue;
                }

                foreach (var recordedCurve in result.Curves)
                {
                    var curveId = new CurveId(recordedCurve.Path, recordedCurve.PropertyName);
                    var binding = EditorCurveBinding.FloatCurve(recordedCurve.Path, typeof(Transform), recordedCurve.PropertyName);
                    var existingCurve = AnimationUtility.GetEditorCurve(sourceAnimationClip, binding);
                    if (existingCurve == null)
                    {
                        continue;
                    }

                    if (!resultMap.TryGetValue(curveId, out var keys))
                    {
                        keys = new List<Keyframe>();
                        resultMap[curveId] = keys;
                    }

                    foreach (var key in existingCurve.keys)
                    {
                        var absoluteKey = key;
                        absoluteKey.time = (float)(sourceClip.start + key.time);
                        keys.Add(absoluteKey);
                    }
                }
            }

            return resultMap;
        }

        static TimelineClip CreateRecordableClip(AnimationTrack track, double start, double end)
        {
            var newClip = track.CreateRecordableClip($"Physics Recording {DateTime.Now:h-m-s}");
            if (newClip == null)
            {
                return null;
            }

            newClip.start = start;
            newClip.duration = Math.Max(MinClipDuration, end - start);
            return newClip;
        }

        static void ExpandClipToInclude(TimelineClip clip, double start, double end)
        {
            var newStart = Math.Min(clip.start, start);
            var newEnd = Math.Max(clip.end, end);

            clip.start = newStart;
            clip.duration = Math.Max(MinClipDuration, newEnd - newStart);
        }

        static void MergeClipShape(TimelineClip targetClip, List<TimelineClip> overlaps, double recordStart, double recordEnd)
        {
            var earliestClip = overlaps[0];
            var latestClip = overlaps[overlaps.Count - 1];

            var newStart = Math.Min(earliestClip.start, recordStart);
            var newEnd = Math.Max(latestClip.end, recordEnd);

            var startEaseIn = earliestClip.easeInDuration;
            var endEaseOut = latestClip.easeOutDuration;
            var clipIn = earliestClip.clipIn;
            targetClip.start = newStart;
            targetClip.duration = Math.Max(MinClipDuration, newEnd - newStart);
            targetClip.clipIn = clipIn;
            targetClip.easeInDuration = Math.Min(startEaseIn, targetClip.duration);
            targetClip.easeOutDuration = Math.Min(endEaseOut, targetClip.duration);
            CopyExtrapolationFrom(latestClip, targetClip);
        }

        static void ApplyCurvesToTargetClip(TimelineClip targetClip, Dictionary<CurveId, List<Keyframe>> sourceCurveKeys, PhysicsRecordingResult result)
        {
            var animationClip = GetOrCreateAnimationClip(targetClip);
            if (animationClip == null)
            {
                return;
            }

            Undo.RecordObject(animationClip, "Apply Physics Recording");

            foreach (var recordedCurve in result.Curves)
            {
                var curveId = new CurveId(recordedCurve.Path, recordedCurve.PropertyName);

                var mergedAbsoluteKeys = new List<Keyframe>();
                if (sourceCurveKeys.TryGetValue(curveId, out var existingKeys))
                {
                    foreach (var key in existingKeys)
                    {
                        if (key.time >= result.StartTime && key.time <= result.EndTime)
                        {
                            continue;
                        }

                        mergedAbsoluteKeys.Add(key);
                    }
                }

                foreach (var key in recordedCurve.Curve.keys)
                {
                    mergedAbsoluteKeys.Add(key);
                }

                mergedAbsoluteKeys.Sort((a, b) => a.time.CompareTo(b.time));

                var localKeys = new Keyframe[mergedAbsoluteKeys.Count];
                for (int i = 0; i < mergedAbsoluteKeys.Count; i++)
                {
                    var localKey = mergedAbsoluteKeys[i];
                    localKey.time = (float)(mergedAbsoluteKeys[i].time - targetClip.start);
                    localKeys[i] = localKey;
                }

                var mergedCurve = new AnimationCurve(localKeys);
                var binding = EditorCurveBinding.FloatCurve(recordedCurve.Path, typeof(Transform), recordedCurve.PropertyName);
                AnimationUtility.SetEditorCurve(animationClip, binding, mergedCurve);
            }
        }

        static AnimationCurve MergeAbsoluteCurves(AnimationCurve existingCurve, AnimationCurve recordedCurve, float startTime, float endTime)
        {
            var mergedKeys = new List<Keyframe>();

            if (existingCurve != null)
            {
                foreach (var key in existingCurve.keys)
                {
                    if (key.time >= startTime && key.time <= endTime)
                    {
                        continue;
                    }

                    mergedKeys.Add(key);
                }
            }

            if (recordedCurve != null)
            {
                mergedKeys.AddRange(recordedCurve.keys);
            }

            mergedKeys.Sort((a, b) => a.time.CompareTo(b.time));
            return new AnimationCurve(mergedKeys.ToArray());
        }

        static void CopyExtrapolationFrom(TimelineClip sourceClip, TimelineClip targetClip)
        {
            if (sourceClip == null || targetClip == null)
            {
                return;
            }

            var preMode = sourceClip.preExtrapolationMode;
            var postMode = sourceClip.postExtrapolationMode;

            double preTime = Math.Max(0d, sourceClip.start - sourceClip.extrapolatedStart);
            double sourceExtrapolatedEnd = sourceClip.extrapolatedStart + sourceClip.extrapolatedDuration;
            double postTime = Math.Max(0d, sourceExtrapolatedEnd - sourceClip.end);

            SetClipExtrapolationMode(targetClip, "preExtrapolationMode", "m_PreExtrapolationMode", preMode);
            SetClipExtrapolationMode(targetClip, "postExtrapolationMode", "m_PostExtrapolationMode", postMode);

            SetClipExtrapolationTime(targetClip, "SetPreExtrapolationTime", "m_PreExtrapolationTime", preTime);
            SetClipExtrapolationTime(targetClip, "SetPostExtrapolationTime", "m_PostExtrapolationTime", postTime);
        }

        static void SetClipExtrapolationMode(TimelineClip clip, string propertyName, string fieldName, TimelineClip.ClipExtrapolation mode)
        {
            if (clip == null)
            {
                return;
            }

            // Newer Timeline versions expose this via a private setter.
            var property = typeof(TimelineClip).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
            {
                property.SetValue(clip, mode, null);
                return;
            }

            // Fallback for versions where mode is backed by a private field.
            var field = typeof(TimelineClip).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }

            if (field.FieldType == typeof(TimelineClip.ClipExtrapolation))
            {
                field.SetValue(clip, mode);
                return;
            }

            if (field.FieldType == typeof(int))
            {
                field.SetValue(clip, (int)mode);
            }
        }

        static void SetClipExtrapolationTime(TimelineClip clip, string methodName, string fieldName, double value)
        {
            if (clip == null)
            {
                return;
            }

            var method = typeof(TimelineClip).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(double) }, null);
            if (method != null)
            {
                method.Invoke(clip, new object[] { value });
                return;
            }

            var field = typeof(TimelineClip).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }

            if (field.FieldType == typeof(double))
            {
                field.SetValue(clip, value);
                return;
            }

            if (field.FieldType == typeof(float))
            {
                field.SetValue(clip, (float)value);
            }
        }

        static bool RangesOverlap(double startA, double endA, double startB, double endB)
        {
            return startA <= endB && startB <= endA;
        }

        static AnimationClip GetAnimationClip(TimelineClip clip)
        {
            var playableAsset = clip != null ? clip.asset as AnimationPlayableAsset : null;
            return playableAsset != null ? playableAsset.clip : null;
        }

        static AnimationClip GetOrCreateAnimationClip(TimelineClip clip)
        {
            var playableAsset = clip != null ? clip.asset as AnimationPlayableAsset : null;
            if (playableAsset == null)
            {
                return null;
            }

            if (playableAsset.clip == null)
            {
                playableAsset.clip = new AnimationClip();
            }

            return playableAsset.clip;
        }
    }
}
