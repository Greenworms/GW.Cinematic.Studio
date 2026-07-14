using System;
using System.Collections.Generic;
using CutsceneEngine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Greenworms.Cinematics.CharacterStory.Spike.Editor
{
    public static class CharacterStoryIsolationSpikeBuilder
    {
        private const string RootFolder = "Assets/CharacterStorySpike";
        private const string GeneratedFolder = RootFolder + "/Generated";
        private const string ScenePath = GeneratedFolder + "/character-story-isolation-spike.unity";
        private const string TimelinePath = GeneratedFolder + "/character-story-isolation-spike.playable";
        private const string ActorAPath = "Assets/Prefabs/actor/actor-BotAI_01.prefab";
        private const string ActorBPath = "Assets/Prefabs/actor/actor-BotAI_02.prefab";

        [MenuItem("Tools/Greenworms/Cinematic Studio/Developer/Spikes/Build Character Story Isolation Spike", priority = 990)]
        public static void BuildFromMenu()
        {
            BuildAndValidate();
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildAndValidate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildAndValidate()
        {
            EnsureFolder("Assets", "CharacterStorySpike");
            EnsureFolder(RootFolder, "Generated");
            DeleteOwnedAsset(ScenePath);
            DeleteOwnedAsset(TimelinePath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Character Story Isolation Spike");
            Transform castRoot = CreateChild(root.transform, "Cast");
            Transform camerasRoot = CreateChild(root.transform, "Cameras");
            Transform timelineRoot = CreateChild(root.transform, "Timeline");
            CreateLighting(root.transform);

            GameObject actorA = InstantiateActor(ActorAPath, "Speaker A", castRoot,
                new Vector3(-1.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), "speaker-a");
            GameObject actorB = InstantiateActor(ActorBPath, "Speaker B", castRoot,
                new Vector3(1.5f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), "speaker-b");

            GameObject directorObject = new GameObject("Character Story Timeline Director");
            directorObject.transform.SetParent(timelineRoot, false);
            PlayableDirector director = directorObject.AddComponent<PlayableDirector>();
            directorObject.AddComponent<AudioListener>();
            Cutscene cutscene = directorObject.AddComponent<Cutscene>();
            cutscene.director = director;

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "character-story-isolation-spike";
            timeline.editorSettings.frameRate = 60f;
            AssetDatabase.CreateAsset(timeline, TimelinePath);
            director.playableAsset = timeline;
            director.playOnAwake = true;
            director.extrapolationMode = DirectorWrapMode.Hold;

            GroupTrack cameraGroup = timeline.CreateTrack<GroupTrack>(null, "CAMERAS");
            CreateCameraShot(timeline, director, cameraGroup, camerasRoot, "Shot 001 - Establishing",
                new Vector3(0f, 2.1f, -6.2f), new Vector3(0f, 1.25f, 0f), 0d, 2.5d);
            CreateCameraShot(timeline, director, cameraGroup, camerasRoot, "Shot 002 - Speaker A",
                new Vector3(0.25f, 1.8f, -3.1f), actorA.transform.position + Vector3.up * 1.35f, 2.5d, 3d);
            CreateCameraShot(timeline, director, cameraGroup, camerasRoot, "Shot 003 - Speaker B",
                new Vector3(-0.25f, 1.8f, -3.1f), actorB.transform.position + Vector3.up * 1.35f, 5.5d, 2.5d);

            SubtitleText subtitleText = CreateSubtitleCanvas(root.transform);
            GroupTrack dialogueGroup = timeline.CreateTrack<GroupTrack>(null, "DIALOGUE");
            SubtitleTrack subtitleTrack = timeline.CreateTrack<SubtitleTrack>(dialogueGroup, "Subtitles");
            director.SetGenericBinding(subtitleTrack, subtitleText);
            CreateSubtitle(subtitleTrack, "Speaker A: Chúng ta bắt đầu câu chuyện ở đây.", 0.5d, 2.75d);
            CreateSubtitle(subtitleTrack, "Speaker B: Camera và Timeline đang lắng nghe.", 4d, 3d);

            AudioSource voiceA = actorA.AddComponent<AudioSource>();
            voiceA.playOnAwake = false;
            AudioSource voiceB = actorB.AddComponent<AudioSource>();
            voiceB.playOnAwake = false;
            AudioTrack voiceATrack = timeline.CreateTrack<AudioTrack>(dialogueGroup, "Voice - speaker-a (asset gap)");
            AudioTrack voiceBTrack = timeline.CreateTrack<AudioTrack>(dialogueGroup, "Voice - speaker-b (asset gap)");
            director.SetGenericBinding(voiceATrack, voiceA);
            director.SetGenericBinding(voiceBTrack, voiceB);

            GroupTrack performanceGroup = timeline.CreateTrack<GroupTrack>(null, "PERFORMANCE");
            BindAnimationTrack(timeline, director, performanceGroup, actorA, "Body - speaker-a (idle only)");
            BindAnimationTrack(timeline, director, performanceGroup, actorB, "Body - speaker-b (idle only)");
            timeline.CreateTrack<GroupTrack>(null, "USER OVERRIDES");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            ValidateScene(scene, timeline);
            Selection.activeObject = timeline;
            Debug.Log("[Character Story Spike] Build and isolation validation passed: " + ScenePath);
        }

        private static GameObject InstantiateActor(
            string prefabPath,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            string actorKey)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Missing spike actor prefab: " + prefabPath);
            GameObject actor = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (actor == null)
                throw new InvalidOperationException("Unable to instantiate spike actor: " + prefabPath);
            actor.name = name;
            actor.transform.SetParent(parent, true);
            actor.transform.SetPositionAndRotation(position, rotation);
            CutsceneActor cutsceneActor = actor.AddComponent<CutsceneActor>();
            SerializedObject serialized = new SerializedObject(cutsceneActor);
            SerializedProperty key = serialized.FindProperty("key");
            if (key == null)
                throw new InvalidOperationException("CutsceneActor.key was not found.");
            key.stringValue = actorKey;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return actor;
        }

        private static void CreateCameraShot(
            TimelineAsset timeline,
            PlayableDirector director,
            GroupTrack group,
            Transform parent,
            string name,
            Vector3 position,
            Vector3 target,
            double start,
            double duration)
        {
            GameObject cameraObject = new GameObject(name);
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.LookRotation(target - position, Vector3.up);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 48f;
            ActivationTrack track = timeline.CreateTrack<ActivationTrack>(group, name);
            TimelineClip clip = track.CreateDefaultClip();
            clip.start = start;
            clip.duration = duration;
            director.SetGenericBinding(track, cameraObject);
        }

        private static SubtitleText CreateSubtitleCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("Character Story Subtitles", typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject textObject = new GameObject("Subtitle Text", typeof(RectTransform));
            textObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.05f);
            rect.anchorMax = new Vector2(0.9f, 0.22f);
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

        private static void CreateSubtitle(SubtitleTrack track, string text, double start, double duration)
        {
            TimelineClip timelineClip = track.CreateClip<SubtitleClip>();
            timelineClip.start = start;
            timelineClip.duration = duration;
            timelineClip.displayName = text;
            SubtitleClip subtitleClip = timelineClip.asset as SubtitleClip;
            if (subtitleClip == null)
                throw new InvalidOperationException("Unable to create SubtitleClip.");
            subtitleClip.text = text;
        }

        private static void BindAnimationTrack(
            TimelineAsset timeline,
            PlayableDirector director,
            GroupTrack group,
            GameObject actor,
            string name)
        {
            Animator animator = actor.GetComponentInChildren<Animator>(true);
            if (animator == null)
                throw new InvalidOperationException(actor.name + " is missing Animator.");
            AnimationTrack track = timeline.CreateTrack<AnimationTrack>(group, name);
            director.SetGenericBinding(track, animator);
        }

        private static void ValidateScene(Scene scene, TimelineAsset timeline)
        {
            List<string> errors = new List<string>();
            RequireCount<PlayableDirector>(scene, 1, errors);
            RequireCount<Cutscene>(scene, 1, errors);
            RequireCount<AudioListener>(scene, 1, errors);
            RequireCount<CutsceneActor>(scene, 2, errors);
            RequireTrackCount<SubtitleTrack>(timeline, 1, errors);
            RequireTrackCount<AudioTrack>(timeline, 2, errors);
            RequireTrackCount<AnimationTrack>(timeline, 2, errors);
            RequireTrackCount<ActivationTrack>(timeline, 3, errors);

            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (CutsceneActor actor in FindComponents<CutsceneActor>(scene))
                if (string.IsNullOrWhiteSpace(actor.Key) || !keys.Add(actor.Key))
                    errors.Add("CutsceneActor key is missing or duplicated: " + actor.Key);

            foreach (MonoBehaviour behaviour in FindComponents<MonoBehaviour>(scene))
            {
                if (behaviour == null)
                    continue;
                string fullName = behaviour.GetType().FullName ?? behaviour.GetType().Name;
                if (fullName.StartsWith("Greenworms.Cinematics.SkullStrike", StringComparison.Ordinal)
                    || fullName.IndexOf("Combat", StringComparison.OrdinalIgnoreCase) >= 0
                    || fullName.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0
                    || fullName.IndexOf("Damage", StringComparison.OrdinalIgnoreCase) >= 0
                    || fullName.IndexOf("Kill", StringComparison.OrdinalIgnoreCase) >= 0)
                    errors.Add("Forbidden combat component in Character Story spike: " + fullName);
            }

            ValidateTimelineEvaluation(scene, errors);

            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static void ValidateTimelineEvaluation(Scene scene, List<string> errors)
        {
            PlayableDirector director = FindComponents<PlayableDirector>(scene)[0];
            TextMeshProUGUI subtitle = FindComponents<TextMeshProUGUI>(scene)[0];
            ValidateAtTime(director, subtitle, 1d, "Speaker A", errors);
            ValidateAtTime(director, subtitle, 4.5d, "Speaker B", errors);
            ValidateAtTime(director, subtitle, 6.5d, "Speaker B", errors);
            director.Stop();
        }

        private static void ValidateAtTime(
            PlayableDirector director,
            TextMeshProUGUI subtitle,
            double time,
            string expectedSubtitle,
            List<string> errors)
        {
            director.time = time;
            director.Evaluate();
            int activeCameraCount = 0;
            foreach (Camera camera in FindComponents<Camera>(director.gameObject.scene))
                if (camera.enabled && camera.gameObject.activeInHierarchy)
                    activeCameraCount++;
            if (activeCameraCount != 1)
                errors.Add("Timeline time " + time + " must have one active camera; found " + activeCameraCount + ".");
            if (subtitle == null || subtitle.text.IndexOf(expectedSubtitle, StringComparison.Ordinal) < 0)
                errors.Add("Timeline time " + time + " did not evaluate subtitle for " + expectedSubtitle + ".");
        }

        private static void RequireCount<T>(Scene scene, int expected, List<string> errors) where T : Component
        {
            int count = FindComponents<T>(scene).Length;
            if (count != expected)
                errors.Add(typeof(T).Name + " count must be " + expected + "; found " + count + ".");
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            List<T> values = new List<T>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                values.AddRange(sceneRoot.GetComponentsInChildren<T>(true));
            return values.ToArray();
        }

        private static void RequireTrackCount<T>(TimelineAsset timeline, int expected, List<string> errors)
            where T : TrackAsset
        {
            int count = 0;
            foreach (TrackAsset track in timeline.GetOutputTracks())
                if (track is T)
                    count++;
            if (count != expected)
                errors.Add(typeof(T).Name + " count must be " + expected + "; found " + count + ".");
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
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
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientIntensity = 1.1f;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void DeleteOwnedAsset(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
        }
    }
}
