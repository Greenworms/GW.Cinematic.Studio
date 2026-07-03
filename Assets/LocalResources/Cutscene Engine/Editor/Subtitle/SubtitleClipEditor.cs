using System;
using System.Text;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(SubtitleClip))]
    public class SubtitleClipEditor : ClipEditor
    {
        public override void OnClipChanged(TimelineClip clip)
        {
            var c = clip.asset as SubtitleClip;

            var director = TimelineEditor.inspectedDirector;
            var track = TimelineEditor.inspectedAsset.GetTrackOf<SubtitleTrack>(c);
            var subtitle = director ? director.GetGenericBinding(track) as SubtitleText : null;

            if (subtitle)
            {
                var duration = 0f;
                switch (subtitle.textDisplayEffect)
                {
                    case TextDisplayEffect.None:
                        break;
                    case TextDisplayEffect.Fade:
                        duration += subtitle.subtitleFadeParameter.fadeInTime;
                        duration += subtitle.subtitleFadeParameter.fadeOutTime;
                        break;
                    case TextDisplayEffect.Typing:
                        var param = c.overrideTypingEffectParameter ? 
                            c.typingEffectParameter : subtitle.typingEffectParameter;
                        duration += TypingEffect.CalcDuration(c.GetText().Normalize(NormalizationForm.FormD), param);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                if (clip.duration < duration)
                {
                    clip.duration = duration;
                }
            }
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var asset = TimelineEditor.inspectedAsset;
            var director = TimelineEditor.inspectedDirector;
            SubtitleText subtitleText = null;
            foreach (var track in asset.GetOutputTracks())
            {
                if(track is not SubtitleTrack) continue;
                foreach (var c in track.GetClips())
                {
                    if (director && c == clip)
                    {
                        
                        subtitleText = director.GetGenericBinding(track) as SubtitleText;
                        break;
                    }
                }
                if(subtitleText) break;
            }
            DrawFadeDurationGUI(clip, subtitleText, in region);
        }

        void DrawFadeDurationGUI(TimelineClip clip, SubtitleText subtitle, in ClipBackgroundRegion region)
        {
            if (!subtitle) return;

            var rect = EditorUtil.GetAdjustedRect(clip, region);
            var duration = clip.duration;
            switch (subtitle.textDisplayEffect)
            {
                case TextDisplayEffect.None:
                    break;
                case TextDisplayEffect.Fade:
                {
                    var fadeInTime = subtitle.subtitleFadeParameter.fadeInTime;
                    var fadeInWidth = rect.width * (fadeInTime / duration);
                    var fadeInRect = rect;
                    fadeInRect.width = (float)fadeInWidth;
                    GUI.Box(fadeInRect, "");
                    
                    var fadeOutTime = subtitle.subtitleFadeParameter.fadeOutTime;
                    var fadeOutWidth = rect.width * (fadeOutTime / duration);
                    var fadeOutRect = rect;
                    var fadeOutRectPos = fadeOutRect.position;
                    fadeOutRectPos.x = (float)(rect.width * ((duration - fadeOutTime) / duration));
                    fadeOutRect.position = fadeOutRectPos;
                    
                    fadeOutRect.width = (float)fadeOutWidth;
                    
                    GUI.Box(fadeOutRect, "");
                    
                    
                    if(fadeInTime + fadeOutTime >= duration)
                    {
                        var seperator = fadeInRect;
                        seperator.width = 1;
                        seperator.center = new Vector2(fadeInRect.max.x, fadeInRect.center.y);
                        var guiColor = GUI.color;
                        GUI.backgroundColor = Color.white;
                        GUI.Box(seperator, "");
                        GUI.backgroundColor = guiColor;
                    }
                    break;
                }
                case TextDisplayEffect.Typing:
                {
                    var subtitleClip = (SubtitleClip)clip.asset;
                    var param = subtitleClip.overrideTypingEffectParameter ? 
                        subtitleClip.typingEffectParameter : subtitle.typingEffectParameter;
                    var normalizedText = subtitleClip.GetText().Normalize(NormalizationForm.FormD);
                    var fadeInTime =
                        param.timeMethod == TypingEffectParameter.TimeMethod.PerChar ?
                        param.timePerChar * normalizedText.Length : 
                        param.totalDuration;
                    
                    var timePerChar = param.timeMethod == TypingEffectParameter.TimeMethod.PerChar ?
                        param.timePerChar : param.totalDuration / normalizedText.Length;
                    

                    foreach (var c in normalizedText)
                    {
                        if (param.HasAdditionalCharSetting(c, out var time))
                            fadeInTime += time - timePerChar;
                    }
                    
                    
                    var fadeInWidth = rect.width * (fadeInTime / duration);
                    var fadeInRect = rect;
                    fadeInRect.width = (float)fadeInWidth;
                    GUI.Box(fadeInRect, "");
                    
                    var fadeOutTime = param.fadeOutTime;
                    var fadeOutWidth = rect.width * (fadeOutTime / duration);
                    var fadeOutRect = rect;
                    var fadeOutRectPos = fadeOutRect.position;
                    fadeOutRectPos.x = (float)(rect.width * ((duration - fadeOutTime) / duration));
                    fadeOutRect.position = fadeOutRectPos;
                    
                    fadeOutRect.width = (float)fadeOutWidth;
                    
                    GUI.Box(fadeOutRect, "");
                    
                    
                    if(fadeInTime + fadeOutTime >= duration)
                    {
                        var seperator = fadeInRect;
                        seperator.width = 1;
                        seperator.center = new Vector2(fadeInRect.max.x, fadeInRect.center.y);
                        var guiColor = GUI.color;
                        GUI.backgroundColor = Color.white;
                        GUI.Box(seperator, "");
                        GUI.backgroundColor = guiColor;
                    }
                    
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}