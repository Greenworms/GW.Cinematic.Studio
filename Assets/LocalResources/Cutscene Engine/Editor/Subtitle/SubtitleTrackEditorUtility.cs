using System.Collections.Generic;
using System.Linq;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
#if LOCALIZATION
using UnityEngine.Localization;
using UnityEditor.Localization;
#endif

namespace CutsceneEngineEditor
{
    /// <summary>
    /// Provides utility methods for manipulating SubtitleTracks in the editor.
    /// </summary>
    public static class SubtitleTrackEditorUtility
    {
        const float DefaultTimePerChar = 0.2f;
        const float DefaultMinDuration = 2f;
        const float DefaultSilence = 1f;

        /// <summary>
        /// Replaces all clips on a track with new clips generated from an array of strings.
        /// Duration is calculated automatically based on text length.
        /// </summary>
        public static void SetCustomDialogues(SubtitleTrack track, IEnumerable<string> dialogues)
        {
            var customDialogues = dialogues.Select(d => new CustomDialogue(d, Mathf.Max(d.Length * DefaultTimePerChar, DefaultMinDuration), DefaultSilence));
            SetCustomDialoguesInternal(track, customDialogues);
        }

#if LOCALIZATION
        /// <summary>
        /// Replaces all clips on a track with new clips generated from an array of LocalizedStrings.
        /// Duration is calculated automatically based on text length.
        /// </summary>
        public static void SetCustomDialogues(SubtitleTrack track, IEnumerable<LocalizedString> dialogues)
        {
            var customDialogues = dialogues.Select(d => new CustomDialogue(d, Mathf.Max(d.GetLocalizedString().Length * DefaultTimePerChar, DefaultMinDuration), DefaultSilence));
            SetCustomDialoguesInternal(track, customDialogues);
        }
#endif

        /// <summary>
        /// Replaces all clips on a track with new clips generated from an array of CustomDialogue objects.
        /// </summary>
        public static void SetCustomDialogues(SubtitleTrack track, IEnumerable<CustomDialogue> dialogues)
        {
            SetCustomDialoguesInternal(track, dialogues);
        }

        static void SetCustomDialoguesInternal(SubtitleTrack track, IEnumerable<CustomDialogue> dialogues)
        {
            ClearTrack(track);

            double startTime = 0;
            foreach (var dialogue in dialogues)
            {
                var clip = CreateSubtitleClip(track, dialogue, startTime);
                startTime = clip.end + dialogue.silenceAfterEnd;
            }

            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }

        static void ClearTrack(SubtitleTrack track)
        {
            Undo.RecordObject(track, "Clear Subtitle Track");
            var clips = track.GetClips().ToArray();
            foreach (var clip in clips)
            {
                track.DeleteClip(clip);
            }
        }

        static TimelineClip CreateSubtitleClip(SubtitleTrack track, CustomDialogue dialogue, double startTime)
        {
            var initialOffset = TimelineEditor.inspectedDirector.time;
            var clip = track.CreateClip<SubtitleClip>();
            var subtitleClipAsset = clip.asset as SubtitleClip;

            subtitleClipAsset.text = dialogue.text;
            clip.start = initialOffset + startTime;
            clip.duration = dialogue.duration;

#if LOCALIZATION
            subtitleClipAsset.useLocalizedString = dialogue.useLocalizedString;
            subtitleClipAsset.localizedString = dialogue.localizedString;
            clip.displayName = dialogue.useLocalizedString 
                ? GetDisplayNameForLocalizedString(dialogue.localizedString) 
                : dialogue.text;
#else
            clip.displayName = dialogue.text;
#endif

            return clip;
        }

#if LOCALIZATION
        static string GetDisplayNameForLocalizedString(LocalizedString localizedString)
        {
            if (localizedString == null) return nameof(SubtitleClip);

            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(localizedString.TableReference);
            if (tableCollection == null) return nameof(SubtitleClip);

            var entry = tableCollection.SharedData.GetEntry(localizedString.TableEntryReference.KeyId);
            return (entry == null || string.IsNullOrWhiteSpace(entry.Key)) ? nameof(SubtitleClip) : entry.Key;
        }
#endif
    }

    /// <summary>
    /// Represents a dialogue unit for creating subtitle clips.
    /// </summary>
    public class CustomDialogue
    {
        public string text;
#if LOCALIZATION
        public bool useLocalizedString;
        public LocalizedString localizedString;
#endif
        
        public float duration;
        public float silenceAfterEnd;


        public CustomDialogue(string text, float duration = 2f, float silenceAfterEnd = 1f)
        {
            this.text = text;
            this.duration = duration;
            this.silenceAfterEnd = silenceAfterEnd;
        }

        public CustomDialogue(string text, float timePerChar, float minDuration, float silenceAfterEnd = 1f)
        {
            this.text = text;
            this.duration = Mathf.Max(text.Length * timePerChar, minDuration);
            this.silenceAfterEnd = silenceAfterEnd;
        }

#if LOCALIZATION
        public CustomDialogue(LocalizedString localizedString, float duration = 2f, float silenceAfterEnd = 1f)
        {
            this.useLocalizedString = true;
            this.localizedString = localizedString;
            this.text = localizedString.GetLocalizedString();
            this.duration = duration;
            this.silenceAfterEnd = silenceAfterEnd;
        }

        public CustomDialogue(LocalizedString localizedString, float timePerChar, float minDuration, float silenceAfterEnd = 1f)
        {
            this.useLocalizedString = true;
            this.localizedString = localizedString;
            var localizedText = localizedString.GetLocalizedString();
            this.text = localizedText;
            this.duration = Mathf.Max(localizedText.Length * timePerChar, minDuration);
            this.silenceAfterEnd = silenceAfterEnd;
        }
#endif
    }
}