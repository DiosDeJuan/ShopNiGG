/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// System for raycasting against interactable objects and invoking their focus and interact actions.
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static InteractionSystem Instance { get; private set; }

        /// <summary>
        /// Fired when an interaction happened, providing the object and input action interacted with.
        /// </summary>
        public static event Action<Interactable, string> onInteractEvent;

        /// <summary>
        /// Layer mask for the interaction surface.
        /// </summary>
        public LayerMask layerMask;

        /// <summary>
        /// The length of the ray that is cast against the layerMask, i.e. an Interactable.
        /// </summary>
        public int rayLength = 2;

        /// <summary>
        /// Maximum height of the arc when picking up or placing an object.
        /// </summary>
        public float arcHeight = 0.5f;

        /// <summary>
        /// The speed used to move on object when placing or picking it up.
        /// </summary>
        public float lerpSpeed = 1.2f;

        /// <summary>
        /// The Interactable that is currently in focus or about to be interacted with.
        /// </summary>
        public Interactable currentInteractable { get; private set; }

        //state of interaction, whether it is enabled or not
        private InteractionState interactionState = InteractionState.All;
        //reference to action map used to detect which key has been pressed later
        private InputActionMap actionMap;
        //the action that was pressed in this frame
        private InputAction lastAction;
        //a dictionary for storing references of currently animating transforms
        private Dictionary<Transform, Coroutine> moveRoutines = new Dictionary<Transform, Coroutine>();


        //initialize references
        void Awake()
        {
            Instance = this;

            actionMap = InputSystem.actions.actionMaps[0]; //Default
            actionMap.actionTriggered += OnAction;
        }


        //Update is called once per frame
        void Update()
        {
            switch(interactionState)
            {
                case InteractionState.All:
                    RaycastColliders();
                    break;
            }
        }


        /// <summary>
        /// Allow disabling this system e.g. in case we have openend a UI menu to not click through it.
        /// </summary>
        public static void SetInteractionState(InteractionState state)
        {
            Instance.interactionState = state;
        }


        /// <summary>
        /// Move a transform to a target position (optionally) assigning it a parent in an arc curve.
        /// </summary>
        public static Coroutine MoveToTargetArc(Transform item, Transform parent, Vector3 targetPosition, Quaternion targetRotation, float otherSpeed = 0, bool local = true)
        {
            if (Instance.moveRoutines.ContainsKey(item))
                Instance.StopCoroutine(Instance.moveRoutines[item]);
            else Instance.moveRoutines.Add(item, null);

            Coroutine coroutine = Instance.StartCoroutine(Instance.MoveToTargetArcRoutine(item, parent, targetPosition, targetRotation, otherSpeed, local));
            Instance.moveRoutines[item] = coroutine;
            return coroutine;
        }


        /// <summary>
        /// Move a transform to a target position (optionally) assigning it a parent in an linear line.
        /// </summary>
        public static Coroutine MoveToTargetLinear(Transform item, Transform parent, Vector3 targetPosition, Quaternion targetRotation, float otherSpeed = 0, bool local = true)
        {
            if (Instance.moveRoutines.ContainsKey(item))
                Instance.StopCoroutine(Instance.moveRoutines[item]);
            else Instance.moveRoutines.Add(item, null);

            Coroutine coroutine = Instance.StartCoroutine(Instance.MoveToTargetLinearRoutine(item, parent, targetPosition, targetRotation, otherSpeed, local));
            Instance.moveRoutines[item] = coroutine;
            return coroutine;
        }


        //react on user input
        private void OnAction(InputAction.CallbackContext context)
        {
            //exclude movement actions
            switch(context.action.name)
            {
                case "Move":
                case "View":
                case "Jump":
                    return;
            }

            lastAction = context.action;
        }


        //find out whether currently a placement mode is active to disable object interaction
        //this can be overridden by using the ShouldSkipSystemChecks on the Interactable
        private bool AreSystemsActive()
        {
            bool hasActive = StorageSystem.Instance.activeMode != PlacementMode.Inactive || PlacementSystem.Instance.activeMode != PlacementMode.Inactive;

            if (currentInteractable == null) return hasActive;
            if (currentInteractable.ShouldSkipSystemChecks()) return false;
            else return hasActive;
        }


        //try to raycast on Interactables, and if hit, call their Interact method with the button/key that was used
        private void RaycastColliders()
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.yellow);

            if (Physics.Raycast(ray, out RaycastHit hit, rayLength, layerMask))
            {
                if (currentInteractable == null || currentInteractable.transform != hit.transform)
                {
                    if (currentInteractable != null && !AreSystemsActive())
                    {
                        //previous interactable
                        currentInteractable.OnLostFocus();
                    }

                    //get new in current focus
                    currentInteractable = hit.collider.gameObject.GetComponent<Interactable>();

                    if (!AreSystemsActive())
                    {
                        currentInteractable.OnBecameFocus();
                    }
                }

                if (!AreSystemsActive() && lastAction != null && lastAction.WasPressedThisFrame())
                {
                    //execute interaction and raise event, if action was performed
                    if (currentInteractable.Interact(lastAction.name))
                        onInteractEvent?.Invoke(currentInteractable, lastAction.name);

                    lastAction = null;
                }
            }
            else if (currentInteractable != null)
            {
                if (!AreSystemsActive())
                {
                    currentInteractable.OnLostFocus();
                }

                currentInteractable = null;
            }
        }


        //mathematical method for calculating an arc between two points based on t = progress
        private Vector3 CalculateArcPosition(Vector3 start, Vector3 end, float height, float t)
        {
            Vector3 position = Vector3.Lerp(start, end, t);
            float arc = height * 4 * (t - t * t);
            position.y += arc;

            return position;
        }
        

        //the actual arc coroutine
        private IEnumerator MoveToTargetArcRoutine(Transform item, Transform parent, Vector3 targetPosition, Quaternion targetRotation, float otherSpeed, bool local)
        {
            float lerpProgress = 0f;
            if (parent != null) item.parent = parent;
            if (otherSpeed == 0) otherSpeed = Instance.lerpSpeed;
            Vector3 startingPosition = local == true ? item.localPosition : item.position;
            Quaternion startingRotation = local == true ? item.localRotation : item.rotation; 

            while (lerpProgress < 1f)
            {
                //update lerp progress, adjust speed if needed
                lerpProgress += otherSpeed * Time.deltaTime;

                //move along the arc
                Vector3 arcPosition = CalculateArcPosition(startingPosition, targetPosition, Instance.arcHeight, lerpProgress);
                Quaternion linearRotation = Quaternion.Lerp(startingRotation, targetRotation, lerpProgress);
                if (item == null) yield break;

                if (local)
                {
                    item.localPosition = arcPosition;
                    item.localRotation = linearRotation;
                }
                else
                {
                    item.position = arcPosition;
                    item.rotation = linearRotation;
                }

                yield return null;
            }

            if (local)
            {
                item.localPosition = targetPosition;
                item.localRotation = targetRotation;
            }
            else
            {
                item.position = targetPosition;
                item.rotation = targetRotation;
            }

            moveRoutines.Remove(item);
        }


        //the actual linear coroutine
        private IEnumerator MoveToTargetLinearRoutine(Transform item, Transform parent, Vector3 targetPosition, Quaternion targetRotation, float otherSpeed, bool local)
        {
            float lerpProgress = 0f;
            if (parent != null) item.parent = parent;
            if (otherSpeed == 0) otherSpeed = Instance.lerpSpeed;
            Vector3 startingPosition = local == true ? item.localPosition : item.position;
            Quaternion startingRotation = local == true ? item.localRotation : item.rotation; 

            while (lerpProgress < 1f)
            {
                //update lerp progress, adjust speed if needed
                lerpProgress += otherSpeed * Time.deltaTime;

                //move linearly down
                Vector3 linearPosition = Vector3.Lerp(startingPosition, targetPosition, lerpProgress);
                Quaternion linearRotation = Quaternion.Lerp(startingRotation, targetRotation, lerpProgress);
                if (item == null) yield break;

                if (local)
                {
                    item.localPosition = linearPosition;
                    item.localRotation = linearRotation;
                }
                else
                {
                    item.position = linearPosition;
                    item.rotation = linearRotation;
                }

                yield return null;
            }

            if (local)
            {
                item.localPosition = targetPosition;
                item.localRotation = targetRotation;
            }
            else
            {
                item.position = targetPosition;
                item.rotation = targetRotation;
            }

            moveRoutines.Remove(item);
        }


        //unsubscribe from events
        void OnDestroy()
        {
            actionMap.actionTriggered -= OnAction;
        }
    }
}