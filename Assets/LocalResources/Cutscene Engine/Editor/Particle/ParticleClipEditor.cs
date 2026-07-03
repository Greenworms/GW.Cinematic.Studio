using CutsceneEngine;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(ParticleClip))]
    public class ParticleClipEditor : ClipEditor
    {
        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            
            var c = clip.asset as ParticleClip;
            options.highlightColor = c.controlChildren ? Color.gray :  Color.clear;
            
            return options;
        }
        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var c = clip.asset as ParticleClip;
            
            
            if (c.connectName)
            {
                var director = TimelineEditor.inspectedDirector;
                if(director)
                {
                    var ps = c.particleSystem.Resolve(director);
                    clip.displayName = ps ? ps.name : "(Missing)";
                }
            }

            var guiColor = GUI.color;
            GUI.color = Color.gray;
            var startRect = region.position;
            startRect.height += 5;
            startRect.width = 5;
            GUI.DrawTexture(startRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);

            if (c.stopOnEnd)
            {
                var endRect = region.position;
                endRect.height += 5;
                endRect.width = 5;
                endRect.position += new Vector2(region.position.width - endRect.width, 0);
                GUI.DrawTexture(endRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            }

            GUI.color = guiColor;
        }
    }
}