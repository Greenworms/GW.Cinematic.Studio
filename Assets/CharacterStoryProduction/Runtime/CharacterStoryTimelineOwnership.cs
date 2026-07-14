using System;
using System.Collections.Generic;
using UnityEngine;

namespace Greenworms.Cinematics.CharacterStory.Production
{
    public sealed class CharacterStoryTimelineOwnership : ScriptableObject
    {
        [SerializeField] private string storyId;
        [SerializeField] private int storySchemaVersion;
        [SerializeField] private int builderVersion;
        [SerializeField] private string[] generatedRootTracks = Array.Empty<string>();

        public string StoryId => storyId;
        public int StorySchemaVersion => storySchemaVersion;
        public int BuilderVersion => builderVersion;
        public IReadOnlyList<string> GeneratedRootTracks => generatedRootTracks;

        public void Configure(string id, int schemaVersion, int version, string[] rootTracks)
        {
            storyId = id;
            storySchemaVersion = schemaVersion;
            builderVersion = version;
            generatedRootTracks = rootTracks != null
                ? (string[])rootTracks.Clone()
                : Array.Empty<string>();
        }
    }
}
