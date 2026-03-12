/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Data container for a tutorial step defining the goal that needs to be completed.
    /// </summary>
    [CreateAssetMenu(fileName = "Tutorial", menuName = "ScriptableObjects/Tutorial")]
    public class TutorialScriptableObject : ScriptableObject
    {
        /// <summary>
        /// Title of the task as a header or general information.
        /// </summary>
        public string title;

        /// <summary>
        /// A description of the task the player needs to accomplish.
        /// </summary>
        public string description;

        /// <summary>
        /// Count of steps required to complete this task.
        /// </summary>
        public int requiredCount = 1;

        /// <summary>
        /// Type of tutorial further defining the underlying logic that detects progress.
        /// </summary>
        public TutorialType type;

        /// <summary>
        /// A string argument that can be used in a TutorialType for further specification, when required.
        /// </summary>
        public string stringArg1;

        /// <summary>
        /// A string argument that can be used in a TutorialType for further specification, when required.
        /// </summary>
        public string stringArg2;

        /// <summary>
        /// An integer argument that can be used in a TutorialType for further specification, when required.
        /// </summary>
        public int intArg1;
    }
}