/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// System for interacting (unpack, pick up, preview, place) with StorageObject on StorageGrid.
    /// </summary>
    public class StorageSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static StorageSystem Instance { get; private set; }

        /// <summary>
        /// Size of each grid cell.
        /// </summary>
        public const float cellSize = 0.25f;         

        /// <summary>
        /// Event fired when entering build mode by picking up or unpacking a StorageObject. True = active, false = inactive.
        /// </summary>
        public static event Action<bool> onBuildActivated;

        /// <summary>
        /// Layer mask for the grid surface.
        /// </summary>
        public LayerMask layerMask;

        /// <summary>
        /// Material for valid placement preview.
        /// </summary>
        public Material validMaterial;

        /// <summary>
        /// Material for invalid placement preview.
        /// </summary>
        public Material invalidMaterial;

        /// <summary>
        /// The length of the ray that is cast against the layerMask, i.e. a grid.
        /// </summary>
        public int rayLength = 5;

        /// <summary>
        /// The speed used to snap or rotate the preview object on the grid.
        /// </summary>
        public int lerpSpeed = 10;

        /// <summary>
        /// Prefab for visual display when picking up a PackageObject.
        /// </summary>
        public GameObject packageContent;

        /// <summary>
        /// Clip to play when a new StorageObject has been placed in the shop, or none if not set.
        /// </summary>
        public AudioClip placeClip;

        /// <summary>
        /// A list of StorageObjects a new game should start with, instead of being placed at runtime.
        /// </summary>
        public List<StorageObject> preplacedObjects = new List<StorageObject>();

        /// <summary>
        /// The currently active placement mode.
        /// </summary>
        public PlacementMode activeMode {  get; private set; }

        /// <summary>
        /// Reference to the StorageObject component on the preview object, set on interaction.
        /// </summary>
        [HideInInspector]
        public StorageObject previewObject;

        //the prefab of the original object that should be instantiated when placed
        private GameObject selectedPrefab;
        //reference to the Transform the preview object moving around
        private Transform previewTransform;
        //tracks the 90-degree rotation state. 0 = not rotated, 1 = 90 degrees, 2 = 180, 3 = 270
        private int previewRotationState = 0;
        //saving the configuration of an existing object to apply it back to the instance later on
        private JSONNode previewObjectConfig = null;
        //the final, snapped position where an object should be placed at
        private Vector3 targetPosition;
        //a dictionary of all StorageGrid components placed in the scene, indexed by their Transform
        private Dictionary<Transform, StorageGrid> grids = new Dictionary<Transform, StorageGrid>();


        //initialize references
        void Awake()
        {
            Instance = this;

            StorageGrid[] childs = GetComponentsInChildren<StorageGrid>();
            for (int i = 0; i < childs.Length; i++)
            {
                grids.Add(childs[i].transform, childs[i]);
            }

            PlayerInput.GetPlayerByIndex(0).onActionTriggered += OnAction;
        }


        //call preview method in active placement
        void Update()
        {
            if (activeMode != PlacementMode.Inactive)
                CreateAndMovePreview();
        }


        /// <summary>
        /// Carry a package and unpack it for placing a StorageObject. 
        /// </summary>
        public void Unpack(PackageObject package)
        {
            selectedPrefab = (package.purchasable as StorageScriptableObject).prefab;

            previewTransform = Instantiate(selectedPrefab, targetPosition, Quaternion.Euler(0, 90 * previewRotationState, 0)).transform;
            previewTransform.GetComponent<Collider>().enabled = false;
            previewObject = previewTransform.GetComponent<StorageObject>();
            previewObject.groundLine.SetActive(true);

            AudioSystem.Play3D(DeliverySystem.Instance.pickupClip, package.transform.position);
            PlayerController.Instance.Carry(package);

            activeMode = PlacementMode.Outside;
            onBuildActivated(true);
        }


        /// <summary>
        /// Pick up an existing StorageObject, put it in a package and create a placement preview for it.
        /// </summary>
        public void PickUp(StorageObject storageObject)
        {
            previewObject = storageObject;
            previewTransform = storageObject.transform;
            previewTransform.GetComponent<Collider>().enabled = false;
            previewTransform.GetComponent<NavMeshObstacle>().enabled = false;
            selectedPrefab = previewObject.storage.prefab;

            foreach(PlacementObject placement in storageObject.placements)
            {
                if (placement.product != null)
                    StoreDatabase.Instance.RemoveProductPlacement(placement.product, placement);
            }

            previewRotationState = previewObject.rotationState;
            targetPosition = previewTransform.position;
            SetOccupied(targetPosition, GetRotatedSize(), false);

            GameObject packaging = Instantiate(DeliverySystem.Instance.packagePrefab, targetPosition, Quaternion.identity);
            PackageObject package = packaging.GetComponent<PackageObject>();
            package.Add(storageObject.storage, 0);
            packaging.GetComponent<Collider>().enabled = false;

            AudioSystem.Play3D(DeliverySystem.Instance.pickupClip, package.transform.position);
            PlayerController.Instance.Carry(package);

            previewObjectConfig = previewObject.SaveToJSON();
            activeMode = PlacementMode.Outside;
            onBuildActivated(true);
        }


        /// <summary>
        /// Place the object on the grid, occupy it and destroy the package we are still carrying around. 
        /// </summary>
        public void Place()
        {
            //instantiate the actual object
            GameObject newObject = Instantiate(selectedPrefab, targetPosition, Quaternion.Euler(0, 90 * previewRotationState, 0));
            SetOccupied(targetPosition, GetRotatedSize(), true);
            AudioSystem.Play3D(placeClip, targetPosition);

            StorageObject storageObject = newObject.GetComponent<StorageObject>();
            storageObject.rotationState = previewRotationState;

            //apply the previously cached data if we've picked up and existing StorageObject
            if (previewObjectConfig != null)
            {
                //remove existing properties we've already modified
                previewObjectConfig.Remove("Transform");
                previewObjectConfig.Remove("rotationState");
                
                storageObject.LoadFromJSON(previewObjectConfig);
            }

            PlayerController.Instance.Drop(true);
            ResetMode();
        }


        /// <summary>
        /// Drops the preview/package object back on the ground.
        /// </summary>
        public void Drop(bool withDestroy = false)
        {
            AudioSystem.Play3D(DeliverySystem.Instance.dropClip, PlayerController.Instance.hands.position);
            PlayerController.Instance.Drop(withDestroy);
            ResetMode();
        }


        //clears everything interaction related and resets back to defaults
        private void ResetMode()
        {
            if (previewObject != null)
                Destroy(previewObject.gameObject);

            UIGame.RemoveAction("LeftClick");
            UIGame.RemoveAction("RightClick");
            UIGame.RemoveAction("F");

            selectedPrefab = null;
            previewRotationState = 0;
            previewObjectConfig = null;

            activeMode = PlacementMode.Inactive;
            onBuildActivated(false);
        }


        //try to raycast on StorageGrid, and if hit, create preview object for placement
        private void CreateAndMovePreview()
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red);

            if (Physics.Raycast(ray, out RaycastHit hit, rayLength, layerMask))
            {
                targetPosition = SnapToGrid(hit.point);

                previewTransform.gameObject.SetActive(true);
                previewTransform.position = Vector3.Lerp(previewTransform.position, targetPosition, Time.deltaTime * lerpSpeed);
                previewTransform.rotation = Quaternion.Lerp(previewTransform.rotation, Quaternion.Euler(0, 90 * previewRotationState, 0), Time.deltaTime * lerpSpeed);

                activeMode = IsPlaceable(targetPosition, GetRotatedSize()) ? PlacementMode.Valid : PlacementMode.Invalid;
                for(int i = 0; i < previewObject.previewRenderers.Length; i++)
                    previewObject.previewRenderers[i].material = activeMode == PlacementMode.Valid ? validMaterial : invalidMaterial;

                if (activeMode == PlacementMode.Valid)
                    UIGame.AddAction("LeftClick", "Place", true);
                
                UIGame.AddAction("RightClick", "Rotate", true);
            }
            else
            {
                activeMode = PlacementMode.Outside;
            }

            if ((int)activeMode < 2)
            {
                previewTransform.gameObject.SetActive(false);
            }

            //remove actions only in case we do not focus another allowed, interactable object (like the trash container)
            if (InteractionSystem.Instance.currentInteractable == null || !InteractionSystem.Instance.currentInteractable.ShouldSkipSystemChecks())
            {
                if (activeMode != PlacementMode.Valid) UIGame.RemoveAction("LeftClick"); //stay only visible when valid
                if (activeMode == PlacementMode.Outside) UIGame.RemoveAction("RightClick"); //also stay visible when valid, and invalid
            }
        }


        //returns whether an object with a specified size fits onto a grid at a world position
        private bool IsPlaceable(Vector3 origin, Vector2Int size)
        {
            bool canPlace = true;

            RaycastGridCells(origin, size, (grid, cellIndex) =>
            {
                if (grid == null || grid.CheckCell(cellIndex))
                {
                    canPlace = false;
                    return;
                }
            });

            return canPlace;
        }


        //marks cells that are affected by the object size as free or occupied
        private void SetOccupied(Vector3 origin, Vector2Int size, bool state)
        {
            RaycastGridCells(origin, size, (grid, cellIndex) =>
            {
                if (grid != null)
                {
                    grid.SetOccupied(cellIndex, state);
                }
            });
        }


        //does raycasts to find corresponding grid and affected cell indices using object position and size 
        private void RaycastGridCells(Vector3 origin, Vector2Int size, Action<StorageGrid, Vector2Int> onCellHit)
        {
            Vector2Int cellIndex = Vector2Int.zero;
            Vector3 cellOffset;
            Vector3 localPos;
            Ray ray;

            // Calculate bottom-left offset from center
            Vector3 originOffset = origin - new Vector3(size.x / 2 * cellSize, 0f, size.y / 2 * cellSize);

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    cellOffset = originOffset + new Vector3(x * cellSize, 0, z * cellSize);
                    ray = new Ray(cellOffset + Vector3.up, Vector3.down);

                    if (Physics.Raycast(ray, out RaycastHit hit, rayLength, layerMask))
                    {
                        StorageGrid grid = grids[hit.transform];
                        localPos = grid.transform.InverseTransformPoint(cellOffset);
                        cellIndex.x = (int)(localPos.x / cellSize);
                        cellIndex.y = (int)(localPos.z / cellSize);

                        onCellHit?.Invoke(grid, cellIndex);
                    }
                    else
                        onCellHit?.Invoke(null, default);
                }
            }
        }


        //snap the position passed in to increments of cellSize
        private Vector3 SnapToGrid(Vector3 position)
        {
            int x = Mathf.RoundToInt(position.x / cellSize);
            int z = Mathf.RoundToInt(position.z / cellSize);
            return new Vector3(x * cellSize, position.y, z * cellSize);
        }

        
        //cycle through rotation state (0, 1, 2, 3)
        private void Rotate()
        {
            previewRotationState = (previewRotationState + 1) % 4;
        }


        //returns the rotation state based on the absolute rotation passed in
        private int GetRotationState(float fromRotation)
        {
            return ((int)(fromRotation / 90) % 4 + 4) % 4;
        }


        //returns the rotated size of the previewed object based on the previewRotationState
        private Vector2Int GetRotatedSize()
        {
            Vector2Int objectSize = previewObject.size;
            return previewRotationState % 2 == 0 ? objectSize : new Vector2Int(objectSize.y, objectSize.x);
        }


        //react on user input
        private void OnAction(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                switch(context.action.name)
                {
                    case "LeftClick":
                        if (activeMode == PlacementMode.Valid)
                            Place();
                        break;
                    case "RightClick":
                        if (previewTransform != null)
                            Rotate();
                        break;
                    case "Action":
                        if (activeMode != PlacementMode.Inactive)
                            Drop();
                        break;
                }
            }
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();

            JSONNode objectArray = new JSONArray();

            StorageObject[] objects = FindObjectsByType<StorageObject>(FindObjectsSortMode.None);
            for(int i = 0; i < objects.Length; i++)
                objectArray[i] = objects[i].SaveToJSON();

            data["StorageObjects"] = objectArray;

            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
            {
                //in case of a new game, initialize preplaced StorageObjects
                for(int i = 0; i < preplacedObjects.Count; i++)
                {
                    StorageObject thisObj = preplacedObjects[i];
                    thisObj.rotationState = GetRotationState(thisObj.transform.rotation.y);

                    Vector3 objectPosition = thisObj.transform.position;
                    Vector2Int rotatedSize = thisObj.rotationState % 2 == 0 ? preplacedObjects[i].size : new Vector2Int(thisObj.size.y, thisObj.size.x);
                    SetOccupied(objectPosition, rotatedSize, true);
                }

                return;
            }

            //destroy preplaced StorageObjects since we have them already in a game save
            for(int i = 0; i < preplacedObjects.Count; i++)
                Destroy(preplacedObjects[i].gameObject);

            //continue with saved data
            JSONArray objectsArray = data["StorageObjects"].AsArray;
            for(int i = 0; i < objectsArray.Count; i++)
            {
                StorageScriptableObject scriptable = ItemDatabase.GetById(typeof(StorageScriptableObject), objectsArray[i]["ScriptableObject"]["id"]) as StorageScriptableObject;

                GameObject go = Instantiate(scriptable.prefab);
                StorageObject obj = go.GetComponent<StorageObject>();
                obj.LoadFromJSON(objectsArray[i]);

                Vector3 position = objectsArray[i]["Transform"]["position"];
                Vector2Int rotatedSize = obj.rotationState % 2 == 0 ? obj.size : new Vector2Int(obj.size.y, obj.size.x);
                SetOccupied(position, rotatedSize, true);
            }
        }
    }
}