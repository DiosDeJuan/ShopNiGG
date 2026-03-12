/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// The interactable store sign for opening or closing the store, i.e. day cycle.
    /// </summary>
    public class OpenSign : Interactable
    {
        /// <summary>
        /// Clip to play when the sign has been turned (open or closed), or none if not set.
        /// </summary>
        public AudioClip turnClip;

        //reference to the animation component
        private Animation anim;


        //initialize references
        void Awake()
        {
            anim = GetComponent<Animation>();
        }


        //initialize variables
        void Start()
        {
            //if a save game has been loaded with the day already started
            if (DayCycleSystem.GetStoreOpenState() != StoreOpenState.Waiting)
            {
                anim.Play("OpenSign");
                anim["OpenSign"].time = 2;
            }
        }


        /// <summary>
        /// Interactable override, adding UI action (if day not started yet).
        /// </summary>
        public override void OnBecameFocus()
        {
            switch(DayCycleSystem.GetStoreOpenState())
            {
                case StoreOpenState.Waiting:
                case StoreOpenState.Closed:
                    UIGame.AddAction("LeftClick", "Use", true);
                    break;
            }
        }


        /// <summary>
        /// Interactable override, react on player interaction.
        /// This object is used to start or finish the day cycle.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick") return false;
            bool interacted = false;

            switch(DayCycleSystem.GetStoreOpenState())
            {
                case StoreOpenState.Waiting:
                    interacted = true;
                    anim.Play("OpenSign");
                    DayCycleSystem.Instance.StartDay();
                    AudioSystem.Play3D(turnClip, transform.position);
                    break;

                case StoreOpenState.Closed:
                    interacted = true;
                    anim.Play("CloseSign");
                    DayCycleSystem.Instance.FinishDay();
                    AudioSystem.Play3D(turnClip, transform.position);
                    break;

                default:
                    UIGame.Instance.ShowMessage("Cannot close, wait until working hours are over");
                    break;
            }

            OnLostFocus();
            return interacted;
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
