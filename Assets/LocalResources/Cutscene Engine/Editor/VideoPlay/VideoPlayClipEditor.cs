using System;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(VideoPlayClip))]
    public class VideoPlayClipEditor : ClipEditor
    {
        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var option = base.GetClipOptions(clip);

            var c = clip.asset as VideoPlayClip;
            switch (c.audioOutputTarget)
            {
                case VideoAudioOutputTarget.Mute:
                    option.highlightColor = Color.clear;
                    break;
                case VideoAudioOutputTarget.Direct:
                    break;
                case VideoAudioOutputTarget.AudioSource:
                    option.highlightColor = new Color(1, 0.8f, 0f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return option;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var c = clip.asset as VideoPlayClip;
            if (c && c.video)
            {
                var texturePreview = AssetPreview.GetMiniThumbnail(c.video);
                
                if (texturePreview)
                {
                    var rect = region.position;
                    rect.height += 20;
                    rect.position -= new Vector2(0, 10);
                    rect.width = texturePreview.width * rect.height / texturePreview.height;
                    GUI.DrawTexture(rect, texturePreview, ScaleMode.ScaleToFit);
                }
            }
        }
    }
}