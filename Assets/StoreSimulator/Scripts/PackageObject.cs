/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Component for a package that can contain products, storage, or other items to be placed in the game.
    /// Some of the variables and method are nearly identical to those in PlacementObject.
    /// </summary>
    public class PackageObject : Interactable
    {
        /// <summary>
        /// Event fired when items have been added or subtracted from this package. Total value.
        /// </summary>
        public event Action<int> onCountChanged;

        /// <summary>
        /// Event fired when the currently assigned PurchasableScriptableObject changed.
        /// </summary>
        public event Action<PurchasableScriptableObject> onPurchasableChanged;

        /// <summary>
        /// A reference to the purchasable inside the package.
        /// </summary>
        public PurchasableScriptableObject purchasable;

        /// <summary>
        /// Select whether row or column should be first filled when stocking products.
        /// </summary>
        public Axis fillOrder = Axis.X;

        /// <summary>
        /// The maximum available space when placing default sized 1x1 products.
        /// </summary>
        public Vector2Int space = Vector2Int.one;

        /// <summary>
        /// Reference to the packaging label displaying an icon of the object inside.
        /// </summary>
        public MeshRenderer label;

        /// <summary>
        /// Parent transform of all individual child position transforms.
        /// </summary>
        public Transform container;

        /// <summary>
        /// Count of items that are currently populated on child positions.
        /// </summary>
        public int count { get; private set;}

        //cache of positions based on product bounds to not recalculate on every add/remove
        private Vector3[] placementPositions = new Vector3[0];


        //initialize references
        void Awake()
        {
            onPurchasableChanged += OnPurchasableChanged;
        }


        /// <summary>
        /// Assigns a new or adds one more purchasable object to the package.
        /// The item is inserted at the next position on the last row/column.
        /// </summary>
        public Vector3 Add(PurchasableScriptableObject newPurchasable)
        {
            if (count == 0)
            {
                placementPositions = GetPlacementPositions(newPurchasable);
                purchasable = newPurchasable;
                onPurchasableChanged?.Invoke(newPurchasable);
            }

            Vector3 position = placementPositions[count];
            count++;
            onCountChanged?.Invoke(count);

            return position;
        }


        /// <summary>
        /// Assigns a new or adds multiple items of a purchasable to the package.
        /// This is specifically used when loading a game to pre-fill existing packages in the scene.
        /// </summary>
        public void Add(PurchasableScriptableObject newPurchasable, int amount)
        {
            switch(newPurchasable)
            {
                case StorageScriptableObject:
                    Vector3 storagePosition = container.TransformPoint(Add(newPurchasable));
                    Instantiate(StorageSystem.Instance.packageContent, storagePosition, transform.rotation, container);
                    break;

                case ProductScriptableObject:
                    ProductScriptableObject product = newPurchasable as ProductScriptableObject;
                    for(int i = 0; i < amount; i++)
                    {
                        if (!IsPlaceable(product))
                            break;

                        Vector3 itemPosition = container.TransformPoint(Add(newPurchasable));
                        Instantiate(product.prefab, itemPosition, transform.rotation, container);
                    }
                    break;
            }
        }


        /// <summary>
        /// Removes one item from the package. If empty, removes the purchasable reference too.
        /// The position removed is the first populated position from the start.
        /// </summary>
        public Transform Remove()
        {
            count--;
            onCountChanged?.Invoke(count);
            
            if (count == 0)
            {
                placementPositions = new Vector3[0];
                purchasable = null;
                onPurchasableChanged?.Invoke(purchasable);
            }

            return container.GetChild(count);
        }


        /// <summary>
        /// Returns whether the current fill count does not exceed the maximum count.
        /// </summary>
        public bool IsPlaceable(PurchasableScriptableObject p)
        {
            if (IsEmpty())
                return GetPlacementPositions(p).Length > 0;

            return count < placementPositions.Length;
        }


        /// <summary>
        /// Returns whether there are no items inside the package.
        /// </summary>
        public bool IsEmpty()
        {
            return count == 0;
        }


        /// <summary>
        /// Returns all possible placement positions based on the storage or product bounds passed in.
        /// </summary>
        public Vector3[] GetPlacementPositions(PurchasableScriptableObject p = null)
        {
            ProductScriptableObject product = null;
            if (p is ProductScriptableObject) product = p as ProductScriptableObject;
            if (p == null && purchasable != null && purchasable is ProductScriptableObject)
                product = purchasable as ProductScriptableObject;
            
            //the product bounds, or maximum if its not a product
            Vector2Int size = product ? product.size : space;
            //the available area of this box
            Vector2 area = new Vector2(space.x, space.y) * PlacementSystem.cellSize;
            //the size of one product when taking cellSize into account
            Vector2 minCells = new Vector2(size.x, size.y) * PlacementSystem.cellSize;
            //the cells that would be taken up when fully occupied in the area, clamped to integer values
            Vector2Int maxCells = new Vector2Int(Mathf.FloorToInt(area.x / minCells.x), Mathf.FloorToInt(area.y / minCells.y));

            if (maxCells.x < 1 || maxCells.y < 1)
            {
                Debug.LogWarning("Item size too large for placement area.");
                return new Vector3[0];
            }

            //count of total positions
            int maxSlots = maxCells.x * maxCells.y;
            Vector3[] result = new Vector3[maxSlots];

            //calculate actual occupied and free cells, and starting offset based on that
            Vector2 occupied = new Vector2(minCells.x * maxCells.x, minCells.y * maxCells.y);
            Vector2 free = new Vector2(area.x - occupied.x, area.y - occupied.y);
            Vector3 offset = new Vector3(-area.x + free.x + minCells.x, -area.y + free.y + minCells.y) / 2f;

            //fill placement positions array
            for (int i = 0; i < maxSlots; i++)
            {
                int row, col;

                if (fillOrder == Axis.X)
                {
                    row = i / maxCells.x;
                    col = i % maxCells.x;
                }
                else
                {
                    col = i / maxCells.y;
                    row = i % maxCells.y;
                }
            
                result[i] = new Vector3(
                    offset.x + col * minCells.x,
                    0,
                    offset.y + row * minCells.y
                );
            }

            return result;
        }


        /// <summary>
        /// Interactable override, adding UI action.
        /// </summary>
        public override void OnBecameFocus()
        {
            UIGame.AddAction("LeftClick", "Pick Up", true);
        }


        /// <summary>
        /// Interactable override, react on player interaction.
        /// This object can be unpacked or picked up.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick") return false;

            UIGame.RemoveAction("LeftClick");
            UIGame.AddAction("F", "Drop");

            switch (purchasable)
            {
                case StorageScriptableObject:
                    StorageSystem.Instance.Unpack(this);
                    break;

                case ProductScriptableObject:
                case null: //empty package
                    PlacementSystem.Instance.PickUp(this);
                    break;
            }

            return true;
        }


        /// <summary>
        /// Interactable override, removing UI action.
        /// </summary>
        public override void OnLostFocus()
        {
            UIGame.RemoveAction("LeftClick");
        }


        //subscribed to purchasable change in order to change the package label icon
        private void OnPurchasableChanged(PurchasableScriptableObject newPurchasable)
        {
            if (newPurchasable == null)
            {
                label.enabled = false;
                label.material.mainTexture = null;
            }
            else
            {
                label.material.mainTexture = newPurchasable.icon.texture;
                label.enabled = true;
            }
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
            data["Transform"]["position"] = transform.position;
            data["Transform"]["rotation"] = transform.rotation;

            if (purchasable != null)
            {
                JSONNode content = new JSONObject();
                content["type"] = purchasable.GetType().ToString();
                content["id"] = purchasable.id;
                content["count"] = count;
                data["ScriptableObject"] = content;
            }

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

            if (!data.HasKey("ScriptableObject"))
                return;

            JSONNode content = data["ScriptableObject"];
            Type type = Type.GetType(content["type"]);
            PurchasableScriptableObject scriptable = ScriptableObject.CreateInstance(type) as PurchasableScriptableObject;

            switch(scriptable)
            {
                case StorageScriptableObject:
                    StorageScriptableObject storage = ItemDatabase.GetById(typeof(StorageScriptableObject), content["id"]) as StorageScriptableObject;
                    Add(storage, 0);
                    break;

                case ProductScriptableObject:
                    ProductScriptableObject product = ItemDatabase.GetById(typeof(ProductScriptableObject), content["id"]) as ProductScriptableObject;
                    Add(product, content["count"]);
                    break;
            }
        }


        //draw some gizmos in the editor
        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
           
            for (int i = 0; i < placementPositions.Length; i++)
            {
                Gizmos.DrawLine(placementPositions[i], placementPositions[i] + new Vector3(0, PlacementSystem.cellSize, 0));
            }
        }


        //unsubscribe from events
        void OnDestroy()
        {
            UIGame.RemoveAction("LeftClick");

            onPurchasableChanged -= OnPurchasableChanged;
        }
    }
}
