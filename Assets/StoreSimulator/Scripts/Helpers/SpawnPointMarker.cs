using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Marks a Transform as a spawn point and draws a coloured sphere gizmo
    /// in the Scene view.  Used for PlayerSpawn, CustomerEntrySpawn,
    /// EmployeeSpawn, ThiefSpawn, DeliverySpawnPoint, etc.
    /// </summary>
    public class SpawnPointMarker : MonoBehaviour
    {
        [Tooltip("Label shown next to the gizmo.")]
        public string label = "Spawn";

        [Tooltip("Gizmo colour in the Scene view.")]
        public Color gizmoColor = Color.green;

        [Tooltip("Radius of the gizmo sphere.")]
        public float gizmoRadius = 0.35f;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, label);
        }
#endif
    }
}
