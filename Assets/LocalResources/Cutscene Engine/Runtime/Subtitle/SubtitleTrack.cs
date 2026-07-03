using System;
using UnityEngine;
#if TMP
using TMPro;
#endif
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace CutsceneEngine
{
    /// <summary>
    /// Timeline track for managing subtitle clips and text display during cutscenes.
    /// Binds to SubtitleText components to control text display with various effects.
    /// </summary>
    [TrackColor(0.5f, 0.9f, 0.6f)]
    [TrackBindingType(typeof(SubtitleText))]
    [TrackClipType(typeof(SubtitleClip))]
    public class SubtitleTrack : TrackAsset
    {
        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var director = gameObject.GetComponent<PlayableDirector>();
            var subtitleText = director.GetGenericBinding(this) as SubtitleText;
            if(subtitleText) subtitleText.SetRawText(string.Empty);
            return base.CreatePlayable(graph, gameObject, clip);
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var binding = director.GetGenericBinding(this) as SubtitleText;
            
            if(!binding) return;
            switch (binding.textType)
            {
                case SubtitleTextType.None:
                    break;
                case SubtitleTextType.Legacy:
                    if(binding.text_legacy) driver.AddFromName<Text>(binding.gameObject, "m_Text");
                    break;
                case SubtitleTextType.TextMesh:
                    if(binding.textMesh) driver.AddFromName<TextMesh>(binding.gameObject, "m_Text");
                    break;
                case SubtitleTextType.UIElements:
                    // Cannot add properties.
                    break;
                
                case SubtitleTextType.TMP:
#if TMP
                    if(binding.text_tmp) driver.AddFromName<TMP_Text>(binding.gameObject, "m_text");
#endif
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}