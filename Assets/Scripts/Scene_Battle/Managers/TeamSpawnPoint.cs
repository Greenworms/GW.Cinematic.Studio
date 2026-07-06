using UnityEngine;
using System.Collections.Generic;

namespace FPS.Managers
{
    public enum Team { TeamA, TeamB }

    /// <summary>
    /// Đánh dấu một điểm hồi sinh cho đội tương ứng.
    /// </summary>
    public class TeamSpawnPoint : MonoBehaviour
    {
        public Team team;

        private void OnEnable()
        {
            TDMSpawnManager.RegisterSpawnPoint(this);
        }

        private void OnDisable()
        {
            TDMSpawnManager.UnregisterSpawnPoint(this);
        }

        // Vẽ Gizmo trên Editor để bạn dễ nhìn thấy Base của mình nằm ở đâu
        private void OnDrawGizmos()
        {
            Gizmos.color = (team == Team.TeamA) ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);
            Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        }
    }
}
