using System;
using System.Text;
using UnityEngine;
using UnityEngine.Playables;

namespace CutsceneEngine
{
    [Serializable]
    public class SubtitleBehaviour : PlayableBehaviour
    {
        public SubtitleClip clip;
        bool _initialized;
        double _timePerChar;
        double _desiredDuration;
        SubtitleText _subtitle;
        TypingEffectParameter _typingEffectParameter;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var subtitle = playerData as SubtitleText;
            _subtitle = subtitle;

            if (!_initialized)
            {
                clip.EnterCallback(this, subtitle);
                if(subtitle)
                {
                    switch (subtitle.textDisplayEffect)
                    {
                        case TextDisplayEffect.None:
                            if (!clip.isVirtual) subtitle.SetRawText(clip.GetText());
                            break;
                        case TextDisplayEffect.Fade:
                            if (!clip.isVirtual) subtitle.SetRawText(clip.GetText());
                            break;
                        case TextDisplayEffect.Typing:
                            InitializeTypingEffect(subtitle, in playable);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _initialized = true;
            }

            if(!clip.isVirtual)
            {
                if(subtitle)
                {
                    if (subtitle.textDisplayEffect == TextDisplayEffect.Typing)
                        UpdateTextInTypingEffect(subtitle, playable.GetTime() / _desiredDuration);
                }
            }
            UpdateAlpha(subtitle, in playable);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (info.effectiveWeight <= 0)
            {
                _initialized = false;
                if(_subtitle) _subtitle.Clear(true);
                
                clip.ExitCallback(this, _subtitle);
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            _initialized = false;
        }

        void InitializeTypingEffect(SubtitleText subtitle, in Playable playable)
        {
            _typingEffectParameter = clip.overrideTypingEffectParameter ? 
                clip.typingEffectParameter : subtitle.typingEffectParameter;
            
            var _normalizedText = clip.GetText().Normalize(NormalizationForm.FormD);
            var _normalizedTextLength = _normalizedText.Length;

            var param = _typingEffectParameter;
                
            _timePerChar = param.timeMethod == TypingEffectParameter.TimeMethod.PerChar
                ? param.timePerChar
                : param.totalDuration / _normalizedTextLength;

            var duration = playable.GetDuration();

            _desiredDuration = param.timeMethod == TypingEffectParameter.TimeMethod.PerChar
                ? param.timePerChar * _normalizedTextLength
                : param.totalDuration;

            foreach (var c in _normalizedText)
            {
                if (param.HasAdditionalCharSetting(c, out var time))
                {
                    _desiredDuration += time - _timePerChar;
                }
            }

            if (_desiredDuration > duration - param.fadeOutTime)
            {
                _desiredDuration = duration - param.fadeOutTime;
            }
        }

        void UpdateTextInTypingEffect(SubtitleText subtitle, double normalizedTime)
        {
            var displayText = clip.GetText();
            if (normalizedTime < 0 || string.IsNullOrWhiteSpace(displayText))
            {
                subtitle.SetRawText(displayText);
                return;
            }

            if (normalizedTime >= 1)
            {
                subtitle.SetRawText(displayText);
                return;
            }

            var elapsed = 0d;
            foreach (var str in TypingEffect.GetTypingText(displayText))
            {
                if(_typingEffectParameter.HasAdditionalCharSetting(str[^1], out var time)) elapsed += time;
                else elapsed += _timePerChar;
                if (elapsed / _desiredDuration < normalizedTime) continue;
                subtitle.SetRawText(str);
                return;
            }
        }

        void UpdateAlpha(SubtitleText subtitle, in Playable playable)
        {
            if(!subtitle) return;
            var time = playable.GetTime();
            var left = playable.GetDuration() - time;
            
            switch (subtitle.textDisplayEffect)
            {
                case TextDisplayEffect.None:
                    subtitle.SetTextAlpha(1);
                    break;
                case TextDisplayEffect.Fade:
                    if (left < subtitle.subtitleFadeParameter.fadeOutTime)
                    {
                        subtitle.SetTextAlpha((float)left / subtitle.subtitleFadeParameter.fadeOutTime);
                    }
                    else if (time < subtitle.subtitleFadeParameter.fadeInTime)
                    {
                        subtitle.SetTextAlpha((float)time / subtitle.subtitleFadeParameter.fadeInTime);
                    }
                    else
                    {
                        subtitle.SetTextAlpha(1);
                    }
                    break;
                case TextDisplayEffect.Typing:
                    if (left < _typingEffectParameter.fadeOutTime)
                    {
                        subtitle.SetTextAlpha((float)left / _typingEffectParameter.fadeOutTime);
                    }
                    else
                    {
                        subtitle.SetTextAlpha(1);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}