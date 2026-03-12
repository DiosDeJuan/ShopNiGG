using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Manages all employees in the store. Employees are unlocked through the Entrepreneur Skill Tree
    /// and can be assigned roles: Cashier (auto-attends customers) or Stocker (auto-restocks shelves).
    /// Addresses RQF8 and RQF9.
    /// </summary>
    public class EmployeeSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static EmployeeSystem Instance { get; private set; }

        /// <summary>
        /// Event fired when an employee is unlocked.
        /// </summary>
        public static event Action<EmployeeScriptableObject> onEmployeeUnlocked;

        /// <summary>
        /// Event fired when an employee role assignment changes.
        /// </summary>
        public static event Action<EmployeeScriptableObject> onEmployeeAssigned;

        /// <summary>
        /// List of all employee assets in the game.
        /// </summary>
        public List<EmployeeScriptableObject> employees;

        /// <summary>
        /// Time range in seconds for a cashier to attend a customer (1-12 seconds depending on products). (RQF9)
        /// </summary>
        public Vector2 cashierAttendTime = new Vector2(1f, 12f);

        /// <summary>
        /// Time in seconds for a stocker to restock one product.
        /// </summary>
        public float stockerRestockTime = 3f;


        //initialize references
        void Awake()
        {
            Instance = this;
        }


        /// <summary>
        /// Try to unlock an employee. Checks if the required skill node is unlocked. (RQF8)
        /// </summary>
        public static bool TryUnlockEmployee(EmployeeScriptableObject employee)
        {
            if (employee.isUnlocked)
            {
                UIGame.AddNotification("Employee already unlocked.");
                return false;
            }

            if (!string.IsNullOrEmpty(employee.requiredSkillNode))
            {
                if (!EntrepreneurTreeSystem.IsNodeUnlocked(employee.requiredSkillNode))
                {
                    UIGame.AddNotification("Prerequisite skill not unlocked:\n" + employee.requiredSkillNode);
                    return false;
                }
            }

            if (!StoreDatabase.CanPurchase(employee.buyPrice))
            {
                long missing = employee.buyPrice - StoreDatabase.Instance.currentMoney;
                UIGame.Instance.ShowMessage("Not enough money. Missing: " + StoreDatabase.FromLongToStringMoney(missing));
                return false;
            }

            StoreDatabase.AddRemoveMoney(-employee.buyPrice);
            employee.isUnlocked = true;
            onEmployeeUnlocked?.Invoke(employee);
            UIGame.AddNotification("Employee unlocked: " + employee.title + "\nRole: " + employee.role.ToString());

            return true;
        }


        /// <summary>
        /// Assign or unassign an employee to their role. (RQF9)
        /// </summary>
        public static bool AssignEmployee(EmployeeScriptableObject employee, bool assign)
        {
            if (!employee.isUnlocked)
            {
                UIGame.AddNotification("Employee must be unlocked first.");
                return false;
            }

            employee.isAssigned = assign;
            onEmployeeAssigned?.Invoke(employee);
            return true;
        }


        /// <summary>
        /// Returns all employees with a specific role that are currently assigned.
        /// </summary>
        public static List<EmployeeScriptableObject> GetAssignedByRole(EmployeeRole role)
        {
            List<EmployeeScriptableObject> result = new List<EmployeeScriptableObject>();

            foreach (EmployeeScriptableObject emp in Instance.employees)
            {
                if (emp.role == role && emp.isAssigned)
                    result.Add(emp);
            }

            return result;
        }


        /// <summary>
        /// Returns the role description for display in the UI. (RQF9)
        /// </summary>
        public static string GetRoleDescription(EmployeeRole role)
        {
            switch (role)
            {
                case EmployeeRole.Cashier:
                    return "Attends customers at the cash register automatically. " +
                           "Processing time varies from 1 to 12 seconds depending on the number of products.";
                case EmployeeRole.Stocker:
                    return "Restocks products on shelves automatically from storage. " +
                           "Keeps shelves full so customers can find what they need.";
                default:
                    return "";
            }
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode.
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
            JSONNode employeeArray = new JSONArray();

            foreach (EmployeeScriptableObject emp in employees)
            {
                JSONNode element = new JSONObject();
                element["id"] = emp.id;
                element["isUnlocked"] = emp.isUnlocked;
                element["isAssigned"] = emp.isAssigned;
                employeeArray.Add(element);
            }

            data["employees"] = employeeArray;
            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            JSONArray employeeArray = data["employees"].AsArray;
            for (int i = 0; i < employeeArray.Count; i++)
            {
                string id = employeeArray[i]["id"].Value;
                foreach (EmployeeScriptableObject emp in employees)
                {
                    if (emp.id == id)
                    {
                        emp.isUnlocked = employeeArray[i]["isUnlocked"].AsBool;
                        emp.isAssigned = employeeArray[i]["isAssigned"].AsBool;
                        break;
                    }
                }
            }
        }
    }
}
