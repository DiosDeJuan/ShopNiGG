/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Helper for methods executed on shop categories. Currently only contains the method on
    /// disabling all category game objects under this parent and instead activating a single one.
    /// </summary>
    public class UIShopCategoryHelper : MonoBehaviour
    {
        /// <summary>
        /// Executes the disable and activation logic using the assigned game objects.
        /// Disables childs and activates the game object passed in.
        /// </summary>
        public void Show(GameObject toEnable)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject == toEnable)
                    continue;

                transform.GetChild(i).gameObject.SetActive(false);
            }

            if (toEnable != null)
                toEnable.SetActive(true);
        } 
    }
}
