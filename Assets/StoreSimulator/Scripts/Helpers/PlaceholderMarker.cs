using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Marks a GameObject as a placeholder that should be replaced with a
    /// proper asset or prefab in a future iteration.  Draws a yellow wire
    /// cube gizmo in the Scene view so placeholders are easy to spot.
    /// </summary>
    public class PlaceholderMarker : MonoBehaviour
    {
        [Tooltip("Brief description of what this placeholder represents.")]
        public string description = "";

        [Tooltip("Suggested replacement prefab path (e.g. Prefabs/Store/CashDesk).")]
        public string suggestedReplacement = "";

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}
