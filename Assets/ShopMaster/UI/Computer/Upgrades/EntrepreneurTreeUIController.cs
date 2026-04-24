// ShopMaster - Entrepreneur Skill Tree System
// Orchestrates the entire Upgrades-tab tree UI:
//   • Builds the visual tree (node prefabs + connection lines) inside the Scroll View Content.
//   • Refreshes node colors and connection colors in response to unlock events.
//   • Displays / hides the tooltip panel.
//   • Shows the player's available points and feedback messages.
//
// Setup:
//   1. Attach this to a root GameObject that is ONLY active when the UPGRADES tab is open.
//      The existing UIShopDesktop tab mechanism can activate/deactivate this root.
//      -- OR -- attach it directly to the Upgrades panel that already exists in UIShopDesktop.
//   2. Assign all Inspector fields (see inline tooltips).
//   3. The Content RectTransform is auto-sized to fit all nodes.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShopMaster
{
    /// <summary>
    /// Master controller for the Entrepreneur Skill Tree UI inside the UPGRADES panel.
    ///
    /// Expected UI hierarchy inside the UPGRADES panel:
    /// <code>
    /// [UpgradesPanel]
    ///   Header
    ///     PointsText         ← TMP_Text: "Puntos: X"
    ///     FeedbackText       ← TMP_Text: temporary feedback messages
    ///   Scroll View
    ///     Viewport
    ///       Content          ← RectTransform (assign to contentContainer)
    ///   Tooltip              ← EntrepreneurTooltipUI (outside the Scroll View)
    /// </code>
    /// </summary>
    public class EntrepreneurTreeUIController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("The ScriptableObject that lists all 36 nodes. " +
                 "Create it with ShopMaster > Build Entrepreneur Tree.")]
        public EntrepreneurTreeData treeData;

        [Header("Prefabs")]
        [Tooltip("Prefab that carries an EntrepreneurNodeUI component. " +
                 "Recommended size: 120 × 60 px.")]
        public GameObject nodePrefab;

        [Tooltip("Prefab that carries an EntrepreneurConnectionUI component (just an Image).")]
        public GameObject connectionPrefab;

        [Header("UI References")]
        [Tooltip("Content RectTransform inside the Scroll View. Nodes are placed here.")]
        public RectTransform contentContainer;

        [Tooltip("TMP_Text label in the header that shows available points.")]
        public TMP_Text pointsText;

        [Tooltip("TMP_Text label for brief feedback / error messages.")]
        public TMP_Text feedbackText;

        [Tooltip("The tooltip component. Must be a sibling of (or outside) the Scroll View.")]
        public EntrepreneurTooltipUI tooltip;

        [Tooltip("Optional: A ScrollRect reference. Used to auto-set content size.")]
        public ScrollRect scrollRect;

        // Padding added around all nodes when auto-sizing the Content panel.
        [Tooltip("Extra padding (pixels) added to each side when calculating Content size.")]
        public float contentPadding = 80f;

        // Node size used only for content-bounds calculation.
        [Tooltip("Expected pixel size of each node prefab. Must match the prefab's RectTransform.")]
        public Vector2 nodeSize = new Vector2(120f, 60f);

        // ── Runtime state ─────────────────────────────────────────────────────────

        // node ID → its UI instance
        private readonly Dictionary<string, EntrepreneurNodeUI> nodeUIMap =
            new Dictionary<string, EntrepreneurNodeUI>();

        // All instantiated connection lines
        private readonly List<EntrepreneurConnectionUI> connections =
            new List<EntrepreneurConnectionUI>();


        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Awake()
        {
            PlayerProgressPoints.onPointsChanged  += OnPointsChanged;
            EntrepreneurTreeManager.onNodeUnlocked += OnNodeUnlocked;
        }


        void OnDestroy()
        {
            PlayerProgressPoints.onPointsChanged  -= OnPointsChanged;
            EntrepreneurTreeManager.onNodeUnlocked -= OnNodeUnlocked;
        }


        void Start()
        {
            BuildTree();
            RefreshPointsDisplay();
        }


        // ── Tree construction ─────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates all node prefabs and connection line prefabs inside <see cref="contentContainer"/>.
        /// Called once on Start; call again only if treeData changes at runtime.
        /// </summary>
        public void BuildTree()
        {
            if (treeData == null)
            {
                Debug.LogWarning("[EntrepreneurTreeUI] treeData is not assigned. Assign an EntrepreneurTreeData asset.");
                return;
            }

            if (nodePrefab == null || connectionPrefab == null)
            {
                Debug.LogWarning("[EntrepreneurTreeUI] nodePrefab or connectionPrefab is not assigned.");
                return;
            }

            ClearTree();

            // ── Instantiate nodes ────────────────────────────────────────────────
            foreach (EntrepreneurNodeData nodeData in treeData.nodes)
            {
                if (nodeData == null) continue;

                GameObject nodeObj = Instantiate(nodePrefab, contentContainer);
                RectTransform rt   = nodeObj.GetComponent<RectTransform>();

                // Anchor top-left, offset from top-left corner of Content.
                rt.anchorMin        = Vector2.up; // (0, 1)
                rt.anchorMax        = Vector2.up;
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = nodeData.uiPosition;

                EntrepreneurNodeUI nodeUI = nodeObj.GetComponent<EntrepreneurNodeUI>();
                nodeUI.Initialize(nodeData, this);
                nodeUIMap[nodeData.id] = nodeUI;
            }

            // ── Instantiate connections (after nodes so local positions are set) ─
            BuildConnections();

            // ── Auto-size Content ─────────────────────────────────────────────────
            AutoSizeContent();
        }


        /// <summary>
        /// Refreshes the color/state of all node visuals and all connection lines.
        /// Call this after any unlock event.
        /// </summary>
        public void RefreshAll()
        {
            foreach (KeyValuePair<string, EntrepreneurNodeUI> pair in nodeUIMap)
                pair.Value?.Refresh();

            RefreshConnections();
        }


        // ── Tooltip ───────────────────────────────────────────────────────────────

        /// <summary>Shows the tooltip for <paramref name="data"/> at <paramref name="worldPos"/>.</summary>
        public void ShowTooltip(EntrepreneurNodeData data, Vector3 worldPos)
        {
            tooltip?.Show(data, worldPos);
        }


        /// <summary>Hides the tooltip.</summary>
        public void HideTooltip()
        {
            tooltip?.Hide();
        }


        // ── Private: tree building ────────────────────────────────────────────────

        private void ClearTree()
        {
            foreach (KeyValuePair<string, EntrepreneurNodeUI> pair in nodeUIMap)
                if (pair.Value != null) Destroy(pair.Value.gameObject);

            foreach (EntrepreneurConnectionUI conn in connections)
                if (conn != null) Destroy(conn.gameObject);

            nodeUIMap.Clear();
            connections.Clear();
        }


        private void BuildConnections()
        {
            foreach (EntrepreneurNodeData nodeData in treeData.nodes)
            {
                if (nodeData == null) continue;
                if (!nodeUIMap.TryGetValue(nodeData.id, out EntrepreneurNodeUI toNode)) continue;

                foreach (string reqId in nodeData.requiredNodeIds)
                {
                    if (string.IsNullOrEmpty(reqId)) continue;
                    if (!nodeUIMap.TryGetValue(reqId, out EntrepreneurNodeUI fromNode)) continue;

                    CreateConnection(
                        fromNode.GetComponent<RectTransform>(),
                        toNode.GetComponent<RectTransform>(),
                        reqId, nodeData.id);
                }
            }
        }


        private void CreateConnection(RectTransform from, RectTransform to,
                                      string fromId, string toId)
        {
            GameObject connObj = Instantiate(connectionPrefab, contentContainer);
            // Render below nodes
            connObj.transform.SetAsFirstSibling();

            EntrepreneurConnectionUI conn = connObj.GetComponent<EntrepreneurConnectionUI>();
            conn.Connect(from, to,
                fromUnlocked: EntrepreneurTreeManager.IsUnlocked(fromId),
                toUnlocked:   EntrepreneurTreeManager.IsUnlocked(toId));

            connections.Add(conn);
        }


        private void RefreshConnections()
        {
            // Destroy old connections and rebuild (cheap enough for a ≤36-node tree).
            foreach (EntrepreneurConnectionUI conn in connections)
                if (conn != null) Destroy(conn.gameObject);

            connections.Clear();
            BuildConnections();
        }


        private void AutoSizeContent()
        {
            if (contentContainer == null || treeData == null) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (EntrepreneurNodeData nodeData in treeData.nodes)
            {
                if (nodeData == null) continue;

                float cx = nodeData.uiPosition.x;
                float cy = nodeData.uiPosition.y; // negative = downward with top-left anchor

                if (cx - nodeSize.x * 0.5f < minX) minX = cx - nodeSize.x * 0.5f;
                if (cx + nodeSize.x * 0.5f > maxX) maxX = cx + nodeSize.x * 0.5f;
                if (cy - nodeSize.y * 0.5f < minY) minY = cy - nodeSize.y * 0.5f;
                if (cy + nodeSize.y * 0.5f > maxY) maxY = cy + nodeSize.y * 0.5f;
            }

            float width  = (maxX - minX) + contentPadding * 2f;
            float height = (maxY - minY) + contentPadding * 2f;

            contentContainer.sizeDelta = new Vector2(
                Mathf.Max(width,  600f),
                Mathf.Max(height, 400f));
        }


        // ── Event callbacks ───────────────────────────────────────────────────────

        private void OnPointsChanged(int points)
        {
            if (pointsText != null)
                pointsText.text = "Puntos: " + points;
        }


        private void OnNodeUnlocked(EntrepreneurNodeData node)
        {
            RefreshAll();

            if (node != null)
                ShowFeedback("¡Desbloqueado: " + node.displayName + "!");
        }


        private void RefreshPointsDisplay()
        {
            if (pointsText != null)
                pointsText.text = "Puntos: " + PlayerProgressPoints.GetPoints();
        }


        // ── Feedback message ──────────────────────────────────────────────────────

        // Active feedback coroutine; kept to allow cancellation when a new message arrives.
        private Coroutine feedbackCoroutine;

        private void ShowFeedback(string msg)
        {
            if (feedbackText == null) return;

            if (feedbackCoroutine != null)
                StopCoroutine(feedbackCoroutine);

            feedbackText.text = msg;
            feedbackCoroutine = StartCoroutine(ClearFeedbackAfterDelay(3f));
        }


        private IEnumerator ClearFeedbackAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (feedbackText != null)
                feedbackText.text = string.Empty;

            feedbackCoroutine = null;
        }
    }
}
