using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace CutsceneEngine
{
    /// <summary>
    /// Context returned by JSON load APIs, used for post-load manual rebinding.
    /// </summary>
    [Serializable]
    public class TimelineJsonLoadContext
    {
        /// <summary>Parsed JSON data used to build the runtime timeline.</summary>
        public TimelineJsonData data;
        /// <summary>Map from serialized track id to created runtime track instance.</summary>
        public Dictionary<string, TrackAsset> trackById = new Dictionary<string, TrackAsset>();
        /// <summary>The runtime timeline asset created from JSON.</summary>
        public TimelineAsset timelineAsset;
    }

    /// <summary>
    /// Aggregate result of manual track binding rebinding.
    /// </summary>
    [Serializable]
    public class TimelineTrackBindingRebindResult
    {
        /// <summary>Total number of track entries evaluated for rebinding.</summary>
        public int totalCandidates;
        /// <summary>Number of tracks successfully rebound.</summary>
        public int reboundCount;
        /// <summary>Number of tracks skipped because binding data was not present in JSON.</summary>
        public int skippedNoBindingDataCount;
        /// <summary>Number of tracks skipped because no override key matched in the input map.</summary>
        public int skippedMissingKeyCount;
        /// <summary>Number of tracks skipped because the runtime track instance could not be found.</summary>
        public int skippedMissingTrackCount;
        /// <summary>Number of tracks skipped because override object type was incompatible.</summary>
        public int skippedTypeMismatchCount;
        /// <summary>Detailed per-track rebinding entries.</summary>
        public List<TimelineTrackBindingRebindEntry> entries = new List<TimelineTrackBindingRebindEntry>();
    }

    /// <summary>
    /// Detailed status for one track rebinding attempt.
    /// </summary>
    [Serializable]
    public class TimelineTrackBindingRebindEntry
    {
        /// <summary>Serialized track id.</summary>
        public string trackId;
        /// <summary>Serialized track name.</summary>
        public string trackName;
        /// <summary>Serialized track type name.</summary>
        public string trackType;
        /// <summary>Key type used for matching override object ("path" or "name").</summary>
        public string keyUsed;
        /// <summary>Requested original binding path from JSON.</summary>
        public string requestedPath;
        /// <summary>Requested original binding name from JSON.</summary>
        public string requestedName;
        /// <summary>Final object assigned as track generic binding when successful.</summary>
        public Object resolvedObject;
        /// <summary>Result status (rebound, missing_key, missing_track, type_mismatch, no_binding_data).</summary>
        public string status;
    }
}
