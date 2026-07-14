using UnityEngine;

namespace Greenworms.Cinematics.CharacterStory.Production
{
    [DisallowMultipleComponent]
    public sealed class CharacterStoryGeneratedOwnership : MonoBehaviour
    {
        [SerializeField] private string storyId;
        [SerializeField] private int storySchemaVersion;
        [SerializeField] private int builderVersion;

        public string StoryId => storyId;
        public int StorySchemaVersion => storySchemaVersion;
        public int BuilderVersion => builderVersion;

        public void Configure(string id, int schemaVersion, int version)
        {
            storyId = id;
            storySchemaVersion = schemaVersion;
            builderVersion = version;
        }
    }

}
