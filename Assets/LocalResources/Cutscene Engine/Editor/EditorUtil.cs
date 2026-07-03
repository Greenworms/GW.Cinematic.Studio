using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    internal static class EditorUtil
    {
        const float RectHeightOffset = 5f;
        public static VisualElement Space(float size = 20)
        {
            var ve = new VisualElement
            {
                style =
                {
                    width = size,
                    height = size
                }
            };
            return ve;
        }

        public static void AddSpace(this VisualElement e, float size = 20)
        {
            e.Add(Space(size));
        }

        public static VisualElement Horizontal()
        {
            var ve = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            return ve;
        }

        public static Button IconButton(Texture icon, Action e = null, int width = 22, int height = 22)
        {
            var button = new Button(e)
            {
                style =
                {
                    width = width,
                    height = height,
                    paddingBottom = 2,
                    paddingLeft = 2,
                    paddingRight = 2,
                    paddingTop = 2
                }
            };
            var image = new Image
            {
                image = icon
            };
            button.Add(image);
            return button;
        }
        public static void Indent(this IStyle style, int level)
        {
            style.marginLeft = level * 20;
        }

        public static void SetVisible(this VisualElement ve, bool visible)
        {
            ve.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void BindVisible<T>(this VisualElement ve, BaseField<T> field, T value, bool invert = false)
        {
            field.RegisterValueChangedCallback(evt =>
            {
                var visible = evt.newValue.Equals(value);
                if (invert) visible = !visible;
                ve.SetVisible(visible);
            });

            var visible = field.value.Equals(value);
            if (invert) visible = !visible;
            ve.SetVisible(visible);
        }

        public static void BindVisible<T>(this VisualElement ve, BaseField<T> field, Predicate<T> predicate, bool invert = false)
        {
            field.RegisterValueChangedCallback(evt =>
            {
                var visible = predicate(evt.newValue);
                if (invert) visible = !visible;
                ve.SetVisible(visible);
            });

            var visible = predicate(field.value);
            if (invert) visible = !visible;
            ve.SetVisible(visible);
        }

        public static void BindEnable<T>(this VisualElement ve, BaseField<T> field, T value, bool invert = false)
        {
            field.RegisterValueChangedCallback(evt =>
            {
                var visible = evt.newValue.Equals(value);
                if (invert) visible = !visible;
                ve.SetEnabled(visible);
            });

            var visible = field.value.Equals(value);
            if (invert) visible = !visible;
            ve.SetEnabled(visible);
        }

        public static void BindEnable<T>(this VisualElement ve, BaseField<T> field, Predicate<T> predicate, bool invert = false)
        {
            field.RegisterValueChangedCallback(evt =>
            {
                var visible = predicate(evt.newValue);
                if (invert) visible = !visible;
                ve.SetEnabled(visible);
            });

            var visible = predicate(field.value);
            if (invert) visible = !visible;
            ve.SetEnabled(visible);
        }

        public static void BindEnable(this VisualElement ve, PropertyField field, Predicate<SerializedProperty> predicate, bool invert = false)
        {
            field.RegisterValueChangeCallback(evt =>
            {
                var visible = predicate(evt.changedProperty);
                if (invert) visible = !visible;
                ve.SetEnabled(visible);
            });
        }

        public static void DrawCurve(Rect rect, AnimationCurve curve, Color color, float thickness = 2f, float basis = 0f, float min = 0f, float max = 1f)
        {
            if (curve == null || curve.length == 0)
            {
                return;
            }

            // 기존 Handles 색상 저장
            var originalColor = Handles.color;
            Handles.color = color;

            // Rect 너비에 따라 해상도 결정 (2픽셀당 1개의 점으로 부드럽게 표현)
            int resolution = Mathf.Max(2, Mathf.CeilToInt(rect.width / 4f));
            var points = new Vector3[resolution + 1];

            var useProvidedRange = !Mathf.Approximately(min, max);
            float rangeMin = min;
            float rangeMax = max;

            if (!useProvidedRange)
            {
                rangeMin = float.MaxValue;
                rangeMax = float.MinValue;
                for (int i = 0; i <= resolution; i++)
                {
                    float t = (float)i / resolution;
                    float value = curve.Evaluate(t);
                    if (value < rangeMin) rangeMin = value;
                    if (value > rangeMax) rangeMax = value;
                }
            }

            rangeMin += basis;
            rangeMax += basis;

            // 2. 계산된 최소/최대 값을 사용하여 점들을 rect에 매핑합니다.
            for (int i = 0; i <= resolution; i++)
            {
                // 0.0에서 1.0 사이의 정규화된 시간
                float t = (float)i / resolution;

                // 정규화된 시간을 기반으로 x 좌표 계산
                float x = rect.x + t * rect.width;

                // t에서 커브 값 평가 + basis 오프셋 적용
                float curveValue = curve.Evaluate(t) + basis;
                
                // 커브 값을 rect의 y 좌표로 변환합니다.
                float y = Remap(curveValue, rangeMin, rangeMax, rect.yMax, rect.yMin);

                points[i] = new Vector3(x, y, 0);
            }

            // 계산된 점들을 이용하여 안티에일리어싱된 폴리라인 그리기
            Handles.DrawAAPolyLine(thickness, points);

            // Handles 색상 복원
            Handles.color = originalColor;
        }
        public static void DrawProgressBarInClip(float progress, in ClipBackgroundRegion region, Color color, float height = 5, bool blackBackground = false)
        {
            var rect = region.position;
            rect.height = height;
            rect.position += new Vector2(0, region.position.height - height + 5);
            DrawProgressBar(progress, rect, color, blackBackground);
        }
        public static void DrawProgressBar(float progress, Rect rect, Color color, bool blackBackground = false)
        {
            var background = rect;
            rect.width = progress * rect.width;
            var guiColor = GUI.color;
            GUI.color = color;
            if(blackBackground)GUI.DrawTexture(background, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = guiColor;
        }
        
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax
            , bool useClamp = false)
        {
            if (Mathf.Approximately(fromMax, fromMin)) return Mathf.Lerp(toMin, toMax, 0.5f);
            if (Mathf.Approximately(toMax, toMin)) return toMax;
            var remapped = (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;

            if (useClamp)
            {
                return Mathf.Clamp(remapped, Mathf.Min(toMin, toMax), Mathf.Max(toMin, toMax));
            }
            return remapped;
        }
        
        
        
        
        
        
        
        public static Rect GetAdjustedRect(TimelineClip clip, ClipBackgroundRegion region)
        {
            var rect = region.position;
            rect.height += RectHeightOffset;

            var clippedDuration = region.endTime - region.startTime;
            var originalWidth = region.position.width * (clip.duration / clippedDuration);
            rect.width = (float)originalWidth;
            
            var multiplier = originalWidth / clip.duration;
            if (region.startTime > 0)
            {
                rect.x -= (float)(region.startTime * multiplier);
            }
            
            return rect;
        }


        public static FadeData GetFadeInData(TimelineClip clip, TrackAsset track)
        {
            if (clip.hasBlendIn)
            {
                return CreateBlendInData(clip, track);
            }
            if (clip.easeInDuration > 0)
            {
                return CreateEaseInData(clip);
            }
            throw new InvalidOperationException("No fade in data available");
        }

        public static FadeData GetFadeOutData(TimelineClip clip, TrackAsset track)
        {
            if (clip.hasBlendOut)
            {
                return CreateBlendOutData(clip, track);
            }
            if (clip.easeOutDuration > 0)
            {
                return CreateEaseOutData(clip);
            }
            throw new InvalidOperationException("No fade out data available");
        }

        public static FadeData CreateEaseInData(TimelineClip clip)
        {
            var endGradientT = (float)(clip.easeInDuration / clip.duration);
            return new FadeData
            {
                Former = clip,
                Later = clip,
                StartGradientT = 0f,
                EndGradientT = endGradientT,
                NormalizedStartX = 0f,
                NormalizedEndX = endGradientT,
            };
        }

        public static FadeData CreateBlendInData(TimelineClip clip, TrackAsset track)
        {
            var formerClip = FindPreviousClip(clip, track);
            var startGradientT = (float)((formerClip.duration - formerClip.blendOutDuration) / formerClip.duration);
            var endGradientT = (float)(clip.blendInDuration / clip.duration);

            return new FadeData
            {
                Former = formerClip,
                Later = clip,
                StartGradientT = startGradientT,
                EndGradientT = endGradientT,
                NormalizedStartX = 0f,
                NormalizedEndX = endGradientT,
            };
        }

        public static FadeData CreateEaseOutData(TimelineClip clip)
        {
            var startGradientT = (float)((clip.duration - clip.easeOutDuration) / clip.duration);
            
            return new FadeData
            {
                Former = clip,
                Later = clip,
                StartGradientT = startGradientT,
                EndGradientT = 1f,
                NormalizedStartX = startGradientT,
                NormalizedEndX = 1f,
            };
        }

        public static FadeData CreateBlendOutData(TimelineClip clip, TrackAsset track)
        {
            var laterClip = FindNextClip(clip, track);
            var startGradientT = (float)((clip.duration - clip.blendOutDuration) / clip.duration);
            var endGradientT = (float)(laterClip.blendInDuration / laterClip.duration);

            return new FadeData
            {
                Former = clip,
                Later = laterClip,
                StartGradientT = startGradientT,
                EndGradientT = endGradientT,
                NormalizedStartX = startGradientT,
                NormalizedEndX = 1f,
            };
        }
        public static TimelineClip FindPreviousClip(TimelineClip clip, TrackAsset track)
        {
            if (clip == null || track == null) return null;
            foreach (var c in track.GetClips().OrderByDescending(x => x.end))
            {
                if (c != clip && c.start <= clip.start) return c;
            }

            return null;
        }

        public static TimelineClip FindNextClip(TimelineClip clip, TrackAsset track)
        {
            if (clip == null || track == null) return null;
            return track.GetClips().FirstOrDefault(c => c != clip && c.start >= clip.start && c.start <= clip.end);
        }

        public static Rect CalculateBlendingRect(TimelineClip clip, ClipBackgroundRegion region, float normalizedStartX, float normalizedEndX, bool isOut = false)
        {
            var rect = GetAdjustedRect(clip, region);
            
            var originalWidth = region.position.width * (clip.duration / (region.endTime - region.startTime));
            rect.width = (float)(originalWidth * (normalizedEndX - normalizedStartX));

            
            if (isOut)
            {
                rect.x = (float)(originalWidth * normalizedStartX);
            }
            
            var blendingStartTime = clip.duration * normalizedStartX;
            var blendingEndTime = clip.duration * normalizedEndX;
            
            var multiplier = (clip.duration / (region.endTime - region.startTime));
            
            if (region.endTime < blendingEndTime)
            {
                var diff = blendingEndTime - region.endTime;
                rect.x += (float)(diff * multiplier);
            }

            if (region.startTime > blendingStartTime)
            {
                rect.width += (float)(region.startTime * multiplier);
                rect.x -= (float)(region.startTime * multiplier);
            }


            
            return rect;
        }

    }
    
    internal struct FadeData
    {
        public TimelineClip Former;
        public TimelineClip Later;
        public float StartGradientT;
        public float EndGradientT;
        public float NormalizedStartX;
        public float NormalizedEndX;
    }
}
