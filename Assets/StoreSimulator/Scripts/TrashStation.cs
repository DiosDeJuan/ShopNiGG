/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Object that will destroy a player's carried package when interacted with.
    /// </summary>
    public class TrashStation : Interactable
    {
        /// <summary>
        /// Clip to play whenever the player's package has been destroyed, or none if not set. 
        /// </summary>
        public AudioClip trashClip;


        /// <summary>
        /// Interactable override, allowing for interaction within active placement mode.
        /// </summary>
        public override bool ShouldSkipSystemChecks()
        {
            return true;
        }


        /// <summary>
        /// Interactable override, adding UI action.
        /// </summary>
        public override void OnBecameFocus()
        {
            if (PlacementSystem.Instance.activeMode != PlacementMode.Inactive || StorageSystem.Instance.activeMode != PlacementMode.Inactive)
                UIGame.AddAction("LeftClick", "Put in Trash", true);
        }


        /// <summary>
        /// Interactable override, react on player interaction.
        /// Destroys the package in the corresponding system depending on which one is currently active.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick") return false;
            
            bool canDestroy = PlacementSystem.Instance.activeMode != PlacementMode.Inactive || StorageSystem.Instance.activeMode != PlacementMode.Inactive;
            if (canDestroy) AudioSystem.Play3D(trashClip, transform.position, 0.1f);

            if (PlacementSystem.Instance.activeMode != PlacementMode.Inactive) PlacementSystem.Instance.Trash();
            else if (StorageSystem.Instance.activeMode != PlacementMode.Inactive) StorageSystem.Instance.Drop(true);

            UIGame.RemoveAction("LeftClick");
            return true;
        }


        /// <summary>
        /// Interactable override, removing UI action.
        /// </summary>
        public override void OnLostFocus()
        {
            UIGame.RemoveAction("LeftClick");
        }
    }
}
