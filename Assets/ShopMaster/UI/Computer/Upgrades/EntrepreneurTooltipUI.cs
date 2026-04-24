// ShopMaster - Entrepreneur Skill Tree System
// Tooltip panel displayed when the player hovers or selects a tree node.
//
// Setup:
//   Place the tooltip GameObject as a child of the Upgrades panel root,
//   NOT inside the Scroll View (so it does not scroll away with the Content).
//   Give the GameObject a CanvasGroup component.
//   Wire up the TMP_Text references in the Inspector.

using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShopMaster
{
    /// <summary>
    /// Shows detailed information about a hovered / selected Entrepreneur Tree node.
    ///
    /// Panel layout expected on the prefab:
    ///   - (root) CanvasGroup  ← for alpha fade
    ///     - titleText         TMP_Text
    ///     - descriptionText   TMP_Text
    ///     - typeText          TMP_Text
    ///     - costText          TMP_Text
    ///     - requirementsText  TMP_Text
    ///     - statusText        TMP_Text
    ///     - (optional) nodeTypeIcon Image
    /// </summary>
    public class EntrepreneurTooltipUI : MonoBehaviour
    {
        [Header("Text References")]
        [Tooltip("Large title at the top of the tooltip.")]
        public TMP_Text titleText;

        [Tooltip("Multi-line description of what this node unlocks.")]
        public TMP_Text descriptionText;

        [Tooltip("Shows the node type (Product / Employee / Security / Upgrade).")]
        public TMP_Text typeText;

        [Tooltip("Shows the cost in points.")]
        public TMP_Text costText;

        [Tooltip("Shows each prerequisite with a ✓ or ✗ prefix.")]
        public TMP_Text requirementsText;

        [Tooltip("Shows the current lock state with appropriate color.")]
        public TMP_Text statusText;

        [Header("Optional")]
        [Tooltip("Icon image tinted with the node type color.")]
        public Image nodeTypeIcon;

        // ── Private ───────────────────────────────────────────────────────────────

        private RectTransform rectTransform;
        private CanvasGroup   canvasGroup;

        // Pixel offset applied to position the tooltip to the right of the cursor.
        private const float XOffset = 90f;


        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup   = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Hide();
        }


        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Populates and shows the tooltip for <paramref name="data"/>.
        /// <paramref name="anchorWorldPos"/> is the world-space position of the node button
        /// used to decide where to place the tooltip panel on screen.
        /// </summary>
        public void Show(EntrepreneurNodeData data, Vector3 anchorWorldPos)
        {
            if (data == null) return;

            PopulateText(data);
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;

            // Position tooltip to the right of the hovered node.
            // TODO: Add bounds-clamping so the tooltip stays within the canvas when near edges.
            if (rectTransform != null)
            {
                Vector3 offset = new Vector3(XOffset, 0f, 0f);
                transform.position = anchorWorldPos + offset;
            }
        }


        /// <summary>Hides the tooltip panel.</summary>
        public void Hide()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }


        // ── Private helpers ───────────────────────────────────────────────────────

        private void PopulateText(EntrepreneurNodeData data)
        {
            // Title
            if (titleText != null)
                titleText.text = data.displayName;

            // Description
            if (descriptionText != null)
                descriptionText.text = string.IsNullOrEmpty(data.description)
                    ? "(Sin descripción)"
                    : data.description;

            // Type
            if (typeText != null)
                typeText.text = NodeTypeName(data.nodeType);

            // Cost
            if (costText != null)
            {
                int pts = PlayerProgressPoints.GetPoints();
                costText.text = "Costo: " + data.cost + " punto(s)  |  Disponibles: " + pts;
            }

            // Requirements
            if (requirementsText != null)
                requirementsText.text = BuildRequirementsText(data);

            // Status
            if (statusText != null)
                PopulateStatus(data.id);

            // Icon tint
            if (nodeTypeIcon != null)
                nodeTypeIcon.color = NodeTypeColor(data.nodeType);
        }


        private string BuildRequirementsText(EntrepreneurNodeData data)
        {
            if (data.requiredNodeIds == null || data.requiredNodeIds.Count == 0)
                return "Sin requisitos.";

            StringBuilder sb = new StringBuilder("Requisitos:\n");
            foreach (string reqId in data.requiredNodeIds)
            {
                if (string.IsNullOrEmpty(reqId)) continue;

                EntrepreneurNodeData req = EntrepreneurTreeManager.Instance?.treeData?.GetNodeById(reqId);
                string reqName = req != null ? req.displayName : reqId;
                bool   met     = EntrepreneurTreeManager.IsUnlocked(reqId);

                sb.AppendLine((met ? "<color=#44DD44>✓ " : "<color=#DD4444>✗ ") + reqName + "</color>");
            }

            return sb.ToString();
        }


        private void PopulateStatus(string nodeId)
        {
            EntrepreneurNodeState state = EntrepreneurTreeManager.GetNodeState(nodeId);

            switch (state)
            {
                case EntrepreneurNodeState.Locked:
                    statusText.text  = "Estado: <color=#888888>Bloqueado 🔒</color>";
                    break;
                case EntrepreneurNodeState.Available:
                    statusText.text  = "Estado: <color=#4499FF>Disponible – haz clic para desbloquear</color>";
                    break;
                case EntrepreneurNodeState.Unlocked:
                    statusText.text  = "Estado: <color=#44DD44>Desbloqueado ✓</color>";
                    break;
            }
        }


        private static string NodeTypeName(EntrepreneurNodeType type)
        {
            switch (type)
            {
                case EntrepreneurNodeType.Product:  return "Tipo: Producto";
                case EntrepreneurNodeType.Employee: return "Tipo: Empleado";
                case EntrepreneurNodeType.Security: return "Tipo: Seguridad";
                case EntrepreneurNodeType.Upgrade:  return "Tipo: Mejora";
                default: return type.ToString();
            }
        }


        private static Color NodeTypeColor(EntrepreneurNodeType type)
        {
            switch (type)
            {
                case EntrepreneurNodeType.Product:  return new Color(0.30f, 0.70f, 1.00f);
                case EntrepreneurNodeType.Employee: return new Color(1.00f, 0.75f, 0.20f);
                case EntrepreneurNodeType.Security: return new Color(1.00f, 0.35f, 0.35f);
                case EntrepreneurNodeType.Upgrade:  return new Color(0.70f, 0.35f, 1.00f);
                default: return Color.white;
            }
        }
    }
}
