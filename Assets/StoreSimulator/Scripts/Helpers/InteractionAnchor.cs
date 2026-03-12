using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Marks a Transform as an interaction anchor — a point where the player
    /// can trigger an interaction (e.g. open laptop, use tablet, operate
    /// checkout).  Draws a blue wire sphere gizmo in the Scene view.
    /// </summary>
    public class InteractionAnchor : MonoBehaviour
    {
        [Tooltip("Identifier used by game systems to locate this anchor.")]
        public string anchorId = "";

        [Tooltip("Optional description for level designers.")]
        public string description = "";

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            string text = string.IsNullOrEmpty(anchorId) ? gameObject.name : anchorId;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f, text);
        }
#endif
    }
}
