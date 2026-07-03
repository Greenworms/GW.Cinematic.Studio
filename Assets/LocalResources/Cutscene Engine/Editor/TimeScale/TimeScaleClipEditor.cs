using System;
using System.Collections.Generic;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(TimeScaleClip))]
    public class TimeScaleClipEditor : ClipEditor
    {
        readonly Dictionary<TimeScaleTrack, float> _maxTimeScales = new Dictionary<TimeScaleTrack, float>();
        
        static Dictionary<TimelineClip, AnimationCurve> curves = new Dictionary<TimelineClip, AnimationCurve>();
        static readonly AnimationCurve DefaultMultiplier = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        const int CurveSamples = 20;
        public TimeScaleClipEditor()
        {
            QueryTimeScales();
        }

        public override void OnClipChanged(TimelineClip clip)
        {
            QueryTimeScales();
        }

        void QueryTimeScales()
        { 
            foreach (var track in TimelineEditor.inspectedAsset.GetOutputTracks())
            {
                if(track is not TimeScaleTrack t) continue;
                _maxTimeScales[t] = 0f;
                foreach (var clip in track.GetClips())
                {
                    var c = clip.asset as TimeScaleClip;
                    if (c.timeScale > _maxTimeScales[t])
                    {
                        _maxTimeScales[t] = c.timeScale;
                    }
                }
            }
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var c = clip.asset as TimeScaleClip;
            var track = TimelineEditor.inspectedAsset.GetTrackOf<TimeScaleTrack>(c);
            if (_maxTimeScales == null || !_maxTimeScales.ContainsKey(track)) return;
            
            var hasFadeIn = clip.easeInDuration > 0 || clip.hasBlendIn;
            var hasFadeOut = clip.easeOutDuration > 0 || clip.hasBlendOut;
            
            var keyframes = new List<Keyframe>();
            var sampleStart = 0f;
            var sampleEnd = 1f;
            if (hasFadeIn)
            {
                var fadeData = clip.hasBlendIn ? EditorUtil.CreateBlendInData(clip, track) : EditorUtil.CreateEaseInData(clip);
                var former = fadeData.Former.asset as TimeScaleClip;
                
                var startValue = EvaluateScaledValue(former, fadeData.StartGradientT);
                var endValue = EvaluateScaledValue(c, fadeData.EndGradientT);
                keyframes.Add(new Keyframe(0, startValue, 0, 0, 0.5f, 0.5f));
                keyframes.Add(new Keyframe(fadeData.NormalizedEndX, endValue, 0, 0, 0.5f, 0.5f));
                sampleStart = Mathf.Max(sampleStart, fadeData.NormalizedEndX);
            }
            else
            {
                keyframes.Add(new Keyframe(0, EvaluateScaledValue(c, 0f)));
            }

            if (hasFadeOut)
            {
                var fadeData = clip.hasBlendOut ? EditorUtil.CreateBlendOutData(clip, track) : EditorUtil.CreateEaseOutData(clip);
                var later = fadeData.Later.asset as TimeScaleClip;
                
                if(Math.Abs(clip.mixInDuration - (clip.duration - clip.mixOutDuration)) > 0.001)
                {
                    var startValue = EvaluateScaledValue(c, fadeData.StartGradientT);
                    keyframes.Add(new Keyframe(fadeData.NormalizedStartX, startValue, 0, 0, 0, 0.5f));
                }
                var endValue = EvaluateScaledValue(later, fadeData.EndGradientT);
                keyframes.Add(new Keyframe(1, endValue, 0, 0, 0.5f, 0.5f));
                sampleEnd = Mathf.Min(sampleEnd, fadeData.NormalizedStartX);
            }
            else
            {
                keyframes.Add(new Keyframe(1, EvaluateScaledValue(c, 1f)));
            }

            AddCurveSamples(c, keyframes, sampleStart, sampleEnd);
            var curve = new AnimationCurve();
            curve.keys = keyframes.ToArray();
            curves[clip] = curve;
            

            if(curves.TryGetValue(clip, out var targetCurve))
            {
                var curveRect = EditorUtil.GetAdjustedRect(clip, region);
                curveRect.height -= 5;
                EditorUtil.DrawCurve(curveRect, targetCurve, Color.green, 2, 1, 0, 2);
            }
            
            var labelRect = region.position;
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 9;
            style.alignment = TextAnchor.UpperLeft;
            GUI.Label(labelRect, $"{c.timeScale:N2}", style);
        }

        static float EvaluateScaledValue(TimeScaleClip clipAsset, float normalizedTime)
        {
            if (clipAsset == null) return 0f;
            var multiplier = clipAsset.multiplier ?? DefaultMultiplier;
            return clipAsset.timeScale * multiplier.Evaluate(Mathf.Clamp01(normalizedTime));
        }

        static void AddCurveSamples(TimeScaleClip clipAsset, List<Keyframe> keyframes, float normalizedStart, float normalizedEnd)
        {
            if (clipAsset?.multiplier == null) return;
            if (normalizedEnd - normalizedStart <= Mathf.Epsilon) return;

            for (int i = 1; i < CurveSamples; i++)
            {
                var t = Mathf.Lerp(normalizedStart, normalizedEnd, i / (float)CurveSamples);
                keyframes.Add(new Keyframe(t, EvaluateScaledValue(clipAsset, t)));
            }
        }
        
    }
}
