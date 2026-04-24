// ShopMaster - Entrepreneur Skill Tree System
// ScriptableObject that holds the complete list of tree nodes.
// Create one instance via: Assets > Create > ShopMaster > EntrepreneurTree > TreeData
// Assign it to the EntrepreneurTreeManager component in the scene.

using System.Collections.Generic;
using UnityEngine;

namespace ShopMaster
{
    /// <summary>
    /// Container ScriptableObject that holds all <see cref="EntrepreneurNodeData"/> assets
    /// that form the Entrepreneur Skill Tree.
    ///
    /// Usage:
    ///   1. Run the editor builder (ShopMaster > Build Entrepreneur Tree) to auto-create
    ///      and populate this asset with the 36 default nodes.
    ///   2. Assign the resulting asset to EntrepreneurTreeManager.treeData.
    ///   3. The tree can be extended by adding additional EntrepreneurNodeData assets here.
    /// </summary>
    [CreateAssetMenu(
        fileName = "EntrepreneurTreeData",
        menuName  = "ShopMaster/EntrepreneurTree/TreeData")]
    public class EntrepreneurTreeData : ScriptableObject
    {
        [Tooltip("All nodes that make up the Entrepreneur Skill Tree.\n" +
                 "Populated automatically by the ShopMaster > Build Entrepreneur Tree editor menu item.")]
        public List<EntrepreneurNodeData> nodes = new List<EntrepreneurNodeData>();

        /// <summary>
        /// Returns the node with the given ID, or null if not found.
        /// </summary>
        public EntrepreneurNodeData GetNodeById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (EntrepreneurNodeData node in nodes)
            {
                if (node != null && node.id == id)
                    return node;
            }

            return null;
        }

        /// <summary>
        /// Returns all nodes of the specified type.
        /// </summary>
        public List<EntrepreneurNodeData> GetNodesByType(EntrepreneurNodeType type)
        {
            List<EntrepreneurNodeData> result = new List<EntrepreneurNodeData>();

            foreach (EntrepreneurNodeData node in nodes)
            {
                if (node != null && node.nodeType == type)
                    result.Add(node);
            }

            return result;
        }
    }
}
