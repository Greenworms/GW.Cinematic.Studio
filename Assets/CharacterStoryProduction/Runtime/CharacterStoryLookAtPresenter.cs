using Greenworms.Cinematics.SkullStrike.CharacterStory;
using UnityEngine;

namespace Greenworms.Cinematics.CharacterStory.Production
{
    [DisallowMultipleComponent]
    public sealed class CharacterStoryLookAtPresenter : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform head;
        [SerializeField] private Transform chest;
        [SerializeField, Range(0f, 1f)] private float headWeight = 0.8f;
        [SerializeField, Range(0f, 1f)] private float chestWeight = 0.2f;
        [SerializeField, Range(0f, 90f)] private float maxAngle = 55f;

        public bool IsConfigured => animator != null && animator.isHuman && head != null;

        public bool Configure(Animator targetAnimator, SkullStrikeDialogueCharacterProfile profile)
        {
            animator = targetAnimator;
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                head = null;
                chest = null;
                return false;
            }

            head = animator.GetBoneTransform(HumanBodyBones.Head);
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (profile != null)
            {
                headWeight = profile.HeadLookWeight;
                chestWeight = profile.ChestLookWeight;
                maxAngle = profile.MaxLookAngle;
            }
            return head != null;
        }

        public void EvaluateTarget(Transform target, float timelineWeight)
        {
            if (!IsConfigured || target == null || timelineWeight <= 0f)
                return;
            Vector3 targetPosition = target.position + Vector3.up * 1.4f;
            ApplyLook(chest, targetPosition, chestWeight * timelineWeight);
            ApplyLook(head, targetPosition, headWeight * timelineWeight);
        }

        private void ApplyLook(Transform bone, Vector3 targetPosition, float weight)
        {
            if (bone == null || weight <= 0f)
                return;
            Vector3 direction = targetPosition - bone.position;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Quaternion clamped = Quaternion.RotateTowards(bone.rotation, desired, maxAngle);
            bone.rotation = Quaternion.Slerp(bone.rotation, clamped, Mathf.Clamp01(weight));
        }
    }
}
