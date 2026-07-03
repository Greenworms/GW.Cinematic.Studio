using System;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(LoopClip))]
    public class LoopClipEditor : ClipEditor
    {
        LoopClip _clip;
        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            _clip = clip.asset as LoopClip;
            if (!_clip) return;
            var director = TimelineEditor.inspectedDirector;
            if (!director) return;
            
            EditorGUI.LabelField(region.position, GetLoopText(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight });
            
            if(_clip.behaviour != null)
            {
                var progress = 0f;
                switch (_clip.escapeMethod)
                {
                    case LoopClip.EscapeMethod.Manual:
                        break;
                    case LoopClip.EscapeMethod.LoopCount:
                        progress = (float)_clip.behaviour.loopCount / _clip.behaviour.targetLoopCount;
                        break;
                    case LoopClip.EscapeMethod.Elapsed:
                        progress = _clip.behaviour.elapsed / _clip.behaviour.minElapseTime;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                    
                var a = _clip.behaviour.ShouldEscape() ? 0.8f : 0.5f;
                EditorUtil.DrawProgressBarInClip(progress, in region, new Color(1,1,1, a));
                
                if (_clip.behaviour.isFinished)
                {
                    var rect = region.position;
                    Handles.color = Color.red;
                    Handles.DrawLine(rect.min, rect.max);
                }
            }
        }

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            _clip = clip.asset as LoopClip;
            var option = base.GetClipOptions(clip);
            option.highlightColor = Color.clear;
            return option;
        }


        string GetLoopText()
        {
            if (!_clip) return "";
            var noBehaviour = _clip.behaviour == null;
            switch (_clip.escapeMethod)
            {
                case LoopClip.EscapeMethod.Manual:
                    return "♾";
                case LoopClip.EscapeMethod.LoopCount:
                    return $"{(noBehaviour ? 0 : _clip.behaviour.loopCount):N0}/{_clip.targetLoopCount:N0}";
                case LoopClip.EscapeMethod.Elapsed:
                    return $"{(noBehaviour ? 0 : _clip.behaviour.elapsed):N1}/{_clip.minElapseTime:N1}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}