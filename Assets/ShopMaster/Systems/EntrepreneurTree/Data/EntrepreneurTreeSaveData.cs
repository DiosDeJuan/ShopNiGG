// ShopMaster - Entrepreneur Skill Tree System
// Serializable data container used when exporting / importing tree state.
// Not used at runtime (PlayerProgressPoints and EntrepreneurTreeManager handle that),
// but useful for editor tooling, JSON export, or future migration to a custom save format.

using System;
using System.Collections.Generic;

namespace ShopMaster
{
    /// <summary>
    /// Plain-old-data snapshot of the Entrepreneur Skill Tree state.
    /// Compatible with UnityEngine.JsonUtility for quick serialization.
    /// </summary>
    [Serializable]
    public class EntrepreneurTreeSaveData
    {
        /// <summary>Current entrepreneur points available to the player.</summary>
        public int progressPoints = 0;

        /// <summary>IDs of all nodes that have been unlocked by the player.</summary>
        public List<string> unlockedNodeIds = new List<string>();

        /// <summary>
        /// Creates an empty snapshot.
        /// </summary>
        public EntrepreneurTreeSaveData() { }

        /// <summary>
        /// Creates a snapshot from the current live state of the tree manager and points system.
        /// </summary>
        public static EntrepreneurTreeSaveData Capture()
        {
            EntrepreneurTreeSaveData snap = new EntrepreneurTreeSaveData();

            if (PlayerProgressPoints.Instance != null)
                snap.progressPoints = PlayerProgressPoints.Instance.currentPoints;

            if (EntrepreneurTreeManager.Instance != null)
                snap.unlockedNodeIds = new List<string>(EntrepreneurTreeManager.GetUnlockedIds());

            return snap;
        }
    }
}
