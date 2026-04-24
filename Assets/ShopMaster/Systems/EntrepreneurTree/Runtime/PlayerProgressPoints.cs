// ShopMaster - Entrepreneur Skill Tree System
// Singleton MonoBehaviour that manages the pool of entrepreneur points earned from achievements.
// Points are separate from the store's money (StoreDatabase.currentMoney).
//
// Setup: Attach this component to a persistent GameObject in the scene (e.g. on the Systems prefab).
//        The component hooks into SaveGameSystem's save/load events for automatic persistence.
//
// TODO: Call PlayerProgressPoints.AddPoints(1) from every achievement completion handler.
//       Achievements can be created as simple methods that subscribe to existing events
//       (e.g. StoreDatabase.onLevelUpdate, CustomerSystem.onCustomerLeft, etc.)

using System;
using UnityEngine;
using FLOBUK.StoreSimulator;

namespace ShopMaster
{
    /// <summary>
    /// Manages the pool of entrepreneur progression points earned through achievements.
    /// Points are the only currency used to unlock nodes in the Entrepreneur Skill Tree;
    /// they are completely separate from the store's financial balance.
    /// </summary>
    public class PlayerProgressPoints : MonoBehaviour
    {
        /// <summary>Returns the active singleton instance.</summary>
        public static PlayerProgressPoints Instance { get; private set; }

        /// <summary>Fired whenever the point total changes (earn or spend).</summary>
        public static event Action<int> onPointsChanged;

        // PlayerPrefs key for persistence (separate from the asset's save file)
        private const string SaveKey = "ShopMaster_ProgressPoints";

        /// <summary>Current total of unspent entrepreneur points.</summary>
        public int currentPoints { get; private set; }


        void Awake()
        {
            // Singleton guard
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Hook into the asset's save / load pipeline.
            // SaveGameSystem fires these static events after its own save/load cycle.
            SaveGameSystem.dataSaveEvent += OnSave;
            SaveGameSystem.dataLoadEvent += OnLoad;
        }


        void OnDestroy()
        {
            SaveGameSystem.dataSaveEvent -= OnSave;
            SaveGameSystem.dataLoadEvent -= OnLoad;
        }


        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds the given number of points to the player's total.
        /// Call this whenever an achievement is completed.
        /// </summary>
        public static void AddPoints(int amount)
        {
            if (Instance == null) return;
            if (amount <= 0) return;

            Instance.currentPoints += amount;
            onPointsChanged?.Invoke(Instance.currentPoints);
        }


        /// <summary>
        /// Returns true if the player has at least <paramref name="amount"/> points available.
        /// </summary>
        public static bool CanSpend(int amount)
        {
            return Instance != null && Instance.currentPoints >= amount;
        }


        /// <summary>
        /// Deducts <paramref name="amount"/> points from the player's total.
        /// Returns true if successful; false if there were not enough points.
        /// </summary>
        public static bool SpendPoints(int amount)
        {
            if (!CanSpend(amount)) return false;

            Instance.currentPoints -= amount;
            onPointsChanged?.Invoke(Instance.currentPoints);
            return true;
        }


        /// <summary>
        /// Returns the current point total without modifying it.
        /// </summary>
        public static int GetPoints()
        {
            return Instance != null ? Instance.currentPoints : 0;
        }


        // ── Save / Load ───────────────────────────────────────────────────────────

        // Called by SaveGameSystem after all base systems have been saved.
        private void OnSave()
        {
            PlayerPrefs.SetInt(SaveKey, currentPoints);
            PlayerPrefs.Save();
        }


        // Called by SaveGameSystem after all base systems have been loaded.
        private void OnLoad()
        {
            currentPoints = PlayerPrefs.GetInt(SaveKey, 0);
            onPointsChanged?.Invoke(currentPoints);
        }


        // ── Debug helpers ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        /// <summary>Editor-only: directly set points (for testing via Inspector context menu).</summary>
        [ContextMenu("Debug: Add 10 Points")]
        private void DebugAddPoints() => AddPoints(10);

        [ContextMenu("Debug: Reset Points")]
        private void DebugResetPoints()
        {
            currentPoints = 0;
            PlayerPrefs.DeleteKey(SaveKey);
            onPointsChanged?.Invoke(0);
        }
#endif
    }
}
