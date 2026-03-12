/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine.Events;
using TMPro;
using System.Dynamic;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Cash object for adding/removing change to UICashDeskRegister in the customer checkout process.
    /// </summary>
    public class CashDeskCash : Interactable
    {
        /// <summary>
        /// Event fired when this cash object is interacted with.
        /// </summary>
        public UnityEvent<long> cashEvent;

        /// <summary>
        /// The amount of cash this object should represent, passed over to the event.
        /// </summary>
        public long eventValue;

        /// <summary>
        /// Label for displaying the amount of cash in the scene.
        /// </summary>
        public TMP_Text display;


        //initialize references
        void Awake()
        {
            if (eventValue < 100)
            {
                display.text = $"{eventValue}¢";
                return;
            }

            decimal amount = eventValue / 100m;
            string currencySymbol = StoreDatabase.cultureInfo.NumberFormat.CurrencySymbol;
            display.text = $"{currencySymbol}{amount.ToString("0.##",  StoreDatabase.cultureInfo)}";
        }


        /// <summary>
        /// Interactable override, adding UI action.
        /// </summary>
        public override void OnBecameFocus()
        {
            UIGame.AddAction("LeftClick", "Add", true);
            UIGame.AddAction("RightClick", "Remove", true);
        }


        /// <summary>
        /// Interactable override, react on player interaction.
        /// This adds or removes the selected amount of cash to UICashDeskRegister.
        /// The event target assignment is done in the Inspector.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick" && actionName != "RightClick")
                return false;

            if (actionName == "LeftClick") cashEvent?.Invoke(eventValue);
            else if (actionName == "RightClick") cashEvent?.Invoke(-eventValue);

            return true;
        }

        
        /// <summary>
        /// Interactable override, removing UI action.
        /// </summary>
        public override void OnLostFocus()
        {
            UIGame.RemoveAction("LeftClick");
            UIGame.RemoveAction("RightClick");
        }
    }
}
