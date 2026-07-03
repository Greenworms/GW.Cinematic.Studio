using System;
using System.Collections.Generic;
using System.Linq;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(ColorClip))]
    public class ColorClipEditor : ClipEditor
    {
        const int MaxGradientKeyframes = 5;
        static readonly Color FadeTriangleColor = new Color(0.1667f, 0.1667f, 0.1667f, 1f);
        const float RectHeightOffset = 5f;

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            options.highlightColor = Color.clear;
            return options;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var colorClip = clip.asset as ColorClip;
            var track = GetColorTrack(colorClip);

            DrawBaseGradient(clip, region, colorClip);
            DrawFadeEffects(clip, region, track);
        }

        ColorTrack GetColorTrack(ColorClip colorClip)
        {
            var asset = TimelineEditor.inspectedAsset;
            return asset.GetTrackOf<ColorTrack>(colorClip);
        }

        void DrawBaseGradient(TimelineClip clip, ClipBackgroundRegion region, ColorClip colorClip)
        {
            var rect = EditorUtil.GetAdjustedRect(clip, region);
            var originalColor = GUI.color;
            GUI.color = Color.clear;
            
            try
            {
                switch (colorClip.colorType)
                {
                    case ColorType.Default:
                        var gradient = new Gradient
                        {
                            colorKeys = new[] { new GradientColorKey(colorClip.color, 0) },
                            alphaKeys = new[] { new GradientAlphaKey(colorClip.color.a, 0) }
                        };
                        EditorGUI.GradientField(rect, GUIContent.none, gradient, true, ColorSpace.Gamma);
                        break;
                    case ColorType.Gradient:
                        EditorGUI.GradientField(rect, GUIContent.none, colorClip.gradient, true, ColorSpace.Gamma);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                GUI.color = originalColor;
            }
        }
        
        void DrawFadeEffects(TimelineClip clip, ClipBackgroundRegion region, ColorTrack track)
        {
            var hasFadeIn = clip.easeInDuration > 0 || clip.hasBlendIn;
            var hasFadeOut = clip.easeOutDuration > 0 || clip.hasBlendOut;

            if (hasFadeIn)
            {
                DrawFadeIn(clip, region, track);
            }

            if (hasFadeOut)
            {
                DrawFadeOut(clip, region, track);
            }
        }

        void DrawFadeIn(TimelineClip clip, ClipBackgroundRegion region, ColorTrack track)
        {
            var fadeData = EditorUtil.GetFadeInData(clip, track);
            var blendingRect = EditorUtil.CalculateBlendingRect(clip, region, fadeData.NormalizedStartX, fadeData.NormalizedEndX);
            var additionalSamples = CollectAdditionalSamples(fadeData, clip);

            var formerClip = fadeData.Former.asset as ColorClip;
            var startColor = formerClip.Evaluate(fadeData.StartGradientT);
            
            var laterClip = fadeData.Later.asset as ColorClip;
            var endColor = laterClip.Evaluate(fadeData.EndGradientT);
            
            DrawGradient(blendingRect, startColor, endColor, additionalSamples.ToArray());

            if (!clip.hasBlendIn && clip.easeInDuration > 0)
            {
                DrawEaseInTriangle(blendingRect);
            }
        }

        void DrawFadeOut(TimelineClip clip, ClipBackgroundRegion region, ColorTrack track)
        {
            var fadeData = EditorUtil.GetFadeOutData(clip, track);
            var blendingRect = EditorUtil.CalculateBlendingRect(clip, region, fadeData.NormalizedStartX, fadeData.NormalizedEndX, true);
            var additionalSamples = CollectAdditionalSamples(fadeData, clip);

            var formerClip = fadeData.Former.asset as ColorClip;
            var startColor = formerClip.Evaluate(fadeData.StartGradientT);
            
            var laterClip = fadeData.Later.asset as ColorClip;
            var endColor = laterClip.Evaluate(fadeData.EndGradientT);
            
            DrawGradient(blendingRect, startColor, endColor, additionalSamples.ToArray());
            if(!clip.hasBlendOut && clip.easeOutDuration > 0)
            {
                DrawEaseOutTriangle(blendingRect);
            }
        }
        
        List<(float time, Color color)> CollectAdditionalSamples(CutsceneEngineEditor.FadeData fadeData, TimelineClip clip)
        {
            var samples = new List<(float time, Color color)>();
            
            CollectGradientSamples(samples, fadeData.Former, fadeData.StartGradientT, fadeData.EndGradientT, true);
            CollectGradientSamples(samples, fadeData.Later, fadeData.StartGradientT, fadeData.EndGradientT, false);
            
            return ProcessSamples(samples, fadeData);
        }

        void CollectGradientSamples(List<(float time, Color color)> samples, TimelineClip timelineClip, 
            float startT, float endT, bool isFormer)
        {
            var clip = timelineClip.asset as ColorClip;
            if (clip.colorType == ColorType.Default) return;

            var gradient = clip.gradient;
            CollectKeyframeSamples(samples, gradient.colorKeys, timelineClip, startT, endT, isFormer);
            CollectKeyframeSamples(samples, gradient.alphaKeys, timelineClip, startT, endT, isFormer);
        }

        void CollectKeyframeSamples<T>(List<(float time, Color color)> samples, T[] keys, TimelineClip timelineClip, 
            float startT, float endT, bool isFormer) where T : struct
        {
            foreach (var key in keys)
            {
                var time = GetKeyTime(key);
                if (ShouldSkipKey(time, startT, endT, isFormer)) continue;
                
                var timelineTime = timelineClip.GetTimelineTime(time);
                samples.Add(((float)timelineTime, Color.clear));
            }
        }

        float GetKeyTime<T>(T key) where T : struct
        {
            return key switch
            {
                GradientColorKey colorKey => colorKey.time,
                GradientAlphaKey alphaKey => alphaKey.time,
                _ => 0f
            };
        }

        bool ShouldSkipKey(float time, float startT, float endT, bool isFormer)
        {
            if (time == 0f || time == 1f) return true;
            return isFormer ? time < startT : time > endT;
        }

        List<(float time, Color color)> ProcessSamples(List<(float time, Color color)> samples, CutsceneEngineEditor.FadeData fadeData)
        {
            for (int i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                var formerT = (float)fadeData.Former.GetNormalizedTime(sample.time);
                var laterT = (float)fadeData.Later.GetNormalizedTime(sample.time);

                var formerClip = fadeData.Former.asset as ColorClip;
                var laterClip = fadeData.Later.asset as ColorClip;
                
                var c0 = formerClip.Evaluate(formerT);
                var c1 = laterClip.Evaluate(laterT);
                
                var normalizedTime = CutsceneEngineUtility.GetNormalizedTime(sample.time, 
                    fadeData.Later.start, fadeData.Later.start + fadeData.Later.blendInDuration);
                
                samples[i] = ((float)normalizedTime, Color.Lerp(c0, c1, (float)normalizedTime));
            }
            return samples;
        }

        static void DrawEaseInTriangle(Rect rect)
        {
            var originalColor = Handles.color;
            Handles.color = FadeTriangleColor;
            
            try
            {
                var topLeft = new Vector3(0, rect.height);
                var bottomRight = new Vector3(rect.width, 0);
                
                Handles.DrawLine(topLeft, bottomRight);
                Handles.DrawAAConvexPolygon(Vector3.zero, topLeft, bottomRight);
            }
            finally
            {
                Handles.color = originalColor;
            }
        }

        static void DrawEaseOutTriangle(Rect rect)
        {
            var originalColor = Handles.color;
            Handles.color = FadeTriangleColor;
            
            try
            {
                Handles.DrawLine(rect.position, rect.max);
                
                var topLeft = new Vector3(rect.x, 0);
                var bottomRight = new Vector3(rect.xMax, rect.height);
                var topRight = new Vector3(rect.xMax, 0);
                    
                Handles.DrawAAConvexPolygon(topLeft, bottomRight, topRight);
            }
            finally
            {
                Handles.color = originalColor;
            }
        }

        static void DrawGradient(Rect rect, Color startColor, Color endColor, params (float time, Color color)[] otherColors)
        {
            var colorKeys = new List<GradientColorKey> { new(startColor, 0) };
            var alphaKeys = new List<GradientAlphaKey> { new(startColor.a, 0) };
            
            if (otherColors != null)
            {
                var validColors = otherColors.Take(MaxGradientKeyframes);
                foreach (var (time, color) in validColors)
                {
                    colorKeys.Add(new GradientColorKey(color, time));
                    alphaKeys.Add(new GradientAlphaKey(color.a, time));
                }
            }
            
            colorKeys.Add(new GradientColorKey(endColor, 1));
            alphaKeys.Add(new GradientAlphaKey(endColor.a, 1));
            
            var gradient = new Gradient
            {
                colorKeys = colorKeys.ToArray(),
                alphaKeys = alphaKeys.ToArray()
            };
            
            EditorGUI.GradientField(rect, GUIContent.none, gradient, true, ColorSpace.Gamma);
        }

    }
}