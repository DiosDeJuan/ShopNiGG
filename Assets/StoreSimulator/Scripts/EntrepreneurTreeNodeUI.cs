using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI representation of a single node in the Entrepreneur Skill Tree.
    /// Handles visual states (Locked / Available / Unlocked / Selected) and click events.
    /// Created programmatically by UpgradesUIController.
    /// </summary>
    public class EntrepreneurTreeNodeUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        //─── Visual Colors ────────────────────────────────────────────────────────
        private static readonly Color COL_LOCKED    = new Color(0.18f, 0.20f, 0.23f, 0.90f);
        private static readonly Color COL_AVAILABLE = new Color(0.78f, 0.57f, 0.10f, 1.00f);
        private static readonly Color COL_UNLOCKED  = new Color(0.14f, 0.65f, 0.30f, 1.00f);
        private static readonly Color COL_SELECTED  = new Color(0.90f, 0.90f, 1.00f, 1.00f);
        private static readonly Color COL_HOVER_TINT = new Color(1.0f, 1.0f, 1.0f, 0.12f);

        private static readonly Color COL_TEXT_DARK  = new Color(0.10f, 0.10f, 0.10f, 1.00f);
        private static readonly Color COL_TEXT_LIGHT = new Color(0.90f, 0.92f, 0.95f, 1.00f);
        private static readonly Color COL_TEXT_DIM   = new Color(0.50f, 0.54f, 0.58f, 1.00f);

        // Category accent colours
        private static readonly Color CAT_PRODUCT  = new Color(0.20f, 0.60f, 0.88f, 1.00f);
        private static readonly Color CAT_EMPLOYEE = new Color(0.92f, 0.62f, 0.12f, 1.00f);
        private static readonly Color CAT_SECURITY = new Color(0.85f, 0.22f, 0.22f, 1.00f);
        private static readonly Color CAT_UPGRADE  = new Color(0.68f, 0.30f, 0.92f, 1.00f);

        //─── Data ─────────────────────────────────────────────────────────────────
        /// <summary>The tree node this UI element represents.</summary>
        public SkillTreeNode node { get; private set; }

        //─── References (set by UpgradesUIController) ─────────────────────────────
        private Image backgroundImage;
        private Image borderImage;
        private Image categoryStripe;
        private TMP_Text titleLabel;
        private TMP_Text statusLabel;
        private Image hoverOverlay;

        //─── State ────────────────────────────────────────────────────────────────
        private bool isSelected;
        private System.Action<EntrepreneurTreeNodeUI> onClickCallback;


        /// <summary>
        /// Initialises the node UI. Called by UpgradesUIController after the GO is created.
        /// </summary>
        public void Initialize(SkillTreeNode nodeData,
                               Image bg, Image border, Image stripe,
                               TMP_Text titleText, TMP_Text statusText,
                               Image hover,
                               System.Action<EntrepreneurTreeNodeUI> clickCallback)
        {
            node = nodeData;
            backgroundImage = bg;
            borderImage = border;
            categoryStripe = stripe;
            titleLabel = titleText;
            statusLabel = statusText;
            hoverOverlay = hover;
            onClickCallback = clickCallback;

            ApplyCategoryStripe();
            RefreshVisuals();
        }


        /// <summary>
        /// Call this whenever the tree state changes to refresh the visual state.
        /// </summary>
        public void RefreshVisuals()
        {
            if (node == null || backgroundImage == null) return;

            NodeDisplayState state = GetDisplayState();

            //background
            Color bgColor;
            if (isSelected)
                bgColor = COL_SELECTED;
            else if (state == NodeDisplayState.Unlocked)
                bgColor = COL_UNLOCKED;
            else if (state == NodeDisplayState.Available)
                bgColor = COL_AVAILABLE;
            else
                bgColor = COL_LOCKED;
            backgroundImage.color = bgColor;

            // Border
            if (borderImage != null)
            {
                borderImage.color = isSelected
                    ? new Color(1f, 1f, 1f, 0.85f)
                    : new Color(0f, 0f, 0f, 0.35f);
            }

            // Title text
            if (titleLabel != null)
            {
                titleLabel.color = (state == NodeDisplayState.Locked && !isSelected)
                    ? COL_TEXT_DIM
                    : (isSelected ? COL_TEXT_DARK : COL_TEXT_LIGHT);

                titleLabel.text = node.title;
            }

            // Status text
            if (statusLabel != null)
            {
                switch (state)
                {
                    case NodeDisplayState.Unlocked:
                        statusLabel.text = "✓";
                        statusLabel.color = isSelected ? COL_TEXT_DARK : COL_TEXT_LIGHT;
                        break;
                    case NodeDisplayState.Available:
                        statusLabel.text = "★";
                        statusLabel.color = isSelected ? COL_TEXT_DARK : COL_TEXT_LIGHT;
                        break;
                    default:
                        statusLabel.text = "🔒";
                        statusLabel.color = COL_TEXT_DIM;
                        break;
                }
            }

            // Canvas Group alpha for locked look
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = (state == NodeDisplayState.Locked) ? 0.65f : 1.0f;
        }


        /// <summary>Sets this node as selected/deselected and refreshes visuals.</summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            RefreshVisuals();
        }


        //──────────────────────────────────────────────────────────────────────────
        // IPointerClickHandler
        //──────────────────────────────────────────────────────────────────────────
        public void OnPointerClick(PointerEventData eventData)
        {
            onClickCallback?.Invoke(this);
        }


        //──────────────────────────────────────────────────────────────────────────
        // IPointerEnterHandler / IPointerExitHandler (hover tint)
        //──────────────────────────────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverOverlay != null)
                hoverOverlay.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hoverOverlay != null)
                hoverOverlay.gameObject.SetActive(false);
        }


        //──────────────────────────────────────────────────────────────────────────
        // Helpers
        //──────────────────────────────────────────────────────────────────────────
        private NodeDisplayState GetDisplayState()
        {
            if (node.isUnlocked) return NodeDisplayState.Unlocked;

            // available = all prerequisites unlocked
            foreach (string prereqId in node.prerequisites)
            {
                if (!string.IsNullOrEmpty(prereqId) && !EntrepreneurTreeSystem.IsNodeUnlocked(prereqId))
                    return NodeDisplayState.Locked;
            }

            return NodeDisplayState.Available;
        }

        private void ApplyCategoryStripe()
        {
            if (categoryStripe == null) return;

            Color c;
            if (node.category == SkillTreeCategory.Product)
                c = CAT_PRODUCT;
            else if (node.category == SkillTreeCategory.Employee)
                c = CAT_EMPLOYEE;
            else if (node.id != null && node.id.StartsWith("seg_"))
                c = CAT_SECURITY;
            else
                c = CAT_UPGRADE;

            categoryStripe.color = c;
        }


        private enum NodeDisplayState { Locked, Available, Unlocked }
    }
}
