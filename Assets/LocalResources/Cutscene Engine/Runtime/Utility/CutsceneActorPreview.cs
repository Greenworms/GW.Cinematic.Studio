using UnityEngine;

namespace CutsceneEngine
{
    /// <summary>
    /// Placeholder that is bound to the Timeline. At runtime it clones a matching origin and rebinds the track.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Cutscene Engine/Cutscene Actor Preview (Cutscene Engine)")]
    public class CutsceneActorPreview : MonoBehaviour
    {
        [SerializeField] bool deactivateOnAwake = true;
        [SerializeField] string key = "actor1";
        [SerializeField] Cutscene cutscene;
        [SerializeField] Animator avatarAnimator;

        public string Key => key;

        void Reset()
        {
            avatarAnimator = GetComponent<Animator>();
            cutscene = GetComponentInParent<Cutscene>();
        }

        void Awake()
        {
            if (!avatarAnimator) avatarAnimator = GetComponent<Animator>();
            if (!cutscene) cutscene = GetComponentInParent<Cutscene>();

            if (cutscene != null)
            {
                cutscene.onStateChanged += OnCutsceneStateChanged;
                if (cutscene.state == CutsceneState.Playing)
                {
                    TryCloneAndRebind();
                }
            }
            
            if (deactivateOnAwake)
            {
                gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (cutscene != null)
            {
                cutscene.onStateChanged -= OnCutsceneStateChanged;
            }
        }

        void OnCutsceneStateChanged(CutsceneState state)
        {
            if (state == CutsceneState.Playing)
            {
                TryCloneAndRebind();
            }
            else if (state == CutsceneState.None)
            {
                ResetBinding();
            }
        }

        void TryCloneAndRebind()
        {
            if (!avatarAnimator)
            {
                Debug.LogWarning($"[{nameof(CutsceneActorPreview)}] Missing Animator reference for avatar \"{name}\".", this);
                return;
            }

            if (!cutscene || !cutscene.director)
            {
                Debug.LogWarning($"[{nameof(CutsceneActorPreview)}] Missing Cutscene/PlayableDirector reference on \"{name}\".", this);
                return;
            }

            var origin = CutsceneActor.Find(key);
            if (!origin)
            {
                Debug.LogWarning($"[{nameof(CutsceneActorPreview)}] Unable to find {nameof(CutsceneActor)} with key \"{key}\".", this);
                return;
            }

            origin.InitializeTransform(transform.position, transform.rotation);
            cutscene.ReplaceBindings(gameObject, origin.gameObject);
            gameObject.SetActive(false);
        }

        void ResetBinding()
        {
            var origin = CutsceneActor.Find(key);
            if(origin)
            {
                cutscene.ReplaceBindings(origin.gameObject, gameObject);
                origin.OnResetBinding();
            }
        }
    }
}
