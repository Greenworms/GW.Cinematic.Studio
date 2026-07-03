using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CutsceneEngine
{
    [AddComponentMenu("Cutscene Engine/Cutscene Skip Button (Cutscene Engine)")]
    public class CutsceneSkipButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Cutscene cutscene;
        [SerializeField] Button button;
        [SerializeField] Image holdProgressImage;
        [SerializeField] CanvasGroup canvasGroup;

        [Header("Behavior")]
        [Min(0f)]
        [SerializeField] float holdTime = 1.5f;
        [Range(0f, 1f)] [SerializeField] float inactiveAlpha = 0f;
        [Range(0f, 1f)] [SerializeField] float activeAlpha = 1f;
        [SerializeField] float fadeInDuration = 0.15f;
        [SerializeField] float fadeOutDuration = 0.5f;
        [SerializeField] float fadeOutDelay = .25f;

#if ENABLE_LEGACY_INPUT_MANAGER
        [Header("Input Manager")]
        [SerializeField] KeyCode skipKey = KeyCode.Escape;
#endif

#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM
        [Header("Input System")]
        [SerializeField] InputActionReference skipAction;
#endif

        float _holdTimer;
        bool _inputHeld;
        bool _skipTriggeredFromInput;
        Coroutine _fadeRoutine;
        Coroutine _fadeOutDelayRoutine;

        void Reset()
        {
            CacheReferences();
            InitializeCanvasGroup();
        }

        void Awake()
        {
            CacheReferences();
            if (button) button.onClick.AddListener(ForceComplete);
        }

        void OnEnable()
        {
            InitializeCanvasGroup();
            ResetHoldProgress();
#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM
            if (skipAction && skipAction.action != null)
            {
                skipAction.action.Enable();
            }
#endif
        }

        void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM
            if (skipAction && skipAction.action != null)
            {
                skipAction.action.Disable();
            }
#endif
        }

        void Update()
        {
            if (!cutscene || cutscene.state == CutsceneState.None)
            {
                return;
            }
            if (button && (!button.IsActive() || !button.interactable))
            {
                ResetHoldProgress();
                ScheduleFadeOut(0f);
                return;
            }

            bool isPressed = false;
            bool pressedThisFrame = false;
            bool releasedThisFrame = false;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (skipKey != KeyCode.None)
            {
                if (Input.GetKey(skipKey)) isPressed = true;
                if (Input.GetKeyDown(skipKey)) pressedThisFrame = true;
                if (Input.GetKeyUp(skipKey)) releasedThisFrame = true;
            }
#endif

#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM
            if (skipAction && skipAction.action != null)
            {
                var action = skipAction.action;
                isPressed |= action.IsPressed();
                pressedThisFrame |= action.WasPressedThisFrame();
                releasedThisFrame |= action.WasReleasedThisFrame();
            }
#endif

            HandleInput(isPressed, pressedThisFrame, releasedThisFrame);
        }

        void HandleInput(bool isPressed, bool pressedThisFrame, bool releasedThisFrame)
        {
            if (pressedThisFrame)
            {
                BeginInputHold();
            }

            if (holdTime <= 0f)
            {
                if (pressedThisFrame)
                {
                    TriggerSkipFromInput();
                }

                if (releasedThisFrame)
                {
                    EndInputHold(_skipTriggeredFromInput);
                    _skipTriggeredFromInput = false;
                    ResetHoldProgress();
                }
                return;
            }

            if (!isPressed)
            {
                if (releasedThisFrame || _holdTimer > 0f)
                {
                    EndInputHold(_skipTriggeredFromInput);
                    _skipTriggeredFromInput = false;
                    ResetHoldProgress();
                }
                return;
            }

            _holdTimer += Time.unscaledDeltaTime;
            UpdateFillAmount(Mathf.Clamp01(_holdTimer / holdTime));

            if (_holdTimer >= holdTime)
            {
                TriggerSkipFromInput();
                ResetHoldProgress();
                EndInputHold(true);
            }
        }

        void SimulateButtonClick()
        {
            if (!button) return;
            button.onClick.Invoke();
        }

        void TriggerSkipFromInput()
        {
            _skipTriggeredFromInput = true;
            if(button) SimulateButtonClick();
            else ForceComplete();
            ScheduleFadeOut(0f);
        }

        void ForceComplete()
        {
            if (!cutscene) return;
            cutscene.Stop();
        }

        void ResetHoldProgress()
        {
            _holdTimer = 0f;
            UpdateFillAmount(0f);
        }

        void UpdateFillAmount(float amount)
        {
            if (!holdProgressImage || holdProgressImage.type != Image.Type.Filled) return;
            holdProgressImage.fillAmount = amount;
        }

        void BeginInputHold()
        {
            if (_inputHeld) return;
            _inputHeld = true;
            CancelFadeOutDelay();
            FadeCanvasGroup(activeAlpha, fadeInDuration);
        }

        void EndInputHold(bool holdCompleted)
        {
            if (!_inputHeld) return;
            _inputHeld = false;
            float delay = holdCompleted ? 0f : fadeOutDelay;
            ScheduleFadeOut(delay);
        }

        void ScheduleFadeOut(float delay)
        {
            CancelFadeOutDelay();
            if (!canvasGroup) return;
            _fadeOutDelayRoutine = StartCoroutine(FadeOutAfterDelay(delay));
        }

        void CancelFadeOutDelay()
        {
            if (_fadeOutDelayRoutine == null) return;
            StopCoroutine(_fadeOutDelayRoutine);
            _fadeOutDelayRoutine = null;
        }

        IEnumerator FadeOutAfterDelay(float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            FadeCanvasGroup(inactiveAlpha, fadeOutDuration);
            _fadeOutDelayRoutine = null;
        }

        void FadeCanvasGroup(float targetAlpha, float duration)
        {
            if (!canvasGroup) return;

            if (Mathf.Approximately(duration, 0f))
            {
                canvasGroup.alpha = targetAlpha;
                return;
            }

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeCanvasRoutine(targetAlpha, duration));
        }

        IEnumerator FadeCanvasRoutine(float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            _fadeRoutine = null;
        }

        void InitializeCanvasGroup()
        {
            if (!canvasGroup) return;
            canvasGroup.alpha = inactiveAlpha;
        }

        void CacheReferences()
        {
            if (!button) button = GetComponent<Button>();
            if (!cutscene) cutscene = GetComponentInParent<Cutscene>();
            if (!holdProgressImage && button && button.targetGraphic is Image targetImage)
            {
                holdProgressImage = targetImage;
            }

            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            if (!canvasGroup) canvasGroup = GetComponentInParent<CanvasGroup>();
        }
    }
}
