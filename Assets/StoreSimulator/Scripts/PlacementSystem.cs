/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// System for placing or taking the contents of a PackageObject on/from a PlacementObject. 
    /// </summary>
    public class PlacementSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static PlacementSystem Instance { get; private set; }

        /// <summary>
        /// Size of each placement cell, defining how much world space a 1x1 product should actually use.
        /// </summary>
        public const float cellSize = 0.25f;     

        /// <summary>
        /// Event fired when a new PlacementObject got into focus, with a reference to it.
        /// </summary>
        public static event Action<PlacementObject> onPlacementFocus;

        /// <summary>
        /// Event fired when placement was performed, delivering product and bool true = place / false = collect.
        /// </summary>
        public static event Action<ProductScriptableObject, bool> onPlacementEvent;

        /// <summary>
        /// Layer mask for the placement surface.
        /// </summary>
        public LayerMask layerMask;

        /// <summary>
        /// The length of the ray that is cast against the layerMask, i.e. a PlacementObject.
        /// </summary>
        public int rayLength = 2;

        /// <summary>
        /// Clip to play when an item was placed in a PlacementObject, or none if not set.
        /// </summary>
        public AudioClip placeClip;

        /// <summary>
        /// Delay in seconds for continued place/collect when holding mouse button down.
        /// </summary>
        public float holdDelay = 0.5f;

        /// <summary>
        /// The currently active placement mode.
        /// </summary>
        public PlacementMode activeMode { get; private set; }

        //the placement that is hit by the placement raycast
        private PlacementObject currentPlacement;
        //the package that we are currently carrying around
        private PackageObject currentPackage;
        //reference to coroutine for delayed mouse press down action
        private Coroutine holdRoutine;
        //the action we are currently clicking or holding down
        private InputAction holdAction;


        //initialize references
        void Awake()
        {
            Instance = this;

            PlayerInput.GetPlayerByIndex(0).onActionTriggered += OnAction;
        }


        //do raycast when carrying a package
        void Update()
        {
            if (!HasPackage())
                return;

            RaycastPlacement();
        }


        /// <summary>
        /// Whether the player currently holds a package or not.
        /// </summary>
        public bool HasPackage()
        {
            return currentPackage != null;
        }


        /// <summary>
        /// Assign a PackageObject for placement and attaches it to the player.
        /// </summary>
        public void PickUp(PackageObject package)
        {
            currentPackage = package;

            AudioSystem.Play3D(DeliverySystem.Instance.pickupClip, package.transform.position);
            PlayerController.Instance.Carry(package);
        }


        /// <summary>
        /// Places an item of the PackageObject onto a PlacementObject.
        /// Does some validation checks and shows errors in the UI.
        /// </summary>
        public bool Place()
        {
            if (activeMode != PlacementMode.Valid)
                return false;

            ProductScriptableObject product = currentPackage.purchasable as ProductScriptableObject;

            if (currentPackage.IsEmpty())
            {
                UIGame.Instance.ShowMessage("Package is empty, nothing to place");
                return false;
            }

            if (currentPlacement.product != null && currentPlacement.product != product)
            {
                UIGame.Instance.ShowMessage("There is already a different product placed");
                return false;
            }

            if (!currentPlacement.IsPlaceable(product))
            {
                UIGame.Instance.ShowMessage("The placement does not have any space left");
                return false;
            }

            if (product.storageType != currentPlacement.storageType)
            {
                UIGame.Instance.ShowMessage("The storage type of the product and placement do not match");
                return false;
            }

            Vector3 targetPosition = currentPlacement.Add(product);
            Quaternion targetRotation = Quaternion.Euler(0, currentPlacement.orientation, 0);
            Transform item = currentPackage.Remove();
            AudioSystem.Play3D(placeClip, item.position, 0.2f);
            onPlacementEvent?.Invoke(product, true);

            InteractionSystem.MoveToTargetArc(item, currentPlacement.container, targetPosition, targetRotation);
            return true;
        }


        /// <summary>
        /// Takes an item out of the PlacementObject and puts it in the PackageObject.
        /// Does some validation checks and shows errors in the UI.
        /// </summary>
        public bool Collect()
        {
            if (activeMode != PlacementMode.Valid)
                return false;

            if (currentPlacement.IsEmpty())
            {
                UIGame.Instance.ShowMessage("Placement is empty, nothing to take");
                return false;
            }

            ProductScriptableObject product = currentPackage.purchasable as ProductScriptableObject;

            if (product != null && currentPlacement.product != product)
            {
                UIGame.Instance.ShowMessage("There is already a different product in the package");
                return false;
            }

            if (!currentPackage.IsPlaceable(product))
            {
                UIGame.Instance.ShowMessage("The package does not have any space left");
                return false;
            }

            if (product != null && product.storageType != currentPlacement.storageType)
            {
                UIGame.Instance.ShowMessage("The storage type of the product and placement do not match");
                return false;
            }

            Vector3 targetPosition = currentPackage.Add(currentPlacement.product);
            Transform item = currentPlacement.Remove();
            AudioSystem.Play3D(placeClip, item.position, 0.2f);
            onPlacementEvent?.Invoke(product, false);

            InteractionSystem.MoveToTargetArc(item, currentPackage.container, targetPosition, Quaternion.identity);
            return true;
        }

        
        /// <summary>
        /// Throw the package that is carried by the player away.
        /// </summary>
        public void Drop()
        {
            if (activeMode == PlacementMode.Inactive)
                return;

            AudioSystem.Play3D(DeliverySystem.Instance.dropClip, PlayerController.Instance.hands.position);
            PlayerController.Instance.Drop();
            ResetMode();
        }


        /// <summary>
        /// Destroy the package that is carried by the player.
        /// </summary>
        public void Trash()
        {           
            PlayerController.Instance.Drop(true);
            ResetMode();
        }


        //clears everything interaction related and resets back to defaults
        private void ResetMode()
        {
            currentPackage = null;

            UIGame.RemoveAction("F");
            UIGame.RemoveAction("LeftClick");
            UIGame.RemoveAction("RightClick");

            activeMode = PlacementMode.Inactive;
            onPlacementFocus?.Invoke(null);
        }


        //try to raycast on PlacementObject, and if hit, set that instance in focus
        private void RaycastPlacement()
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.green);

            if (Physics.Raycast(ray, out RaycastHit hit, rayLength, layerMask))
            {
                if (currentPlacement == null || currentPlacement.transform != hit.transform)
                {
                    currentPlacement = hit.transform.GetComponent<PlacementObject>();
                    onPlacementFocus?.Invoke(currentPlacement);
                }

                activeMode = PlacementMode.Valid;
                UIGame.AddAction("LeftClick", "Add", true);
                UIGame.AddAction("RightClick", "Take", true);
            }
            else
            {
                activeMode = PlacementMode.Outside;
            }

            if ((int)activeMode < 2 && currentPlacement != null)
            {
                onPlacementFocus?.Invoke(null);
                currentPlacement = null;
            }

            if (activeMode != PlacementMode.Valid)
            {
                //remove actions only in case we do not focus another allowed, interactable object (like the trash container)
                if (InteractionSystem.Instance.currentInteractable == null || !InteractionSystem.Instance.currentInteractable.ShouldSkipSystemChecks())
                {
                    UIGame.RemoveAction("LeftClick");
                    UIGame.RemoveAction("RightClick");
                }
            }
        }


        //react on user input
        private void OnAction(InputAction.CallbackContext context)
        {
            //do nothing when not carrying a package
            if (!HasPackage())
                return;

            if (context.started)
            {
                switch (context.action.name)
                {
                    case "LeftClick":
                    case "RightClick":
                        OnActionHoldStart(context.action);
                        break;
                    case "Action":
                        OnActionHoldCanceled();
                        Drop();
                        break;
                }
            }

            if (context.canceled && context.action == holdAction)
            {
                OnActionHoldCanceled();
            }
        }


        private void OnActionHoldStart(InputAction action)
        {
            if(holdAction != null && holdAction != action)
            {
                OnActionHoldCanceled();
            }

            holdAction = action;
            holdRoutine = StartCoroutine(OnActionHold());
        }
        

        private void OnActionHoldCanceled()
        {
            if(holdRoutine != null)
            {
                StopCoroutine(holdRoutine);
                holdRoutine = null;   
                holdAction = null;
            }
        }
        

        private IEnumerator OnActionHold()
        {
            while(true)
            {
                bool isValid = true;

                switch (holdAction.name)
                {
                    case "LeftClick":
                        isValid = Place();
                        break;
                    case "RightClick":
                        isValid = Collect();
                        break;
                }

                if (!isValid) yield break;
                yield return new WaitForSeconds(holdDelay);         
            }
        }
    }
}
