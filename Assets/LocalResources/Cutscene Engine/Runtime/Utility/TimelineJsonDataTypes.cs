using System;
using System.Collections.Generic;

namespace CutsceneEngine
{
    /// <summary>
    /// Root JSON payload for a serialized TimelineAsset.
    /// </summary>
    [Serializable]
    public class TimelineJsonData
    {
        /// <summary>Serialization format version.</summary>
        public int version;
        /// <summary>Timeline asset name.</summary>
        public string name;
        /// <summary>Duration mode enum name (stored as string).</summary>
        public string durationMode;
        /// <summary>Fixed duration value used when duration mode is fixed.</summary>
        public double fixedDuration;
        /// <summary>All serialized tracks, including nested child tracks.</summary>
        public List<TrackJsonData> tracks = new List<TrackJsonData>();
    }

    /// <summary>
    /// Serialized track data.
    /// </summary>
    [Serializable]
    public class TrackJsonData
    {
        /// <summary>Unique track identifier within this JSON payload.</summary>
        public string id;
        /// <summary>Parent track id. Empty for root tracks.</summary>
        public string parentId;
        /// <summary>Track type name (assembly-qualified).</summary>
        public string type;
        /// <summary>Track display name.</summary>
        public string name;
        /// <summary>Whether the track is muted.</summary>
        public bool muted;
        /// <summary>Whether the track is locked.</summary>
        public bool locked;
        /// <summary>Raw track object payload from JsonUtility.</summary>
        public string serializedJson;
        /// <summary>Track generic binding reference data.</summary>
        public ObjectReferenceData binding;
        /// <summary>Additional serialized object references found on the track object.</summary>
        public List<ObjectReferenceFieldData> objectReferences = new List<ObjectReferenceFieldData>();
        /// <summary>Serialized clips contained in this track.</summary>
        public List<ClipJsonData> clips = new List<ClipJsonData>();
        /// <summary>Serialized markers contained in this track.</summary>
        public List<MarkerJsonData> markers = new List<MarkerJsonData>();
    }

    /// <summary>
    /// Serialized clip data.
    /// </summary>
    [Serializable]
    public class ClipJsonData
    {
        /// <summary>Clip playable asset type name (assembly-qualified).</summary>
        public string type;
        /// <summary>Clip display name.</summary>
        public string displayName;
        /// <summary>Timeline start time (seconds).</summary>
        public double start;
        /// <summary>Clip duration (seconds).</summary>
        public double duration;
        /// <summary>Clip-in offset (seconds).</summary>
        public double clipIn;
        /// <summary>Playback time scale.</summary>
        public double timeScale;
        /// <summary>Ease-in duration (seconds).</summary>
        public double easeInDuration;
        /// <summary>Ease-out duration (seconds).</summary>
        public double easeOutDuration;
        /// <summary>Raw clip asset payload from JsonUtility.</summary>
        public string serializedJson;
        /// <summary>Serialized object references found on the clip asset object.</summary>
        public List<ObjectReferenceFieldData> objectReferences = new List<ObjectReferenceFieldData>();
    }

    /// <summary>
    /// Serialized marker data.
    /// </summary>
    [Serializable]
    public class MarkerJsonData
    {
        /// <summary>Marker type name (assembly-qualified).</summary>
        public string type;
        /// <summary>Marker timeline time (seconds).</summary>
        public double time;
        /// <summary>Raw marker payload from JsonUtility.</summary>
        public string serializedJson;
        /// <summary>Serialized object references found on the marker object.</summary>
        public List<ObjectReferenceFieldData> objectReferences = new List<ObjectReferenceFieldData>();
    }

    /// <summary>
    /// Serialized reference metadata for one field on an object.
    /// </summary>
    [Serializable]
    public class ObjectReferenceFieldData
    {
        /// <summary>Field name where the reference should be restored.</summary>
        public string fieldName;
        /// <summary>True when this field represents ExposedReference&lt;T&gt;.defaultValue.</summary>
        public bool isExposedReferenceDefaultValue;
        /// <summary>Stored reference information used for resolution.</summary>
        public ObjectReferenceData reference;
    }

    /// <summary>
    /// Serialized object reference payload.
    /// </summary>
    [Serializable]
    public class ObjectReferenceData
    {
        /// <summary>Reference mode (for example, relative game object or relative component).</summary>
        public string mode;
        /// <summary>Relative path from the PlayableDirector root.</summary>
        public string path;
        /// <summary>Original referenced object name for fallback matching.</summary>
        public string name;
        /// <summary>Component type name when mode represents a component reference.</summary>
        public string componentType;
    }
}
