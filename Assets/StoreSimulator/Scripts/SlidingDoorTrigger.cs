/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// A script for the sliding door to automatically open and close once another collider is nearby or left.
    /// </summary>
    public class SlidingDoorTrigger : MonoBehaviour
    {
        //counter on how many colliders are in range
        private int inRange = 0;

        //reference to the animation component
        private Animation anim;


        //initialize references
        void Awake()
        {
            anim = GetComponent<Animation>();
        }


        //open door on first trigger
        private void OnTriggerEnter(Collider other)
        {
            if (inRange == 0)
                anim.Play("StoreDoor_Open");

            inRange++;
        }


        //close door when last trigger left
        private void OnTriggerExit(Collider other)
        {
            inRange--;

            if (inRange == 0)
                anim.Play("StoreDoor_Close");
        }
    }
}
