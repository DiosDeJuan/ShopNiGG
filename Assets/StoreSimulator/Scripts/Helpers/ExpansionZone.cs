using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Marks a Transform as an expansion zone — an area reserved for future
    /// sales floor or storage expansions.  Draws a cyan wire cube in the
    /// Scene view matching the zone dimensions.
    /// </summary>
    public class ExpansionZone : MonoBehaviour
    {
        [Tooltip("Type of expansion this zone supports.")]
        public ExpansionType expansionType = ExpansionType.SalesFloor;

        [Tooltip("Size in Unity units (metres) of this expansion slot.")]
        public Vector3 slotSize = new Vector3(4f, 3f, 4f);

        [Tooltip("Whether this slot has been purchased / activated.")]
        public bool isActivated = false;

        private void OnDrawGizmos()
        {
            Gizmos.color = isActivated ? new Color(0f, 1f, 1f, 0.25f) : new Color(0f, 1f, 1f, 0.10f);
            Gizmos.DrawCube(transform.position + Vector3.up * slotSize.y * 0.5f, slotSize);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * slotSize.y * 0.5f, slotSize);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            string label = expansionType.ToString() + (isActivated ? " [Active]" : " [Available]");
            UnityEditor.Handles.Label(transform.position + Vector3.up * (slotSize.y + 0.3f), label);
        }
#endif
    }
}
