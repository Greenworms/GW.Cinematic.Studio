using UnityEngine;

namespace CutsceneEngine
{
    [AddComponentMenu("Cutscene Engine/Cutscene Trigger (Cutscene Engine)")]
    [DisallowMultipleComponent]
    public class CutsceneTrigger : MonoBehaviour
    {
        public enum CutsceneTriggerTiming
        {
            Awake,
            OnEnable,
            Start,
            OnDisable,
            OnDestroy,
            TriggerEnter
        }

        [Tooltip("Cutscene to play when a configured event fires. Defaults to the Cutscene on this GameObject.")]
        public Cutscene cutscene;

        [Header("Trigger Timing")]
        [Tooltip("Choose when the cutscene should play.")]
        public CutsceneTriggerTiming triggerTiming = CutsceneTriggerTiming.TriggerEnter;
        [Tooltip("Optional tag requirement for trigger colliders.")]
        public string requiredTag;

        [Header("Playback Options")]
        [Tooltip("When enabled, the cutscene plays at the specified marker name.")]
        public bool playFromMarker;
        [Tooltip("Marker name to start from when Play From Marker is enabled.")]
        public string markerName;
        [Tooltip("If true, the first successful play prevents additional plays.")]
        public bool playOnlyOnce;

        bool _hasPlayed;

        void Reset()
        {
            cutscene = GetComponent<Cutscene>();
        }

        void Awake()
        {
            if (ShouldPlay(CutsceneTriggerTiming.Awake))
                TryPlay();
        }

        void OnEnable()
        {
            if (ShouldPlay(CutsceneTriggerTiming.OnEnable))
                TryPlay();
        }

        void Start()
        {
            if (ShouldPlay(CutsceneTriggerTiming.Start))
                TryPlay();
        }

        void OnDisable()
        {
            if (ShouldPlay(CutsceneTriggerTiming.OnDisable))
                TryPlay();
        }

        void OnDestroy()
        {
            if (ShouldPlay(CutsceneTriggerTiming.OnDestroy))
                TryPlay();
        }

        void OnTriggerEnter(Collider other)
        {
            if (ShouldPlay(CutsceneTriggerTiming.TriggerEnter))
                HandleTrigger(other.gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (ShouldPlay(CutsceneTriggerTiming.TriggerEnter))
                HandleTrigger(other.gameObject);
        }

        void HandleTrigger(GameObject other)
        {
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
                return;

            TryPlay();
        }

        void TryPlay()
        {
            if (playOnlyOnce && _hasPlayed)
                return;

            if (!cutscene)
            {
                cutscene = GetComponent<Cutscene>();
                if (!cutscene)
                {
                    Debug.LogWarning($"[{nameof(CutsceneTrigger)}] Missing Cutscene reference on {name}.", this);
                    return;
                }
            }

            if (playFromMarker && !string.IsNullOrEmpty(markerName))
                cutscene.PlayAt(markerName);
            else
                cutscene.Play();

            _hasPlayed = true;
        }

        bool ShouldPlay(CutsceneTriggerTiming timingEvent) => triggerTiming == timingEvent;
    }
}
