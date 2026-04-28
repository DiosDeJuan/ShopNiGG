using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Bridges the Entrepreneur Skill Tree system to the existing "UPGRADES" tab inside
    /// UIShopDesktop. Builds the entire tree UI programmatically at runtime so no prefab
    /// modifications are needed.
    ///
    /// HOW TO USE:
    ///   1. In your Game scene, create an empty GameObject (e.g. "UpgradesUIController").
    ///   2. Add this component to it.
    ///   3. Optionally assign the shopDesktop field in the Inspector for a direct reference.
    ///      If left empty, the controller finds UIShopDesktop automatically.
    ///   4. Ensure EntrepreneurTreeSystem exists somewhere in the scene (or add it to the
    ///      same object). It will be created automatically if missing.
    ///   5. Press Play. The UPGRADES tab on the in-game computer now shows the tree.
    /// </summary>
    public class UpgradesUIController : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────────
        /// <summary>
        /// Optional: drag the UIShopDesktop prefab instance here.
        /// If null, the controller searches the scene automatically.
        /// </summary>
        public UIShopDesktop shopDesktop;

        // ─── Colours ─────────────────────────────────────────────────────────────
        private static readonly Color COL_BG_PANEL   = new Color(0.07f, 0.11f, 0.17f, 1.00f);
        private static readonly Color COL_BG_HEADER  = new Color(0.04f, 0.07f, 0.13f, 1.00f);
        private static readonly Color COL_BG_INFO    = new Color(0.05f, 0.09f, 0.15f, 1.00f);
        private static readonly Color COL_BG_SCROLL  = new Color(0.03f, 0.06f, 0.10f, 1.00f);
        private static readonly Color COL_BTN_UNLOCK = new Color(0.16f, 0.66f, 0.30f, 1.00f);
        private static readonly Color COL_BTN_LOCKED = new Color(0.28f, 0.30f, 0.35f, 1.00f);
        private static readonly Color COL_TEXT_WHITE = new Color(0.90f, 0.92f, 0.96f, 1.00f);
        private static readonly Color COL_TEXT_DIM   = new Color(0.50f, 0.55f, 0.60f, 1.00f);
        private static readonly Color COL_SEPARATOR  = new Color(0.15f, 0.20f, 0.30f, 1.00f);

        // ─── Layout constants ─────────────────────────────────────────────────────
        private const float NODE_W      = 150f;
        private const float NODE_H      = 60f;
        private const float H_SPACING   = 180f;  // column spacing
        private const float V_SPACING   = 130f;  // row spacing
        private const float CONTENT_PAD = 120f;  // padding around the tree

        // ─── Runtime UI references ────────────────────────────────────────────────
        private TMP_Text pointsLabel;
        private TMP_Text infoNameLabel;
        private TMP_Text infoDescLabel;
        private TMP_Text infoCostLabel;
        private TMP_Text infoReqsLabel;
        private TMP_Text infoStatusLabel;
        private TMP_Text messageLabel;
        private Button unlockButton;
        private TMP_Text unlockButtonLabel;
        private RectTransform nodesLayer;
        private RectTransform linesLayer;
        private RectTransform contentArea;

        // ─── State ────────────────────────────────────────────────────────────────
        private EntrepreneurTreeNodeUI selectedNodeUI;
        private Dictionary<string, EntrepreneurTreeNodeUI> nodeUIs =
            new Dictionary<string, EntrepreneurTreeNodeUI>();
        private List<EntrepreneurTreeConnectionLineUI> lineUIs =
            new List<EntrepreneurTreeConnectionLineUI>();
        private Dictionary<string, Vector2> nodePositions =
            new Dictionary<string, Vector2>();

        // ─── Lifecycle ────────────────────────────────────────────────────────────
        void Start()
        {
            StartCoroutine(BuildAfterFrame());
        }


        //wait one frame to ensure all other systems (UIShopDesktop, EntrepreneurTreeSystem) have run Awake+Start
        private IEnumerator BuildAfterFrame()
        {
            yield return null;

            EnsureTreeSystemExists();

            if (shopDesktop == null)
                shopDesktop = FindObjectOfType<UIShopDesktop>();

            if (shopDesktop == null)
            {
                Debug.LogWarning("[UpgradesUIController] UIShopDesktop not found in scene. Tree UI cannot be built.");
                yield break;
            }

            Transform categories = FindDeepByName(shopDesktop.transform, "Categories");
            if (categories == null)
            {
                Debug.LogWarning("[UpgradesUIController] Could not find 'Categories' container inside UIShopDesktop.");
                yield break;
            }

            //build the tree panel as a new direct child of Categories
            GameObject treeRoot = BuildTreePanel(categories);

            //override the "Button - Upgrades" to show our panel instead of "Expansions"
            WireUpgradesButton(shopDesktop.transform, categories, treeRoot);

            //build node and line geometry
            CalculateNodePositions();
            BuildNodes();
            BuildConnectionLines();
            ResizeContentArea();

            //subscribe to game events
            EntrepreneurTreeSystem.onProgressPointsChanged += OnPointsChanged;
            EntrepreneurTreeSystem.onNodeUnlocked += OnNodeUnlocked;

            //initial UI refresh
            RefreshAllNodeVisuals();
            UpdatePointsLabel();
            ShowPlaceholderInfo();
        }


        void OnDestroy()
        {
            EntrepreneurTreeSystem.onProgressPointsChanged -= OnPointsChanged;
            EntrepreneurTreeSystem.onNodeUnlocked -= OnNodeUnlocked;
        }

        // ─── Panel construction ────────────────────────────────────────────────────

        /// <summary>
        /// Creates the entire EntrepreneurTreeRoot panel hierarchy as a direct child of
        /// the given categories transform. Returns the root GameObject.
        /// </summary>
        private GameObject BuildTreePanel(Transform categories)
        {
            // Root panel - fills Categories area
            GameObject root = CreatePanel(categories, "EntrepreneurTreeRoot",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, COL_BG_PANEL);
            root.SetActive(false); // hidden until the Upgrades button is clicked

            RectTransform rootRT = root.GetComponent<RectTransform>();

            // ── Header ─────────────────────────────────────────────────────────
            GameObject header = CreatePanel(root.transform, "Header",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -50f), new Vector2(0f, 0f), COL_BG_HEADER);

            // Title
            TMP_Text title = CreateLabel(header.transform, "TitleLabel",
                "🌳  Árbol del Emprendedor",
                new Vector2(0f, 0f), new Vector2(0.65f, 1f),
                Vector2.zero, Vector2.zero,
                18f, FontStyles.Bold, COL_TEXT_WHITE, TextAlignmentOptions.MidlineLeft);
            title.margin = new Vector4(12f, 0, 0, 0);

            // Points display
            pointsLabel = CreateLabel(header.transform, "PointsLabel",
                "Puntos disponibles: —",
                new Vector2(0.65f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                13f, FontStyles.Normal, new Color(0.80f, 0.85f, 0.40f, 1f),
                TextAlignmentOptions.MidlineRight);
            pointsLabel.margin = new Vector4(0, 0, 12f, 0);

            // Separator line under header
            CreatePanel(root.transform, "HeaderSeparator",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -52f), new Vector2(0f, -50f), COL_SEPARATOR);

            // ── Main Area (scroll + info) ───────────────────────────────────────
            GameObject mainArea = CreatePanel(root.transform, "MainArea",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, -52f), Color.clear);

            // ── Info Panel (right 32%) ──────────────────────────────────────────
            GameObject infoPanel = CreatePanel(mainArea.transform, "InfoPanel",
                new Vector2(0.68f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, COL_BG_INFO);

            BuildInfoPanel(infoPanel.transform);

            // Vertical separator
            CreatePanel(mainArea.transform, "InfoSeparator",
                new Vector2(0.68f, 0f), new Vector2(0.68f, 1f),
                new Vector2(-1f, 0f), new Vector2(1f, 0f), COL_SEPARATOR);

            // ── Tree Scroll View (left 68%) ─────────────────────────────────────
            GameObject scrollArea = CreatePanel(mainArea.transform, "TreeScrollArea",
                new Vector2(0f, 0f), new Vector2(0.68f, 1f),
                Vector2.zero, Vector2.zero, COL_BG_SCROLL);

            BuildScrollView(scrollArea.transform);

            // ── Message overlay (bottom of root) ───────────────────────────────
            messageLabel = CreateLabel(root.transform, "MessageLabel",
                string.Empty,
                new Vector2(0f, 0f), new Vector2(0.68f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 32f),
                11f, FontStyles.Italic, new Color(1f, 0.85f, 0.25f, 1f),
                TextAlignmentOptions.Center);
            messageLabel.overflowMode = TextOverflowModes.Ellipsis;

            return root;
        }


        private void BuildInfoPanel(Transform parent)
        {
            float y = 0f;
            float padX = 14f;

            // Node name
            infoNameLabel = CreateLabel(parent, "InfoName", "Selecciona un nodo",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -50f), new Vector2(0f, -14f),
                14f, FontStyles.Bold, COL_TEXT_WHITE, TextAlignmentOptions.TopLeft);
            infoNameLabel.margin = new Vector4(padX, 0, padX, 0);
            infoNameLabel.enableWordWrapping = true;

            // Separator
            CreatePanel(parent, "Sep1",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(padX, -52f), new Vector2(-padX, -50f), COL_SEPARATOR);

            // Description
            infoDescLabel = CreateLabel(parent, "InfoDesc", string.Empty,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -130f), new Vector2(0f, -56f),
                11f, FontStyles.Normal, new Color(0.72f, 0.78f, 0.82f, 1f),
                TextAlignmentOptions.TopLeft);
            infoDescLabel.margin = new Vector4(padX, 0, padX, 0);
            infoDescLabel.enableWordWrapping = true;
            infoDescLabel.overflowMode = TextOverflowModes.Ellipsis;

            // Cost label
            infoCostLabel = CreateLabel(parent, "InfoCost", string.Empty,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -152f), new Vector2(0f, -132f),
                11f, FontStyles.Bold, new Color(0.85f, 0.75f, 0.20f, 1f),
                TextAlignmentOptions.TopLeft);
            infoCostLabel.margin = new Vector4(padX, 0, padX, 0);

            // Requirements label
            infoReqsLabel = CreateLabel(parent, "InfoReqs", string.Empty,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -240f), new Vector2(0f, -156f),
                10f, FontStyles.Normal, new Color(0.85f, 0.50f, 0.50f, 1f),
                TextAlignmentOptions.TopLeft);
            infoReqsLabel.margin = new Vector4(padX, 0, padX, 0);
            infoReqsLabel.enableWordWrapping = true;

            // Status label
            infoStatusLabel = CreateLabel(parent, "InfoStatus", string.Empty,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -262f), new Vector2(0f, -242f),
                10f, FontStyles.Italic, new Color(0.50f, 0.80f, 0.55f, 1f),
                TextAlignmentOptions.TopLeft);
            infoStatusLabel.margin = new Vector4(padX, 0, padX, 0);

            // Separator above button
            CreatePanel(parent, "Sep2",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(padX, 52f), new Vector2(-padX, 54f), COL_SEPARATOR);

            // Unlock button
            GameObject btnGO = CreatePanel(parent, "UnlockButton",
                new Vector2(0.1f, 0f), new Vector2(0.9f, 0f),
                new Vector2(0f, 8f), new Vector2(0f, 46f), COL_BTN_LOCKED).gameObject;

            Button btn = btnGO.AddComponent<Button>();
            Image btnImg = btnGO.GetComponent<Image>();
            btn.targetGraphic = btnImg;
            btn.transition = Selectable.Transition.ColorTint;

            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
            cb.pressedColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            btn.colors = cb;

            unlockButtonLabel = CreateLabel(btnGO.transform, "UnlockBtnLabel",
                "Selecciona un nodo",
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                12f, FontStyles.Bold, COL_TEXT_WHITE, TextAlignmentOptions.Center);

            unlockButton = btn;
            unlockButton.onClick.AddListener(OnUnlockButtonClicked);
        }


        private void BuildScrollView(Transform parent)
        {
            // ScrollRect wrapper
            GameObject scrollViewGO = new GameObject("TreeScrollView",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollViewGO.transform.SetParent(parent, false);

            RectTransform scrollRT = scrollViewGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;

            scrollViewGO.GetComponent<Image>().color = Color.clear;

            ScrollRect scrollRect = scrollViewGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject viewportGO = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollViewGO.transform, false);

            RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewportRT.pivot = new Vector2(0f, 1f);

            viewportGO.GetComponent<Mask>().showMaskGraphic = false;
            viewportGO.GetComponent<Image>().color = Color.white; // mask needs a graphic

            // Content area
            GameObject contentGO = new GameObject("ContentArea", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);

            contentArea = contentGO.GetComponent<RectTransform>();
            contentArea.anchorMin = new Vector2(0f, 1f);
            contentArea.anchorMax = new Vector2(0f, 1f);
            contentArea.pivot = new Vector2(0f, 1f);
            contentArea.anchoredPosition = Vector2.zero;
            contentArea.sizeDelta = new Vector2(2200f, 1600f); // will be resized after layout

            scrollRect.viewport = viewportRT;
            scrollRect.content = contentArea;

            // Lines layer (behind nodes)
            GameObject linesGO = new GameObject("LinesLayer", typeof(RectTransform));
            linesGO.transform.SetParent(contentArea, false);
            linesLayer = linesGO.GetComponent<RectTransform>();
            linesLayer.anchorMin = Vector2.zero;
            linesLayer.anchorMax = Vector2.one;
            linesLayer.offsetMin = Vector2.zero;
            linesLayer.offsetMax = Vector2.zero;

            // Nodes layer (above lines)
            GameObject nodesGO = new GameObject("NodesLayer", typeof(RectTransform));
            nodesGO.transform.SetParent(contentArea, false);
            nodesLayer = nodesGO.GetComponent<RectTransform>();
            nodesLayer.anchorMin = Vector2.zero;
            nodesLayer.anchorMax = Vector2.one;
            nodesLayer.offsetMin = Vector2.zero;
            nodesLayer.offsetMax = Vector2.zero;
        }

        // ─── Layout ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes pixel positions for every node using BFS depth assignment.
        /// Nodes at the same depth are centred horizontally.
        /// </summary>
        private void CalculateNodePositions()
        {
            if (EntrepreneurTreeSystem.Instance == null) return;

            List<SkillTreeNode> allNodes = EntrepreneurTreeSystem.Instance.nodes;
            nodePositions.Clear();

            // ── 1. Assign depth via BFS ─────────────────────────────────────────
            var depths = new Dictionary<string, int>();
            var childrenOf = new Dictionary<string, List<string>>();

            foreach (SkillTreeNode n in allNodes)
            {
                if (!childrenOf.ContainsKey(n.id))
                    childrenOf[n.id] = new List<string>();

                foreach (string prereqId in n.prerequisites)
                {
                    if (string.IsNullOrEmpty(prereqId)) continue;
                    if (!childrenOf.ContainsKey(prereqId))
                        childrenOf[prereqId] = new List<string>();
                    childrenOf[prereqId].Add(n.id);
                }
            }

            var queue = new Queue<string>();
            foreach (SkillTreeNode n in allNodes)
            {
                bool hasPrereqs = false;
                foreach (string p in n.prerequisites)
                    if (!string.IsNullOrEmpty(p)) { hasPrereqs = true; break; }

                if (!hasPrereqs)
                {
                    depths[n.id] = 0;
                    queue.Enqueue(n.id);
                }
            }

            // Handle nodes not reachable from roots (circular or disconnected)
            int maxGuard = allNodes.Count * 2;
            int guard = 0;
            while (queue.Count > 0 && guard++ < maxGuard)
            {
                string cur = queue.Dequeue();
                if (!depths.ContainsKey(cur)) depths[cur] = 0;
                int curDepth = depths[cur];

                if (!childrenOf.ContainsKey(cur)) continue;
                foreach (string child in childrenOf[cur])
                {
                    int newDepth = curDepth + 1;
                    if (!depths.ContainsKey(child) || depths[child] < newDepth)
                    {
                        depths[child] = newDepth;
                        queue.Enqueue(child);
                    }
                }
            }

            // ── 2. Group nodes by depth ─────────────────────────────────────────
            var byLevel = new Dictionary<int, List<SkillTreeNode>>();
            int maxDepth = 0;
            foreach (SkillTreeNode n in allNodes)
            {
                int d = depths.ContainsKey(n.id) ? depths[n.id] : 0;
                if (!byLevel.ContainsKey(d)) byLevel[d] = new List<SkillTreeNode>();
                byLevel[d].Add(n);
                maxDepth = Mathf.Max(maxDepth, d);
            }

            // ── 3. Calculate pixel positions ────────────────────────────────────
            // sort each level: Products first, then Employees, then Upgrades/Security
            for (int lvl = 0; lvl <= maxDepth; lvl++)
            {
                if (!byLevel.ContainsKey(lvl)) continue;
                byLevel[lvl].Sort((a, b) =>
                {
                    int catOrderA = (a.category == SkillTreeCategory.Product) ? 0
                                  : (a.category == SkillTreeCategory.Employee) ? 1 : 2;
                    int catOrderB = (b.category == SkillTreeCategory.Product) ? 0
                                  : (b.category == SkillTreeCategory.Employee) ? 1 : 2;
                    int diff = catOrderA - catOrderB;
                    return diff != 0 ? diff : string.Compare(a.id, b.id, System.StringComparison.Ordinal);
                });

                List<SkillTreeNode> levelNodes = byLevel[lvl];
                float totalW = (levelNodes.Count - 1) * H_SPACING;
                float startX = CONTENT_PAD + (GetMaxColumnsInTree(byLevel, maxDepth) * H_SPACING - totalW) * 0.5f;

                for (int i = 0; i < levelNodes.Count; i++)
                {
                    float x = startX + i * H_SPACING;
                    float y = -(CONTENT_PAD + lvl * V_SPACING);
                    nodePositions[levelNodes[i].id] = new Vector2(x, y);
                }
            }
        }


        private int GetMaxColumnsInTree(Dictionary<int, List<SkillTreeNode>> byLevel, int maxDepth)
        {
            int max = 1;
            for (int i = 0; i <= maxDepth; i++)
                if (byLevel.ContainsKey(i)) max = Mathf.Max(max, byLevel[i].Count);
            return max;
        }


        private void ResizeContentArea()
        {
            if (contentArea == null || nodePositions.Count == 0) return;

            float maxX = 0f, maxY = 0f;
            foreach (Vector2 p in nodePositions.Values)
            {
                maxX = Mathf.Max(maxX, p.x);
                maxY = Mathf.Max(maxY, Mathf.Abs(p.y));
            }

            contentArea.sizeDelta = new Vector2(
                maxX + NODE_W + CONTENT_PAD,
                maxY + NODE_H + CONTENT_PAD
            );
        }

        // ─── Node creation ────────────────────────────────────────────────────────

        private void BuildNodes()
        {
            if (EntrepreneurTreeSystem.Instance == null) return;

            foreach (SkillTreeNode node in EntrepreneurTreeSystem.Instance.nodes)
            {
                if (!nodePositions.ContainsKey(node.id)) continue;
                Vector2 pos = nodePositions[node.id];
                EntrepreneurTreeNodeUI ui = CreateNodeUI(node, pos);
                nodeUIs[node.id] = ui;
            }
        }


        private EntrepreneurTreeNodeUI CreateNodeUI(SkillTreeNode node, Vector2 position)
        {
            // Root GO
            GameObject nodeGO = new GameObject("Node_" + node.id,
                typeof(RectTransform), typeof(CanvasGroup));
            nodeGO.transform.SetParent(nodesLayer, false);

            RectTransform nodeRT = nodeGO.GetComponent<RectTransform>();
            nodeRT.anchorMin = new Vector2(0f, 1f);
            nodeRT.anchorMax = new Vector2(0f, 1f);
            nodeRT.pivot = new Vector2(0.5f, 0.5f);
            nodeRT.sizeDelta = new Vector2(NODE_W, NODE_H);
            // position.y is already negative (distance from top)
            nodeRT.anchoredPosition = new Vector2(position.x + NODE_W * 0.5f, position.y - NODE_H * 0.5f);

            // Background image
            Image bgImage = nodeGO.AddComponent<Image>();

            // Category stripe (4px left border)
            GameObject stripeGO = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripeGO.transform.SetParent(nodeGO.transform, false);
            RectTransform stripeRT = stripeGO.GetComponent<RectTransform>();
            stripeRT.anchorMin = new Vector2(0f, 0f);
            stripeRT.anchorMax = new Vector2(0f, 1f);
            stripeRT.offsetMin = Vector2.zero;
            stripeRT.offsetMax = new Vector2(4f, 0f);
            Image stripeImg = stripeGO.GetComponent<Image>();

            // Border (outer outline)
            GameObject borderGO = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGO.transform.SetParent(nodeGO.transform, false);
            RectTransform borderRT = borderGO.GetComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = Vector2.zero;
            borderRT.offsetMax = Vector2.zero;
            Image borderImg = borderGO.GetComponent<Image>();
            borderImg.color = new Color(0f, 0f, 0f, 0.3f);

            // Truncate long titles to fit
            string displayTitle = node.title.Length > 22
                ? node.title.Substring(0, 20) + "…"
                : node.title;

            // Title label
            TMP_Text titleText = CreateLabel(nodeGO.transform, "TitleLabel",
                displayTitle,
                new Vector2(0.04f, 0.30f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                9.5f, FontStyles.Bold, Color.white, TextAlignmentOptions.TopLeft);
            titleText.enableWordWrapping = true;
            titleText.overflowMode = TextOverflowModes.Ellipsis;

            // Status icon (bottom-right)
            TMP_Text statusText = CreateLabel(nodeGO.transform, "StatusLabel",
                string.Empty,
                new Vector2(0f, 0f), new Vector2(1f, 0.35f),
                Vector2.zero, Vector2.zero,
                10f, FontStyles.Normal, Color.white, TextAlignmentOptions.BottomRight);
            statusText.margin = new Vector4(0, 0, 4f, 2f);

            // Hover overlay
            GameObject hoverGO = new GameObject("HoverOverlay", typeof(RectTransform), typeof(Image));
            hoverGO.transform.SetParent(nodeGO.transform, false);
            RectTransform hoverRT = hoverGO.GetComponent<RectTransform>();
            hoverRT.anchorMin = Vector2.zero;
            hoverRT.anchorMax = Vector2.one;
            hoverRT.offsetMin = Vector2.zero;
            hoverRT.offsetMax = Vector2.zero;
            Image hoverImg = hoverGO.GetComponent<Image>();
            hoverImg.color = new Color(1f, 1f, 1f, 0.10f);
            hoverGO.SetActive(false);

            // Add GraphicRaycaster-compatible component
            nodeGO.AddComponent<EntrepreneurTreeNodeUI>().Initialize(
                node, bgImage, borderImg, stripeImg, titleText, statusText, hoverImg,
                OnNodeClicked);

            return nodeGO.GetComponent<EntrepreneurTreeNodeUI>();
        }

        // ─── Connection Lines ─────────────────────────────────────────────────────

        private void BuildConnectionLines()
        {
            if (EntrepreneurTreeSystem.Instance == null) return;

            foreach (SkillTreeNode node in EntrepreneurTreeSystem.Instance.nodes)
            {
                if (!nodePositions.ContainsKey(node.id)) continue;
                Vector2 toPos = NodeCenter(node.id);

                foreach (string prereqId in node.prerequisites)
                {
                    if (string.IsNullOrEmpty(prereqId) || !nodePositions.ContainsKey(prereqId)) continue;
                    Vector2 fromPos = NodeCenter(prereqId);

                    CreateConnectionLine(prereqId, node.id, fromPos, toPos);
                }
            }
        }


        private void CreateConnectionLine(string fromId, string toId, Vector2 fromPos, Vector2 toPos)
        {
            GameObject lineGO = new GameObject("Line_" + fromId + "_to_" + toId,
                typeof(RectTransform), typeof(Image));
            lineGO.transform.SetParent(linesLayer, false);

            RectTransform rt = lineGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = lineGO.GetComponent<Image>();

            EntrepreneurTreeConnectionLineUI lineUI = lineGO.AddComponent<EntrepreneurTreeConnectionLineUI>();
            lineUI.Initialize(fromId, toId, fromPos, toPos, img);
            lineUIs.Add(lineUI);
        }


        private Vector2 NodeCenter(string nodeId)
        {
            if (!nodePositions.ContainsKey(nodeId)) return Vector2.zero;
            Vector2 p = nodePositions[nodeId];
            return new Vector2(p.x + NODE_W * 0.5f, p.y - NODE_H * 0.5f);
        }

        // ─── Interaction ──────────────────────────────────────────────────────────

        private void OnNodeClicked(EntrepreneurTreeNodeUI clickedUI)
        {
            if (selectedNodeUI != null)
                selectedNodeUI.SetSelected(false);

            selectedNodeUI = clickedUI;
            selectedNodeUI.SetSelected(true);

            ShowNodeInfo(clickedUI.node);
            HideMessage();
        }


        private void ShowNodeInfo(SkillTreeNode node)
        {
            infoNameLabel.text = node.title;
            infoDescLabel.text = node.description;

            infoCostLabel.text = node.pointCost == 0
                ? "Coste: Gratis"
                : "Coste: " + node.pointCost + " punto(s) de progreso";

            // Requirements
            if (node.prerequisites.Count == 0 ||
                (node.prerequisites.Count == 1 && string.IsNullOrEmpty(node.prerequisites[0])))
            {
                infoReqsLabel.text = "Sin requisitos previos";
                infoReqsLabel.color = new Color(0.50f, 0.80f, 0.55f, 1f);
            }
            else
            {
                var lines = new System.Text.StringBuilder("Requisitos:\n");
                foreach (string prereqId in node.prerequisites)
                {
                    if (string.IsNullOrEmpty(prereqId)) continue;
                    bool met = EntrepreneurTreeSystem.IsNodeUnlocked(prereqId);
                    SkillTreeNode prereq = EntrepreneurTreeSystem.GetNode(prereqId);
                    string name = prereq != null ? prereq.title : prereqId;
                    lines.AppendLine((met ? "✓ " : "✗ ") + name);
                }
                infoReqsLabel.text = lines.ToString().TrimEnd();
                infoReqsLabel.color = new Color(0.85f, 0.50f, 0.50f, 1f);
            }

            // Status
            if (node.isUnlocked)
            {
                infoStatusLabel.text = "Estado: Desbloqueado ✓";
                infoStatusLabel.color = new Color(0.30f, 0.85f, 0.50f, 1f);
                SetUnlockButtonState(false, "Ya desbloqueado");
                return;
            }

            string blockReason = EntrepreneurTreeSystem.GetUnlockBlockReason(node.id);
            if (blockReason == null)
            {
                infoStatusLabel.text = "Estado: Disponible";
                infoStatusLabel.color = new Color(0.85f, 0.78f, 0.20f, 1f);
            string costLabel = node.pointCost == 0
                ? "Desbloquear (gratis)"
                : "Desbloquear (" + node.pointCost + " pto)";
            SetUnlockButtonState(true, costLabel);
            }
            else
            {
                infoStatusLabel.text = "Estado: Bloqueado";
                infoStatusLabel.color = COL_TEXT_DIM;
                SetUnlockButtonState(false, "Bloqueado");
            }
        }


        private void ShowPlaceholderInfo()
        {
            infoNameLabel.text = "Selecciona un nodo";
            infoDescLabel.text = "Haz click en un nodo del árbol para ver sus detalles.";
            infoCostLabel.text = string.Empty;
            infoReqsLabel.text = string.Empty;
            infoStatusLabel.text = string.Empty;
            SetUnlockButtonState(false, "Selecciona un nodo");
        }


        private void SetUnlockButtonState(bool enabled, string label)
        {
            if (unlockButton == null) return;
            unlockButton.interactable = enabled;
            unlockButton.GetComponent<Image>().color = enabled ? COL_BTN_UNLOCK : COL_BTN_LOCKED;
            if (unlockButtonLabel != null) unlockButtonLabel.text = label;
        }


        private void OnUnlockButtonClicked()
        {
            if (selectedNodeUI == null) return;

            bool success = EntrepreneurTreeSystem.TryUnlockNode(selectedNodeUI.node.id);
            if (!success)
            {
                string reason = EntrepreneurTreeSystem.GetUnlockBlockReason(selectedNodeUI.node.id);
                ShowMessage(reason ?? "No se puede desbloquear.");
            }
        }

        // ─── Event handlers ───────────────────────────────────────────────────────

        private void OnPointsChanged(int points)
        {
            UpdatePointsLabel();
        }


        private void OnNodeUnlocked(SkillTreeNode node)
        {
            RefreshAllNodeVisuals();
            RefreshAllLines();

            // Update info panel if the unlocked node is selected
            if (selectedNodeUI != null && selectedNodeUI.node.id == node.id)
                ShowNodeInfo(selectedNodeUI.node);
        }


        private void UpdatePointsLabel()
        {
            if (pointsLabel == null || EntrepreneurTreeSystem.Instance == null) return;
            int pts = EntrepreneurTreeSystem.Instance.progressPoints;
            pointsLabel.text = "Puntos disponibles: " + pts;
        }


        private void RefreshAllNodeVisuals()
        {
            foreach (EntrepreneurTreeNodeUI ui in nodeUIs.Values)
                ui.RefreshVisuals();
        }


        private void RefreshAllLines()
        {
            foreach (EntrepreneurTreeConnectionLineUI line in lineUIs)
                line.RefreshColor();
        }


        private void ShowMessage(string text)
        {
            if (messageLabel == null) return;
            messageLabel.text = text;
            StopAllCoroutines();
            StartCoroutine(FadeOutMessage(4f));
        }


        private void HideMessage()
        {
            if (messageLabel == null) return;
            messageLabel.text = string.Empty;
        }


        private IEnumerator FadeOutMessage(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (messageLabel != null) messageLabel.text = string.Empty;
        }

        // ─── Button wiring ────────────────────────────────────────────────────────

        /// <summary>
        /// Finds "Button - Upgrades" in the taskbar/desktop hierarchy and replaces
        /// its onClick to show EntrepreneurTreeRoot instead of the old Expansions panel.
        /// </summary>
        private void WireUpgradesButton(Transform desktopRoot, Transform categories, GameObject treeRoot)
        {
            Button upgradesBtn = FindButtonByName(desktopRoot, "Button - Upgrades");
            if (upgradesBtn == null)
            {
                Debug.LogWarning("[UpgradesUIController] 'Button - Upgrades' not found. Wire manually.");
                return;
            }

            UIShopCategoryHelper helper = categories.GetComponent<UIShopCategoryHelper>();
            if (helper == null)
            {
                Debug.LogWarning("[UpgradesUIController] UIShopCategoryHelper not found on Categories.");
                return;
            }

            // Replace ALL onClick listeners (including persistent prefab ones) with our own.
            // This redirects the Upgrades button from the old "Expansions" panel to the tree.
            upgradesBtn.onClick = new Button.ButtonClickedEvent();
            upgradesBtn.onClick.AddListener(() =>
            {
                helper.Show(treeRoot);
                RefreshAllNodeVisuals();
                RefreshAllLines();
                UpdatePointsLabel();
            });
        }

        // ─── Scene helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Ensures EntrepreneurTreeSystem exists in the scene; creates it if missing.
        /// </summary>
        private void EnsureTreeSystemExists()
        {
            if (EntrepreneurTreeSystem.Instance != null) return;
            GameObject go = new GameObject("EntrepreneurTreeSystem");
            go.AddComponent<EntrepreneurTreeSystem>();
            Debug.Log("[UpgradesUIController] Created EntrepreneurTreeSystem automatically.");
        }


        private Transform FindDeepByName(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDeepByName(root.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }


        private Button FindButtonByName(Transform root, string name)
        {
            Transform t = FindDeepByName(root, name);
            return t != null ? t.GetComponent<Button>() : null;
        }

        // ─── UI builder helpers ───────────────────────────────────────────────────

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            go.GetComponent<Image>().color = color;
            return go;
        }


        private static TMP_Text CreateLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            float fontSize, FontStyles fontStyle, Color color,
            TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = false;

            return tmp;
        }
    }
}
