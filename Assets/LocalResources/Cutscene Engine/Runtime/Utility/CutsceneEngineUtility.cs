using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace CutsceneEngine
{
    /// <summary>
    /// Shared runtime utility APIs for Cutscene Engine timeline access, math helpers, and JSON save/load.
    /// </summary>
    public static class CutsceneEngineUtility
    {
        const double Tolerance = 0.000001;
        internal static void SmartDestroy(Object target)
        {
#if UNITY_EDITOR
            if(!Application.isPlaying) Object.DestroyImmediate(target);
            else
#endif
            Object.Destroy(target);
        }
        internal static bool IsGreaterThan(this double a, double b)
        {
            return (a - b) > Tolerance;
        }
        
        internal static bool IsGreaterThanOrEqual(this double a, double b)
        {
            return (a > b) || a.IsApproximately(b);
        }

        internal static bool IsLessThan(this double a, double b)
        {
            return (b - a) > Tolerance;
        }
        
        internal static bool IsLessThanOrEqual(this double a, double b)
        {
            return (a < b) || a.IsApproximately(b);
        }
        
        internal static bool IsApproximately(this double a, double b)
        {
            return Math.Abs(a - b) <= Tolerance;
        }
        
        /// <summary>
        /// Enumerates all tracks bound to the specified object in the director.
        /// </summary>
        /// <param name="director">Director that owns the playable timeline.</param>
        /// <param name="binding">Binding object to match.</param>
        /// <returns>Tracks whose generic binding equals the given object.</returns>
        public static IEnumerable<TrackAsset> GetTracks(this PlayableDirector director, Object binding)
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if (playableBinding.sourceObject is TrackAsset track && director.GetGenericBinding(track) == binding) yield return track;
            }
        }
        
        
        /// <summary>
        /// Enumerates all tracks of the requested track type.
        /// </summary>
        /// <typeparam name="T">Track type to collect.</typeparam>
        /// <param name="director">Director that owns the playable timeline.</param>
        /// <returns>All matching tracks of type <typeparamref name="T"/>.</returns>
        public static IEnumerable<T> GetTracks<T>(this PlayableDirector director) where T : TrackAsset
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if (playableBinding.sourceObject is T track) yield return track;
            }
        }

        /// <summary>
        /// Returns the first track of the requested type.
        /// </summary>
        /// <typeparam name="T">Track type to find.</typeparam>
        /// <param name="director">Director that owns the playable timeline.</param>
        /// <returns>The first matching track, or <c>null</c> when not found.</returns>
        public static T GetTrack<T>(this PlayableDirector director) where T : TrackAsset
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if(playableBinding.sourceObject is T track) return track;
            }

            return null;
        }

        /// <summary>
        /// Returns the first track of the requested type that matches the predicate.
        /// </summary>
        /// <typeparam name="T">Track type to find.</typeparam>
        /// <param name="director">Director that owns the playable timeline.</param>
        /// <param name="predicate">Additional filter condition.</param>
        /// <returns>The first matching track, or <c>null</c> when not found.</returns>
        public static T GetTrack<T>(this PlayableDirector director, Predicate<T> predicate) where T : TrackAsset
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if(playableBinding.sourceObject is T track && predicate.Invoke(track)) return track;
            }

            return null;
        }
        
        /// <summary>
        /// Returns the first track of the requested type bound to the specified object.
        /// </summary>
        /// <typeparam name="T">Track type to find.</typeparam>
        /// <param name="director">Director that owns the playable timeline.</param>
        /// <param name="binding">Binding object to match.</param>
        /// <returns>The first matching track, or <c>null</c> when not found.</returns>
        public static T GetTrack<T>(this PlayableDirector director, Object binding) where T : TrackAsset
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if(playableBinding.sourceObject is T track && director.GetGenericBinding(track) == binding) return track;
            }

            return null;
        }

        /// <summary>
        /// Enumerates all tracks of the requested type bound to the specified object.
        /// </summary>
        /// <typeparam name="T">Track type to collect.</typeparam>
        /// <param name="director">Director that owns the playable timeline.</param>
        /// <param name="binding">Binding object to match.</param>
        /// <returns>All matching tracks of type <typeparamref name="T"/>.</returns>
        public static IEnumerable<T> GetTracks<T>(this PlayableDirector director, Object binding) where T : TrackAsset
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if (playableBinding.sourceObject is T track && director.GetGenericBinding(track) == binding) yield return track;
            }
        }

        /// <summary>
        /// Finds the track that owns the specified playable clip asset in a director.
        /// </summary>
        /// <typeparam name="T">Track type to search.</typeparam>
        /// <param name="director">Director that owns the playable timeline.</param>
        /// <param name="clip">Playable clip asset instance to locate.</param>
        /// <returns>The owning track, or <c>null</c> when not found.</returns>
        public static T GetTrackOf<T>(this PlayableDirector director, PlayableAsset clip) where T : TrackAsset
        {
            foreach (var playableBinding in director.playableAsset.outputs)
            {
                if(playableBinding.sourceObject is T track)
                {
                    foreach (var timelineClip in track.GetClips())
                    {
                        if (timelineClip.asset == clip) return track;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the track that owns the specified playable clip asset in a timeline asset.
        /// </summary>
        /// <typeparam name="T">Track type to search.</typeparam>
        /// <param name="asset">Timeline asset to inspect.</param>
        /// <param name="clip">Playable clip asset instance to locate.</param>
        /// <returns>The owning track, or <c>null</c> when not found.</returns>
        public static T GetTrackOf<T>(this TimelineAsset asset, PlayableAsset clip) where T : TrackAsset
        {
            foreach (var binding in asset.outputs)
            {
                if(binding.sourceObject is T track)
                {
                    foreach (var timelineClip in track.GetClips())
                    {
                        if (timelineClip.asset == clip) return track;
                    }
                }
            }

            return null;
        }
        

        /// <summary>
        /// Converts absolute timeline time to normalized [0, 1] clip-local time.
        /// </summary>
        /// <param name="clip">Clip used to read start and end times.</param>
        /// <param name="time">Absolute timeline time in seconds.</param>
        /// <returns>Normalized time in range [0, 1].</returns>
        public static double GetNormalizedTime(this TimelineClip clip, double time)
        {
            return GetNormalizedTime(time, clip.start, clip.end);
        }

        /// <summary>
        /// Converts absolute time to normalized [0, 1] range using custom bounds.
        /// </summary>
        /// <param name="time">Absolute time value.</param>
        /// <param name="start">Range start value.</param>
        /// <param name="end">Range end value.</param>
        /// <returns>Normalized value clamped between 0 and 1.</returns>
        public static double GetNormalizedTime(double time, double start, double end)
        {
            if (time <= start) return 0f;
            if (time >= end) return 1f;
            return (time - start) / (end - start);
        }


        /// <summary>
        /// Converts normalized [0, 1] clip-local time to absolute timeline time.
        /// </summary>
        /// <param name="clip">Clip used to read start and end times.</param>
        /// <param name="normalizedTime">Normalized time in range [0, 1].</param>
        /// <returns>Absolute timeline time in seconds.</returns>
        public static double GetTimelineTime(this TimelineClip clip, float normalizedTime)
        {
            return GetTimelineTime(normalizedTime, clip.start, clip.end);
        }

        /// <summary>
        /// Converts normalized [0, 1] range to absolute time using custom bounds.
        /// </summary>
        /// <param name="normalizedTime">Normalized value in range [0, 1].</param>
        /// <param name="start">Range start value.</param>
        /// <param name="end">Range end value.</param>
        /// <returns>Absolute time value.</returns>
        public static double GetTimelineTime(float normalizedTime, double start, double end)
        {
            return start + normalizedTime * (end - start);
        }
        const int CurrentVersion = 2;
        const BindingFlags InstanceFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const string RelativeGameObjectMode = "relative_game_object";
        const string RelativeComponentMode = "relative_component";
        const string GlobalGameObjectMode = "global_game_object";
        const string GlobalComponentMode = "global_component";

        /// <summary>
        /// Serialize a timeline asset to JSON.
        /// </summary>
        /// <param name="timelineAsset">
        /// Source timeline asset to serialize. Root/child tracks, clips, markers, and supported serialized fields are exported.
        /// </param>
        /// <param name="director">
        /// Required PlayableDirector used as the reference root. Object references are saved only when the target is
        /// under <c>director.transform</c>; non-child references are skipped with a warning.
        /// </param>
        /// <param name="prettyPrint">
        /// If <c>true</c>, writes indented JSON for readability. If <c>false</c>, writes compact JSON.
        /// </param>
        /// <returns>Serialized timeline JSON string in <see cref="TimelineJsonData"/> format.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="timelineAsset"/> or <paramref name="director"/> is null.
        /// </exception>
        public static string ToJson(TimelineAsset timelineAsset, PlayableDirector director, bool prettyPrint = true)
        {
            if (!timelineAsset)
                throw new ArgumentNullException(nameof(timelineAsset));
            if (!director)
                throw new ArgumentNullException(nameof(director));

            var data = new TimelineJsonData
            {
                version = CurrentVersion,
                name = timelineAsset.name,
                durationMode = GetEnumPropertyName(timelineAsset, "durationMode"),
                fixedDuration = GetDoubleProperty(timelineAsset, "fixedDuration")
            };
            var skippedReferenceWarnings = new HashSet<string>();

            var nextTrackId = 0;
            foreach (var rootTrack in timelineAsset.GetRootTracks())
            {
                SerializeTrackRecursive(rootTrack, null, director, data, ref nextTrackId, skippedReferenceWarnings);
            }

            return JsonUtility.ToJson(data, prettyPrint);
        }

        /// <summary>
        /// Deserialize a timeline from JSON.
        /// </summary>
        /// <param name="json">
        /// Input JSON string. Usually produced by <see cref="ToJson"/>, but compatible JSON with the same schema can also be loaded.
        /// </param>
        /// <param name="director">
        /// Optional target director used to restore director-relative references and track bindings during load.
        /// If null, timeline data is still created but no director binding/assignment is performed.
        /// </param>
        /// <param name="assignToDirector">
        /// When <c>true</c> and <paramref name="director"/> is not null, assigns the loaded timeline to
        /// <c>director.playableAsset</c> and rebuilds the graph.
        /// </param>
        /// <returns>New runtime <see cref="TimelineAsset"/> instance created from JSON.</returns>
        /// <exception cref="InvalidOperationException">Thrown when parsing or timeline reconstruction fails.</exception>
        public static TimelineAsset FromJson(string json, PlayableDirector director = null, bool assignToDirector = true)
        {
            if (!TryFromJson(json, out var timelineAsset, director, assignToDirector))
                throw new InvalidOperationException("Failed to deserialize Timeline JSON.");

            return timelineAsset;
        }

        /// <summary>
        /// Try to parse Timeline JSON data.
        /// </summary>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="data">
        /// Parsed <see cref="TimelineJsonData"/> on success; otherwise null.
        /// </param>
        /// <returns><c>true</c> if parsing succeeds and produces a non-null data object; otherwise <c>false</c>.</returns>
        public static bool TryParseTimelineJsonData(string json, out TimelineJsonData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                data = JsonUtility.FromJson<TimelineJsonData>(json);
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CutsceneEngineUtility] JSON parse failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to deserialize a timeline from JSON.
        /// </summary>
        /// <param name="json">Input JSON text to deserialize.</param>
        /// <param name="timelineAsset">
        /// Created runtime timeline asset when successful; otherwise null.
        /// </param>
        /// <param name="director">
        /// Optional target director used for binding restoration and optional timeline assignment.
        /// </param>
        /// <param name="assignToDirector">
        /// When <c>true</c> and <paramref name="director"/> is provided, assigns the loaded timeline to the director and rebuilds its graph.
        /// </param>
        /// <returns><c>true</c> if timeline creation succeeds; otherwise <c>false</c>.</returns>
        public static bool TryFromJson(string json, out TimelineAsset timelineAsset, PlayableDirector director = null, bool assignToDirector = true)
        {
            return TryFromJson(json, out timelineAsset, out _, director, assignToDirector);
        }

        /// <summary>
        /// Try to deserialize a timeline from JSON and return load context for manual rebinding.
        /// </summary>
        /// <param name="json">Input JSON text to deserialize.</param>
        /// <param name="timelineAsset">
        /// Created runtime timeline asset when successful; otherwise null.
        /// </param>
        /// <param name="loadContext">
        /// Additional load metadata for post-load operations (parsed data, created track map, loaded timeline).
        /// Use this context with <see cref="RebindTrackBindings"/> for manual override rebinding.
        /// </param>
        /// <param name="director">
        /// Optional target director used for automatic binding restoration and optional timeline assignment.
        /// </param>
        /// <param name="assignToDirector">
        /// When <c>true</c> and <paramref name="director"/> is provided, assigns the loaded timeline to the director and rebuilds its graph.
        /// </param>
        /// <returns><c>true</c> if timeline creation succeeds; otherwise <c>false</c>.</returns>
        public static bool TryFromJson(string json, out TimelineAsset timelineAsset, out TimelineJsonLoadContext loadContext, PlayableDirector director = null, bool assignToDirector = true)
        {
            timelineAsset = null;
            loadContext = null;
            if (!TryParseTimelineJsonData(json, out var data))
                return false;
            var loadedTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            loadedTimeline.name = string.IsNullOrEmpty(data.name) ? "TimelineFromJson" : data.name;

            SetEnumPropertyByName(loadedTimeline, "durationMode", data.durationMode);
            SetDoubleProperty(loadedTimeline, "fixedDuration", data.fixedDuration);

            if (director && assignToDirector)
            {
                director.playableAsset = loadedTimeline;
            }

            var createdTracks = new Dictionary<string, TrackAsset>();
            var pendingTracks = data.tracks != null ? new List<TrackJsonData>(data.tracks) : new List<TrackJsonData>();
            var guard = 0;

            while (pendingTracks.Count > 0 && guard < 1024)
            {
                guard++;
                var progressed = false;

                for (var i = pendingTracks.Count - 1; i >= 0; i--)
                {
                    var trackData = pendingTracks[i];
                    if (trackData == null)
                    {
                        pendingTracks.RemoveAt(i);
                        progressed = true;
                        continue;
                    }

                    if (!CanCreateTrackNow(trackData.parentId, createdTracks))
                        continue;

                    if (!TryCreateTrack(loadedTimeline, director, trackData, createdTracks, out var track))
                    {
                        pendingTracks.RemoveAt(i);
                        progressed = true;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(trackData.id))
                        createdTracks[trackData.id] = track;
                    pendingTracks.RemoveAt(i);
                    progressed = true;
                }

                if (!progressed)
                    break;
            }

            if (pendingTracks.Count > 0)
            {
                Debug.LogWarning($"[CutsceneEngineUtility] Could not create {pendingTracks.Count} track(s). Check parent/type consistency.");
            }

            if (director && assignToDirector)
            {
                director.RebuildGraph();
            }

            timelineAsset = loadedTimeline;
            loadContext = new TimelineJsonLoadContext
            {
                data = data,
                trackById = createdTracks,
                timelineAsset = loadedTimeline
            };
            return true;
        }

        /// <summary>
        /// Save a timeline to a JSON file.
        /// </summary>
        /// <param name="timelineAsset">Source timeline asset to serialize and save.</param>
        /// <param name="filePath">
        /// Destination file path. Parent directory is created automatically when it does not exist.
        /// </param>
        /// <param name="director">
        /// Required director used as the reference root while serializing object references.
        /// </param>
        /// <param name="prettyPrint">
        /// If <c>true</c>, saves indented JSON; if <c>false</c>, saves compact JSON.
        /// </param>
        /// <returns><c>true</c> if serialization and file write succeed; otherwise <c>false</c>.</returns>
        public static bool SaveToFile(TimelineAsset timelineAsset, string filePath, PlayableDirector director, bool prettyPrint = true)
        {
            if (!timelineAsset || !director || string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var json = ToJson(timelineAsset, director, prettyPrint);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CutsceneEngineUtility] Failed to save timeline JSON to '{filePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a timeline from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the JSON file created by timeline save APIs.</param>
        /// <param name="timelineAsset">
        /// Created runtime timeline asset when successful; otherwise null.
        /// </param>
        /// <param name="director">
        /// Optional target director used for automatic reference restoration and optional playableAsset assignment.
        /// </param>
        /// <param name="assignToDirector">
        /// When <c>true</c> and <paramref name="director"/> is provided, assigns the loaded timeline to the director and rebuilds its graph.
        /// </param>
        /// <returns><c>true</c> if the file is read and deserialization succeeds; otherwise <c>false</c>.</returns>
        public static bool TryLoadFromFile(string filePath, out TimelineAsset timelineAsset, PlayableDirector director = null, bool assignToDirector = true)
        {
            timelineAsset = null;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                var json = File.ReadAllText(filePath);
                return TryFromJson(json, out timelineAsset, director, assignToDirector);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CutsceneEngineUtility] Failed to load timeline JSON from '{filePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Rebind track generic bindings using a direct reference map.
        /// </summary>
        /// <param name="director">
        /// Target director that receives the new generic bindings via <c>SetGenericBinding</c>.
        /// </param>
        /// <param name="loadContext">
        /// Load context returned by the <c>TryFromJson(..., out TimelineJsonLoadContext, ...)</c> overload.
        /// Contains the serialized track ids and runtime track instances to rebind.
        /// </param>
        /// <param name="referenceMap">
        /// User-provided map from binding keys to new object references.
        /// Key lookup priority is: <c>binding.path</c> -> <c>binding.name</c> -> path leaf name (legacy fallback).
        /// Values should be <see cref="GameObject"/> or <see cref="Component"/> objects that match each track binding type.
        /// </param>
        /// <remarks>
        /// Key priority: binding.path -> binding.name -> last segment of binding.path.
        /// </remarks>
        /// <returns>
        /// Detailed result DTO with per-track statuses and aggregate counts (success, missing key/track, type mismatch, etc.).
        /// </returns>
        public static TimelineTrackBindingRebindResult RebindTrackBindings(PlayableDirector director, TimelineJsonLoadContext loadContext, IReadOnlyDictionary<string, Object> referenceMap)
        {
            var result = new TimelineTrackBindingRebindResult();

            if (!director || loadContext == null || loadContext.data == null || referenceMap == null)
                return result;

            var tracks = loadContext.data.tracks;
            if (tracks == null)
                return result;

            for (var i = 0; i < tracks.Count; i++)
            {
                var trackData = tracks[i];
                var entry = new TimelineTrackBindingRebindEntry
                {
                    trackId = trackData?.id,
                    trackName = trackData?.name,
                    trackType = trackData?.type,
                    requestedPath = trackData?.binding?.path
                };
                result.entries.Add(entry);
                result.totalCandidates++;

                if (trackData == null || trackData.binding == null)
                {
                    entry.status = "no_binding_data";
                    result.skippedNoBindingDataCount++;
                    continue;
                }

                entry.requestedName = GetRequestedBindingName(trackData.binding);

                if (loadContext.trackById == null || string.IsNullOrEmpty(trackData.id) || !loadContext.trackById.TryGetValue(trackData.id, out var track) || track == null)
                {
                    entry.status = "missing_track";
                    result.skippedMissingTrackCount++;
                    continue;
                }

                if (!TryResolveBindingOverride(trackData.binding, referenceMap, out var sourceObject, out var keyUsed))
                {
                    entry.status = "missing_key";
                    result.skippedMissingKeyCount++;
                    continue;
                }

                entry.keyUsed = keyUsed;
                var boundObject = ConvertObjectForTrackBinding(track, sourceObject);
                if (!boundObject)
                {
                    entry.status = "type_mismatch";
                    result.skippedTypeMismatchCount++;
                    continue;
                }

                try
                {
                    director.SetGenericBinding(track, boundObject);
                    entry.resolvedObject = boundObject;
                    entry.status = "rebound";
                    result.reboundCount++;
                }
                catch
                {
                    entry.status = "type_mismatch";
                    result.skippedTypeMismatchCount++;
                }
            }

            return result;
        }

        static void SerializeTrackRecursive(TrackAsset track, string parentId, PlayableDirector director, TimelineJsonData data, ref int nextTrackId, HashSet<string> skippedReferenceWarnings)
        {
            if (track == null)
                return;

            var trackData = new TrackJsonData
            {
                id = $"track_{nextTrackId++}",
                parentId = parentId,
                type = track.GetType().AssemblyQualifiedName,
                name = track.name,
                muted = GetBoolProperty(track, "muted"),
                locked = GetBoolProperty(track, "locked"),
                serializedJson = SafeToJson(track),
                objectReferences = CollectObjectReferences(track, director, $"{track.GetType().Name}({track.name})", skippedReferenceWarnings)
            };

            if (director)
            {
                var binding = director.GetGenericBinding(track);
                trackData.binding = BuildObjectReferenceData(binding, director, track.GetType().Name, $"trackBinding:{track.name}", skippedReferenceWarnings);
            }

            foreach (var clip in track.GetClips())
            {
                if (clip == null || clip.asset == null)
                    continue;

                var clipData = new ClipJsonData
                {
                    type = clip.asset.GetType().AssemblyQualifiedName,
                    displayName = clip.displayName,
                    start = clip.start,
                    duration = clip.duration,
                    clipIn = clip.clipIn,
                    timeScale = clip.timeScale,
                    easeInDuration = clip.easeInDuration,
                    easeOutDuration = clip.easeOutDuration,
                    serializedJson = SafeToJson(clip.asset),
                    objectReferences = CollectObjectReferences(clip.asset, director, clip.asset.GetType().Name, skippedReferenceWarnings)
                };

                trackData.clips.Add(clipData);
            }

            foreach (var marker in track.GetMarkers())
            {
                if (marker == null)
                    continue;

                var markerData = new MarkerJsonData
                {
                    type = marker.GetType().AssemblyQualifiedName,
                    time = marker.time,
                    serializedJson = SafeToJson(marker),
                    objectReferences = CollectObjectReferences(marker, director, marker.GetType().Name, skippedReferenceWarnings)
                };

                trackData.markers.Add(markerData);
            }

            data.tracks.Add(trackData);

            foreach (var child in track.GetChildTracks())
            {
                SerializeTrackRecursive(child, trackData.id, director, data, ref nextTrackId, skippedReferenceWarnings);
            }
        }

        static bool TryCreateTrack(TimelineAsset timelineAsset, PlayableDirector director, TrackJsonData trackData, Dictionary<string, TrackAsset> createdTracks, out TrackAsset createdTrack)
        {
            createdTrack = null;
            var trackType = ResolveType(trackData.type);
            if (trackType == null || !typeof(TrackAsset).IsAssignableFrom(trackType))
            {
                Debug.LogWarning($"[CutsceneEngineUtility] Unknown track type: {trackData.type}");
                return false;
            }

            TrackAsset parentTrack = null;
            if (!string.IsNullOrEmpty(trackData.parentId) && createdTracks.TryGetValue(trackData.parentId, out var parent))
                parentTrack = parent;

            try
            {
                createdTrack = timelineAsset.CreateTrack(trackType, parentTrack, string.IsNullOrEmpty(trackData.name) ? trackType.Name : trackData.name);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CutsceneEngineUtility] Failed creating track '{trackData.name}' ({trackData.type}): {ex.Message}");
                return false;
            }

            if (createdTrack == null)
                return false;

            SafeFromJsonOverwrite(trackData.serializedJson, createdTrack);
            if (!string.IsNullOrEmpty(trackData.name))
                createdTrack.name = trackData.name;

            SetBoolProperty(createdTrack, "muted", trackData.muted);
            SetBoolProperty(createdTrack, "locked", trackData.locked);
            ApplyObjectReferences(createdTrack, trackData.objectReferences, director);

            DeserializeClips(createdTrack, trackData.clips, director);
            DeserializeMarkers(createdTrack, trackData.markers, director);

            if (director && trackData.binding != null)
            {
                var binding = ResolveObjectReferenceData(trackData.binding, director);
                if (binding)
                    director.SetGenericBinding(createdTrack, binding);
            }

            return true;
        }

        static void DeserializeClips(TrackAsset track, List<ClipJsonData> clips, PlayableDirector director)
        {
            if (clips == null || track == null)
                return;

            foreach (var clipData in clips)
            {
                if (clipData == null)
                    continue;

                var clipType = ResolveType(clipData.type);
                if (clipType == null || !typeof(PlayableAsset).IsAssignableFrom(clipType))
                {
                    Debug.LogWarning($"[CutsceneEngineUtility] Unknown clip type: {clipData.type}");
                    continue;
                }

                TimelineClip clip;
                try
                {
                    clip = CreateClip(track, clipType);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CutsceneEngineUtility] Failed creating clip ({clipData.type}) on track '{track.name}': {ex.Message}");
                    continue;
                }

                if (clip == null)
                    continue;

                clip.start = clipData.start;
                clip.duration = Math.Max(0.0001d, clipData.duration);
                if (!string.IsNullOrEmpty(clipData.displayName))
                    clip.displayName = clipData.displayName;

                TrySetDoubleProperty(clip, "clipIn", clipData.clipIn);
                TrySetDoubleProperty(clip, "timeScale", Math.Max(0.0001d, clipData.timeScale));
                TrySetDoubleProperty(clip, "easeInDuration", Math.Max(0d, clipData.easeInDuration));
                TrySetDoubleProperty(clip, "easeOutDuration", Math.Max(0d, clipData.easeOutDuration));

                if (clip.asset != null)
                {
                    SafeFromJsonOverwrite(clipData.serializedJson, clip.asset);
                    ApplyObjectReferences(clip.asset, clipData.objectReferences, director);
                }
            }
        }

        static void DeserializeMarkers(TrackAsset track, List<MarkerJsonData> markers, PlayableDirector director)
        {
            if (markers == null || track == null)
                return;

            foreach (var markerData in markers)
            {
                if (markerData == null)
                    continue;

                var markerType = ResolveType(markerData.type);
                if (markerType == null || !typeof(IMarker).IsAssignableFrom(markerType))
                {
                    Debug.LogWarning($"[CutsceneEngineUtility] Unknown marker type: {markerData.type}");
                    continue;
                }

                IMarker marker;
                try
                {
                    marker = CreateMarker(track, markerType, markerData.time);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CutsceneEngineUtility] Failed creating marker ({markerData.type}) on track '{track.name}': {ex.Message}");
                    continue;
                }

                if (marker == null)
                    continue;

                SafeFromJsonOverwrite(markerData.serializedJson, marker);
                ApplyObjectReferences(marker, markerData.objectReferences, director);
            }
        }

        static string SafeToJson(object target)
        {
            if (target == null)
                return string.Empty;

            try
            {
                return JsonUtility.ToJson(target);
            }
            catch
            {
                return string.Empty;
            }
        }

        static void SafeFromJsonOverwrite(string json, object target)
        {
            if (target == null || string.IsNullOrEmpty(json))
                return;

            try
            {
                JsonUtility.FromJsonOverwrite(json, target);
            }
            catch
            {
                // Ignore malformed or incompatible serialized payloads.
            }
        }

        static List<ObjectReferenceFieldData> CollectObjectReferences(object target, PlayableDirector director, string ownerType, HashSet<string> skippedReferenceWarnings)
        {
            var result = new List<ObjectReferenceFieldData>();
            if (target == null)
                return result;

            var type = target.GetType();
            var fields = GetSerializableFields(type);
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                if (typeof(Object).IsAssignableFrom(fieldType))
                {
                    var obj = field.GetValue(target) as Object;
                    var referenceData = BuildObjectReferenceData(obj, director, ownerType, field.Name, skippedReferenceWarnings);
                    if (referenceData == null)
                        continue;

                    result.Add(new ObjectReferenceFieldData
                    {
                        fieldName = field.Name,
                        isExposedReferenceDefaultValue = false,
                        reference = referenceData
                    });
                    continue;
                }

                if (!IsExposedReferenceType(fieldType))
                    continue;

                var boxed = field.GetValue(target);
                if (boxed == null)
                    continue;

                var defaultValueField = fieldType.GetField("defaultValue", InstanceFieldFlags);
                if (defaultValueField == null)
                    continue;

                var defaultValue = defaultValueField.GetValue(boxed) as Object;
                var exposedReferenceData = BuildObjectReferenceData(defaultValue, director, ownerType, field.Name + ".defaultValue", skippedReferenceWarnings);
                if (exposedReferenceData == null)
                    continue;

                result.Add(new ObjectReferenceFieldData
                {
                    fieldName = field.Name,
                    isExposedReferenceDefaultValue = true,
                    reference = exposedReferenceData
                });
            }

            return result;
        }

        static void ApplyObjectReferences(object target, List<ObjectReferenceFieldData> objectReferences, PlayableDirector director)
        {
            if (target == null || objectReferences == null)
                return;

            var targetType = target.GetType();
            foreach (var fieldReference in objectReferences)
            {
                if (fieldReference == null || fieldReference.reference == null || string.IsNullOrEmpty(fieldReference.fieldName))
                    continue;

                var field = FindField(targetType, fieldReference.fieldName);
                if (field == null)
                    continue;

                var resolved = ResolveObjectReferenceData(fieldReference.reference, director);
                if (!resolved)
                    continue;

                if (fieldReference.isExposedReferenceDefaultValue && IsExposedReferenceType(field.FieldType))
                {
                    try
                    {
                        ApplyExposedReferenceDefaultValue(target, field, resolved);
                    }
                    catch
                    {
                        // Ignore field assignment failures.
                    }
                    continue;
                }

                var converted = ConvertObjectForField(field.FieldType, resolved);
                if (converted != null)
                {
                    try
                    {
                        field.SetValue(target, converted);
                    }
                    catch
                    {
                        // Ignore field assignment failures.
                    }
                }
            }
        }

        static void ApplyExposedReferenceDefaultValue(object target, FieldInfo field, Object resolved)
        {
            if (target == null || field == null || !resolved)
                return;

            var boxed = field.GetValue(target);
            if (boxed == null)
            {
                boxed = Activator.CreateInstance(field.FieldType);
            }

            var defaultValueField = field.FieldType.GetField("defaultValue", InstanceFieldFlags);
            if (defaultValueField == null)
                return;

            defaultValueField.SetValue(boxed, resolved);
            field.SetValue(target, boxed);
        }

        static Object ConvertObjectForField(Type fieldType, Object source)
        {
            if (fieldType == null || !source)
                return null;

            if (fieldType.IsInstanceOfType(source))
                return source;

            if (fieldType == typeof(GameObject) && source is Component sourceComponent)
                return sourceComponent.gameObject;

            if (typeof(Component).IsAssignableFrom(fieldType))
            {
                if (source is GameObject go)
                    return go.GetComponent(fieldType);

                if (source is Component component)
                    return component.GetComponent(fieldType);
            }

            if (fieldType == typeof(Transform))
            {
                if (source is GameObject sourceGameObject)
                    return sourceGameObject.transform;

                if (source is Component sourceComponent2)
                    return sourceComponent2.transform;
            }

            return null;
        }

        static string GetRequestedBindingName(ObjectReferenceData bindingData)
        {
            if (bindingData == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(bindingData.name))
                return bindingData.name;

            return GetPathLeafName(bindingData.path);
        }

        static string GetPathLeafName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            var idx = path.LastIndexOf('/');
            if (idx < 0 || idx == path.Length - 1)
                return path;

            return path.Substring(idx + 1);
        }

        static bool TryResolveBindingOverride(ObjectReferenceData bindingData, IReadOnlyDictionary<string, Object> referenceMap, out Object sourceObject, out string keyUsed)
        {
            sourceObject = null;
            keyUsed = string.Empty;
            if (bindingData == null || referenceMap == null)
                return false;

            if (!string.IsNullOrEmpty(bindingData.path) && referenceMap.TryGetValue(bindingData.path, out sourceObject) && sourceObject)
            {
                keyUsed = "path";
                return true;
            }

            if (!string.IsNullOrEmpty(bindingData.name) && referenceMap.TryGetValue(bindingData.name, out sourceObject) && sourceObject)
            {
                keyUsed = "name";
                return true;
            }

            var fallbackName = GetPathLeafName(bindingData.path);
            if (!string.IsNullOrEmpty(fallbackName) &&
                !string.Equals(fallbackName, bindingData.name, StringComparison.Ordinal) &&
                referenceMap.TryGetValue(fallbackName, out sourceObject) &&
                sourceObject)
            {
                keyUsed = "name";
                return true;
            }

            sourceObject = null;
            keyUsed = string.Empty;
            return false;
        }

        static Object ConvertObjectForTrackBinding(TrackAsset track, Object source)
        {
            if (!track || !source)
                return null;

            Type requiredType = null;
            var attribute = track.GetType().GetCustomAttribute<TrackBindingTypeAttribute>(true);
            if (attribute != null)
            {
                var attributeType = attribute.GetType();
                var typeProperty = attributeType.GetProperty("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (typeProperty != null && typeof(Type).IsAssignableFrom(typeProperty.PropertyType))
                {
                    requiredType = typeProperty.GetValue(attribute, null) as Type;
                }
                else
                {
                    var typeField = attributeType.GetField("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (typeField != null && typeof(Type).IsAssignableFrom(typeField.FieldType))
                        requiredType = typeField.GetValue(attribute) as Type;
                }
            }

            if (requiredType == null || requiredType == typeof(Object))
                return source;

            if (requiredType.IsInstanceOfType(source))
                return source;

            if (requiredType == typeof(GameObject))
            {
                if (source is Component sourceComponent)
                    return sourceComponent.gameObject;
                return null;
            }

            if (typeof(Component).IsAssignableFrom(requiredType))
            {
                if (source is GameObject go)
                    return go.GetComponent(requiredType);

                if (source is Component component)
                {
                    if (requiredType.IsInstanceOfType(component))
                        return component;
                    return component.GetComponent(requiredType);
                }
            }

            return null;
        }

        static bool CanCreateTrackNow(string parentId, Dictionary<string, TrackAsset> createdTracks)
        {
            return string.IsNullOrEmpty(parentId) || createdTracks.ContainsKey(parentId);
        }

        static ObjectReferenceData BuildObjectReferenceData(Object target, PlayableDirector director, string ownerType, string ownerField, HashSet<string> skippedReferenceWarnings)
        {
            if (!target)
                return null;

            if (target is GameObject go)
            {
                if (TryBuildTransformPath(go.transform, director, out var path))
                {
                    return new ObjectReferenceData
                    {
                        mode = RelativeGameObjectMode,
                        path = path,
                        name = go.name
                    };
                }

                LogSkippedReferenceWarning(skippedReferenceWarnings, director, ownerType, ownerField, target);
                return null;
            }

            if (target is Component component)
            {
                if (TryBuildTransformPath(component.transform, director, out var path))
                {
                    return new ObjectReferenceData
                    {
                        mode = RelativeComponentMode,
                        path = path,
                        name = component.transform ? component.transform.name : component.name,
                        componentType = component.GetType().AssemblyQualifiedName
                    };
                }

                LogSkippedReferenceWarning(skippedReferenceWarnings, director, ownerType, ownerField, target);
            }

            return null;
        }

        static Object ResolveObjectReferenceData(ObjectReferenceData referenceData, PlayableDirector director)
        {
            if (referenceData == null || string.IsNullOrEmpty(referenceData.mode))
                return null;

            switch (referenceData.mode)
            {
                case RelativeGameObjectMode:
                {
                    var transform = ResolveRelativeTransform(director, referenceData.path);
                    return transform ? transform.gameObject : null;
                }
                case RelativeComponentMode:
                {
                    var transform = ResolveRelativeTransform(director, referenceData.path);
                    return ResolveComponent(transform, referenceData.componentType);
                }
                case GlobalGameObjectMode:
                case GlobalComponentMode:
                    // Legacy v1 global mode is not resolved in director-relative v2.
                    return null;
            }

            return null;
        }

        static Object ResolveComponent(Transform transform, string componentTypeName)
        {
            if (!transform || string.IsNullOrEmpty(componentTypeName))
                return null;

            var componentType = ResolveType(componentTypeName);
            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
                return null;

            return transform.GetComponent(componentType);
        }

        static bool TryBuildTransformPath(Transform target, PlayableDirector director, out string path)
        {
            path = string.Empty;
            if (!target || !director || !director.transform)
                return false;

            var root = director.transform;
            if (target == root || target.IsChildOf(root))
            {
                path = GetRelativePath(root, target);
                return true;
            }

            return false;
        }

        static Transform ResolveRelativeTransform(PlayableDirector director, string relativePath)
        {
            if (!director || !director.transform)
                return null;

            if (string.IsNullOrEmpty(relativePath))
                return director.transform;

            return director.transform.Find(relativePath);
        }

        static string GetRelativePath(Transform root, Transform target)
        {
            if (!root || !target)
                return string.Empty;

            if (root == target)
                return string.Empty;

            var parts = new List<string>();
            var current = target;
            while (current && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            if (current != root)
                return string.Empty;

            parts.Reverse();
            return string.Join("/", parts);
        }

        static void LogSkippedReferenceWarning(HashSet<string> skippedReferenceWarnings, PlayableDirector director, string ownerType, string ownerField, Object target)
        {
            if (target == null)
                return;

            var directorName = director ? director.name : "(null)";
            var ownerTypeValue = string.IsNullOrEmpty(ownerType) ? "(unknown)" : ownerType;
            var ownerFieldValue = string.IsNullOrEmpty(ownerField) ? "(unknown)" : ownerField;
            var key = $"{ownerTypeValue}|{ownerFieldValue}|{target.GetInstanceID()}|{directorName}";

            if (skippedReferenceWarnings != null && !skippedReferenceWarnings.Add(key))
                return;

            Debug.LogWarning(
                $"[CutsceneEngineUtility] Skipped reference while saving Timeline JSON. " +
                $"ownerType='{ownerTypeValue}', field='{ownerFieldValue}', target='{target.name}' ({target.GetType().Name}), " +
                $"director='{directorName}', reason='not under PlayableDirector hierarchy'.");
        }

        static IMarker CreateMarker(TrackAsset track, Type markerType, double time)
        {
            if (track == null || markerType == null)
                return null;

            // Newer Timeline versions expose TrackAsset.CreateMarker(Type, double).
            var directMethod = typeof(TrackAsset).GetMethod(
                "CreateMarker",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type), typeof(double) },
                null);
            if (directMethod != null)
            {
                return directMethod.Invoke(track, new object[] { markerType, time }) as IMarker;
            }

            // Fallback for Timeline versions exposing only generic CreateMarker<T>(double).
            var methods = typeof(TrackAsset).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method.Name != "CreateMarker" || !method.IsGenericMethodDefinition)
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(double))
                    continue;

                try
                {
                    var generic = method.MakeGenericMethod(markerType);
                    return generic.Invoke(track, new object[] { time }) as IMarker;
                }
                catch
                {
                    // Try next overload.
                }
            }

            return null;
        }

        static TimelineClip CreateClip(TrackAsset track, Type clipType)
        {
            if (track == null || clipType == null)
                return null;

            // Newer Timeline versions expose TrackAsset.CreateClip(Type).
            var directMethod = typeof(TrackAsset).GetMethod(
                "CreateClip",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type) },
                null);
            if (directMethod != null)
            {
                return directMethod.Invoke(track, new object[] { clipType }) as TimelineClip;
            }

            // Fallback for Timeline versions exposing only generic CreateClip<T>().
            var methods = typeof(TrackAsset).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method.Name != "CreateClip" || !method.IsGenericMethodDefinition)
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 0)
                    continue;

                try
                {
                    var generic = method.MakeGenericMethod(clipType);
                    return generic.Invoke(track, null) as TimelineClip;
                }
                catch
                {
                    // Try next overload.
                }
            }

            return null;
        }

        static List<FieldInfo> GetSerializableFields(Type type)
        {
            var result = new List<FieldInfo>();
            while (type != null && type != typeof(object))
            {
                var fields = type.GetFields(InstanceFieldFlags | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (IsSerializableField(field))
                        result.Add(field);
                }

                type = type.BaseType;
            }

            return result;
        }

        static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null && type != typeof(object))
            {
                var field = type.GetField(fieldName, InstanceFieldFlags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;

                type = type.BaseType;
            }

            return null;
        }

        static bool IsSerializableField(FieldInfo field)
        {
            if (field == null || field.IsStatic || field.IsLiteral)
                return false;

            if (field.IsDefined(typeof(NonSerializedAttribute), true))
                return false;

            if (field.IsPublic)
                return true;

            return field.IsDefined(typeof(SerializeField), true);
        }

        static bool IsExposedReferenceType(Type fieldType)
        {
            return fieldType != null && fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(ExposedReference<>);
        }

        static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var type = Type.GetType(typeName, false);
            if (type != null)
                return type;

            var shortName = typeName.Split(',')[0].Trim();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName, false) ?? assembly.GetType(shortName, false);
                if (type != null)
                    return type;
            }

            return null;
        }

        static bool GetBoolProperty(object target, string propertyName)
        {
            if (target == null || string.IsNullOrEmpty(propertyName))
                return false;

            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || !property.CanRead || property.PropertyType != typeof(bool))
                return false;

            try
            {
                return (bool)property.GetValue(target, null);
            }
            catch
            {
                return false;
            }
        }

        static void SetBoolProperty(object target, string propertyName, bool value)
        {
            if (target == null || string.IsNullOrEmpty(propertyName))
                return;

            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || !property.CanWrite || property.PropertyType != typeof(bool))
                return;

            try
            {
                property.SetValue(target, value, null);
            }
            catch
            {
                // Ignore non-settable properties on this Timeline version.
            }
        }

        static double GetDoubleProperty(object target, string propertyName)
        {
            if (target == null || string.IsNullOrEmpty(propertyName))
                return 0d;

            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || !property.CanRead)
                return 0d;

            try
            {
                var value = property.GetValue(target, null);
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0d;
            }
        }

        static void SetDoubleProperty(object target, string propertyName, double value)
        {
            TrySetDoubleProperty(target, propertyName, value);
        }

        static void TrySetDoubleProperty(object target, string propertyName, double value)
        {
            if (target == null || string.IsNullOrEmpty(propertyName))
                return;

            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || !property.CanWrite)
                return;

            try
            {
                if (property.PropertyType == typeof(double))
                {
                    property.SetValue(target, value, null);
                }
                else if (property.PropertyType == typeof(float))
                {
                    property.SetValue(target, (float)value, null);
                }
                else if (property.PropertyType == typeof(int))
                {
                    property.SetValue(target, Mathf.RoundToInt((float)value), null);
                }
            }
            catch
            {
                // Ignore non-settable properties on this Timeline version.
            }
        }

        static string GetEnumPropertyName(object target, string propertyName)
        {
            if (target == null || string.IsNullOrEmpty(propertyName))
                return string.Empty;

            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || !property.CanRead || !property.PropertyType.IsEnum)
                return string.Empty;

            try
            {
                var value = property.GetValue(target, null);
                return value != null ? value.ToString() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        static void SetEnumPropertyByName(object target, string propertyName, string enumName)
        {
            if (target == null || string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(enumName))
                return;

            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
                return;

            try
            {
                var value = Enum.Parse(property.PropertyType, enumName);
                property.SetValue(target, value, null);
            }
            catch
            {
                // Ignore unknown enum values.
            }
        }

    }
}
