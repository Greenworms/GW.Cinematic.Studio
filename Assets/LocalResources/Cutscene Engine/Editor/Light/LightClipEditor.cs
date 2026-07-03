using System;
using System.Collections.Generic;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(LightClip))]
    public class LightClipEditor : ClipEditor
    {
        static Dictionary<TimelineClip, AnimationCurve> noiseCurve = new Dictionary<TimelineClip, AnimationCurve>();

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);

            var c = clip.asset as LightClip;
            options.highlightColor = Color.clear;
            
            return options;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var c = clip.asset as LightClip;
            var asset = TimelineEditor.inspectedAsset;
            var track = asset.GetTrackOf<LightTrack>(c);
            
            // 트랙의 위치를 옮길때 null이 될 수 있음.
            if(track == null) return;
            
            if (track.rangeControl != ValueControlOption.None)
            {

                var handleColor = Handles.color;
                Handles.BeginGUI();
                Handles.color = Color.green;
                Handles.DrawWireDisc(region.position.center, Vector3.forward, c.range);
                Handles.color = handleColor;
                Handles.EndGUI();
            }
            
            var hasFadeIn = clip.easeInDuration > 0 || clip.hasBlendIn;
            var hasFadeOut = clip.easeOutDuration > 0 || clip.hasBlendOut;

            var colorRect = EditorUtil.GetAdjustedRect(clip, region);
            colorRect.position = new Vector2(colorRect.x, region.position.height);
            colorRect.height = 5;

            if(track.colorControl != ValueControlOption.None)
            {
                var guiColor = GUI.color;
                GUI.color = c.color;
                GUI.DrawTexture(colorRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
                GUI.color = guiColor;
            }
            
            if (hasFadeIn)
            {
                DrawFade(clip, region, track, true);
            }

            if (hasFadeOut)
            {
                DrawFade(clip, region, track, false);
            }
            
            
            RecalcNoise(clip, track, in region);
            var noiseColor = LightClipInspector.active && LightClipInspector.active.target == clip.asset ? Color.black : Color.gray;
            var rect = EditorUtil.GetAdjustedRect(clip, region);
            rect.height *= 0.9f;
            EditorUtil.DrawCurve(rect, noiseCurve[clip], noiseColor, 1);


            if(track.intensityControl != ValueControlOption.None)
            {
                var labelRect = region.position;
                var style = new GUIStyle(GUI.skin.label);
                style.fontSize = 9;
                var prefix = track.intensityControl switch
                {
                    ValueControlOption.None => "",
                    ValueControlOption.Replace => "R: ",
                    ValueControlOption.Add => "A: ",
                    ValueControlOption.Multiply => "M: ",
                    _ => ""
                };
                GUI.Label(labelRect, $"{prefix}{c.intensity}", style);
            }
        }

        void DrawFade(TimelineClip clip, ClipBackgroundRegion region, LightTrack track, bool fadeIn)
        {
            var fadeData = fadeIn ? EditorUtil.GetFadeInData(clip, track) : EditorUtil.GetFadeOutData(clip, track);
            var blendingRect = EditorUtil.CalculateBlendingRect(clip, region, fadeData.NormalizedStartX, fadeData.NormalizedEndX, true);
            blendingRect.position = new Vector2(blendingRect.x, region.position.height - 1);
            blendingRect.height = 7;

            var formerClip = fadeData.Former.asset as LightClip;
            var startColor = formerClip.color;
            
            var laterClip = fadeData.Later.asset as LightClip;
            var endColor = laterClip.color;
            
            DrawGradient(blendingRect, startColor, endColor);
        }

        static void DrawGradient(Rect rect, Color startColor, Color endColor)
        {
            var gradient = new Gradient
            {
                colorKeys = new []{new GradientColorKey(startColor, 0), new GradientColorKey(endColor, 1)},
                alphaKeys = new []{new GradientAlphaKey(startColor.a, 0), new GradientAlphaKey(endColor.a, 1)},
            };
            
            EditorGUI.GradientField(rect, GUIContent.none, gradient, true, ColorSpace.Gamma);
        }

        static void RecalcNoise(TimelineClip clip, LightTrack track, in ClipBackgroundRegion region)
        {
            var c = clip.asset as LightClip;
            var sample = (int)Mathf.Clamp(Mathf.Abs(c.intensityNoiseSpeed * region.position.width * 0.05f), 128, 255);
            // var sample = 128;
            var keyframes = new Keyframe[sample];
            for (int i = 0; i < keyframes.Length; i++)
            {
                var t = (float)i / keyframes.Length;
                var v = track.intensityControl == ValueControlOption.None ? 1 : c.intensity;
                if(c.useIntensityNoise)
                {
                    var noise = Mathf.Pow(Mathf.PerlinNoise1D(t * (float)clip.duration * c.intensityNoiseSpeed + c.intensityNoiseOffset),
                        c.intensityNoisePower);
                    noise *= c.intensityNoiseStrength;
                    v += noise;
                }

                if (c.useIntensityCurve)
                {
                    if (!c.useIntensityNoise) v = 1;
                    v *= c.intensityCurve.Evaluate(t);
                }
                
                keyframes[i] = new Keyframe(t, v, 0, 0, 0, 0);
            }

            var hasFadeIn = clip.easeInDuration > 0 || clip.hasBlendIn;
            var hasFadeOut = clip.easeOutDuration > 0 || clip.hasBlendOut;
            
            if(hasFadeIn)
            {
                var fadeData = EditorUtil.GetFadeInData(clip, track);
                var otherClip = fadeData.Former;
                var otherC = otherClip.asset as LightClip;
                if (otherC.useIntensityCurve || otherC.useIntensityNoise)
                {
                    // 다른 클립의 곡선이 이미 계산되어 딕셔너리에 있다면 가져와서 사용
                    if (noiseCurve.TryGetValue(otherClip, out var otherCurve))
                    {
                        // 현재 클립의 keyframes 값을 다른 클립의 곡선 값과 선형 보간
                        for (int i = 0; i < keyframes.Length; i++)
                        {
                            var key = keyframes[i];
                            var normalizedTime = key.time;
                            var time = normalizedTime * clip.duration;
                            if (fadeData.NormalizedStartX <= normalizedTime && normalizedTime <= fadeData.NormalizedEndX)
                            {
                                var bt = time; // blending time.
                                var otherT = bt / otherClip.duration; // normalized time in other clip.
                                key.value = Mathf.Lerp(key.value, otherCurve.Evaluate((float)otherT), 1f - (float)(bt / clip.blendInDuration));
                                keyframes[i] = key;
                            }
                        }
                    }
                }
            }

            if (hasFadeOut)
            {
                var fadeData = EditorUtil.GetFadeOutData(clip, track);
                var otherClip = fadeData.Later;
                var otherC = otherClip.asset as LightClip;
                if (otherC.useIntensityCurve || otherC.useIntensityNoise)
                {
                    // 다른 클립의 곡선이 이미 계산되어 딕셔너리에 있다면 가져와서 사용
                    if (noiseCurve.TryGetValue(otherClip, out var otherCurve))
                    {
                        var blendStartTime = clip.duration - clip.blendOutDuration;
                        // 현재 클립의 keyframes 값을 다른 클립의 곡선 값과 선형 보간
                        for (int i = 0; i < keyframes.Length; i++)
                        {
                            var key = keyframes[i];
                            var normalizedTime = key.time;
                            var time = normalizedTime * clip.duration;
                            if (fadeData.NormalizedStartX <= normalizedTime && normalizedTime <= fadeData.NormalizedEndX)
                            {
                                var bt = (time - blendStartTime); // blending time.
                                var otherT = bt / otherClip.duration; // normalized time in other clip.
                                key.value = Mathf.Lerp(key.value, otherCurve.Evaluate((float)otherT), (float)(bt / clip.blendOutDuration));
                                keyframes[i] = key;
                            }
                        }
                    }
                }
            }
            
            
            var curve = new AnimationCurve(keyframes);
            noiseCurve[clip] = curve;
            if(LightClipInspector.active) LightClipInspector.active.NoiseRepaintCallback();
        }
        
        static double GetTimelineTimeFromNormalizedBlend(
            double blendInTime,
            double blendOutTime,
            double normalizedBlendTime)
        {
            // blendInTime 에서 offset 만큼 보정
            return blendInTime + normalizedBlendTime * (blendOutTime - blendInTime);
        }
    }
}