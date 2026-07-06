using UnityEngine;
using System.Collections.Generic;

namespace FPS.Managers
{
    /// <summary>
    /// Quản lý toàn bộ các điểm hồi sinh trên map và ánh xạ logical team
    /// sang physical side của map cho từng trận đấu.
    /// </summary>
    public class TDMSpawnManager : MonoBehaviour
    {
        private static readonly List<TeamSpawnPoint> teamASpawns = new List<TeamSpawnPoint>();
        private static readonly List<TeamSpawnPoint> teamBSpawns = new List<TeamSpawnPoint>();
        private static readonly Queue<int> recentTeamASpawnIds = new Queue<int>();
        private static readonly Queue<int> recentTeamBSpawnIds = new Queue<int>();
        private static bool sidesSwapped;

        public static bool SidesSwapped => sidesSwapped;

        public static void ResetRuntimeState()
        {
            teamASpawns.Clear();
            teamBSpawns.Clear();
            recentTeamASpawnIds.Clear();
            recentTeamBSpawnIds.Clear();
            sidesSwapped = false;
        }

        public static void ConfigureSideSwap(bool swapSides)
        {
            sidesSwapped = swapSides;
            recentTeamASpawnIds.Clear();
            recentTeamBSpawnIds.Clear();
        }

        public static void RegisterSpawnPoint(TeamSpawnPoint sp)
        {
            if (sp == null)
                return;

            List<TeamSpawnPoint> targetList = sp.team == Team.TeamA ? teamASpawns : teamBSpawns;
            if (!targetList.Contains(sp))
                targetList.Add(sp);
        }

        public static void UnregisterSpawnPoint(TeamSpawnPoint sp)
        {
            if (sp == null)
                return;

            if (sp.team == Team.TeamA)
                teamASpawns.Remove(sp);
            else
                teamBSpawns.Remove(sp);
        }

        /// <summary>
        /// Trả về một spawn point cho logical team (0 = TeamA, 1 = TeamB).
        /// Nếu trận đang bật đảo side, team sẽ dùng cụm spawn của phía đối diện.
        /// Hạn chế lặp lại điểm vừa dùng gần đây để nhịp trận đa dạng hơn.
        /// </summary>
        public static Transform GetRandomSpawnPoint(int teamIndex)
        {
            List<TeamSpawnPoint> spawnList = ResolveSpawnList(teamIndex);
            if (spawnList.Count == 0)
                return null;

            Queue<int> recentQueue = teamIndex == 0 ? recentTeamASpawnIds : recentTeamBSpawnIds;
            TeamSpawnPoint selectedSpawn = SelectSpawnPoint(spawnList, recentQueue);
            return selectedSpawn != null ? selectedSpawn.transform : null;
        }

        private static List<TeamSpawnPoint> ResolveSpawnList(int teamIndex)
        {
            bool isTeamA = teamIndex == 0;
            if (!sidesSwapped)
                return isTeamA ? teamASpawns : teamBSpawns;

            return isTeamA ? teamBSpawns : teamASpawns;
        }

        private static TeamSpawnPoint SelectSpawnPoint(List<TeamSpawnPoint> spawnList, Queue<int> recentQueue)
        {
            TrimRecentQueue(spawnList, recentQueue);

            List<TeamSpawnPoint> availableSpawns = null;
            if (recentQueue.Count > 0 && recentQueue.Count < spawnList.Count)
            {
                availableSpawns = new List<TeamSpawnPoint>(spawnList.Count);
                foreach (TeamSpawnPoint spawn in spawnList)
                {
                    if (spawn == null)
                        continue;

                    if (!recentQueue.Contains(spawn.GetInstanceID()))
                        availableSpawns.Add(spawn);
                }
            }

            List<TeamSpawnPoint> candidateList = availableSpawns != null && availableSpawns.Count > 0
                ? availableSpawns
                : spawnList;

            TeamSpawnPoint selectedSpawn = candidateList[Random.Range(0, candidateList.Count)];
            RegisterRecentUsage(spawnList, recentQueue, selectedSpawn);
            return selectedSpawn;
        }

        private static void TrimRecentQueue(List<TeamSpawnPoint> spawnList, Queue<int> recentQueue)
        {
            int historyLimit = Mathf.Clamp(spawnList.Count / 3, 1, 3);
            while (recentQueue.Count > historyLimit)
                recentQueue.Dequeue();
        }

        private static void RegisterRecentUsage(List<TeamSpawnPoint> spawnList, Queue<int> recentQueue, TeamSpawnPoint selectedSpawn)
        {
            if (selectedSpawn == null || spawnList.Count <= 1)
                return;

            int selectedId = selectedSpawn.GetInstanceID();
            recentQueue.Enqueue(selectedId);
            TrimRecentQueue(spawnList, recentQueue);
        }
    }
}
