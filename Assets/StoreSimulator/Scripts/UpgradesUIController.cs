using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Integrates the Entrepreneur Tree into the existing computer UPGRADES flow.
    /// Keeps access to the original Expansions panel and provides robust fallback lookup.
    /// </summary>
    public class UpgradesUIController : MonoBehaviour
    {
        private const string LOG_PREFIX = "[EntrepreneurTree] ";

        [Header("Optional Inspector References")]
        public UIShopDesktop shopDesktop;
        public Button upgradesButton;
        public GameObject upgradesRootPanel;
        public GameObject originalExpansionsPanel;
        public Transform treeRootParent;

        private static readonly Color COL_BG_PANEL = new Color(0.07f, 0.11f, 0.17f, 1.00f);
        private static readonly Color COL_BG_HEADER = new Color(0.04f, 0.07f, 0.13f, 1.00f);
        private static readonly Color COL_BG_INFO = new Color(0.05f, 0.09f, 0.15f, 1.00f);
        private static readonly Color COL_BG_SCROLL = new Color(0.03f, 0.06f, 0.10f, 1.00f);
        private static readonly Color COL_BTN_UNLOCK = new Color(0.16f, 0.66f, 0.30f, 1.00f);
        private static readonly Color COL_BTN_LOCKED = new Color(0.28f, 0.30f, 0.35f, 1.00f);
        private static readonly Color COL_TEXT_WHITE = new Color(0.90f, 0.92f, 0.96f, 1.00f);
        private static readonly Color COL_TEXT_DIM = new Color(0.50f, 0.55f, 0.60f, 1.00f);
        private static readonly Color COL_SEPARATOR = new Color(0.15f, 0.20f, 0.30f, 1.00f);

        private const float NODE_W = 165f;
        private const float NODE_H = 66f;
        private const float H_SPACING = 210f;
        private const float V_SPACING = 128f;
        private const float CONTENT_PAD = 110f;

        private GameObject treeRoot;
        private RectTransform treeContentPanel;
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
        private Button treeTabButton;
        private Button expansionsTabButton;
        private Button expansionsReturnButton;

        private bool initialized;
        private EntrepreneurTreeNodeUI selectedNodeUI;
        private readonly Dictionary<string, EntrepreneurTreeNodeUI> nodeUIs = new Dictionary<string, EntrepreneurTreeNodeUI>();
        private readonly List<EntrepreneurTreeConnectionLineUI> lineUIs = new List<EntrepreneurTreeConnectionLineUI>();
        private readonly Dictionary<string, Vector2> nodePositions = new Dictionary<string, Vector2>();
        private Coroutine messageCoroutine;

        void Awake()
        {
            EnsureTreeSystemExists();
            ResolveReferences();
        }

        void Start()
        {
            StartCoroutine(InitializeAfterFrame());
        }

        void OnDestroy()
        {
            EntrepreneurTreeSystem.OnProgressPointsChanged -= OnPointsChanged;
            EntrepreneurTreeSystem.onNodeUnlocked -= OnNodeUnlocked;
        }

        private IEnumerator InitializeAfterFrame()
        {
            yield return null;

            if (initialized || !ValidateReferences())
                yield break;

            treeRoot = FindOrCreateTreeRoot();
            if (treeRoot == null)
            {
                Debug.LogWarning(LOG_PREFIX + "No se pudo crear/encontrar EntrepreneurTreeRoot.");
                yield break;
            }

            BuildTreeUIIfNeeded(treeRoot);
            BuildGraphIfNeeded();
            WireUpgradesButton();
            WireSubtabsAndFallbacks();

            EntrepreneurTreeSystem.OnProgressPointsChanged -= OnPointsChanged;
            EntrepreneurTreeSystem.onNodeUnlocked -= OnNodeUnlocked;
            EntrepreneurTreeSystem.OnProgressPointsChanged += OnPointsChanged;
            EntrepreneurTreeSystem.onNodeUnlocked += OnNodeUnlocked;

            RefreshAllNodeVisuals();
            RefreshAllLines();
            UpdatePointsLabel();
            ShowPlaceholderInfo();
            initialized = true;
        }

        private void ResolveReferences()
        {
            if (shopDesktop == null)
            {
                shopDesktop = FindObjectOfType<UIShopDesktop>();
                if (shopDesktop == null)
                    Debug.LogWarning(LOG_PREFIX + "No se encontró UIShopDesktop (fallback FindObjectOfType falló).");
                else
                    Debug.Log(LOG_PREFIX + "UIShopDesktop resuelto por fallback automático.");
            }

            if (upgradesRootPanel == null && shopDesktop != null)
            {
                UIShopCategoryHelper helper = shopDesktop.GetComponentInChildren<UIShopCategoryHelper>(true);
                if (helper != null)
                {
                    upgradesRootPanel = helper.gameObject;
                    Debug.Log(LOG_PREFIX + "Usando fallback: contenedor UPGRADES = UIShopCategoryHelper.");
                }
            }

            if (treeRootParent == null && upgradesRootPanel != null)
                treeRootParent = upgradesRootPanel.transform;

            if (upgradesButton == null && shopDesktop != null)
                upgradesButton = FindButtonByName(shopDesktop.transform, "Button - Upgrades");

            if (originalExpansionsPanel == null && upgradesRootPanel != null)
                originalExpansionsPanel = ResolveOriginalExpansionsPanel(upgradesRootPanel.transform);
        }

        private bool ValidateReferences()
        {
            if (shopDesktop == null)
            {
                Debug.LogWarning(LOG_PREFIX + "No se encontró UIShopDesktop. Abortando inicialización del árbol.");
                return false;
            }

            if (upgradesRootPanel == null)
            {
                Debug.LogWarning(LOG_PREFIX + "No se encontró el contenedor de UPGRADES.");
                return false;
            }

            if (treeRootParent == null)
            {
                Debug.LogWarning(LOG_PREFIX + "No se encontró treeRootParent. Usando fallback con contenedor UPGRADES.");
                treeRootParent = upgradesRootPanel.transform;
            }

            if (upgradesButton == null)
                Debug.LogWarning(LOG_PREFIX + "No se encontró Button - Upgrades. Debe asignarse manualmente en Inspector.");

            if (originalExpansionsPanel == null)
                Debug.LogWarning(LOG_PREFIX + "No se encontró panel de Expansions. Se mantiene árbol funcional pero sin subtab Expansiones.");

            return true;
        }

        private GameObject ResolveOriginalExpansionsPanel(Transform root)
        {
            UIShopCategory[] categories = root.GetComponentsInChildren<UIShopCategory>(true);
            foreach (UIShopCategory category in categories)
            {
                if (category != null && category.purchasable is ExpansionScriptableObject)
                    return category.gameObject;
            }

            Transform namedFallback = FindDeepByName(root, "Expansions");
            if (namedFallback != null)
            {
                Debug.Log(LOG_PREFIX + "Usando fallback por nombre para Expansions.");
                return namedFallback.gameObject;
            }

            return null;
        }

        private GameObject FindOrCreateTreeRoot()
        {
            Transform existing = FindDeepByName(treeRootParent, "EntrepreneurTreeRoot");
            if (existing != null)
                return existing.gameObject;

            GameObject root = CreatePanel(treeRootParent, "EntrepreneurTreeRoot",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, COL_BG_PANEL);
            root.SetActive(false);
            return root;
        }

        private void BuildTreeUIIfNeeded(GameObject root)
        {
            Transform main = FindDeepByName(root.transform, "MainArea");
            if (main != null)
            {
                CacheExistingUIReferences(root);
                return;
            }

            GameObject header = CreatePanel(root.transform, "Header",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -54f), new Vector2(0f, 0f), COL_BG_HEADER);

            TMP_Text title = CreateLabel(header.transform, "TitleLabel", "Árbol del Emprendedor",
                new Vector2(0f, 0f), new Vector2(0.42f, 1f),
                Vector2.zero, Vector2.zero, 16f, FontStyles.Bold, COL_TEXT_WHITE, TextAlignmentOptions.MidlineLeft);
            title.margin = new Vector4(12f, 0, 0, 0);

            treeTabButton = CreateTabButton(header.transform, "TreeTabButton", "Árbol",
                new Vector2(0.43f, 0.16f), new Vector2(0.60f, 0.84f));
            expansionsTabButton = CreateTabButton(header.transform, "ExpansionsTabButton", "Expansiones",
                new Vector2(0.61f, 0.16f), new Vector2(0.82f, 0.84f));

            pointsLabel = CreateLabel(header.transform, "PointsLabel", "Puntos disponibles: —",
                new Vector2(0.82f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, 12f, FontStyles.Bold, new Color(0.88f, 0.82f, 0.22f, 1f), TextAlignmentOptions.MidlineRight);
            pointsLabel.margin = new Vector4(0, 0, 12f, 0);

            CreatePanel(root.transform, "HeaderSeparator",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -56f), new Vector2(0f, -54f), COL_SEPARATOR);

            GameObject mainArea = CreatePanel(root.transform, "MainArea",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, -56f), Color.clear);

            treeContentPanel = CreatePanel(mainArea.transform, "TreeContentPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear).GetComponent<RectTransform>();

            GameObject infoPanel = CreatePanel(treeContentPanel, "InfoPanel",
                new Vector2(0.67f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, COL_BG_INFO);
            BuildInfoPanel(infoPanel.transform);

            CreatePanel(treeContentPanel, "InfoSeparator",
                new Vector2(0.67f, 0f), new Vector2(0.67f, 1f),
                new Vector2(-1f, 0f), new Vector2(1f, 0f), COL_SEPARATOR);

            GameObject scrollArea = CreatePanel(treeContentPanel, "TreeScrollArea",
                new Vector2(0f, 0f), new Vector2(0.67f, 1f), Vector2.zero, Vector2.zero, COL_BG_SCROLL);
            BuildScrollView(scrollArea.transform);

            messageLabel = CreateLabel(root.transform, "MessageLabel", string.Empty,
                new Vector2(0f, 0f), new Vector2(0.67f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 30f), 11f, FontStyles.Italic,
                new Color(1f, 0.82f, 0.20f, 1f), TextAlignmentOptions.Center);
            messageLabel.overflowMode = TextOverflowModes.Ellipsis;

            WireTreeTabButtons();
        }

        private void CacheExistingUIReferences(GameObject root)
        {
            pointsLabel = FindDeepByName(root.transform, "PointsLabel")?.GetComponent<TMP_Text>();
            infoNameLabel = FindDeepByName(root.transform, "InfoName")?.GetComponent<TMP_Text>();
            infoDescLabel = FindDeepByName(root.transform, "InfoDesc")?.GetComponent<TMP_Text>();
            infoCostLabel = FindDeepByName(root.transform, "InfoCost")?.GetComponent<TMP_Text>();
            infoReqsLabel = FindDeepByName(root.transform, "InfoReqs")?.GetComponent<TMP_Text>();
            infoStatusLabel = FindDeepByName(root.transform, "InfoStatus")?.GetComponent<TMP_Text>();
            messageLabel = FindDeepByName(root.transform, "MessageLabel")?.GetComponent<TMP_Text>();
            unlockButton = FindDeepByName(root.transform, "UnlockButton")?.GetComponent<Button>();
            unlockButtonLabel = FindDeepByName(root.transform, "UnlockBtnLabel")?.GetComponent<TMP_Text>();
            nodesLayer = FindDeepByName(root.transform, "NodesLayer")?.GetComponent<RectTransform>();
            linesLayer = FindDeepByName(root.transform, "LinesLayer")?.GetComponent<RectTransform>();
            contentArea = FindDeepByName(root.transform, "ContentArea")?.GetComponent<RectTransform>();
            treeContentPanel = FindDeepByName(root.transform, "TreeContentPanel")?.GetComponent<RectTransform>();
            treeTabButton = FindDeepByName(root.transform, "TreeTabButton")?.GetComponent<Button>();
            expansionsTabButton = FindDeepByName(root.transform, "ExpansionsTabButton")?.GetComponent<Button>();

            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveAllListeners();
                unlockButton.onClick.AddListener(OnUnlockButtonClicked);
            }

            WireTreeTabButtons();
        }

        private void BuildInfoPanel(Transform parent)
        {
            const float padX = 14f;

            infoNameLabel = CreateLabel(parent, "InfoName", "Selecciona un nodo",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -52f), new Vector2(0f, -14f), 14f, FontStyles.Bold, COL_TEXT_WHITE, TextAlignmentOptions.TopLeft);
            infoNameLabel.margin = new Vector4(padX, 0, padX, 0);
            infoNameLabel.enableWordWrapping = true;

            CreatePanel(parent, "Sep1",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(padX, -54f), new Vector2(-padX, -52f), COL_SEPARATOR);

            infoDescLabel = CreateLabel(parent, "InfoDesc", string.Empty,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -130f), new Vector2(0f, -58f), 11f, FontStyles.Normal,
                new Color(0.72f, 0.78f, 0.82f, 1f), TextAlignmentOptions.TopLeft);
            infoDescLabel.margin = new Vector4(padX, 0, padX, 0);
            infoDescLabel.enableWordWrapping = true;
            infoDescLabel.overflowMode = TextOverflowModes.Ellipsis;

            infoCostLabel = CreateLabel(parent, "InfoCost", string.Empty,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -154f), new Vector2(0f, -132f), 11f, FontStyles.Bold,
                new Color(0.85f, 0.75f, 0.20f, 1f), TextAlignmentOptions.TopLeft);
            infoCostLabel.margin = new Vector4(padX, 0, padX, 0);

            infoReqsLabel = CreateLabel(parent, "InfoReqs", string.Empty,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -244f), new Vector2(0f, -158f), 10f, FontStyles.Normal,
                new Color(0.85f, 0.50f, 0.50f, 1f), TextAlignmentOptions.TopLeft);
            infoReqsLabel.margin = new Vector4(padX, 0, padX, 0);
            infoReqsLabel.enableWordWrapping = true;

            infoStatusLabel = CreateLabel(parent, "InfoStatus", string.Empty,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -266f), new Vector2(0f, -246f), 10f, FontStyles.Italic,
                new Color(0.50f, 0.80f, 0.55f, 1f), TextAlignmentOptions.TopLeft);
            infoStatusLabel.margin = new Vector4(padX, 0, padX, 0);

            CreatePanel(parent, "Sep2",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(padX, 52f), new Vector2(-padX, 54f), COL_SEPARATOR);

            GameObject btnGO = CreatePanel(parent, "UnlockButton",
                new Vector2(0.1f, 0f), new Vector2(0.9f, 0f),
                new Vector2(0f, 8f), new Vector2(0f, 46f), COL_BTN_LOCKED);

            unlockButton = btnGO.AddComponent<Button>();
            Image btnImg = btnGO.GetComponent<Image>();
            unlockButton.targetGraphic = btnImg;
            unlockButton.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = unlockButton.colors;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
            cb.pressedColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            unlockButton.colors = cb;

            unlockButtonLabel = CreateLabel(btnGO.transform, "UnlockBtnLabel", "Selecciona un nodo",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 12f, FontStyles.Bold, COL_TEXT_WHITE, TextAlignmentOptions.Center);

            unlockButton.onClick.AddListener(OnUnlockButtonClicked);
        }

        private void BuildScrollView(Transform parent)
        {
            GameObject scrollViewGO = new GameObject("TreeScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
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
            scrollRect.scrollSensitivity = 22f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollViewGO.transform, false);
            RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewportRT.pivot = new Vector2(0f, 1f);
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;
            viewportGO.GetComponent<Image>().color = Color.white;

            GameObject contentGO = new GameObject("ContentArea", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            contentArea = contentGO.GetComponent<RectTransform>();
            contentArea.anchorMin = new Vector2(0f, 1f);
            contentArea.anchorMax = new Vector2(0f, 1f);
            contentArea.pivot = new Vector2(0f, 1f);
            contentArea.anchoredPosition = Vector2.zero;
            contentArea.sizeDelta = new Vector2(2200f, 1600f);

            scrollRect.viewport = viewportRT;
            scrollRect.content = contentArea;

            GameObject linesGO = new GameObject("LinesLayer", typeof(RectTransform));
            linesGO.transform.SetParent(contentArea, false);
            linesLayer = linesGO.GetComponent<RectTransform>();
            linesLayer.anchorMin = Vector2.zero;
            linesLayer.anchorMax = Vector2.one;
            linesLayer.offsetMin = Vector2.zero;
            linesLayer.offsetMax = Vector2.zero;

            GameObject nodesGO = new GameObject("NodesLayer", typeof(RectTransform));
            nodesGO.transform.SetParent(contentArea, false);
            nodesLayer = nodesGO.GetComponent<RectTransform>();
            nodesLayer.anchorMin = Vector2.zero;
            nodesLayer.anchorMax = Vector2.one;
            nodesLayer.offsetMin = Vector2.zero;
            nodesLayer.offsetMax = Vector2.zero;
        }

        private void BuildGraphIfNeeded()
        {
            if (nodesLayer == null || linesLayer == null || contentArea == null || EntrepreneurTreeSystem.Instance == null)
            {
                Debug.LogWarning(LOG_PREFIX + "No se pudo construir el grafo: referencias de UI incompletas.");
                return;
            }

            if (nodeUIs.Count > 0)
                return;

            CalculateNodePositions();
            BuildNodes();
            BuildConnectionLines();
            ResizeContentArea();
        }

        private void CalculateNodePositions()
        {
            List<SkillTreeNode> allNodes = EntrepreneurTreeSystem.Instance.nodes;
            nodePositions.Clear();
            Dictionary<string, int> depths = new Dictionary<string, int>();
            Dictionary<string, List<string>> childrenOf = new Dictionary<string, List<string>>();

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

            Queue<string> queue = new Queue<string>();
            foreach (SkillTreeNode n in allNodes)
            {
                bool hasPrereqs = false;
                foreach (string p in n.prerequisites)
                {
                    if (!string.IsNullOrEmpty(p)) { hasPrereqs = true; break; }
                }
                if (!hasPrereqs)
                {
                    depths[n.id] = 0;
                    queue.Enqueue(n.id);
                }
            }

            int guard = 0;
            int maxGuard = allNodes.Count * 3;
            while (queue.Count > 0 && guard++ < maxGuard)
            {
                string cur = queue.Dequeue();
                int curDepth = depths.ContainsKey(cur) ? depths[cur] : 0;
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

            if (guard >= maxGuard)
                Debug.LogWarning(LOG_PREFIX + "Se alcanzó el límite de iteraciones del layout (posible ciclo en dependencias).");

            Dictionary<int, List<SkillTreeNode>> byLevel = new Dictionary<int, List<SkillTreeNode>>();
            int maxDepth = 0;
            foreach (SkillTreeNode n in allNodes)
            {
                int d = depths.ContainsKey(n.id) ? depths[n.id] : 0;
                if (!byLevel.ContainsKey(d)) byLevel[d] = new List<SkillTreeNode>();
                byLevel[d].Add(n);
                if (d > maxDepth) maxDepth = d;
            }

            for (int lvl = 0; lvl <= maxDepth; lvl++)
            {
                if (!byLevel.ContainsKey(lvl)) continue;
                byLevel[lvl].Sort((a, b) =>
                {
                    int catOrderA = (a.category == SkillTreeCategory.Product) ? 0 : (a.category == SkillTreeCategory.Employee ? 1 : 2);
                    int catOrderB = (b.category == SkillTreeCategory.Product) ? 0 : (b.category == SkillTreeCategory.Employee ? 1 : 2);
                    int diff = catOrderA - catOrderB;
                    return diff != 0 ? diff : string.Compare(a.id, b.id, System.StringComparison.Ordinal);
                });

                List<SkillTreeNode> levelNodes = byLevel[lvl];
                float totalW = (levelNodes.Count - 1) * H_SPACING;
                float maxColumnsWidth = GetMaxColumns(byLevel, maxDepth) * H_SPACING;
                float startX = CONTENT_PAD + (maxColumnsWidth - totalW) * 0.5f;
                for (int i = 0; i < levelNodes.Count; i++)
                {
                    float x = startX + i * H_SPACING;
                    float y = -(CONTENT_PAD + lvl * V_SPACING);
                    nodePositions[levelNodes[i].id] = new Vector2(x, y);
                }
            }
        }

        private int GetMaxColumns(Dictionary<int, List<SkillTreeNode>> byLevel, int maxDepth)
        {
            int max = 1;
            for (int i = 0; i <= maxDepth; i++)
                if (byLevel.ContainsKey(i) && byLevel[i].Count > max)
                    max = byLevel[i].Count;
            return max;
        }

        private void ResizeContentArea()
        {
            float maxX = 0f;
            float maxY = 0f;
            foreach (Vector2 p in nodePositions.Values)
            {
                if (p.x > maxX) maxX = p.x;
                float absY = Mathf.Abs(p.y);
                if (absY > maxY) maxY = absY;
            }
            contentArea.sizeDelta = new Vector2(maxX + NODE_W + CONTENT_PAD, maxY + NODE_H + CONTENT_PAD);
        }

        private void BuildNodes()
        {
            foreach (SkillTreeNode node in EntrepreneurTreeSystem.Instance.nodes)
            {
                if (!nodePositions.ContainsKey(node.id)) continue;
                EntrepreneurTreeNodeUI ui = CreateNodeUI(node, nodePositions[node.id]);
                if (ui != null)
                    nodeUIs[node.id] = ui;
            }
        }

        private EntrepreneurTreeNodeUI CreateNodeUI(SkillTreeNode node, Vector2 position)
        {
            GameObject nodeGO = new GameObject("Node_" + node.id, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            nodeGO.transform.SetParent(nodesLayer, false);

            RectTransform nodeRT = nodeGO.GetComponent<RectTransform>();
            nodeRT.anchorMin = new Vector2(0f, 1f);
            nodeRT.anchorMax = new Vector2(0f, 1f);
            nodeRT.pivot = new Vector2(0.5f, 0.5f);
            nodeRT.sizeDelta = new Vector2(NODE_W, NODE_H);
            nodeRT.anchoredPosition = new Vector2(position.x + NODE_W * 0.5f, position.y - NODE_H * 0.5f);

            Image bgImage = nodeGO.GetComponent<Image>();

            GameObject stripeGO = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripeGO.transform.SetParent(nodeGO.transform, false);
            RectTransform stripeRT = stripeGO.GetComponent<RectTransform>();
            stripeRT.anchorMin = new Vector2(0f, 0f);
            stripeRT.anchorMax = new Vector2(0f, 1f);
            stripeRT.offsetMin = Vector2.zero;
            stripeRT.offsetMax = new Vector2(5f, 0f);
            Image stripeImg = stripeGO.GetComponent<Image>();

            GameObject borderGO = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGO.transform.SetParent(nodeGO.transform, false);
            RectTransform borderRT = borderGO.GetComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = Vector2.zero;
            borderRT.offsetMax = Vector2.zero;
            Image borderImg = borderGO.GetComponent<Image>();
            borderImg.color = new Color(0f, 0f, 0f, 0.3f);

            string safeTitle = string.IsNullOrEmpty(node.title) ? node.id : node.title;
            string displayTitle = safeTitle.Length > 26 ? safeTitle.Substring(0, 23) + "…" : safeTitle;
            TMP_Text titleText = CreateLabel(nodeGO.transform, "TitleLabel", displayTitle,
                new Vector2(0.05f, 0.32f), new Vector2(0.98f, 0.96f),
                Vector2.zero, Vector2.zero, 10f, FontStyles.Bold, Color.white, TextAlignmentOptions.TopLeft);
            titleText.enableWordWrapping = true;
            titleText.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text statusText = CreateLabel(nodeGO.transform, "StatusLabel", string.Empty,
                new Vector2(0f, 0f), new Vector2(1f, 0.34f),
                Vector2.zero, Vector2.zero, 12f, FontStyles.Normal, Color.white, TextAlignmentOptions.BottomRight);
            statusText.margin = new Vector4(0, 0, 6f, 2f);

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

            EntrepreneurTreeNodeUI ui = nodeGO.AddComponent<EntrepreneurTreeNodeUI>();
            ui.Initialize(node, bgImage, borderImg, stripeImg, titleText, statusText, hoverImg, OnNodeClicked);
            return ui;
        }

        private void BuildConnectionLines()
        {
            foreach (SkillTreeNode node in EntrepreneurTreeSystem.Instance.nodes)
            {
                if (!nodePositions.ContainsKey(node.id)) continue;
                Vector2 toPos = NodeCenter(node.id);
                foreach (string prereqId in node.prerequisites)
                {
                    if (string.IsNullOrEmpty(prereqId) || !nodePositions.ContainsKey(prereqId)) continue;
                    CreateConnectionLine(prereqId, node.id, NodeCenter(prereqId), toPos);
                }
            }
        }

        private void CreateConnectionLine(string fromId, string toId, Vector2 fromPos, Vector2 toPos)
        {
            GameObject lineGO = new GameObject("Line_" + fromId + "_to_" + toId, typeof(RectTransform), typeof(Image));
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
            Vector2 p = nodePositions[nodeId];
            return new Vector2(p.x + NODE_W * 0.5f, p.y - NODE_H * 0.5f);
        }

        private void WireUpgradesButton()
        {
            if (upgradesButton == null) return;
            upgradesButton.onClick = new Button.ButtonClickedEvent();
            upgradesButton.onClick.AddListener(ShowTreeTab);
        }

        private void WireTreeTabButtons()
        {
            if (treeTabButton != null)
            {
                treeTabButton.onClick.RemoveAllListeners();
                treeTabButton.onClick.AddListener(ShowTreeTab);
            }

            if (expansionsTabButton != null)
            {
                expansionsTabButton.onClick.RemoveAllListeners();
                expansionsTabButton.onClick.AddListener(ShowExpansionsTab);
                expansionsTabButton.gameObject.SetActive(originalExpansionsPanel != null);
            }
        }

        private void WireSubtabsAndFallbacks()
        {
            if (originalExpansionsPanel == null) return;

            Transform existing = FindDeepByName(originalExpansionsPanel.transform, "BackToTreeButton");
            if (existing != null)
            {
                expansionsReturnButton = existing.GetComponent<Button>();
                if (expansionsReturnButton != null)
                {
                    expansionsReturnButton.onClick.RemoveAllListeners();
                    expansionsReturnButton.onClick.AddListener(ShowTreeTab);
                }
                return;
            }

            GameObject backBtnGO = CreatePanel(originalExpansionsPanel.transform, "BackToTreeButton",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-170f, -42f), new Vector2(-12f, -8f), new Color(0.17f, 0.39f, 0.66f, 0.92f));
            expansionsReturnButton = backBtnGO.AddComponent<Button>();
            expansionsReturnButton.targetGraphic = backBtnGO.GetComponent<Image>();
            expansionsReturnButton.onClick.AddListener(ShowTreeTab);

            CreateLabel(backBtnGO.transform, "BackToTreeLabel", "Árbol del Emprendedor",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 11f, FontStyles.Bold, COL_TEXT_WHITE, TextAlignmentOptions.Center);
        }

        private void ShowTreeTab()
        {
            if (treeRoot == null) return;

            UIShopCategoryHelper helper = upgradesRootPanel != null ? upgradesRootPanel.GetComponent<UIShopCategoryHelper>() : null;
            if (helper != null) helper.Show(treeRoot);
            else treeRoot.SetActive(true);

            if (treeContentPanel != null)
                treeContentPanel.gameObject.SetActive(true);

            RefreshAllNodeVisuals();
            RefreshAllLines();
            UpdatePointsLabel();
        }

        private void ShowExpansionsTab()
        {
            if (originalExpansionsPanel == null)
            {
                ShowMessage("No se encontró el panel de Expansiones.");
                return;
            }

            UIShopCategoryHelper helper = upgradesRootPanel != null ? upgradesRootPanel.GetComponent<UIShopCategoryHelper>() : null;
            if (helper != null) helper.Show(originalExpansionsPanel);
            else
            {
                originalExpansionsPanel.SetActive(true);
                if (treeRoot != null) treeRoot.SetActive(false);
            }
        }

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
            if (infoNameLabel == null) return;

            infoNameLabel.text = node.title;
            infoDescLabel.text = node.description;
            infoCostLabel.text = node.pointCost == 0 ? "Coste: Gratis" : "Coste: " + node.pointCost + " punto(s)";

            if (node.prerequisites.Count == 0)
            {
                infoReqsLabel.text = "Sin requisitos previos";
                infoReqsLabel.color = new Color(0.50f, 0.80f, 0.55f, 1f);
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder("Requisitos:\n");
                foreach (string prereqId in node.prerequisites)
                {
                    if (string.IsNullOrEmpty(prereqId)) continue;
                    bool met = EntrepreneurTreeSystem.IsNodeUnlocked(prereqId);
                    SkillTreeNode prereq = EntrepreneurTreeSystem.GetNode(prereqId);
                    string name = prereq != null ? prereq.title : prereqId;
                    sb.AppendLine((met ? "✓ " : "✗ ") + name);
                }
                infoReqsLabel.text = sb.ToString().TrimEnd();
                infoReqsLabel.color = new Color(0.85f, 0.50f, 0.50f, 1f);
            }

            if (node.isUnlocked)
            {
                infoStatusLabel.text = "Estado: Desbloqueado ✓";
                infoStatusLabel.color = new Color(0.30f, 0.85f, 0.50f, 1f);
                SetUnlockButtonState(false, "Ya desbloqueado");
                return;
            }

            string blockReason = EntrepreneurTreeSystem.GetUnlockBlockReason(node.id);
            if (string.IsNullOrEmpty(blockReason))
            {
                infoStatusLabel.text = "Estado: Disponible";
                infoStatusLabel.color = new Color(0.85f, 0.78f, 0.20f, 1f);
                string costLabel = node.pointCost == 0 ? "Desbloquear (gratis)" : "Desbloquear (" + node.pointCost + " pto)";
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
            if (infoNameLabel == null) return;

            infoNameLabel.text = "Selecciona un nodo";
            infoDescLabel.text = "Haz click en un nodo para ver nombre, descripción, coste y requisitos.";
            infoCostLabel.text = string.Empty;
            infoReqsLabel.text = string.Empty;
            infoStatusLabel.text = string.Empty;
            SetUnlockButtonState(false, "Selecciona un nodo");
        }

        private void SetUnlockButtonState(bool enabled, string label)
        {
            if (unlockButton == null) return;

            unlockButton.interactable = enabled;
            Image img = unlockButton.GetComponent<Image>();
            if (img != null)
                img.color = enabled ? COL_BTN_UNLOCK : COL_BTN_LOCKED;

            if (unlockButtonLabel != null)
                unlockButtonLabel.text = label;
        }

        private void OnUnlockButtonClicked()
        {
            if (selectedNodeUI == null) return;

            bool success = EntrepreneurTreeSystem.TryUnlockNode(selectedNodeUI.node.id);
            if (!success)
            {
                string reason = EntrepreneurTreeSystem.GetUnlockBlockReason(selectedNodeUI.node.id);
                ShowMessage(reason ?? "No se puede desbloquear.");
                ShowNodeInfo(selectedNodeUI.node);
            }
        }

        private void OnPointsChanged(int points)
        {
            UpdatePointsLabel();
            if (selectedNodeUI != null)
                ShowNodeInfo(selectedNodeUI.node);
        }

        private void OnNodeUnlocked(SkillTreeNode node)
        {
            RefreshAllNodeVisuals();
            RefreshAllLines();
            if (selectedNodeUI != null)
                ShowNodeInfo(selectedNodeUI.node);
        }

        private void UpdatePointsLabel()
        {
            if (pointsLabel == null || EntrepreneurTreeSystem.Instance == null) return;
            pointsLabel.text = "Puntos disponibles: " + EntrepreneurTreeSystem.Instance.progressPoints;
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
            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);
            messageCoroutine = StartCoroutine(FadeOutMessage(4f));
        }

        private void HideMessage()
        {
            if (messageLabel == null) return;

            messageLabel.text = string.Empty;
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }
        }

        private IEnumerator FadeOutMessage(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (messageLabel != null)
                messageLabel.text = string.Empty;
            messageCoroutine = null;
        }

        private void EnsureTreeSystemExists()
        {
            if (EntrepreneurTreeSystem.Instance != null) return;
            GameObject go = new GameObject("EntrepreneurTreeSystem");
            go.AddComponent<EntrepreneurTreeSystem>();
            Debug.Log(LOG_PREFIX + "No se encontró EntrepreneurTreeSystem; se creó automáticamente.");
        }

        private Transform FindDeepByName(Transform root, string name)
        {
            if (root == null) return null;
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

        private Button CreateTabButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject btnGO = CreatePanel(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero, new Color(0.17f, 0.39f, 0.66f, 0.92f));
            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnGO.GetComponent<Image>();

            CreateLabel(btnGO.transform, name + "Label", text,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 11f, FontStyles.Bold, COL_TEXT_WHITE, TextAlignmentOptions.Center);

            return btn;
        }

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
