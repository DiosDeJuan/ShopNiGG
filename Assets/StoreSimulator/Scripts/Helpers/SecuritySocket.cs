using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Marks a Transform as a socket where a security device (camera, alarm
    /// gate, guard post) will be installed later.  Draws a red wire sphere
    /// gizmo to make sockets visible in the Scene view.
    /// </summary>
    public class SecuritySocket : MonoBehaviour
    {
        public enum SecurityDeviceType
        {
            Camera,
            AlarmGate,
            GuardPost
        }

        [Tooltip("Type of security device expected at this socket.")]
        public SecurityDeviceType deviceType = SecurityDeviceType.Camera;

        [Tooltip("Whether a device is currently installed.")]
        public bool isInstalled = false;

        private void OnDrawGizmos()
        {
            Gizmos.color = isInstalled ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.8f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                deviceType.ToString() + (isInstalled ? " [OK]" : " [Empty]"));
        }
#endif
    }
}
