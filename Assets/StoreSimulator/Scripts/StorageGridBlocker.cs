/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Allows blocking specific cells that should not be available on a StorageGrid parent on start.
    /// </summary>
    public class StorageGridBlocker : MonoBehaviour
    {
        /// <summary>
        /// Count of tiles to block on the x/z axis.
        /// </summary>
        public Vector2Int size;

        //reference to the parent grid component
        private StorageGrid grid;


        //initialize references
        void Awake()
        {
            grid = transform.parent.GetComponent<StorageGrid>();
        }


        //initialize variables
        void Start()
        {
            Vector3 localPos = grid.transform.InverseTransformPoint(transform.position);
            Vector2Int cellIndex = new Vector2Int((int)(localPos.x / StorageSystem.cellSize), (int)(localPos.z / StorageSystem.cellSize));

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    grid.SetOccupied(cellIndex + new Vector2Int(x, z), true);
                }
            }
        }


        //draw some gizmos in the editor
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 0.1f);
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawCube(new Vector3(size.x * StorageSystem.cellSize / 2, 0.025f, size.y * StorageSystem.cellSize / 2),
                            new Vector3(size.x * StorageSystem.cellSize, 0.05f, size.y * StorageSystem.cellSize));
        }
    }
}