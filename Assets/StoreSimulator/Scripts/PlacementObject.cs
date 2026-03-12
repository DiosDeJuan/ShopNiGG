/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Component for a placement that stores products that have been placed by the player.
    /// Some of the variables and method are nearly identical to those in PackageObject.
    /// </summary>
    public class PlacementObject : MonoBehaviour
    {
        /// <summary>
        /// Event fired when items have been added or subtracted from this placement. Total value.
        /// </summary>
        public event Action<int> onCountChanged;

        /// <summary>
        /// Event fired when the currently assigned ProductScriptableObject changed.
        /// </summary>
        public event Action<ProductScriptableObject> onProductChanged;
      
        /// <summary>
        /// A reference to the product on the placement.
        /// </summary>
        [HideInInspector]
        public ProductScriptableObject product;

        /// <summary>
        /// The type of storage determining what kind of products can be placed here.
        /// </summary>
        public StorageType storageType;

        /// <summary>
        /// Select whether row or column should be first filled when stocking products.
        /// </summary>
        public Axis fillOrder = Axis.X;

        /// <summary>
        /// The maximum available space when placing default sized 1x1 products.
        /// </summary>
        public Vector2Int space = Vector2Int.one;

        /// <summary>
        /// Local rotation on the Y-axis products should have in the placement to face the customer.
        /// </summary>
        public int orientation;

        /// <summary>
        /// Reference to the renderer that is highlighted when focused.
        /// </summary>
        public MeshRenderer outline;

        /// <summary>
        /// Location for where customers should go to when trying to collect an item.
        /// </summary>
        public Transform grabSpot;

        /// <summary>
        /// Parent transform of all individual child position transforms.
        /// </summary>
        public Transform container;

        /// <summary>
        /// Count of items that are currently populated on child positions.
        /// </summary>
        public int count { get; private set; }

        //cache of positions based on product bounds to not recalculate on every add/remove
        private Vector3[] placementPositions = new Vector3[0];


        //initialize references
        void Awake()
        {
            if (outline != null)
            {
                outline.enabled = false;
                PlacementSystem.onPlacementFocus += OnPlacementFocus;
            }
        }


        /// <summary>
        /// Assigns a new or adds one more product object to the placement.
        /// The item is inserted at the next position on the row/column.
        /// </summary>
        public Vector3 Add(ProductScriptableObject newProduct)
        {
            if (count == 0)
            {
                StoreDatabase.Instance.AddProductPlacement(newProduct, this);
                placementPositions = GetPlacementPositions(newProduct);
                product = newProduct;
                onProductChanged?.Invoke(newProduct);
            }

            Vector3 position = placementPositions[count];
            count++;
            onCountChanged?.Invoke(count);

            return position;
        }


        /// <summary>
        /// Removes one item from the placement. If empty, removes the product reference too.
        /// The item removed is the last one that was placed.
        /// </summary>
        public Transform Remove()
        {
            count--;
            onCountChanged?.Invoke(count);
            
            if (count == 0)
            {
                StoreDatabase.Instance.RemoveProductPlacement(product, this);
                placementPositions = new Vector3[0];
                product = null;
                onProductChanged?.Invoke(product);
            }

            return container.GetChild(count);
        }


        /// <summary>
        /// Returns whether the current fill count does not exceed the maximum count.
        /// </summary>
        public bool IsPlaceable(ProductScriptableObject p)
        {
            if(IsEmpty())
                return GetPlacementPositions(p).Length > 0;

            return count < placementPositions.Length;
        }


        /// <summary>
        /// Returns whether there are no items inside the placement.
        /// </summary>
        public bool IsEmpty()
        {
            return count == 0;
        }


        /// <summary>
        /// Returns all possible placement positions based on the product bounds passed in.
        /// </summary>
        public Vector3[] GetPlacementPositions(ProductScriptableObject p = null)
        {
            if(p == null && product != null)
                p = product;

            //the product bounds, or maximum if its not a product
            Vector2Int size = p ? p.size : space;
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


        //subscribed to listen to the currently focused placement by the player
        private void OnPlacementFocus(PlacementObject placement)
        {
            outline.enabled = placement == this;
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
            PlacementSystem.onPlacementFocus -= OnPlacementFocus;
        }
    }
}