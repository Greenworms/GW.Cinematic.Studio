#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
using System;
using System.Linq;
using System.Reflection;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Timeline;

#if URP || HDRP
using VolumeProfileAsset = UnityEngine.Rendering.VolumeProfile;
#elif UNITY_POST_PROCESSING_STACK_V2
using VolumeProfileAsset = UnityEngine.Rendering.PostProcessing.PostProcessProfile;
#endif

namespace CutsceneEngineEditor
{
    [MenuEntry("Add From Volume Profile", MenuPriority.AddItem.addCustomClip)]
    public class VolumeTrackAddFromProfileAction : TimelineAction
    {
        const int ObjectPickerControlId = 908203;
        const string UndoLabel = "Add From Volume Profile";
        static readonly MethodInfo TimelineCreateClipOnTrackMethod = ResolveCreateClipMethod();
        static readonly MethodInfo TimelineGetCandidateTimeMethod = ResolveGetCandidateTimeMethod();

        class PendingPickerContext
        {
            public TimelineAsset timeline;
            public TrackAsset[] tracks;
            public double candidateTime;
            public bool pickerOpened;
            public VolumeProfileAsset lastSelectedProfile;
        }

        static PendingPickerContext _pendingContext;

        public override ActionValidity Validate(ActionContext context)
        {
            var tracks = context.tracks?.ToArray();
            if (tracks == null || tracks.Length == 0)
            {
                return ActionValidity.NotApplicable;
            }

            if (tracks.Any(track => track == null || !(track is VolumeTrack)))
            {
                return ActionValidity.NotApplicable;
            }

            if (tracks.Any(track => track.lockedInHierarchy))
            {
                return ActionValidity.Invalid;
            }

            return ActionValidity.Valid;
        }

        public override bool Execute(ActionContext context)
        {
            if (_pendingContext != null)
            {
                return false;
            }

            var tracks = context.tracks.Where(track => track is VolumeTrack).ToArray();
            if (tracks.Length == 0)
            {
                return false;
            }

            var timeline = context.timeline != null ? context.timeline : TimelineEditor.inspectedAsset;
            if (timeline == null)
            {
                timeline = tracks[0].timelineAsset;
            }

            if (timeline == null)
            {
                return false;
            }

            var candidateTime = context.invocationTime ?? ResolveFallbackTime(tracks);
            _pendingContext = new PendingPickerContext
            {
                timeline = timeline,
                tracks = tracks,
                candidateTime = candidateTime,
                pickerOpened = false
            };

            var searchFilter = $"t:{typeof(VolumeProfileAsset).Name}";
            EditorGUIUtility.ShowObjectPicker<VolumeProfileAsset>(null, false, searchFilter, ObjectPickerControlId);
            EditorApplication.update += WaitForPickerSelection;
            return true;
        }

        static void WaitForPickerSelection()
        {
            if (_pendingContext == null)
            {
                EditorApplication.update -= WaitForPickerSelection;
                return;
            }

            var currentPickerControlId = EditorGUIUtility.GetObjectPickerControlID();
            if (!_pendingContext.pickerOpened)
            {
                if (currentPickerControlId == ObjectPickerControlId)
                {
                    _pendingContext.pickerOpened = true;
                }

                return;
            }

            if (currentPickerControlId == ObjectPickerControlId)
            {
                var liveSelection = EditorGUIUtility.GetObjectPickerObject() as VolumeProfileAsset;
                if (liveSelection && liveSelection != _pendingContext.lastSelectedProfile)
                {
                    _pendingContext.lastSelectedProfile = liveSelection;
                }
                return;
            }

            var selectedProfile = EditorGUIUtility.GetObjectPickerObject() as VolumeProfileAsset;
            var pendingContext = _pendingContext;
            _pendingContext = null;
            EditorApplication.update -= WaitForPickerSelection;

            if (!selectedProfile)
            {
                selectedProfile = pendingContext.lastSelectedProfile;
            }

            if (!selectedProfile)
            {
                return;
            }

            CreateClipsFromSelectedProfile(pendingContext, selectedProfile);
        }

        static void CreateClipsFromSelectedProfile(PendingPickerContext context, VolumeProfileAsset sourceProfile)
        {
            if (context == null || context.timeline == null || sourceProfile == null)
            {
                return;
            }

            var tracks = context.tracks.Where(track => track != null && !track.lockedInHierarchy).ToArray();
            if (tracks.Length == 0)
            {
                return;
            }

            var changed = false;
            foreach (var track in tracks)
            {
                TimelineClip clip;
                try
                {
                    clip = CreateClipOnTrack(track, context.candidateTime);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    continue;
                }

                if (clip == null)
                {
                    continue;
                }

                var volumeClip = clip.asset as VolumeClip;
                if (volumeClip == null)
                {
                    continue;
                }

                var temporaryProfile = volumeClip.volumeProfile;
                var copiedProfile = VolumeClipEditor.CreateProfileCopy(sourceProfile, sourceProfile.name);
                if (!copiedProfile)
                {
                    continue;
                }

                AssetDatabase.AddObjectToAsset(copiedProfile, context.timeline);
                Undo.RegisterCreatedObjectUndo(copiedProfile, UndoLabel);

                clip.displayName = sourceProfile.name;
                copiedProfile.name = clip.displayName;
                volumeClip.volumeProfile = copiedProfile;

                if (temporaryProfile != null && temporaryProfile != copiedProfile)
                {
                    Undo.DestroyObjectImmediate(temporaryProfile);
                }

                EditorUtility.SetDirty(copiedProfile);
                EditorUtility.SetDirty(volumeClip);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            EditorUtility.SetDirty(context.timeline);
            AssetDatabase.SaveAssetIfDirty(context.timeline);
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved | RefreshReason.ContentsModified);
        }

        static MethodInfo ResolveCreateClipMethod()
        {
            var timelineHelpersType = Type.GetType("UnityEditor.Timeline.TimelineHelpers, Unity.Timeline.Editor");
            if (timelineHelpersType == null)
            {
                return null;
            }

            return timelineHelpersType.GetMethod(
                "CreateClipOnTrack",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Type), typeof(TrackAsset), typeof(double) },
                null);
        }

        static MethodInfo ResolveGetCandidateTimeMethod()
        {
            var timelineHelpersType = Type.GetType("UnityEditor.Timeline.TimelineHelpers, Unity.Timeline.Editor");
            if (timelineHelpersType == null)
            {
                return null;
            }

            return timelineHelpersType.GetMethod(
                "GetCandidateTime",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Vector2?), typeof(TrackAsset[]) },
                null);
        }

        static double ResolveFallbackTime(TrackAsset[] tracks)
        {
            if (TimelineGetCandidateTimeMethod != null)
            {
                try
                {
                    var reflectedValue = TimelineGetCandidateTimeMethod.Invoke(null, new object[] { null, tracks });
                    if (reflectedValue is double reflectedTime)
                    {
                        return reflectedTime;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            if (TimelineEditor.inspectedDirector)
            {
                return TimelineEditor.inspectedDirector.time;
            }

            return tracks.Length > 0 ? tracks.Max(track => track.end) : 0d;
        }

        static TimelineClip CreateClipOnTrack(TrackAsset track, double candidateTime)
        {
            if (TimelineCreateClipOnTrackMethod != null)
            {
                try
                {
                    return TimelineCreateClipOnTrackMethod.Invoke(null, new object[] { typeof(VolumeClip), track, candidateTime }) as TimelineClip;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            var fallbackClip = track.CreateClip<VolumeClip>();
            fallbackClip.start = candidateTime;
            return fallbackClip;
        }
    }
}
#endif
