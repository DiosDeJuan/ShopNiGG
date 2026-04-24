// ShopMaster - Entrepreneur Skill Tree System
// Draws a UI line (connection) between two tree node RectTransforms.
// Attach to a simple prefab that contains only an Image component.
//
// The line is drawn by:
//   1. Positioning this RectTransform at the midpoint between the two nodes.
//   2. Scaling its width to the distance between nodes.
//   3. Rotating it to the angle between nodes.
//
// All nodes and connections must be children of the SAME Content RectTransform
// so that local-position math is consistent.

using UnityEngine;
using UnityEngine.UI;

namespace ShopMaster
{
    /// <summary>
    /// Renders a straight line between two <see cref="EntrepreneurNodeUI"/> nodes
    /// using a UI Image whose RectTransform is scaled and rotated at runtime.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectTransform))]
    public class EntrepreneurConnectionUI : MonoBehaviour
    {
        [Tooltip("Pixel height of the line.")]
        public float lineThickness = 3f;

        [Tooltip("Color when neither endpoint is unlocked (grey).")]
        public Color lockedColor   = new Color(0.40f, 0.40f, 0.40f, 0.55f);

        [Tooltip("Color when the source node is unlocked but not the target (accent).")]
        public Color partialColor  = new Color(0.80f, 0.65f, 0.10f, 0.75f);

        [Tooltip("Color when both the source and target nodes are unlocked (green).")]
        public Color unlockedColor = new Color(0.15f, 0.75f, 0.25f, 0.85f);

        // ── Private refs ──────────────────────────────────────────────────────────

        private RectTransform rectTransform;
        private Image lineImage;


        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            lineImage     = GetComponent<Image>();

            // Pivot at center so rotation and position work correctly.
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }


        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Positions, scales, and rotates this connection to span from
        /// <paramref name="from"/> to <paramref name="to"/>.
        ///
        /// Both RectTransforms must share the same parent (the Content container).
        /// </summary>
        public void Connect(RectTransform from, RectTransform to, bool fromUnlocked, bool toUnlocked)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (lineImage     == null) lineImage     = GetComponent<Image>();

            // Use local positions so the math is independent of Content offset / anchors.
            Vector2 fromPos = from.localPosition;
            Vector2 toPos   = to.localPosition;

            // Midpoint
            rectTransform.localPosition = (Vector3)((fromPos + toPos) * 0.5f);

            // Length = distance between nodes
            float distance = Vector2.Distance(fromPos, toPos);
            rectTransform.sizeDelta = new Vector2(distance, lineThickness);

            // Angle so the line points from → to
            float angle = Mathf.Atan2(toPos.y - fromPos.y, toPos.x - fromPos.x) * Mathf.Rad2Deg;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Color
            lineImage.color = toUnlocked && fromUnlocked ? unlockedColor
                            : fromUnlocked               ? partialColor
                            :                              lockedColor;
        }


        /// <summary>
        /// Recolours the line based on unlock states without recalculating geometry.
        /// </summary>
        public void SetColor(bool fromUnlocked, bool toUnlocked)
        {
            if (lineImage == null) return;

            lineImage.color = toUnlocked && fromUnlocked ? unlockedColor
                            : fromUnlocked               ? partialColor
                            :                              lockedColor;
        }
    }
}
