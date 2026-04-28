using UnityEngine;
using UnityEngine.UI;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Renders a visual line between two node positions inside the Entrepreneur Tree scroll view.
    /// Uses a rotated Image (the standard Unity-UI line technique).
    /// Created programmatically by UpgradesUIController.
    /// </summary>
    public class EntrepreneurTreeConnectionLineUI : MonoBehaviour
    {
        private static readonly Color COL_LINE_LOCKED    = new Color(0.28f, 0.30f, 0.34f, 0.65f);
        private static readonly Color COL_LINE_ACTIVE    = new Color(0.18f, 0.68f, 0.35f, 0.85f);
        private static readonly Color COL_LINE_AVAILABLE = new Color(0.80f, 0.58f, 0.12f, 0.70f);

        /// <summary>Source node ID (parent).</summary>
        public string fromNodeId { get; private set; }

        /// <summary>Target node ID (child).</summary>
        public string toNodeId { get; private set; }

        private Image lineImage;
        private RectTransform rt;


        /// <summary>
        /// Initialises the connection line and positions it between two anchor points.
        /// Both positions should be in the same coordinate space as this transform's parent.
        /// </summary>
        public void Initialize(string fromId, string toId,
                               Vector2 fromPos, Vector2 toPos,
                               Image image)
        {
            fromNodeId = fromId;
            toNodeId = toId;
            lineImage = image;
            rt = image.rectTransform;

            PositionLine(fromPos, toPos);
            RefreshColor();
        }


        /// <summary>
        /// Recalculates the line geometry when node positions change.
        /// </summary>
        public void PositionLine(Vector2 fromPos, Vector2 toPos)
        {
            if (rt == null) return;

            Vector2 diff = toPos - fromPos;
            float distance = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = (fromPos + toPos) * 0.5f;
            rt.sizeDelta = new Vector2(distance, 3f);       // 3 px thick
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }


        /// <summary>
        /// Call after any node unlock to update the line colour.
        /// </summary>
        public void RefreshColor()
        {
            if (lineImage == null) return;

            bool fromUnlocked = EntrepreneurTreeSystem.IsNodeUnlocked(fromNodeId);
            bool toUnlocked = EntrepreneurTreeSystem.IsNodeUnlocked(toNodeId);

            if (fromUnlocked && toUnlocked)
                lineImage.color = COL_LINE_ACTIVE;
            else if (fromUnlocked)
                lineImage.color = COL_LINE_AVAILABLE;
            else
                lineImage.color = COL_LINE_LOCKED;
        }
    }
}
