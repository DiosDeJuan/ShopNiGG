/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// An object that can be placed on a StorageGrid and offers PlacementObject(s) as storage for products.
    /// </summary>
    public class StorageObject : Interactable
    {
        /// <summary>
        /// Reference to the data that applies to this object.
        /// </summary>
        public StorageScriptableObject storage;

        /// <summary>
        /// The size of this object, used to calculate and occupy grid cells when placed.
        /// </summary>
        public Vector2Int size;

        /// <summary>
        /// Reference to the visual object that displays the bounding area of the object.
        /// </summary>
        public GameObject groundLine;

        /// <summary>
        /// All references to renderers for switching them to green or red when using this object as a preview.
        /// </summary>
        public Renderer[] previewRenderers;

        /// <summary>
        /// Whether PlacementObject references should be retrieved from child transforms automatically.
        /// </summary>
        public bool placementFromChildren = true;

        /// <summary>
        /// Reference to PlacementObject components this object has.
        /// </summary>
        public PlacementObject[] placements;

        /// <summary>
        /// Tracks the 90-degree rotation state. 0 = not rotated, 1 = 90 degrees, 2 = 180, 3 = 270
        /// </summary>
        [HideInInspector]
        public int rotationState = 0;
        

        //initialize references
        void Awake()
        {
            groundLine.SetActive(false);

            if (placementFromChildren)
            {
                List<PlacementObject> list = new List<PlacementObject>(placements);
                list.AddRange(GetComponentsInChildren<PlacementObject>());
                placements = list.ToArray();
            }

            StorageSystem.onBuildActivated += OnEnterBuildMode;
        }


        //initialize variables
        void Start()
        {
            //do not enable this object's colliders if we're in preview
            //only do it for an object when it is placed on a grid
            if (StorageSystem.Instance.previewObject != this)
            {
                GetComponent<Collider>().enabled = true;
                GetComponent<NavMeshObstacle>().enabled = true;
            }
        }


        /// <summary>
        /// Interactable override, adding UI action.
        /// </summary>
        public override void OnBecameFocus()
        {
            UIGame.AddAction("LeftClick", "Move", true);
        }


        /// <summary>
        /// Interactable override, react on player interaction.
        /// This object can get picked up by the player.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick") return false;

            UIGame.RemoveAction("LeftClick");
            UIGame.AddAction("F", "Drop");

            StorageSystem.Instance.PickUp(this);
            return true;
        }


        /// <summary>
        /// Interactable override, removing UI action.
        /// </summary>
        public override void OnLostFocus()
        {
            UIGame.RemoveAction("LeftClick");
        }


        //toggle display of bounding area in build mode
        private void OnEnterBuildMode(bool state)
        {
            groundLine.SetActive(state);
        }


        //draw some gizmos in the editor
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 1f);

            if (Mathf.RoundToInt(transform.eulerAngles.y) % 180 == 0)
                Gizmos.DrawWireCube(transform.position, new Vector3(size.x * StorageSystem.cellSize, 0.001f, size.y * StorageSystem.cellSize));
            else
                Gizmos.DrawWireCube(transform.position, new Vector3(size.y * StorageSystem.cellSize, 0.001f, size.x * StorageSystem.cellSize));
        }


        //unsubscribe from events
        void OnDestroy()
        {
            StorageSystem.onBuildActivated -= OnEnterBuildMode;
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
            data["Transform"]["position"] = transform.position;
            data["Transform"]["rotation"] = transform.rotation;

            data["ScriptableObject"]["id"] = storage.id;
            data["rotationState"] = rotationState;

            JSONNode placementArray = new JSONArray();
            for(int i = 0; i < placements.Length; i++)
            {
                JSONNode placementContent = new JSONObject();
                if (placements[i].product != null)
                {
                    placementContent["index"] = i;
                    placementContent["productId"] = placements[i].product.id;
                    placementContent["count"] = placements[i].count;
                }

                placementArray[i] = placementContent;
            }
            data["PlacementObjects"] = placementArray;

            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data.HasKey("Transform"))
            {
                transform.position = data["Transform"]["position"];
                transform.rotation = data["Transform"]["rotation"];
            }

            if (data.HasKey("rotationState"))
                rotationState = data["rotationState"].AsInt;

            JSONArray placementArray = data["PlacementObjects"].AsArray;
            for(int i = 0; i < placementArray.Count; i++)
            {
                if (!placementArray[i].HasKey("productId"))
                    continue;

                ProductScriptableObject product = ItemDatabase.GetById(typeof(ProductScriptableObject), placementArray[i]["productId"]) as ProductScriptableObject;
                PlacementObject placement = placements[placementArray[i]["index"].AsInt];
               
                int count = placementArray[i]["count"].AsInt;
                Quaternion worldRotation = transform.rotation * Quaternion.Euler(0, placement.orientation, 0);

                for(int j = 0; j < count; j++)
                {
                    Vector3 localPosition = placement.Add(product);
                    Vector3 worldPosition = placement.container.TransformPoint(localPosition);
                    Instantiate(product.prefab, worldPosition, worldRotation, placement.container);
                }
            }
        }
    }
}
