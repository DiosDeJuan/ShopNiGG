using System;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Manages the store security system with three levels of automatic shoplifter arrest.
    /// Level 1 = 33%, Level 2 = 66%, Level 3 = 99% automatic arrest chance.
    /// Security levels depend on prior skills in the Entrepreneur Skill Tree.
    /// Addresses RQF10, RQF11, RQF12, RQF18.
    /// </summary>
    public class SecuritySystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static SecuritySystem Instance { get; private set; }

        /// <summary>
        /// Event fired when the security level changes.
        /// </summary>
        public static event Action<int> onSecurityLevelChanged;

        /// <summary>
        /// Event fired when a shoplifter is automatically arrested by security.
        /// </summary>
        public static event Action onAutomaticArrest;

        /// <summary>
        /// The current security level (0 = none, 1-3 = active levels). (RQF11)
        /// </summary>
        public int currentSecurityLevel { get; private set; }

        /// <summary>
        /// Skill tree node IDs required for each security level. (RQF12)
        /// Index 0 = level 1 prerequisite, index 1 = level 2, index 2 = level 3.
        /// </summary>
        public string[] requiredSkillNodes = new string[3];

        /// <summary>
        /// Auto-arrest percentages for each security level. (RQF11)
        /// </summary>
        private static readonly int[] arrestChances = { 0, 33, 66, 99 };


        //initialize references
        void Awake()
        {
            Instance = this;
        }


        /// <summary>
        /// Returns the auto-arrest chance percentage for the current security level.
        /// </summary>
        public static int GetArrestChance()
        {
            return arrestChances[Instance.currentSecurityLevel];
        }


        /// <summary>
        /// Attempt to automatically arrest a shoplifter based on current security level. (RQF18)
        /// Returns true if the shoplifter was arrested automatically.
        /// </summary>
        public static bool TryAutomaticArrest()
        {
            if (Instance.currentSecurityLevel == 0)
                return false;

            int chance = GetArrestChance();
            bool arrested = UnityEngine.Random.Range(0, 100) < chance;

            if (arrested)
                onAutomaticArrest?.Invoke();

            return arrested;
        }


        /// <summary>
        /// Try to upgrade to a higher security level. Checks prerequisite skill nodes. (RQF12)
        /// </summary>
        public static bool TryUpgradeLevel()
        {
            int nextLevel = Instance.currentSecurityLevel + 1;

            if (nextLevel > 3)
            {
                UIGame.AddNotification("Security is already at maximum level.");
                return false;
            }

            string requiredNode = Instance.requiredSkillNodes[nextLevel - 1];
            if (!string.IsNullOrEmpty(requiredNode))
            {
                if (!EntrepreneurTreeSystem.IsNodeUnlocked(requiredNode))
                {
                    UIGame.AddNotification("Prerequisite skill not unlocked:\n" + requiredNode);
                    return false;
                }
            }

            Instance.currentSecurityLevel = nextLevel;
            onSecurityLevelChanged?.Invoke(nextLevel);
            UIGame.AddNotification("Security upgraded to Level " + nextLevel +
                                   "\nAuto-arrest chance: " + arrestChances[nextLevel] + "%");

            return true;
        }


        /// <summary>
        /// Returns whether the player has any security level unlocked. (RQF10)
        /// If false, the player must catch shoplifters manually.
        /// </summary>
        public static bool HasSecurity()
        {
            return Instance.currentSecurityLevel > 0;
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode.
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
            data["currentSecurityLevel"] = currentSecurityLevel;
            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            currentSecurityLevel = data["currentSecurityLevel"].AsInt;
        }
    }
}
