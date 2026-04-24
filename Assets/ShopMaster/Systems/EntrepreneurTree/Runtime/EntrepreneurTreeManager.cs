// ShopMaster - Entrepreneur Skill Tree System
// Core runtime manager: validates prerequisites, spends points, fires unlock events,
// and bridges unlocked nodes to the asset's gameplay systems via gameplayKey.
//
// Setup:
//   1. Attach this to a persistent scene GameObject (same one as PlayerProgressPoints).
//   2. Assign the EntrepreneurTreeData ScriptableObject to the treeData field.
//   3. After each node unlock the manager calls ApplyGameplayEffect().
//      Fill in the TODO blocks there to connect with real asset systems.

using System;
using System.Collections.Generic;
using UnityEngine;
using FLOBUK.StoreSimulator;

namespace ShopMaster
{
    /// <summary>
    /// Central runtime manager for the Entrepreneur Skill Tree.
    /// Handles unlock validation, point spending, persistence, and gameplay-effect dispatch.
    /// </summary>
    public class EntrepreneurTreeManager : MonoBehaviour
    {
        /// <summary>Returns the active singleton instance.</summary>
        public static EntrepreneurTreeManager Instance { get; private set; }

        /// <summary>Fired whenever a node is successfully unlocked.</summary>
        public static event Action<EntrepreneurNodeData> onNodeUnlocked;

        [Tooltip("ScriptableObject that contains all node definitions for the skill tree.")]
        public EntrepreneurTreeData treeData;

        // Persistence key (separate from the asset's save file)
        private const string UnlockedSaveKey = "ShopMaster_UnlockedNodes";

        // Runtime set of unlocked node IDs
        private readonly HashSet<string> unlockedNodes = new HashSet<string>();


        void Awake()
        {
            // Singleton guard
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            SaveGameSystem.dataSaveEvent += OnSave;
            SaveGameSystem.dataLoadEvent += OnLoad;
        }


        void OnDestroy()
        {
            SaveGameSystem.dataSaveEvent -= OnSave;
            SaveGameSystem.dataLoadEvent -= OnLoad;
        }


        // ── Query API ─────────────────────────────────────────────────────────────

        /// <summary>Returns true if the node with the given ID has been unlocked.</summary>
        public static bool IsUnlocked(string nodeId)
        {
            return Instance != null && Instance.unlockedNodes.Contains(nodeId);
        }


        /// <summary>
        /// Returns the current visual / interaction state for a node.
        /// Locked  → prerequisites not met.
        /// Available → prerequisites met, not yet purchased.
        /// Unlocked → already purchased.
        /// </summary>
        public static EntrepreneurNodeState GetNodeState(string nodeId)
        {
            if (Instance == null) return EntrepreneurNodeState.Locked;

            if (Instance.unlockedNodes.Contains(nodeId))
                return EntrepreneurNodeState.Unlocked;

            return AreRequirementsMet(nodeId)
                ? EntrepreneurNodeState.Available
                : EntrepreneurNodeState.Locked;
        }


        /// <summary>
        /// Returns true if every prerequisite of the given node is already unlocked.
        /// Always returns true for root nodes (empty requirements list).
        /// </summary>
        public static bool AreRequirementsMet(string nodeId)
        {
            if (Instance == null || Instance.treeData == null) return false;

            EntrepreneurNodeData node = Instance.treeData.GetNodeById(nodeId);
            if (node == null) return false;

            foreach (string reqId in node.requiredNodeIds)
            {
                if (!string.IsNullOrEmpty(reqId) && !IsUnlocked(reqId))
                    return false;
            }

            return true;
        }


        /// <summary>Returns all node definitions from the TreeData asset.</summary>
        public static List<EntrepreneurNodeData> GetAllNodes()
        {
            if (Instance == null || Instance.treeData == null)
                return new List<EntrepreneurNodeData>();

            return Instance.treeData.nodes;
        }


        /// <summary>Returns a read-only copy of all currently unlocked node IDs.</summary>
        public static IReadOnlyCollection<string> GetUnlockedIds()
        {
            return Instance != null
                ? (IReadOnlyCollection<string>)Instance.unlockedNodes
                : new HashSet<string>();
        }


        // ── Unlock API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to unlock the node with the given ID.
        /// Validates: node exists, not already unlocked, prerequisites met, enough points.
        /// Shows player-facing messages via UIGame on failure.
        /// Fires <see cref="onNodeUnlocked"/> and calls <see cref="ApplyGameplayEffect"/> on success.
        /// </summary>
        /// <returns>True if the node was successfully unlocked.</returns>
        public static bool TryUnlock(string nodeId)
        {
            if (Instance == null || Instance.treeData == null)
            {
                Debug.LogWarning("[EntrepreneurTree] TryUnlock called but Manager or TreeData is missing.");
                return false;
            }

            EntrepreneurNodeData node = Instance.treeData.GetNodeById(nodeId);

            // Node not found
            if (node == null)
            {
                ShowMessage("Nodo no encontrado: " + nodeId);
                return false;
            }

            // Already unlocked
            if (IsUnlocked(nodeId))
            {
                ShowMessage("\"" + node.displayName + "\" ya está desbloqueado.");
                return false;
            }

            // Check prerequisites
            foreach (string reqId in node.requiredNodeIds)
            {
                if (!string.IsNullOrEmpty(reqId) && !IsUnlocked(reqId))
                {
                    EntrepreneurNodeData req = Instance.treeData.GetNodeById(reqId);
                    string reqName = req != null ? req.displayName : reqId;
                    ShowMessage("Requisito pendiente: " + reqName);
                    return false;
                }
            }

            // Check points
            if (!PlayerProgressPoints.CanSpend(node.cost))
            {
                int missing = node.cost - PlayerProgressPoints.GetPoints();
                ShowMessage("Puntos insuficientes. Faltan: " + missing);
                return false;
            }

            // Commit unlock
            PlayerProgressPoints.SpendPoints(node.cost);
            Instance.unlockedNodes.Add(nodeId);

            // Trigger gameplay systems
            ApplyGameplayEffect(node);

            onNodeUnlocked?.Invoke(node);

            if (UIGame.Instance != null)
                UIGame.AddNotification("¡Desbloqueado: " + node.displayName + "!", otherColor: Color.green);

            return true;
        }


