using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Data asset for employees that can be unlocked via the Entrepreneur Skill Tree.
    /// Each employee has a role (Cashier or Stocker) and depends on a prior skill node. (RQF8, RQF9)
    /// </summary>
    [CreateAssetMenu(fileName = "Employee", menuName = "ScriptableObjects/Employee")]
    public class EmployeeScriptableObject : PurchasableScriptableObject
    {
        /// <summary>
        /// The role this employee fulfills: Cashier or Stocker.
        /// </summary>
        public EmployeeRole role;

        /// <summary>
        /// Description of what this employee role does.
        /// </summary>
        [TextArea]
        public string description;

        /// <summary>
        /// ID of the skill tree node required to unlock this employee.
        /// </summary>
        public string requiredSkillNode;

        /// <summary>
        /// Whether this employee has been unlocked.
        /// </summary>
        public bool isUnlocked;

        /// <summary>
        /// Whether this employee is currently assigned and active.
        /// </summary>
        public bool isAssigned;
    }
}
