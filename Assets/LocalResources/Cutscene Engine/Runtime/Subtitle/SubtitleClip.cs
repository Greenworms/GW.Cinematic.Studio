using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
#if LOCALIZATION
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
#endif

using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    /// <summary>
    /// Timeline clip for displaying subtitle text with various display effects.
    /// Supports localization and custom typing effect parameters.
    /// </summary>
    [Serializable]
    [System.ComponentModel.DisplayName("Subtitle Clip")]
    public class SubtitleClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.Extrapolation;
        /// <summary>
        /// Event called when the subtitle clip starts playing.
        /// </summary>
        public static event Action<SubtitleClip, SubtitleBehaviour, SubtitleText> OnClipEnter;
        
        /// <summary>
        /// Event called when the subtitle clip stops playing.
        /// </summary>
        public static event Action<SubtitleClip, SubtitleBehaviour, SubtitleText> OnClipExit; 
        [Tooltip("If this value is true, the text will not be displayed in the UI.\n" +
                 "However, the SubtitleClip.OnClipEnter event will be called when the clip starts, " +
                 "and the SubtitleClip.OnClipExit event will be called when the clip ends. " +
                 "In other words, you can display the text you want at the timing set by the clip.")]
        public bool isVirtual;
        [Tooltip("The text to be displayed in the UI. This field is ignored if the useLocalizedString is true.")]
        [TextArea]public string text;
#if LOCALIZATION
        public bool useLocalizedString;
        public LocalizedString localizedString;
#endif
        [Tooltip("If SubtitleText uses a TypingEffect, this will override the parameters of that effect once.\n\n" +
                 "You can use it to express the speech of a character who speaks slowly, to add more delay to exclamation marks, etc.\n")]
        public bool overrideTypingEffectParameter;
        public TypingEffectParameter typingEffectParameter;
        
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<SubtitleBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.clip = this;

            return playable;
        }

        /// <summary>
        /// Gets the text to display, handling localization if enabled.
        /// </summary>
        /// <returns>The text string to display.</returns>
        public string GetText()
        {
#if LOCALIZATION
            if (useLocalizedString)
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
                {
                    LocalizationSettings.SelectedLocale = LocalizationSettings.ProjectLocale;
                    return localizedString.GetLocalizedString();
                }
#endif
                var result = localizedString.GetLocalizedString();
                if (result == null) return string.Empty;
                return result;
            }
#endif
            return text ?? string.Empty;
        }

        internal void EnterCallback(SubtitleBehaviour behaviour, SubtitleText subtitle)
        {
            OnClipEnter?.Invoke(this, behaviour, subtitle);
        }
        internal void ExitCallback(SubtitleBehaviour behaviour, SubtitleText subtitle)
        {
            OnClipExit?.Invoke(this, behaviour, subtitle);
        }
    }
}