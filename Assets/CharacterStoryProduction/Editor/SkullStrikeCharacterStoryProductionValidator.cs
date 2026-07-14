using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutsceneEngine;
using Greenworms.Cinematics.CharacterStory.Production;
using Greenworms.Cinematics.SkullStrike.CharacterStory;
using Greenworms.Cinematics.SkullStrike.CharacterStory.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace Greenworms.Cinematics.CharacterStory.Production.Editor
{
    public static class SkullStrikeCharacterStoryProductionValidator
    {
        private const string GeneratedRootName = "__CHARACTER_STORY_GENERATED__";
        private const string UserOverridesName = "USER OVERRIDES";

        [MenuItem(
            "Tools/Greenworms/Cinematic Studio/Production/Character Story/04. Validate Built Story",
            false,
            204)]
        private static void ValidateSelectedStory()
        {
            SkullStrikeCharacterStoryDefinition story =
                Selection.activeObject as SkullStrikeCharacterStoryDefinition;
            if (story == null)
                throw new InvalidOperationException("Select a Character Story Definition first.");
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(story))
                ?.Replace('\\', '/');
            CharacterStoryBuildResult result = new CharacterStoryBuildResult(
                folder + "/" + story.StoryId + ".unity",
                folder + "/" + story.StoryId + ".playable",
                folder + "/" + story.StoryId + "-manifest.asset",
                folder + "/Recorder/" + story.StoryId + "-recorder-profile.asset");
            IReadOnlyList<string> issues = Validate(story, result);
            if (issues.Count > 0)
                throw new InvalidOperationException(string.Join("\n", issues));
            Debug.Log("[Character Story] Production validation passed: " + story.StoryId);
        }

        public static IReadOnlyList<string> Validate(
            SkullStrikeCharacterStoryDefinition story,
            CharacterStoryBuildResult result)
        {
            List<string> issues = new List<string>();
            if (story == null)
            {
                issues.Add("STORY_NULL — Story Definition is missing.");
                return issues;
            }
            RequireAsset(result.ScenePath, "SCENE_MISSING", issues);
            RequireAsset(result.TimelinePath, "TIMELINE_MISSING", issues);
            RequireAsset(result.ManifestPath, "MANIFEST_MISSING", issues);
            RequireAsset(result.RecorderProfilePath, "RECORDER_PROFILE_MISSING", issues);

            Scene scene = SceneManager.GetActiveScene();
            if (!string.Equals(scene.path, result.ScenePath, StringComparison.Ordinal))
            {
                issues.Add("SCENE_NOT_OPEN — Open the built Story scene before validation: "
                    + result.ScenePath);
                return issues;
            }
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject[] generatedRoots = roots
                .Where(root => root.name == GeneratedRootName)
                .ToArray();
            if (generatedRoots.Length != 1)
                issues.Add("GENERATED_ROOT — Expected exactly one generated scene root.");
            else
                ValidateSceneOwnership(story, generatedRoots[0], issues);
            if (roots.Count(root => root.name == UserOverridesName) != 1)
                issues.Add("USER_OVERRIDES_ROOT — Expected exactly one USER OVERRIDES root.");

            PlayableDirector[] directors = FindComponents<PlayableDirector>(roots);
            Cutscene[] cutscenes = FindComponents<Cutscene>(roots);
            AudioListener[] listeners = FindComponents<AudioListener>(roots);
            if (directors.Length != 1)
                issues.Add("DIRECTOR_COUNT — Expected one PlayableDirector, found " + directors.Length + ".");
            if (cutscenes.Length != 1)
                issues.Add("CUTSCENE_COUNT — Expected one Cutscene, found " + cutscenes.Length + ".");
            if (listeners.Length != 1)
                issues.Add("LISTENER_COUNT — Expected one AudioListener, found " + listeners.Length + ".");
            ValidateMissingScripts(roots, issues);
            ValidateCombatIsolation(roots, issues);

            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(result.TimelinePath);
            if (timeline != null)
                ValidateTimeline(story, result.TimelinePath, timeline,
                    directors.Length == 1 ? directors[0] : null, roots, issues);
            ValidatePortableDependencies(story, result, issues);
            return issues;
        }

        private static void ValidateSceneOwnership(
            SkullStrikeCharacterStoryDefinition story,
            GameObject root,
            ICollection<string> issues)
        {
            CharacterStoryGeneratedOwnership[] ownership =
                root.GetComponents<CharacterStoryGeneratedOwnership>();
            if (ownership.Length != 1
                || ownership[0].StoryId != story.StoryId
                || ownership[0].StorySchemaVersion != story.SchemaVersion
                || ownership[0].BuilderVersion
                    != SkullStrikeCharacterStoryStudioSceneBuilder.CurrentBuilderVersion)
                issues.Add("SCENE_OWNERSHIP — Generated scene ownership/version is missing or stale.");
        }

        private static void ValidateTimeline(
            SkullStrikeCharacterStoryDefinition story,
            string timelinePath,
            TimelineAsset timeline,
            PlayableDirector director,
            GameObject[] roots,
            ICollection<string> issues)
        {
            string[] required = { "CAMERAS", "DIALOGUE", "PERFORMANCE", "ENVIRONMENT AUDIO" };
            string[] rootNames = timeline.GetRootTracks().Select(track => track.name).ToArray();
            foreach (string name in required)
                if (rootNames.Count(candidate => candidate == name) != 1)
                    issues.Add("TRACK_GROUP — Expected one generated root track named " + name + ".");
            if (rootNames.Count(candidate => candidate == UserOverridesName) != 1)
                issues.Add("USER_OVERRIDES_TRACK — Expected one USER OVERRIDES root track.");

            CharacterStoryTimelineOwnership[] ownership = AssetDatabase
                .LoadAllAssetsAtPath(timelinePath)
                .OfType<CharacterStoryTimelineOwnership>()
                .ToArray();
            if (ownership.Length != 1
                || ownership[0].StoryId != story.StoryId
                || ownership[0].StorySchemaVersion != story.SchemaVersion
                || ownership[0].BuilderVersion
                    != SkullStrikeCharacterStoryStudioSceneBuilder.CurrentBuilderVersion
                || required.Except(ownership[0].GeneratedRootTracks).Any())
                issues.Add("TIMELINE_OWNERSHIP — Timeline ownership/version is missing or stale.");

            if (director == null || director.playableAsset != timeline)
            {
                issues.Add("DIRECTOR_TIMELINE — Director is not bound to the built Timeline.");
                return;
            }
            foreach (TrackAsset track in FlattenTracks(timeline))
            {
                if (track is CharacterStoryLookAtTrack)
                {
                    if (!(director.GetGenericBinding(track) is CharacterStoryLookAtPresenter))
                        issues.Add("LOOK_AT_BINDING — " + track.name + " has no presenter binding.");
                    foreach (TimelineClip clip in track.GetClips())
                    {
                        CharacterStoryLookAtClip asset = clip.asset as CharacterStoryLookAtClip;
                        if (asset == null || asset.Target.Resolve(director) == null)
                            issues.Add("LOOK_AT_TARGET — " + clip.displayName + " has no resolved target.");
                    }
                }
            }
            ValidateCameraCoverage(story, director, roots, issues);
        }

        private static void ValidateCameraCoverage(
            SkullStrikeCharacterStoryDefinition story,
            PlayableDirector director,
            GameObject[] roots,
            ICollection<string> issues)
        {
            Camera[] cameras = FindComponents<Camera>(roots);
            if (cameras.Length != story.DialogueBeats.Count)
                issues.Add("CAMERA_COUNT — Expected one generated camera per dialogue beat.");
            double cursor = 0d;
            for (int i = 0; i < story.DialogueBeats.Count; i++)
            {
                CharacterStoryDialogueBeat beat = story.DialogueBeats[i];
                double start = beat.ResolveStartTime((float)cursor);
                double duration = Math.Max(0.01d, beat.ResolveDuration());
                director.time = start + Math.Min(duration * 0.5d, duration - 0.001d);
                director.Evaluate();
                int active = cameras.Count(camera =>
                    camera.enabled && camera.gameObject.activeInHierarchy);
                if (active != 1)
                    issues.Add("CAMERA_COVERAGE — Beat " + beat.BeatId
                        + " evaluates with " + active + " active cameras.");
                cursor = Math.Max(cursor, start + duration);
            }
            director.Stop();
        }

        private static void ValidateMissingScripts(GameObject[] roots, ICollection<string> issues)
        {
            foreach (GameObject root in roots)
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    transform.gameObject);
                if (count > 0)
                    issues.Add("MISSING_SCRIPT — " + GetHierarchyPath(transform)
                        + " has " + count + " missing scripts.");
            }
        }

        private static void ValidateCombatIsolation(GameObject[] roots, ICollection<string> issues)
        {
            foreach (MonoBehaviour behaviour in FindComponents<MonoBehaviour>(roots))
            {
                if (behaviour == null)
                    continue;
                string assembly = behaviour.GetType().Assembly.GetName().Name;
                if (assembly == "Greenworms.Cinematics.SkullStrike")
                    issues.Add("COMBAT_COMPONENT — Story scene contains combat component "
                        + behaviour.GetType().FullName + ".");
            }
        }

        private static void ValidatePortableDependencies(
            SkullStrikeCharacterStoryDefinition story,
            CharacterStoryBuildResult result,
            ICollection<string> issues)
        {
            string storyPath = AssetDatabase.GetAssetPath(story);
            string[] dependencies = AssetDatabase.GetDependencies(new[]
            {
                storyPath,
                result.ScenePath,
                result.TimelinePath,
                result.ManifestPath,
                result.RecorderProfilePath
            }, true);
            foreach (string dependency in dependencies)
            {
                if (Path.IsPathRooted(dependency)
                    || dependency.Contains("/Users/", StringComparison.OrdinalIgnoreCase))
                    issues.Add("NON_PORTABLE_DEPENDENCY — " + dependency);
                if (dependency.Contains(
                        "/Content/Assets/SkullStrikeCinematic/CharacterStory/",
                        StringComparison.OrdinalIgnoreCase)
                    && (dependency.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        || dependency.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase)
                        || dependency.EndsWith(".controller", StringComparison.OrdinalIgnoreCase)))
                    issues.Add("CONTENT_BOUNDARY — Authoring content contains generated/source asset: "
                        + dependency);
            }
        }

        private static IEnumerable<TrackAsset> FlattenTracks(TimelineAsset timeline)
        {
            foreach (TrackAsset root in timeline.GetRootTracks())
            {
                yield return root;
                foreach (TrackAsset child in FlattenChildren(root))
                    yield return child;
            }
        }

        private static IEnumerable<TrackAsset> FlattenChildren(TrackAsset parent)
        {
            foreach (TrackAsset child in parent.GetChildTracks())
            {
                yield return child;
                foreach (TrackAsset descendant in FlattenChildren(child))
                    yield return descendant;
            }
        }

        private static void RequireAsset(string path, string code, ICollection<string> issues)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.LoadMainAssetAtPath(path) == null)
                issues.Add(code + " — Missing asset: " + path);
        }

        private static T[] FindComponents<T>(GameObject[] roots) where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
