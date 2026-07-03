#if URP
using System;
using System.Collections.Generic;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(Light2DClip))]
    public class Light2DClipEditor : ClipEditor
    {
        static Dictionary<TimelineClip, AnimationCurve> noiseCurve = new Dictionary<TimelineClip, AnimationCurve>();

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            options.highlightColor = Color.clear;
            return options;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var c = clip.asset as Light2DClip;
            var asset = TimelineEditor.inspectedAsset;
            var track = asset.GetTrackOf<Light2DTrack>(c);
            
            if(track == null) return;
            
            if (track.pointLightOuterRadiusControl != ValueControlOption.None)
            {
                var handleColor = Handles.color;
                Handles.BeginGUI();
                Handles.color = Color.green;
                Handles.DrawWireDisc(region.position.center, Vector3.forward, c.pointLightOuterRadius);
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
            var noiseColor = Light2DClipInspector.active && Light2DClipInspector.active.target == clip.asset ? Color.black : Color.gray;
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

        void DrawFade(TimelineClip clip, ClipBackgroundRegion region, Light2DTrack track, bool fadeIn)
        {
            var fadeData = fadeIn ? EditorUtil.GetFadeInData(clip, track) : EditorUtil.GetFadeOutData(clip, track);
            var blendingRect = EditorUtil.CalculateBlendingRect(clip, region, fadeData.NormalizedStartX, fadeData.NormalizedEndX, true);
            blendingRect.position = new Vector2(blendingRect.x, region.position.height - 1);
            blendingRect.height = 7;

            var formerClip = fadeData.Former.asset as Light2DClip;
            var startColor = formerClip.color;
            
            var laterClip = fadeData.Later.asset as Light2DClip;
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

        static void RecalcNoise(TimelineClip clip, Light2DTrack track, in ClipBackgroundRegion region)
        {
            var c = clip.asset as Light2DClip;
            var sample = (int)Mathf.Clamp(Mathf.Abs(c.intensityNoiseSpeed * region.position.width * 0.05f), 128, 255);
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
                var otherC = otherClip.asset as Light2DClip;
                if (otherC.useIntensityCurve || otherC.useIntensityNoise)
                {
                    if (noiseCurve.TryGetValue(otherClip, out var otherCurve))
                    {
                        for (int i = 0; i < keyframes.Length; i++)
                        {
                            var key = keyframes[i];
                            var normalizedTime = key.time;
                            var time = normalizedTime * clip.duration;
                            if (fadeData.NormalizedStartX <= normalizedTime && normalizedTime <= fadeData.NormalizedEndX)
                            {
                                var bt = time;
                                var otherT = bt / otherClip.duration;
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
                var otherC = otherClip.asset as Light2DClip;
                if (otherC.useIntensityCurve || otherC.useIntensityNoise)
                {
                    if (noiseCurve.TryGetValue(otherClip, out var otherCurve))
                    {
                        var blendStartTime = clip.duration - clip.blendOutDuration;
                        for (int i = 0; i < keyframes.Length; i++)
                        {
                            var key = keyframes[i];
                            var normalizedTime = key.time;
                            var time = normalizedTime * clip.duration;
                            if (fadeData.NormalizedStartX <= normalizedTime && normalizedTime <= fadeData.NormalizedEndX)
                            {
                                var bt = (time - blendStartTime);
                                var otherT = bt / otherClip.duration;
                                key.value = Mathf.Lerp(key.value, otherCurve.Evaluate((float)otherT), (float)(bt / clip.blendOutDuration));
                                keyframes[i] = key;
                            }
                        }
                    }
                }
            }
            
            var curve = new AnimationCurve(keyframes);
            noiseCurve[clip] = curve;
            if(Light2DClipInspector.active) Light2DClipInspector.active.NoiseRepaintCallback();
        }
    }
}
#endif