        // ── Gameplay Effect Dispatch ──────────────────────────────────────────────

        /// <summary>
        /// Dispatches gameplay effects to the appropriate asset system when a node is unlocked.
        /// All integrations that depend on a real asset system are marked with TODO comments.
        /// </summary>
        private static void ApplyGameplayEffect(EntrepreneurNodeData node)
        {
            switch (node.nodeType)
            {
                case EntrepreneurNodeType.Product:
                    // TODO: Unlock the product category identified by node.gameplayKey.
                    //       This should enable buying/ordering those products in the asset's
                    //       delivery or shop system.  Example:
                    //         ItemDatabase.SetProductCategoryUnlocked(node.gameplayKey, true);
                    Debug.Log("[EntrepreneurTree] Product category unlocked: " + node.gameplayKey);
                    break;

                case EntrepreneurNodeType.Employee:
                    // TODO: Unlock the EmployeeScriptableObject whose id matches node.gameplayKey.
                    //       Example integration with the existing EmployeeSystem:
                    //         EmployeeScriptableObject emp = EmployeeSystem.Instance.employees
                    //             .Find(e => e.id == node.gameplayKey);
                    //         if (emp != null) emp.isUnlocked = true;
                    Debug.Log("[EntrepreneurTree] Employee slot unlocked: " + node.gameplayKey);
                    break;

                case EntrepreneurNodeType.Security:
                    // TODO: Upgrade the SecuritySystem to the level specified in node.gameplayKey.
                    //       SecuritySystem.TryUpgradeLevel() checks its own requiredSkillNodes array,
                    //       so either wire that or call it directly and skip the prerequisite check.
                    //       Example:
                    //         SecuritySystem.TryUpgradeLevel();
                    Debug.Log("[EntrepreneurTree] Security level unlocked: " + node.gameplayKey);
                    break;

                case EntrepreneurNodeType.Upgrade:
                    ApplyNamedUpgrade(node.gameplayKey);
                    break;
            }
        }


        /// <summary>
        /// Applies a named passive upgrade effect.
        /// Extend this switch as new upgrade nodes are added to the tree.
        /// </summary>
        private static void ApplyNamedUpgrade(string key)
        {
            switch (key)
            {
                case "caffeine":
                    // TODO: Increase employee movement/work speed by 10%.
                    //       Possible integration: modify EmployeeSystem.stockerRestockTime
                    //       or a dedicated employee speed multiplier field.
                    //       Example:
                    //         EmployeeSystem.Instance.stockerRestockTime *= 0.9f;
                    Debug.Log("[EntrepreneurTree] Upgrade 'Cafeína' applied: +10% employee speed.");
                    break;

                case "charismatic":
                    // TODO: Increase cashier sales rate by 5%.
                    //       Possible integration: add a salesMultiplier field to EmployeeSystem
                    //       and apply it to CheckoutItem pricing or CashDesk revenue calculations.
                    Debug.Log("[EntrepreneurTree] Upgrade 'Carismático' applied: +5% cashier sales.");
                    break;

                default:
                    Debug.LogWarning("[EntrepreneurTree] Unknown upgrade key: " + key);
                    break;
            }
        }


        // ── Save / Load ───────────────────────────────────────────────────────────

        private void OnSave()
        {
            // Serialise the unlocked set as a comma-separated string in PlayerPrefs.
            PlayerPrefs.SetString(UnlockedSaveKey, string.Join(",", unlockedNodes));
            PlayerPrefs.Save();
        }


        private void OnLoad()
        {
            unlockedNodes.Clear();

            string raw = PlayerPrefs.GetString(UnlockedSaveKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;

            foreach (string id in raw.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                    unlockedNodes.Add(id);
            }
        }


        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void ShowMessage(string text)
        {
            if (UIGame.Instance != null)
                UIGame.Instance.ShowMessage(text);
            else
                Debug.Log("[EntrepreneurTree] " + text);
        }


        // ── Debug helpers ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        [ContextMenu("Debug: Unlock All Nodes")]
        private void DebugUnlockAll()
        {
            if (treeData == null) return;
            foreach (EntrepreneurNodeData node in treeData.nodes)
            {
                if (node != null)
                    unlockedNodes.Add(node.id);
            }
            onNodeUnlocked?.Invoke(null);
            Debug.Log("[EntrepreneurTree] All nodes unlocked (debug).");
        }

        [ContextMenu("Debug: Reset All Nodes")]
        private void DebugResetAll()
        {
            unlockedNodes.Clear();
            PlayerPrefs.DeleteKey(UnlockedSaveKey);
            onNodeUnlocked?.Invoke(null);
            Debug.Log("[EntrepreneurTree] All nodes reset (debug).");
        }
#endif
    }
}
