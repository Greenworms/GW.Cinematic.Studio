using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CutsceneEngine;
using Greenworms.Cinematics;
using Greenworms.Cinematics.CharacterStory.Production;
using Greenworms.Cinematics.SkullStrike.CharacterStory;
using Greenworms.Cinematics.SkullStrike.CharacterStory.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.TestTools;

namespace Greenworms.Cinematics.CharacterStory.Production.Editor.Tests
{
    public sealed class SkullStrikeCharacterStoryStudioSceneBuilderTests
    {
        private const string Root = "Assets/__CharacterStoryProductionTests";
        private const string StoryPath = Root + "/phase3-story.asset";
        private const string BotAi01ProfilePath =
            "Packages/com.greenworms.skullstrike.cinematic/Content/Assets/SkullStrikeCinematic/CharacterStory/Profiles/botai-01-dialogue-profile.asset";

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (AssetDatabase.IsValidFolder(Root))
                AssetDatabase.DeleteAsset(Root);
            AssetDatabase.Refresh();
        }

        [Test]
        public void StudioBuilderCreatesTimelineBindingsAndPreservesUserOverridesOnRebuild()
        {
            SkullStrikeCharacterStoryDefinition story = CreateStoryAsset();

            CharacterStoryBuildResult first =
                SkullStrikeCharacterStoryStudioSceneBuilder.BuildStory(story, false);

            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(first.ScenePath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<TimelineAsset>(first.TimelinePath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(first.ManifestPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(first.RecorderProfilePath), Is.Not.Null);
            CharacterStoryGeneratedOwnership sceneOwnership =
                FindComponents<CharacterStoryGeneratedOwnership>(SceneManager.GetActiveScene()).Single();
            Assert.That(sceneOwnership.StoryId, Is.EqualTo(story.StoryId));
            Assert.That(sceneOwnership.StorySchemaVersion, Is.EqualTo(story.SchemaVersion));
            Assert.That(sceneOwnership.BuilderVersion,
                Is.EqualTo(SkullStrikeCharacterStoryStudioSceneBuilder.CurrentBuilderVersion));
            CharacterStoryTimelineOwnership timelineOwnership = AssetDatabase
                .LoadAllAssetsAtPath(first.TimelinePath)
                .OfType<CharacterStoryTimelineOwnership>()
                .Single();
            Assert.That(timelineOwnership.StoryId, Is.EqualTo(story.StoryId));
            Assert.That(timelineOwnership.GeneratedRootTracks,
                Is.EquivalentTo(new[] { "CAMERAS", "DIALOGUE", "PERFORMANCE", "ENVIRONMENT AUDIO" }));
            AssertSceneStructureAndEvaluation();

            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(first.TimelinePath);
            GroupTrack overrides = timeline.GetRootTracks().OfType<GroupTrack>()
                .Single(track => track.name == "USER OVERRIDES");
            timeline.CreateTrack<GroupTrack>(overrides, "Manual Track");
            EditorUtility.SetDirty(timeline);
            GameObject userRoot = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(root => root.name == "USER OVERRIDES");
            new GameObject("Manual Scene Object").transform.SetParent(userRoot.transform, false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            story = AssetDatabase.LoadAssetAtPath<SkullStrikeCharacterStoryDefinition>(StoryPath);
            CharacterStoryBuildResult second =
                SkullStrikeCharacterStoryStudioSceneBuilder.BuildStory(story, false);

            Assert.That(second.ScenePath, Is.EqualTo(first.ScenePath));
            TimelineAsset rebuilt = AssetDatabase.LoadAssetAtPath<TimelineAsset>(second.TimelinePath);
            string[] rootNames = rebuilt.GetRootTracks().Select(track => track.name).ToArray();
            Assert.That(rootNames.Count(name => name == "DIALOGUE"), Is.EqualTo(1));
            Assert.That(rootNames.Count(name => name == "PERFORMANCE"), Is.EqualTo(1));
            Assert.That(rootNames.Count(name => name == "ENVIRONMENT AUDIO"), Is.EqualTo(1));
            Assert.That(rootNames.Count(name => name == "CAMERAS"), Is.EqualTo(1));
            GroupTrack rebuiltOverrides = rebuilt.GetRootTracks().OfType<GroupTrack>()
                .Single(track => track.name == "USER OVERRIDES");
            Assert.That(rebuiltOverrides.GetChildTracks().Select(track => track.name),
                Does.Contain("Manual Track"));
            GameObject preservedRoot = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(root => root.name == "USER OVERRIDES");
            Assert.That(preservedRoot.transform.Find("Manual Scene Object"), Is.Not.Null);
            AssertSceneStructureAndEvaluation();
        }

        [Test]
        public void ProductionValidatorAcceptsBuiltStoryAndPortableHandoffAssets()
        {
            SkullStrikeCharacterStoryDefinition story = CreateStoryAsset();
            CharacterStoryBuildResult result =
                SkullStrikeCharacterStoryStudioSceneBuilder.BuildStory(story, false);
            story = AssetDatabase.LoadAssetAtPath<SkullStrikeCharacterStoryDefinition>(StoryPath);

            IReadOnlyList<string> issues =
                SkullStrikeCharacterStoryProductionValidator.Validate(story, result);

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        [UnityTest]
        public IEnumerator BuiltStoryCanSeekReplayAndDisableItsGeneratedLayerInPlayMode()
        {
            SkullStrikeCharacterStoryDefinition story = CreateStoryAsset();
            SkullStrikeCharacterStoryStudioSceneBuilder.BuildStory(story, false);

            yield return new EnterPlayMode();

            Scene scene = SceneManager.GetActiveScene();
            PlayableDirector director = FindComponents<PlayableDirector>(scene).Single();
            TextMeshProUGUI subtitle = FindComponents<TextMeshProUGUI>(scene).Single();
            Camera[] cameras = FindComponents<Camera>(scene);
            director.Stop();
            director.time = 1d;
            director.Evaluate();
            Assert.That(subtitle.text, Does.Contain("Actor A: First line"));
            Assert.That(cameras.Count(camera => camera.enabled && camera.gameObject.activeInHierarchy),
                Is.EqualTo(1));

            director.Stop();
            director.time = 0d;
            director.Play();
            yield return null;
            director.time = 4d;
            director.Evaluate();
            Assert.That(subtitle.text, Does.Contain("Actor B: Second line"));
            Assert.That(cameras.Count(camera => camera.enabled && camera.gameObject.activeInHierarchy),
                Is.EqualTo(1));

            GameObject generated = scene.GetRootGameObjects()
                .Single(root => root.name == "__CHARACTER_STORY_GENERATED__");
            GameObject overrides = scene.GetRootGameObjects()
                .Single(root => root.name == "USER OVERRIDES");
            generated.SetActive(false);
            Assert.That(overrides.activeInHierarchy, Is.True);
            Assert.That(FindComponents<MonoBehaviour>(scene)
                .Where(component => component != null && component.gameObject.activeInHierarchy)
                .Select(component => component.GetType().Assembly.GetName().Name),
                Does.Not.Contain("Greenworms.Cinematics.SkullStrike"));

            yield return new ExitPlayMode();
        }

        [Test]
        public void PerformanceTimelineUsesProfileClipsAndNeverReferencesCombatAssembly()
        {
            SkullStrikeCharacterStoryDefinition story = CreateStoryAsset();
            Assert.That(story.Cast[0].DialogueProfile.TalkClip, Is.Not.Null);
            Assert.That(story.Cast[0].DialogueProfile.ListenClip, Is.Not.Null);
            Assert.That(story.Cast[0].DialogueProfile.TalkClip.length, Is.GreaterThan(0f));

            CharacterStoryBuildResult result =
                SkullStrikeCharacterStoryStudioSceneBuilder.BuildStory(story, false);

            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(result.TimelinePath);
            GroupTrack performance = timeline.GetRootTracks().OfType<GroupTrack>()
                .Single(track => track.name == "PERFORMANCE");
            AnimationTrack[] bodyTracks = performance.GetChildTracks().OfType<AnimationTrack>().ToArray();
            Assert.That(bodyTracks, Has.Length.EqualTo(2));
            Assert.That(bodyTracks.SelectMany(track => track.GetClips()).Count(), Is.EqualTo(4));
            Assert.That(bodyTracks.SelectMany(track => track.GetClips()).Select(clip => clip.displayName),
                Has.Some.Contains("[Approximate]"));
            Assert.That(typeof(CharacterStoryLookAtPresenter).Assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name), Does.Not.Contain("Greenworms.Cinematics.SkullStrike"));
        }

        [Test]
        public void BotAi01ProfileBuildsGestureListenAndLookAtTracksWithoutCombatComponents()
        {
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets", "__CharacterStoryProductionTests");
            SkullStrikeDialogueCharacterProfile profile =
                AssetDatabase.LoadAssetAtPath<SkullStrikeDialogueCharacterProfile>(BotAi01ProfilePath);
            Assert.That(profile, Is.Not.Null);
            GameObject map = CreatePrefab("real-profile-map", false);
            CharacterStoryCastMember actorA = new CharacterStoryCastMember();
            actorA.Configure("actor-a", "Actor A", CharacterStoryCastRole.Speaker,
                profile.ActorPrefab, profile, new Vector3(-1.5f, 0f, 0f), Vector3.zero);
            CharacterStoryCastMember actorB = new CharacterStoryCastMember();
            actorB.Configure("actor-b", "Actor B", CharacterStoryCastRole.Listener,
                profile.ActorPrefab, profile, new Vector3(1.5f, 0f, 0f), Vector3.zero);
            CharacterStoryDialogueBeat beatA = CreateBeat("beat-01", "actor-a", "actor-b", "Wave");
            CharacterStoryDialogueBeat beatB = CreateBeat("beat-02", "actor-b", "actor-a", "Applause");
            SetGesture(beatA, "wave");
            SetGesture(beatB, "applause");
            SkullStrikeCharacterStoryDefinition story =
                ScriptableObject.CreateInstance<SkullStrikeCharacterStoryDefinition>();
            story.Configure("botai-profile-story", "BotAI Profile Story", "Real profile test.",
                8f, "vi", new[] { actorA, actorB }, new[] { beatA, beatB }, map);
            AssetDatabase.CreateAsset(story, Root + "/botai-profile-story.asset");

            CharacterStoryBuildResult result =
                SkullStrikeCharacterStoryStudioSceneBuilder.BuildStory(story, false);

            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(result.TimelinePath);
            GroupTrack performance = timeline.GetRootTracks().OfType<GroupTrack>()
                .Single(track => track.name == "PERFORMANCE");
            Assert.That(performance.GetChildTracks().OfType<AnimationTrack>()
                .SelectMany(track => track.GetClips()).Count(), Is.EqualTo(4));
            Assert.That(performance.GetChildTracks().OfType<CharacterStoryLookAtTrack>().Count(),
                Is.EqualTo(2));
            CharacterStoryLookAtPresenter[] presenters =
                FindComponents<CharacterStoryLookAtPresenter>(SceneManager.GetActiveScene());
            Assert.That(presenters, Has.Length.EqualTo(2));
            Assert.That(presenters.All(presenter => presenter.IsConfigured), Is.True);
            Animator[] animators = FindComponents<Animator>(SceneManager.GetActiveScene());
            Assert.That(animators, Has.Length.EqualTo(2));
            Assert.That(animators.All(animator => animator.runtimeAnimatorController != null
                && animator.runtimeAnimatorController.name.EndsWith("-story")), Is.True);
            MonoBehaviour[] behaviours = FindComponents<MonoBehaviour>(SceneManager.GetActiveScene());
            Assert.That(behaviours.Select(item => item.GetType().Assembly.GetName().Name),
                Does.Not.Contain("Greenworms.Cinematics.SkullStrike"));
        }

        [Test]
        public void ProductionAdapterReferencesStoryButNotCombatAssembly()
        {
            string[] references = typeof(SkullStrikeCharacterStoryStudioSceneBuilder)
                .Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.That(references, Does.Contain("Greenworms.Cinematics.SkullStrike.CharacterStory"));
            Assert.That(references, Does.Not.Contain("Greenworms.Cinematics.SkullStrike"));
            Assert.That(references, Does.Not.Contain("Greenworms.Cinematics.SkullStrike.Editor"));
        }

        [Test]
        public void CameraPlannerKeepsAllConversationShotsOnOneSideOfActorLine()
        {
            Vector3 speaker = new Vector3(-1.5f, 0f, 0f);
            Vector3 listener = new Vector3(1.5f, 0f, 0f);
            CharacterStoryCameraKind[] kinds =
            {
                CharacterStoryCameraKind.Establishing,
                CharacterStoryCameraKind.TwoShot,
                CharacterStoryCameraKind.OverShoulderSpeaker,
                CharacterStoryCameraKind.OverShoulderListener,
                CharacterStoryCameraKind.CloseUpSpeaker,
                CharacterStoryCameraKind.Reaction,
                CharacterStoryCameraKind.Insert
            };
            Vector3 line = (listener - speaker).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, line).normalized;
            Vector3 center = (speaker + listener) * 0.5f;
            foreach (CharacterStoryCameraKind kind in kinds)
            {
                SkullStrikeCharacterStoryStudioSceneBuilder.CameraPose pose =
                    SkullStrikeCharacterStoryStudioSceneBuilder.CalculateCameraPose(
                        kind, speaker, listener, Vector3.zero, new Vector3(0f, 1.4f, 0f));
                Assert.That(Vector3.Dot(pose.Position - center, side), Is.GreaterThan(0f), kind.ToString());
                Assert.That((pose.LookTarget - pose.Position).sqrMagnitude, Is.GreaterThan(0.01f));
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void CameraPlannerSupportsTwoToFourActorBlocking(int actorCount)
        {
            Vector3[] positions = new Vector3[actorCount];
            for (int i = 0; i < actorCount; i++)
            {
                float angle = i * Mathf.PI * 2f / actorCount;
                positions[i] = new Vector3(Mathf.Cos(angle) * 2f, 0f, Mathf.Sin(angle) * 2f);
            }
            for (int i = 0; i < actorCount; i++)
            {
                Vector3 subject = positions[i];
                Vector3 target = positions[(i + 1) % actorCount];
                SkullStrikeCharacterStoryStudioSceneBuilder.CameraPose pose =
                    SkullStrikeCharacterStoryStudioSceneBuilder.CalculateCameraPose(
                        CharacterStoryCameraKind.OverShoulderSpeaker,
                        subject,
                        target,
                        Vector3.zero,
                        new Vector3(0f, 1.4f, 0f));
                Assert.That(float.IsNaN(pose.Position.x), Is.False);
                Assert.That((pose.LookTarget - pose.Position).sqrMagnitude, Is.GreaterThan(0.01f));
            }
        }

        private static SkullStrikeCharacterStoryDefinition CreateStoryAsset()
        {
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets", "__CharacterStoryProductionTests");
            GameObject mapPrefab = CreatePrefab("map", false);
            GameObject actorA = CreatePrefab("actor-a", true);
            GameObject actorB = CreatePrefab("actor-b", true);
            SkullStrikeDialogueCharacterProfile profileA = CreateProfile("profile-a", actorA);
            SkullStrikeDialogueCharacterProfile profileB = CreateProfile("profile-b", actorB);

            CharacterStoryCastMember memberA = new CharacterStoryCastMember();
            memberA.Configure("actor-a", "Actor A", CharacterStoryCastRole.Speaker,
                actorA, profileA, new Vector3(-1.5f, 0f, 0f), new Vector3(0f, 90f, 0f));
            CharacterStoryCastMember memberB = new CharacterStoryCastMember();
            memberB.Configure("actor-b", "Actor B", CharacterStoryCastRole.Listener,
                actorB, profileB, new Vector3(1.5f, 0f, 0f), new Vector3(0f, -90f, 0f));

            CharacterStoryDialogueBeat beatA = CreateBeat("beat-01", "actor-a", "actor-b", "First line");
            CharacterStoryDialogueBeat beatB = CreateBeat("beat-02", "actor-b", "actor-a", "Second line");
            SkullStrikeCharacterStoryDefinition story =
                ScriptableObject.CreateInstance<SkullStrikeCharacterStoryDefinition>();
            story.Configure(
                "phase3-story",
                "Phase 3 Story",
                "Timeline builder test.",
                8f,
                "vi",
                new[] { memberA, memberB },
                new[] { beatA, beatB },
                mapPrefab);
            story.ConfigureOutput(CinematicOutputFormat.Landscape, new Vector2Int(1920, 1080), 60);
            AssetDatabase.CreateAsset(story, StoryPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<SkullStrikeCharacterStoryDefinition>(StoryPath);
        }

        private static GameObject CreatePrefab(string name, bool withAnimator)
        {
            GameObject instance = new GameObject(name);
            if (withAnimator)
                instance.AddComponent<Animator>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, Root + "/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        private static SkullStrikeDialogueCharacterProfile CreateProfile(string id, GameObject actor)
        {
            AnimationClip performance = new AnimationClip { name = id + "-performance" };
            AnimationUtility.SetEditorCurve(
                performance,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.x"),
                AnimationCurve.Linear(0f, 0f, 1f, 0f));
            AssetDatabase.CreateAsset(performance, Root + "/" + id + "-performance.anim");
            SkullStrikeDialogueCharacterProfile profile =
                ScriptableObject.CreateInstance<SkullStrikeDialogueCharacterProfile>();
            profile.Configure(
                id,
                actor,
                CharacterStoryCapability.Idle
                    | CharacterStoryCapability.Talk
                    | CharacterStoryCapability.Listen
                    | CharacterStoryCapability.LookAt,
                "Movement",
                "Talk",
                "Listen",
                configuredTalkClip: performance,
                configuredListenClip: performance,
                configuredTalkFidelity: CharacterStoryPerformanceFidelity.Approximate,
                configuredListenFidelity: CharacterStoryPerformanceFidelity.Approximate);
            AssetDatabase.CreateAsset(profile, Root + "/" + id + ".asset");
            return profile;
        }

        private static CharacterStoryDialogueBeat CreateBeat(
            string id,
            string speaker,
            string listener,
            string text)
        {
            CharacterStoryCameraInstruction camera = new CharacterStoryCameraInstruction();
            camera.Configure(CharacterStoryCameraKind.Auto, speaker, listener, 48f,
                Vector3.zero, new Vector3(0f, 1.4f, 0f));
            CharacterStoryDialogueBeat beat = new CharacterStoryDialogueBeat();
            beat.Configure(
                id,
                speaker,
                listener,
                text,
                CharacterStoryBeatStartMode.Sequential,
                0f,
                CharacterStoryDurationPolicy.Manual,
                3f,
                configuredLookTargetKey: listener,
                configuredCameraInstruction: camera);
            return beat;
        }

        private static void SetGesture(CharacterStoryDialogueBeat beat, string gestureId)
        {
            CharacterStoryCameraInstruction camera = beat.CameraInstruction;
            beat.Configure(
                beat.BeatId,
                beat.SpeakerKey,
                beat.ListenerKey,
                beat.SubtitleText,
                beat.StartMode,
                beat.StartTime,
                beat.DurationPolicy,
                beat.Duration,
                beat.VoiceClip,
                beat.LookTargetKey,
                beat.EmotionTag,
                gestureId,
                beat.SubtitleStyleId,
                camera);
        }

        private static void AssertSceneStructureAndEvaluation()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayableDirector director = FindComponents<PlayableDirector>(scene).Single();
            Assert.That(FindComponents<Cutscene>(scene), Has.Length.EqualTo(1));
            Assert.That(FindComponents<CutsceneActor>(scene), Has.Length.EqualTo(2));
            Assert.That(FindComponents<AudioListener>(scene), Has.Length.EqualTo(1));
            Assert.That(FindComponents<Camera>(scene), Has.Length.EqualTo(2));
            director.time = 1d;
            director.Evaluate();
            TextMeshProUGUI subtitle = FindComponents<TextMeshProUGUI>(scene).Single();
            Assert.That(subtitle.text, Does.Contain("Actor A: First line"));
            Assert.That(
                FindComponents<Camera>(scene).Count(
                    camera => camera.enabled && camera.gameObject.activeInHierarchy),
                Is.EqualTo(1));
            director.time = 4d;
            director.Evaluate();
            Assert.That(subtitle.text, Does.Contain("Actor B: Second line"));
            Assert.That(
                FindComponents<Camera>(scene).Count(
                    camera => camera.enabled && camera.gameObject.activeInHierarchy),
                Is.EqualTo(1));
            director.Stop();
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            List<T> values = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                values.AddRange(root.GetComponentsInChildren<T>(true));
            return values.ToArray();
        }
    }
}
