using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// The Entrepreneur Skill Tree is a progression system that shows skills, products, employees,
    /// and upgrades. Each node has a cost in points and may require prior nodes to be unlocked.
    /// Addresses RQF3 and RQF4.
    /// </summary>
    public class EntrepreneurTreeSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static EntrepreneurTreeSystem Instance { get; private set; }

        /// <summary>
        /// Event fired when a skill tree node is unlocked.
        /// </summary>
        public static event Action<SkillTreeNode> onNodeUnlocked;

        /// <summary>
        /// All nodes in the entrepreneur skill tree. (RQF3)
        /// </summary>
        public List<SkillTreeNode> nodes = new List<SkillTreeNode>();


        //initialize references
        void Awake()
        {
            Instance = this;
        }


        /// <summary>
        /// Checks whether a specific node (by ID) has been unlocked.
        /// </summary>
        public static bool IsNodeUnlocked(string nodeId)
        {
            if (Instance == null) return false;

            foreach (SkillTreeNode node in Instance.nodes)
            {
                if (node.id == nodeId)
                    return node.isUnlocked;
            }

            return false;
        }


        /// <summary>
        /// Attempt to unlock a node. Validates prerequisites and point cost. (RQF4)
        /// </summary>
        public static bool TryUnlockNode(string nodeId)
        {
            SkillTreeNode node = GetNode(nodeId);
            if (node == null)
            {
                UIGame.AddNotification("Skill tree node not found.");
                return false;
            }

            if (node.isUnlocked)
            {
                UIGame.AddNotification("Node already unlocked.");
                return false;
            }

            //check prerequisites (RQF4)
            foreach (string prereqId in node.prerequisites)
            {
                if (!string.IsNullOrEmpty(prereqId) && !IsNodeUnlocked(prereqId))
                {
                    SkillTreeNode prereq = GetNode(prereqId);
                    string prereqName = prereq != null ? prereq.title : prereqId;
                    UIGame.AddNotification("Prerequisite not met:\n" + prereqName);
                    return false;
                }
            }

            //check if player has enough points/money
            if (!StoreDatabase.CanPurchase(node.pointCost))
            {
                long missing = node.pointCost - StoreDatabase.Instance.currentMoney;
                UIGame.Instance.ShowMessage("Not enough funds. Missing: " + StoreDatabase.FromLongToStringMoney(missing));
                return false;
            }

            //deduct cost and unlock
            StoreDatabase.AddRemoveMoney(-node.pointCost);
            node.isUnlocked = true;

            onNodeUnlocked?.Invoke(node);
            UIGame.AddNotification("Unlocked: " + node.title, otherColor: Color.green);

            return true;
        }


        /// <summary>
        /// Returns a node by its ID, or null if not found.
        /// </summary>
        public static SkillTreeNode GetNode(string nodeId)
        {
            if (Instance == null) return null;

            foreach (SkillTreeNode node in Instance.nodes)
            {
                if (node.id == nodeId)
                    return node;
            }

            return null;
        }


        /// <summary>
        /// Returns all nodes by category. (RQF3)
        /// </summary>
        public static List<SkillTreeNode> GetNodesByCategory(SkillTreeCategory category)
        {
            List<SkillTreeNode> result = new List<SkillTreeNode>();

            foreach (SkillTreeNode node in Instance.nodes)
            {
                if (node.category == category)
                    result.Add(node);
            }

            return result;
        }


        /// <summary>
        /// Returns all unlocked nodes.
        /// </summary>
        public static List<SkillTreeNode> GetUnlockedNodes()
        {
            List<SkillTreeNode> result = new List<SkillTreeNode>();

            foreach (SkillTreeNode node in Instance.nodes)
            {
                if (node.isUnlocked)
                    result.Add(node);
            }

            return result;
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode.
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
            JSONNode nodeArray = new JSONArray();

            foreach (SkillTreeNode node in nodes)
            {
                if (!node.isUnlocked) continue;

                JSONNode element = new JSONObject();
                element["id"] = node.id;
                element["isUnlocked"] = node.isUnlocked;
                nodeArray.Add(element);
            }

            data["nodes"] = nodeArray;
            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            JSONArray nodeArray = data["nodes"].AsArray;
            for (int i = 0; i < nodeArray.Count; i++)
            {
                string id = nodeArray[i]["id"].Value;
                foreach (SkillTreeNode node in nodes)
                {
                    if (node.id == id)
                    {
                        node.isUnlocked = nodeArray[i]["isUnlocked"].AsBool;
                        break;
                    }
                }
            }
        }
    }


    /// <summary>
    /// Represents a single node in the Entrepreneur Skill Tree. (RQF3)
    /// Each node has a category, cost, prerequisites, and unlock state.
    /// </summary>
    [Serializable]
    public class SkillTreeNode
    {
        /// <summary>
        /// Unique identifier for this node.
        /// </summary>
        public string id;

        /// <summary>
        /// Display name of the node.
        /// </summary>
        public string title;

        /// <summary>
        /// Description of what this node unlocks or improves.
        /// </summary>
        [TextArea]
        public string description;

        /// <summary>
        /// Category this node belongs to: Skill, Product, Employee, or Upgrade. (RQF3)
        /// </summary>
        public SkillTreeCategory category;

        /// <summary>
        /// Cost in points (money) to unlock this node. (RQF3)
        /// </summary>
        public long pointCost;

        /// <summary>
        /// IDs of prerequisite nodes that must be unlocked first. (RQF4)
        /// </summary>
        public List<string> prerequisites = new List<string>();

        /// <summary>
        /// Whether this node has been unlocked.
        /// </summary>
        public bool isUnlocked;

        /// <summary>
        /// Optional icon for the node in the UI.
        /// </summary>
        public Sprite icon;
    }
}
