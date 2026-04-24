// ShopMaster - Entrepreneur Skill Tree System
// UI component attached to each instantiated node prefab.
// Handles visual state, click-to-unlock, and pointer events for tooltip.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ShopMaster
{
    /// <summary>
    /// Drives the visual appearance of a single Entrepreneur Tree node button.
    ///
    /// Required on the Node Prefab:
    ///   - Image component (backgroundImage) — receives colour tinting per state.
    ///   - (optional) Image component (iconImage) — shows the node's icon sprite.
    ///   - TMP_Text (nameText) — displays displayName.
    ///   - TMP_Text (costText) — displays cost or "✓" when unlocked.
    ///
    /// Events handled:
    ///   PointerEnter → show tooltip
    ///   PointerExit  → hide tooltip
    ///   PointerClick → attempt unlock, refresh entire tree
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EntrepreneurNodeUI : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [Header("Visual References")]
        [Tooltip("The main background Image whose color reflects the node state.")]
        public Image backgroundImage;

        [Tooltip("Optional icon Image. Deactivated when no sprite is assigned to the node.")]
        public Image iconImage;

        [Tooltip("Text label that shows the node's display name.")]
        public TMP_Text nameText;

        [Tooltip("Text label that shows the cost in points, or a check-mark when unlocked.")]
        public TMP_Text costText;

        [Tooltip("Optional text label that shows the node type (Product / Employee / Security / Upgrade).")]
        public TMP_Text typeText;

        [Header("State Colors")]
        [Tooltip("Background color when the node is locked (prerequisites not met).")]
        public Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Tooltip("Background color when the node is available (prerequisites met, not yet bought).")]
        public Color availableColor = new Color(0.15f, 0.50f, 0.90f, 1f);

        [Tooltip("Background color when the node has been unlocked.")]
        public Color unlockedColor = new Color(0.15f, 0.75f, 0.25f, 1f);

        // ── Private state ─────────────────────────────────────────────────────────

        private EntrepreneurNodeData nodeData;
        private EntrepreneurTreeUIController uiController;


        // ── Initialization ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="EntrepreneurTreeUIController"/> after instantiating this node.
        /// </summary>
        public void Initialize(EntrepreneurNodeData data, EntrepreneurTreeUIController controller)
        {
            nodeData     = data;
            uiController = controller;

            // Populate static labels
            if (nameText != null)
                nameText.text = data.displayName;

            if (typeText != null)
                typeText.text = data.nodeType.ToString();

            // Icon
            if (iconImage != null)
            {
                if (data.icon != null)
                {
                    iconImage.sprite = data.icon;
                    iconImage.gameObject.SetActive(true);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }

            Refresh();
        }


        // ── Visual refresh ────────────────────────────────────────────────────────

        /// <summary>
        /// Re-queries the node state from <see cref="EntrepreneurTreeManager"/> and updates visuals.
        /// Called on initialization and after any unlock event.
        /// </summary>
        public void Refresh()
        {
            if (nodeData == null) return;

            EntrepreneurNodeState state = EntrepreneurTreeManager.GetNodeState(nodeData.id);

            switch (state)
            {
                case EntrepreneurNodeState.Locked:
                    if (backgroundImage != null) backgroundImage.color = lockedColor;
                    if (costText != null)         costText.text = nodeData.cost + "pt";
                    break;

                case EntrepreneurNodeState.Available:
                    if (backgroundImage != null) backgroundImage.color = availableColor;
                    if (costText != null)         costText.text = nodeData.cost + "pt";
                    break;

                case EntrepreneurNodeState.Unlocked:
                    if (backgroundImage != null) backgroundImage.color = unlockedColor;
                    if (costText != null)         costText.text = "✓";
                    break;
            }
        }


        // ── Pointer events ────────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (uiController != null && nodeData != null)
                uiController.ShowTooltip(nodeData, transform.position);
        }


        public void OnPointerExit(PointerEventData eventData)
        {
            if (uiController != null)
                uiController.HideTooltip();
        }


        public void OnPointerClick(PointerEventData eventData)
        {
            if (nodeData == null) return;

            EntrepreneurTreeManager.TryUnlock(nodeData.id);

            // Refresh the whole tree so connection colors and node states update.
            uiController?.RefreshAll();
        }


        // ── Accessors ─────────────────────────────────────────────────────────────

        /// <summary>Returns the underlying node data asset.</summary>
        public EntrepreneurNodeData GetNodeData() => nodeData;
    }
}
