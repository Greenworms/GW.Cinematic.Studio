using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutsceneEngine;
using Greenworms.Cinematics;
using Greenworms.Cinematics.CharacterStory.Production;
using Greenworms.Cinematics.SkullStrike.CharacterStory;
using Greenworms.Cinematics.SkullStrike.CharacterStory.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Greenworms.Cinematics.CharacterStory.Production.Editor
{
    public sealed class SkullStrikeCharacterStoryStudioSceneBuilder : ICharacterStorySceneBuilder
    {
        public const int CurrentBuilderVersion = 1;
        private const string GeneratedRootName = "__CHARACTER_STORY_GENERATED__";
        private const string UserOverridesName = "USER OVERRIDES";
        private static readonly string[] GeneratedRootTrackNames =
        {
            "CAMERAS",
            "DIALOGUE",
            "PERFORMANCE",
            "ENVIRONMENT AUDIO"
        };

        public string DisplayName => "Studio Cutscene Engine";

        public CharacterStoryBuildResult Build(SkullStrikeCharacterStoryDefinition story)
        {
            return BuildStory(story, true);
        }

        public static CharacterStoryBuildResult BuildStory(
            SkullStrikeCharacterStoryDefinition story,
            bool promptToSaveCurrentScenes = true)
        {
            List<CharacterStoryValidationIssue> issues = SkullStrikeCharacterStoryValidator.Validate(story);
            List<string> errors = issues
                .Where(issue => issue.Severity == CharacterStoryValidationSeverity.Error)
                .Select(issue => issue.Code + " — " + issue.Path + " — " + issue.Message)
                .ToList();
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));
            if (promptToSaveCurrentScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Character Story build was cancelled.");

            string storyAssetPath = AssetDatabase.GetAssetPath(story);
            if (string.IsNullOrWhiteSpace(storyAssetPath))
                throw new InvalidOperationException("Character Story must be saved as an asset before build.");
            string folder = Path.GetDirectoryName(storyAssetPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(folder))
                throw new InvalidOperationException("Unable to resolve Character Story asset folder.");
            EnsureFolder(folder + "/Recorder");
            EnsureFolder(folder + "/Sequences");

            string scenePath = folder + "/" + story.StoryId + ".unity";
            string timelinePath = folder + "/" + story.StoryId + ".playable";
            string manifestPath = folder + "/" + story.StoryId + "-manifest.asset";
            string recorderPath = folder + "/Recorder/" + story.StoryId + "-recorder-profile.asset";

            Scene scene = OpenOrCreateScene(scenePath);
            GameObject userOverrides = EnsureUserOverridesRoot(scene);
            DeleteGeneratedSceneRoot(scene);
            GameObject generatedRoot = new GameObject(GeneratedRootName);
            CharacterStoryGeneratedOwnership sceneOwnership =
                generatedRoot.AddComponent<CharacterStoryGeneratedOwnership>();
            sceneOwnership.Configure(
                story.StoryId,
                story.SchemaVersion,
                CurrentBuilderVersion);
            generatedRoot.transform.SetAsFirstSibling();

            Transform environmentRoot = CreateChild(generatedRoot.transform, "ENVIRONMENT");
            Transform castRoot = CreateChild(generatedRoot.transform, "CAST");
            Transform camerasRoot = CreateChild(generatedRoot.transform, "CAMERAS");
            Transform timelineRoot = CreateChild(generatedRoot.transform, "TIMELINE");
            CreateLighting(generatedRoot.transform);

            GameObject map = InstantiatePrefab(story.MapPrefab, "Environment — Map");
            map.transform.SetParent(environmentRoot, true);

            Dictionary<string, GameObject> actors = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            Dictionary<string, AudioSource> voices = new Dictionary<string, AudioSource>(StringComparer.Ordinal);
            for (int i = 0; i < story.Cast.Count; i++)
            {
                CharacterStoryCastMember member = story.Cast[i];
                GameObject actor = InstantiatePrefab(member.ActorPrefab, member.DisplayName);
                actor.transform.SetParent(castRoot, true);
                actor.transform.SetPositionAndRotation(
                    member.StartPosition,
                    Quaternion.Euler(member.StartRotationEuler));
                ConfigureStoryAnimator(
                    actor, member, folder + "/Sequences/" + story.StoryId + "-"
                        + member.ActorKey + "-story.controller");
                NormalizeAudioSources(actor);
                AddCutsceneActor(actor, member.ActorKey);
                GameObject voiceObject = new GameObject("Voice — " + member.ActorKey);
                voiceObject.transform.SetParent(actor.transform, false);
                AudioSource voice = voiceObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                actors.Add(member.ActorKey, actor);
                voices.Add(member.ActorKey, voice);
            }

            GameObject directorObject = new GameObject("Character Story Director");
            directorObject.transform.SetParent(timelineRoot, false);
            PlayableDirector director = directorObject.AddComponent<PlayableDirector>();
            director.playOnAwake = true;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            directorObject.AddComponent<AudioListener>();
            Cutscene cutscene = directorObject.AddComponent<Cutscene>();
            cutscene.director = director;

            TimelineAsset timeline = LoadOrCreateTimeline(timelinePath, story.TargetFrameRate);
            director.playableAsset = timeline;
            RemoveGeneratedTracks(timeline);
            GroupTrack userOverridesTrack = EnsureUserOverridesTrack(timeline);
            BuildTimeline(
                story,
                timeline,
                director,
                actors,
                voices,
                camerasRoot,
                generatedRoot.transform,
                userOverridesTrack);
            CreateOrUpdateTimelineOwnership(timeline, timelinePath, story);

            CreateOrUpdateManifest(story, scenePath, timelinePath, manifestPath);
            CreateOrUpdateRecorderProfile(story, recorderPath);
            userOverrides.transform.SetAsLastSibling();
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();

            Debug.Log("[Character Story] Built Studio scene and Timeline: " + scenePath);
            return new CharacterStoryBuildResult(scenePath, timelinePath, manifestPath, recorderPath);
        }

        private static void CreateOrUpdateTimelineOwnership(
            TimelineAsset timeline,
            string timelinePath,
            SkullStrikeCharacterStoryDefinition story)
        {
            CharacterStoryTimelineOwnership ownership = AssetDatabase
                .LoadAllAssetsAtPath(timelinePath)
                .OfType<CharacterStoryTimelineOwnership>()
                .FirstOrDefault();
            if (ownership == null)
            {
                ownership = ScriptableObject.CreateInstance<CharacterStoryTimelineOwnership>();
                ownership.name = "Character Story Ownership";
                AssetDatabase.AddObjectToAsset(ownership, timeline);
            }
            ownership.Configure(
                story.StoryId,
                story.SchemaVersion,
                CurrentBuilderVersion,
                GeneratedRootTrackNames);
            EditorUtility.SetDirty(ownership);
        }

        private static void BuildTimeline(
            SkullStrikeCharacterStoryDefinition story,
            TimelineAsset timeline,
            PlayableDirector director,
            Dictionary<string, GameObject> actors,
            Dictionary<string, AudioSource> voices,
            Transform camerasRoot,
            Transform generatedRoot,
            GroupTrack userOverrides)
        {
            List<BeatTiming> timings = ResolveTimings(story);

            GroupTrack cameraGroup = timeline.CreateTrack<GroupTrack>(null, "CAMERAS");
            CreateDialogueCameraCoverage(
                story, timings, timeline, director, cameraGroup, camerasRoot, actors);

            SubtitleText subtitleText = CreateSubtitleCanvas(generatedRoot, story.TargetResolution);
            GroupTrack dialogueGroup = timeline.CreateTrack<GroupTrack>(null, "DIALOGUE");
            SubtitleTrack subtitleTrack = timeline.CreateTrack<SubtitleTrack>(dialogueGroup, "Subtitles");
            director.SetGenericBinding(subtitleTrack, subtitleText);
            for (int i = 0; i < timings.Count; i++)
            {
                BeatTiming timing = timings[i];
                CharacterStoryCastMember speaker = story.Cast.First(
                    member => string.Equals(member.ActorKey, timing.Beat.SpeakerKey, StringComparison.Ordinal));
                CreateSubtitle(
                    subtitleTrack,
                    speaker.DisplayName + ": " + timing.Beat.SubtitleText,
                    timing.Start,
                    timing.Duration);
            }

            foreach (KeyValuePair<string, AudioSource> entry in voices)
            {
                AudioTrack voiceTrack = timeline.CreateTrack<AudioTrack>(dialogueGroup, "Voice — " + entry.Key);
                director.SetGenericBinding(voiceTrack, entry.Value);
                foreach (BeatTiming timing in timings)
                {
                    if (!string.Equals(timing.Beat.SpeakerKey, entry.Key, StringComparison.Ordinal)
                        || timing.Beat.VoiceClip == null)
                        continue;
                    TimelineClip clip = voiceTrack.CreateClip<AudioPlayableAsset>();
                    AudioPlayableAsset playable = clip.asset as AudioPlayableAsset;
                    playable.clip = timing.Beat.VoiceClip;
                    playable.loop = false;
                    clip.start = timing.Start;
                    clip.duration = timing.Beat.VoiceClip.length;
                    clip.displayName = timing.Beat.BeatId + " — voice";
                }
            }

            GroupTrack performanceGroup = timeline.CreateTrack<GroupTrack>(null, "PERFORMANCE");
            foreach (KeyValuePair<string, GameObject> entry in actors)
            {
                Animator animator = entry.Value.GetComponentInChildren<Animator>(true);
                if (animator == null)
                    continue;
                AnimationTrack bodyTrack = timeline.CreateTrack<AnimationTrack>(
                    performanceGroup, "Body — " + entry.Key);
                director.SetGenericBinding(bodyTrack, animator);
                CharacterStoryCastMember member = story.Cast.First(
                    cast => string.Equals(cast.ActorKey, entry.Key, StringComparison.Ordinal));
                foreach (BeatTiming timing in timings)
                {
                    AnimationClip performanceClip = ResolvePerformanceClip(
                        member, timing.Beat, entry.Key, out string performanceId,
                        out CharacterStoryPerformanceFidelity fidelity);
                    if (ReferenceEquals(performanceClip, null))
                        continue;
                    TimelineClip clip = bodyTrack.CreateClip<AnimationPlayableAsset>();
                    AnimationPlayableAsset playable = clip.asset as AnimationPlayableAsset;
                    playable.clip = performanceClip;
                    playable.applyFootIK = true;
                    clip.start = timing.Start;
                    clip.duration = Math.Min(timing.Duration, performanceClip.length);
                    double ease = Math.Min(member.DialogueProfile.TransitionDuration, clip.duration * 0.5d);
                    clip.easeInDuration = ease;
                    clip.easeOutDuration = ease;
                    clip.displayName = timing.Beat.BeatId + " — " + performanceId
                        + (fidelity == CharacterStoryPerformanceFidelity.Exact
                            ? string.Empty
                            : " [" + fidelity + "]");
                }

                BuildLookAtTrack(
                    member, entry.Key, entry.Value, animator, timings, timeline, director,
                    performanceGroup, actors);
            }

            GroupTrack environmentAudio = timeline.CreateTrack<GroupTrack>(null, "ENVIRONMENT AUDIO");
            AudioSource ambienceSource = CreateAudioSource(generatedRoot, "Ambience Audio");
            AudioSource musicSource = CreateAudioSource(generatedRoot, "Music Audio");
            AudioTrack ambienceTrack = timeline.CreateTrack<AudioTrack>(environmentAudio, "Ambience");
            AudioTrack musicTrack = timeline.CreateTrack<AudioTrack>(environmentAudio, "Music");
            director.SetGenericBinding(ambienceTrack, ambienceSource);
            director.SetGenericBinding(musicTrack, musicSource);

        }

        private static AnimationClip ResolvePerformanceClip(
            CharacterStoryCastMember member,
            CharacterStoryDialogueBeat beat,
            string actorKey,
            out string performanceId,
            out CharacterStoryPerformanceFidelity fidelity)
        {
            performanceId = string.Empty;
            fidelity = CharacterStoryPerformanceFidelity.Exact;
            SkullStrikeDialogueCharacterProfile profile = member.DialogueProfile;
            if (ReferenceEquals(profile, null))
                return null;

            bool speakerMatch = string.Equals(beat.SpeakerKey, actorKey, StringComparison.Ordinal);
            bool listenerMatch = string.Equals(beat.ListenerKey, actorKey, StringComparison.Ordinal);
            if (speakerMatch)
            {
                if (!string.IsNullOrWhiteSpace(beat.GestureId)
                    && profile.TryGetGesture(beat.GestureId, out CharacterStoryGestureMapping gesture)
                    && gesture.AnimationClip != null)
                {
                    performanceId = beat.GestureId;
                    fidelity = gesture.Fidelity;
                    return gesture.AnimationClip;
                }
                performanceId = "talk";
                fidelity = profile.TalkFidelity;
                return profile.TalkClip;
            }

            if (listenerMatch)
            {
                performanceId = "listen";
                fidelity = profile.ListenFidelity;
                return profile.ListenClip;
            }

            return null;
        }

        private static void ConfigureStoryAnimator(
            GameObject actor,
            CharacterStoryCastMember member,
            string controllerPath)
        {
            Animator animator = actor.GetComponentInChildren<Animator>(true);
            if (animator == null)
                return;
            animator.applyRootMotion = false;
            SkullStrikeDialogueCharacterProfile profile = member.DialogueProfile;
            AnimationClip idleClip = ReferenceEquals(profile, null) ? null : profile.IdleClip;
            if (idleClip == null)
            {
                animator.runtimeAnimatorController = null;
                return;
            }

            if (AssetDatabase.LoadMainAssetAtPath(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.AddState("Story Idle");
            idle.motion = idleClip;
            stateMachine.defaultState = idle;
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(controller);
        }

        private static void BuildLookAtTrack(
            CharacterStoryCastMember member,
            string actorKey,
            GameObject actor,
            Animator animator,
            IReadOnlyList<BeatTiming> timings,
            TimelineAsset timeline,
            PlayableDirector director,
            GroupTrack performanceGroup,
            IReadOnlyDictionary<string, GameObject> actors)
        {
            SkullStrikeDialogueCharacterProfile profile = member.DialogueProfile;
            if (ReferenceEquals(profile, null)
                || !profile.HasCapability(CharacterStoryCapability.LookAt))
                return;
            CharacterStoryLookAtPresenter presenter = actor.AddComponent<CharacterStoryLookAtPresenter>();
            if (!presenter.Configure(animator, profile))
            {
                UnityEngine.Object.DestroyImmediate(presenter);
                return;
            }

            CharacterStoryLookAtTrack track = timeline.CreateTrack<CharacterStoryLookAtTrack>(
                performanceGroup, "Look At — " + actorKey);
            director.SetGenericBinding(track, presenter);
            for (int i = 0; i < timings.Count; i++)
            {
                BeatTiming timing = timings[i];
                string targetKey = string.Equals(timing.Beat.SpeakerKey, actorKey, StringComparison.Ordinal)
                    ? timing.Beat.LookTargetKey
                    : string.Equals(timing.Beat.ListenerKey, actorKey, StringComparison.Ordinal)
                        ? timing.Beat.SpeakerKey
                        : string.Empty;
                if (string.IsNullOrWhiteSpace(targetKey)
                    || !actors.TryGetValue(targetKey, out GameObject target))
                    continue;

                TimelineClip timelineClip = track.CreateClip<CharacterStoryLookAtClip>();
                CharacterStoryLookAtClip lookAt = timelineClip.asset as CharacterStoryLookAtClip;
                PropertyName referenceName = new PropertyName(
                    "character-story-look-at-" + actorKey + "-" + timing.Beat.BeatId);
                lookAt.Target.exposedName = referenceName;
                lookAt.Weight = 1f;
                director.SetReferenceValue(referenceName, target.transform);
                timelineClip.start = timing.Start;
                timelineClip.duration = timing.Duration;
                double ease = Math.Min(profile.TransitionDuration, timing.Duration * 0.5d);
                timelineClip.easeInDuration = ease;
                timelineClip.easeOutDuration = ease;
                timelineClip.displayName = timing.Beat.BeatId + " — look at " + targetKey;
            }
        }

        private static void CreateDialogueCameraCoverage(
            SkullStrikeCharacterStoryDefinition story,
            IReadOnlyList<BeatTiming> timings,
            TimelineAsset timeline,
            PlayableDirector director,
            GroupTrack group,
            Transform parent,
            IReadOnlyDictionary<string, GameObject> actors)
        {
            for (int i = 0; i < timings.Count; i++)
            {
                BeatTiming timing = timings[i];
                CharacterStoryCameraInstruction instruction = timing.Beat.CameraInstruction;
                CharacterStoryCameraKind kind = instruction.Kind == CharacterStoryCameraKind.Auto
                    ? (i % 2 == 0
                        ? CharacterStoryCameraKind.OverShoulderSpeaker
                        : CharacterStoryCameraKind.Reaction)
                    : instruction.Kind;
                string subjectKey = string.IsNullOrWhiteSpace(instruction.SubjectKey)
                    ? timing.Beat.SpeakerKey
                    : instruction.SubjectKey;
                string targetKey = string.IsNullOrWhiteSpace(instruction.LookTargetKey)
                    ? timing.Beat.ListenerKey
                    : instruction.LookTargetKey;
                GameObject subject = actors.TryGetValue(subjectKey, out GameObject resolvedSubject)
                    ? resolvedSubject
                    : actors[timing.Beat.SpeakerKey];
                GameObject target = actors.TryGetValue(targetKey, out GameObject resolvedTarget)
                    ? resolvedTarget
                    : subject;
                CameraPose pose = CalculateCameraPose(
                    kind,
                    subject.transform.position,
                    target.transform.position,
                    instruction.PositionOffset,
                    instruction.LookOffset);

                string name = "Shot " + (i + 1).ToString("000") + " — " + kind + " — " + timing.Beat.BeatId;
                GameObject cameraObject = new GameObject(name);
                cameraObject.transform.SetParent(parent, false);
                cameraObject.transform.SetPositionAndRotation(
                    pose.Position,
                    Quaternion.LookRotation(pose.LookTarget - pose.Position, Vector3.up));
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.fieldOfView = instruction.FieldOfView;

                double start = i == 0 ? 0d : timing.Start;
                double end = i + 1 < timings.Count ? timings[i + 1].Start : story.TargetDuration;
                end = Math.Max(end, start + 1d / story.TargetFrameRate);
                ActivationTrack track = timeline.CreateTrack<ActivationTrack>(group, name);
                TimelineClip clip = track.CreateDefaultClip();
                clip.start = start;
                clip.duration = end - start;
                director.SetGenericBinding(track, cameraObject);
            }
        }

        public static CameraPose CalculateCameraPose(
            CharacterStoryCameraKind kind,
            Vector3 subject,
            Vector3 target,
            Vector3 positionOffset,
            Vector3 lookOffset)
        {
            Vector3 line = target - subject;
            line.y = 0f;
            if (line.sqrMagnitude < 0.0001f)
                line = Vector3.forward;
            line.Normalize();
            Vector3 cameraSide = Vector3.Cross(Vector3.up, line).normalized;
            Vector3 center = (subject + target) * 0.5f;
            Vector3 position;
            Vector3 lookTarget;
            switch (kind)
            {
                case CharacterStoryCameraKind.Establishing:
                    position = center + cameraSide * 7f + Vector3.up * 3f;
                    lookTarget = center + Vector3.up * 1.2f;
                    break;
                case CharacterStoryCameraKind.TwoShot:
                    position = center + cameraSide * 4.5f + Vector3.up * 1.9f;
                    lookTarget = center + Vector3.up * 1.3f;
                    break;
                case CharacterStoryCameraKind.OverShoulderListener:
                    position = target - line * 0.85f + cameraSide * 0.55f + Vector3.up * 1.65f;
                    lookTarget = subject + lookOffset;
                    break;
                case CharacterStoryCameraKind.CloseUpSpeaker:
                    position = subject + line * 2.1f + cameraSide * 0.35f + Vector3.up * 1.55f;
                    lookTarget = subject + lookOffset;
                    break;
                case CharacterStoryCameraKind.Reaction:
                    position = target - line * 2.1f + cameraSide * 0.35f + Vector3.up * 1.55f;
                    lookTarget = target + lookOffset;
                    break;
                case CharacterStoryCameraKind.Insert:
                    position = center + cameraSide * 2.6f + Vector3.up * 1.1f;
                    lookTarget = center + lookOffset * 0.5f;
                    break;
                case CharacterStoryCameraKind.OverShoulderSpeaker:
                case CharacterStoryCameraKind.Auto:
                default:
                    position = subject + line * 0.85f + cameraSide * 0.55f + Vector3.up * 1.65f;
                    lookTarget = target + lookOffset;
                    break;
            }
            return new CameraPose(position + positionOffset, lookTarget);
        }

        private static List<BeatTiming> ResolveTimings(SkullStrikeCharacterStoryDefinition story)
        {
            List<BeatTiming> result = new List<BeatTiming>();
            double cursor = 0d;
            for (int i = 0; i < story.DialogueBeats.Count; i++)
            {
                CharacterStoryDialogueBeat beat = story.DialogueBeats[i];
                double start = beat.StartMode == CharacterStoryBeatStartMode.Absolute
                    ? beat.StartTime
                    : cursor;
                double duration = beat.ResolveDuration();
                result.Add(new BeatTiming(beat, start, duration));
                cursor = Math.Max(cursor, start + duration);
            }
            return result;
        }

        private static void CreateSubtitle(
            SubtitleTrack track,
            string text,
            double start,
            double duration)
        {
            TimelineClip clip = track.CreateClip<SubtitleClip>();
            clip.start = start;
            clip.duration = duration;
            clip.displayName = text;
            SubtitleClip subtitle = clip.asset as SubtitleClip;
            if (subtitle == null)
                throw new InvalidOperationException("Unable to create Cutscene Engine SubtitleClip.");
            subtitle.text = text;
        }

        private static SubtitleText CreateSubtitleCanvas(Transform parent, Vector2Int resolution)
        {
            GameObject canvasObject = new GameObject("Character Story Subtitles", typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = resolution;

            GameObject panelObject = new GameObject("Subtitle Safe Area", typeof(RectTransform));
            panelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.1f, 0.05f);
            panel.anchorMax = new Vector2(0.9f, 0.22f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            GameObject textObject = new GameObject("Subtitle Text", typeof(RectTransform));
            textObject.transform.SetParent(panelObject.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 42f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = string.Empty;
            SubtitleText subtitle = textObject.AddComponent<SubtitleText>();
            subtitle.textDisplayEffect = TextDisplayEffect.None;
            return subtitle;
        }

        private static TimelineAsset LoadOrCreateTimeline(string path, int frameRate)
        {
            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(timeline, path);
            }
            timeline.editorSettings.frameRate = frameRate;
            return timeline;
        }

        private static void RemoveGeneratedTracks(TimelineAsset timeline)
        {
            TrackAsset[] roots = timeline.GetRootTracks().ToArray();
            for (int i = 0; i < roots.Length; i++)
                if (!string.Equals(roots[i].name, UserOverridesName, StringComparison.Ordinal))
                    timeline.DeleteTrack(roots[i]);
        }

        private static GroupTrack EnsureUserOverridesTrack(TimelineAsset timeline)
        {
            GroupTrack existing = timeline.GetRootTracks()
                .OfType<GroupTrack>()
                .FirstOrDefault(track => string.Equals(track.name, UserOverridesName, StringComparison.Ordinal));
            return existing ?? timeline.CreateTrack<GroupTrack>(null, UserOverridesName);
        }

        private static Scene OpenOrCreateScene(string scenePath)
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null
                ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static GameObject EnsureUserOverridesRoot(Scene scene)
        {
            GameObject existing = scene.GetRootGameObjects()
                .FirstOrDefault(root => string.Equals(root.name, UserOverridesName, StringComparison.Ordinal));
            return existing ?? new GameObject(UserOverridesName);
        }

        private static void DeleteGeneratedSceneRoot(Scene scene)
        {
            GameObject existing = scene.GetRootGameObjects()
                .FirstOrDefault(root => string.Equals(root.name, GeneratedRootName, StringComparison.Ordinal));
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing);
        }

        private static GameObject InstantiatePrefab(GameObject prefab, string name)
        {
            if (prefab == null)
                throw new InvalidOperationException("Cannot instantiate a null Character Story prefab.");
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = name;
            return instance;
        }

        private static void AddCutsceneActor(GameObject actor, string actorKey)
        {
            CutsceneActor cutsceneActor = actor.GetComponent<CutsceneActor>() ?? actor.AddComponent<CutsceneActor>();
            SerializedObject serialized = new SerializedObject(cutsceneActor);
            SerializedProperty key = serialized.FindProperty("key");
            if (key == null)
                throw new InvalidOperationException("CutsceneActor.key was not found.");
            key.stringValue = actorKey;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NormalizeAudioSources(GameObject actor)
        {
            foreach (AudioSource source in actor.GetComponentsInChildren<AudioSource>(true))
            {
                source.playOnAwake = false;
                source.Stop();
            }
        }

        private static AudioSource CreateAudioSource(Transform parent, string name)
        {
            GameObject value = new GameObject(name);
            value.transform.SetParent(parent, false);
            AudioSource source = value.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject lightObject = new GameObject("Character Story Key Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.intensity = 1.2f;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void CreateOrUpdateManifest(
            SkullStrikeCharacterStoryDefinition story,
            string scenePath,
            string timelinePath,
            string manifestPath)
        {
            CinematicScenarioManifest manifest =
                AssetDatabase.LoadAssetAtPath<CinematicScenarioManifest>(manifestPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<CinematicScenarioManifest>();
                AssetDatabase.CreateAsset(manifest, manifestPath);
            }
            manifest.Configure(
                story.StoryId,
                "Skullstrike",
                story.DisplayName,
                story.Description,
                "character-story",
                story.StoryId,
                scenePath,
                timelinePath,
                story.TargetDuration);
            manifest.ConfigureOutput(story.OutputFormat, story.TargetResolution, story.TargetFrameRate);
            EditorUtility.SetDirty(manifest);
        }

        private static void CreateOrUpdateRecorderProfile(
            SkullStrikeCharacterStoryDefinition story,
            string recorderPath)
        {
            CinematicRecorderProfile profile =
                AssetDatabase.LoadAssetAtPath<CinematicRecorderProfile>(recorderPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CinematicRecorderProfile>();
                AssetDatabase.CreateAsset(profile, recorderPath);
            }
            profile.Configure(
                story.OutputFormat,
                story.TargetResolution,
                story.TargetFrameRate,
                "Recordings/Skullstrike/CharacterStories/" + story.StoryId);
            EditorUtility.SetDirty(profile);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Invalid folder path: " + path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct BeatTiming
        {
            public BeatTiming(CharacterStoryDialogueBeat beat, double start, double duration)
            {
                Beat = beat;
                Start = start;
                Duration = duration;
            }

            public CharacterStoryDialogueBeat Beat { get; }
            public double Start { get; }
            public double Duration { get; }
        }

        public readonly struct CameraPose
        {
            public CameraPose(Vector3 position, Vector3 lookTarget)
            {
                Position = position;
                LookTarget = lookTarget;
            }

            public Vector3 Position { get; }
            public Vector3 LookTarget { get; }
        }
    }
}
