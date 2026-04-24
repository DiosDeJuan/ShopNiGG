// ShopMaster - Entrepreneur Skill Tree System
// ScriptableObject that describes a single node in the Entrepreneur Skill Tree.
// Create instances via: Assets > Create > ShopMaster > EntrepreneurTree > NodeData
// Or use the editor utility: ShopMaster > Build Entrepreneur Tree

using System.Collections.Generic;
using UnityEngine;

namespace ShopMaster
{
    /// <summary>
    /// Data asset for one node in the Entrepreneur Skill Tree.
    /// Each node represents a product category, employee slot, security level, or upgrade.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NodeData_New",
        menuName  = "ShopMaster/EntrepreneurTree/NodeData")]
    public class EntrepreneurNodeData : ScriptableObject
    {
        [Tooltip("Unique string identifier. Used for save/load, prerequisites, and gameplay integration.\n" +
                 "Examples: 'products_basic_1', 'employee_1', 'security_1', 'upgrade_caffeine'")]
        public string id;

        [Tooltip("Human-readable name displayed on the node button.")]
        public string displayName;

        [TextArea(2, 4)]
        [Tooltip("Short description shown in the tooltip. List what this node unlocks.")]
        public string description;

        [Tooltip("Category that determines which gameplay system is notified on unlock.")]
        public EntrepreneurNodeType nodeType;

        [Tooltip("Optional icon shown on the node button. Leave null to show text only.")]
        public Sprite icon;

        [Tooltip("Position (in pixels) of this node inside the Scroll View Content RectTransform.\n" +
                 "X = horizontal offset from left edge. Y = vertical offset from top edge (use negative values to go down).")]
        public Vector2 uiPosition;

        [Tooltip("Cost in entrepreneur points to unlock this node. Default is 1.")]
        public int cost = 1;

        [Tooltip("IDs of nodes that must be unlocked BEFORE this node becomes available.\n" +
                 "Leave empty for root nodes.")]
        public List<string> requiredNodeIds = new List<string>();

        [Tooltip("Key string forwarded to the gameplay integration layer when this node is unlocked.\n" +
                 "For products: matches the product-group key in the asset's product system.\n" +
                 "For employees: matches the employee asset ID in EmployeeSystem.\n" +
                 "For security: '1', '2', or '3' for the target security level.\n" +
                 "For upgrades: 'caffeine' or 'charismatic' (see EntrepreneurTreeManager.ApplyGameplayEffect).")]
        public string gameplayKey;
    }
}
