#if TMP
using TMPro;
#endif

using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace CutsceneEngine
{
    /// <summary>
    /// Component for displaying subtitles with various text display effects.
    /// Supports Legacy Text, Legacy TextMesh, TextMesh Pro (TMP), and UI Elements.
    /// </summary>
    [AddComponentMenu("Cutscene Engine/Subtitle Text (Cutscene Engine)")]
    public class SubtitleText : MonoBehaviour
    {
        /// <summary>
        /// Gets the type of text component being used.
        /// </summary>
        public SubtitleTextType textType => _textType;

        [Tooltip("Whether to ignore the time scale." +
                 "If this value is true, fade effects and Typing effects will ignore Time.timeScale and proceed according to real time.")]
        public bool ignoreTimeScale;
        
        [Tooltip("If this value is true, the game object will be disabled when an empty string should be displayed.")]
        public bool deactivateIfEmpty;

        public string prefix;
        
        [Tooltip("Determines the visual effect to use when displaying subtitles.")]
        public TextDisplayEffect textDisplayEffect;
        
        /// <summary>
        /// This parameter is used when textDisplayEffect is Fade.
        /// </summary>
        public SubtitleFadeParameter subtitleFadeParameter;
        
        /// <summary>
        /// Parameter used when textDisplayEffect is Typing.
        /// </summary>
        public TypingEffectParameter typingEffectParameter;
        
        [Tooltip("A Text component for the legacy UI. Automatically queries the component on Start().")]
        public Text text_legacy;
        
        [Tooltip("This is a legacy TextMesh component. It automatically queries the component on Start().")]
        public TextMesh textMesh;
#if TMP
        [Tooltip("The base text component for TextMesh Pro (TMP). The component is automatically queried in Start().")]
        public TMP_Text text_tmp;
#endif
        [Tooltip("The UIDocument component for using UIElements at runtime. The component is queried in Start().")]
        public UIDocument uiDocument;
        
        [Tooltip("The name or path of the Label element to search for in the UIDocument. The path uses '/' as a separator.")]
        public string elementName = "Subtitle";
        
        [Tooltip("This is a Label queried by searching for elementName in uiDocument.")]
        public Label text_uielement;

        [Tooltip("Called whenever the string drawn in SubtitleText changes. If using the Typing Effect, it is called every time a character is typed.")]
        public UnityEvent<string> onChanged;

        Coroutine _typing;
        Coroutine _fade;
        SubtitleTextType _textType;
        bool _gotComponent;

        void OnValidate()
        {
            QueryTextComponent();
        }

        void Start()
        {
            QueryTextComponent();
            if(deactivateIfEmpty && string.IsNullOrWhiteSpace(GetText())) gameObject.SetActive(false);
        }

        void QueryTextComponent()
        {
#if TMP
            text_tmp = GetComponent<TMP_Text>();
            _gotComponent = text_tmp;
            if(_gotComponent)
            {
                _textType = SubtitleTextType.TMP;
                return;
            }
#endif
            
            text_legacy = GetComponent<Text>();
            _gotComponent = text_legacy;
            if (_gotComponent)
            {
                _textType = SubtitleTextType.Legacy;
                return;
            }

            textMesh = GetComponent<TextMesh>();
            _gotComponent = textMesh;
            if (_gotComponent)
            {
                _textType = SubtitleTextType.TextMesh;
                return;
            }
            
            uiDocument = GetComponent<UIDocument>();
            if (!uiDocument || uiDocument.rootVisualElement == null) return;
            
            var paths = elementName.Split('/');
            if (paths.Length > 1)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    text_uielement = uiDocument.rootVisualElement.Q<Label>(paths[i]);
                }
            }
            else
            {
                text_uielement = uiDocument.rootVisualElement.Q<Label>(elementName);
            }
                
            _gotComponent = text_uielement != null;
            if (_gotComponent)
            {
                _textType = SubtitleTextType.UIElements;
                return;
            }
        }


        /// <summary>
        /// Passes a string to be displayed in the subtitle. The string will be displayed over a period of time based on the currently set textDisplayType.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public void SetText(string text)
        {
            if (!_gotComponent)
            {
                Debug.LogError($"Cannot Find Text Component in this GameObject.");
                return;
            }

            switch (textDisplayEffect)
            {
                case TextDisplayEffect.None:
                    SetRawText(text);
                    break;
                case TextDisplayEffect.Fade:
                    SetTextAlpha(0);
                    SetRawText(text);
                    CrossFade(1, subtitleFadeParameter.fadeInTime, null);
                    break;
                case TextDisplayEffect.Typing:
                    if (_typing != null) StopCoroutine(_typing);
                    _typing = StartCoroutine(Typing(text));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Clears the currently set string.
        /// If true is passed as an argument, the text will be cleared immediately, otherwise it will fade out and disappear according to the parameters of the effect set in textDisplayEffect.
        /// If deactivateIfEmpty is true, the GameObject will be deactivated when the effect ends or the text becomes invisible.
        /// </summary>
        /// <param name="immediately">If true, clears the text immediately without any fade effect.</param>
        public void Clear(bool immediately = false)
        {
            if (immediately)
            {
                SetRawText("");
                return;
            }
            switch (textDisplayEffect)
            {
                case TextDisplayEffect.None:
                    SetRawText("");
                    break;
                case TextDisplayEffect.Fade:
                    CrossFade(0, subtitleFadeParameter.fadeOutTime, () => SetRawText(""));
                    break;
                case TextDisplayEffect.Typing:
                    CrossFade(0, typingEffectParameter.fadeOutTime, () => SetRawText(""));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }

        IEnumerator Typing(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                SetRawText(text);
                yield break;
            }
            
            var timePerChar = typingEffectParameter.timeMethod == TypingEffectParameter.TimeMethod.PerChar
                ? typingEffectParameter.timePerChar
                : TypingEffect.CalcDuration(text, typingEffectParameter) / text.Normalize(NormalizationForm.FormD).Length;
            
            var wait = new WaitForSeconds(timePerChar);
            var waitIgnoreTimeScale = new WaitForSecondsRealtime(timePerChar);
            foreach (var str in TypingEffect.GetTypingText(text))
            {
                SetRawText(str);

                if (typingEffectParameter.HasAdditionalCharSetting(str[^1], out var time))
                {
                    yield return new WaitForSeconds(time);
                }
                else yield return ignoreTimeScale ? waitIgnoreTimeScale : wait;
            }

            _typing = null;
        }

        string GetText()
        {
            if (!_gotComponent) return string.Empty;
            switch (_textType)
            {
                case SubtitleTextType.None:
                    return string.Empty;
                case SubtitleTextType.Legacy:
                    return text_legacy.text;
#if TMP
                case SubtitleTextType.TMP:
                    return text_tmp.text;
#endif
                case SubtitleTextType.UIElements:
                    return text_uielement.text;
                case SubtitleTextType.TextMesh:
                    return textMesh.text;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void SetRawText(string text)
        {
            var isEmpty = string.IsNullOrWhiteSpace(text);
            switch (_textType)
            {
                case SubtitleTextType.None:
                    break;
                case SubtitleTextType.Legacy:
                    text_legacy.text = isEmpty ? string.Empty : $"{prefix}{text}";
                    break;
                case SubtitleTextType.TextMesh:
                    textMesh.text = isEmpty ? string.Empty : $"{prefix}{text}";
                    break;
#if TMP
                case SubtitleTextType.TMP:
                    text_tmp.text = isEmpty ? string.Empty : $"{prefix}{text}";
                    break;
#endif
                case SubtitleTextType.UIElements:
                    text_uielement.text = isEmpty ? string.Empty : $"{prefix}{text}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            if (_textType != SubtitleTextType.UIElements)
            {
                if (deactivateIfEmpty && isEmpty) gameObject.SetActive(false);
                else if(!gameObject.activeSelf)gameObject.SetActive(true);
            }
            
            onChanged.Invoke(text);
        }

        internal void SetTextAlpha(float alpha)
        {
            switch (_textType)
            {
                case SubtitleTextType.None:
                    break;
                case SubtitleTextType.Legacy:
                {
                    var color = text_legacy.color;
                    color.a = alpha;
                    text_legacy.color = color;
                    break;
                }
                case SubtitleTextType.TextMesh:
                {
                    var color = textMesh.color;
                    color.a = alpha;
                    textMesh.color = color;
                    break;
                }
#if TMP
                case SubtitleTextType.TMP:
                {
                    text_tmp.alpha = alpha;
                    break;
                }
#endif

                case SubtitleTextType.UIElements:
                {
                    var color = text_uielement.style.color.value;
                    color.a = alpha;
                    text_uielement.style.color = color;
                    break;
                }
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        void CrossFade(float alpha, float duration, Action complete)
        {
            if(_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(CrossFadeCoroutine(alpha, duration, complete));
        }
        
        IEnumerator CrossFadeCoroutine(float alpha, float duration, Action complete)
        {
            for (float f = 0f; f < duration; f += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                var color = _textType switch
                {
                    SubtitleTextType.None => Color.clear,
                    SubtitleTextType.Legacy => text_legacy.color,
#if TMP
                    SubtitleTextType.TMP => text_tmp.color,
#endif
                    SubtitleTextType.UIElements => text_uielement.style.color.value,
                    SubtitleTextType.TextMesh => textMesh.color,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var target = color;
                target.a = alpha;

                var normalizedTime = f / duration;
                switch (textDisplayEffect)
                {
                    case TextDisplayEffect.Fade:
                        normalizedTime *= subtitleFadeParameter.curve.Evaluate(normalizedTime);
                        break;
                    case TextDisplayEffect.Typing:
                        normalizedTime *= typingEffectParameter.fadeOutCurve.Evaluate(normalizedTime);
                        break;
                }
                color = Color.Lerp(color, target, normalizedTime);

                switch (_textType)
                {
                    case SubtitleTextType.None:
                        break;
                    case SubtitleTextType.Legacy:
                        text_legacy.color = color;
                        break;
#if TMP
                    case SubtitleTextType.TMP:
                        text_tmp.color = color;
                        break;    
#endif
                    case SubtitleTextType.UIElements:
                        text_uielement.style.color = color;
                        break;
                    
                    case SubtitleTextType.TextMesh:
                        textMesh.color = color;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                yield return null;
            }

            complete?.Invoke();
            _fade = null;
        }
    }
}