/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI hints for an input action, displaying a specific key-action combination available.
    /// </summary>
    public class UIActionHint : MonoBehaviour
    {
        /// <summary>
        /// Label for key that needs to be pressed for this action.
        /// </summary>
        public TMP_Text key;

        /// <summary>
        /// Label for displaying the action description.
        /// </summary>
        public TMP_Text action;

        /// <summary>
        /// Image to display instead of button text.
        /// </summary>
        public Image icon;


        /// <summary>
        /// Initialize with key name and action description.
        /// If the Sprite reference is not null, an image is displayed instead of the key.‚
        /// </summary>
        public void Initialize(string keyText, string actionText, Sprite iconSprite)
        {
            if (iconSprite != null)
            {
                icon.sprite = iconSprite;
                icon.gameObject.SetActive(true);
            }
            else
            {
                key.text = keyText;
                key.gameObject.SetActive(true);
            }

            action.text = actionText;
        }
    }
}
