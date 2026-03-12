/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// A grid that consists of individual cells where StorageSystem can place StorageObject on it.
    /// </summary>
    public class StorageGrid : MonoBehaviour
    {
        /// <summary>
        /// Count of tiles to instantiate on the x/z axis.
        /// </summary>
        public Vector2Int size;

        //a multidimensional array describing the state of each cell in the grid
        private bool[,] cells;

        //reference to the collider component
        private BoxCollider col;

        //reference to the mesh renderer component
        private MeshRenderer meshRenderer;


        //initialize references
        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            col = GetComponent<BoxCollider>();
            col.size = new Vector3(StorageSystem.cellSize * size.x, 0, StorageSystem.cellSize * size.y);
            col.center = col.size / 2;

            cells = new bool[size.x, size.y];
        }


        //initialize variables
        void Start()
        {
            CreateMesh();

            StorageSystem.onBuildActivated += OnBuildActivate;
        }


        //toggle visibility of the grid renderer
        private void OnBuildActivate(bool state)
        {
            meshRenderer.enabled = state;
        }


        /// <summary>
        /// Does a collider check whether a point is within its bounds.
        /// </summary>
        public bool IsPositionOnGrid(Vector3 hitPoint)
        {
            return col.ClosestPoint(hitPoint) == hitPoint;
        }


        /// <summary>
        /// Checks a specific cell in the cells array, returning true when occupied.
        /// </summary>
        public bool CheckCell(Vector2Int index)
        {
            try
            {
                return cells[index.x, index.y];
            }
            catch (IndexOutOfRangeException)
            {
                return true;
            }
        }


        /// <summary>
        /// Mark a single cell as free or occupied.
        /// </summary>
        public void SetOccupied(Vector2Int index, bool state)
        {
            try
            {
                cells[index.x, index.y] = state;
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
        }


        //dynamically creates mesh for the renderer enabled at runtime
        //this is just a quad with full size uv
        private void CreateMesh()
        {
            meshRenderer.material.mainTextureScale = size;
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            float offsetY = 0.0f;

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(0, offsetY, 0), new Vector3(StorageSystem.cellSize * size.x, offsetY, 0),
                new Vector3(0, offsetY, StorageSystem.cellSize * size.y), new Vector3(StorageSystem.cellSize * size.x, offsetY, StorageSystem.cellSize * size.y)
            };
            
            int[] tris = new int[6] { 0, 2, 1, 2, 3, 1 };
            Vector3[] normals = new Vector3[4] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
            Vector2[] uv = new Vector2[4] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };

            mesh.vertices = vertices;
            mesh.triangles = tris;
            mesh.normals = normals;          
            mesh.uv = uv;
            meshFilter.mesh = mesh;
        }


        //draw some gizmos in the editor
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 1, 0, 0.1f);
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawCube(new Vector3(size.x * StorageSystem.cellSize / 2, 0.025f, size.y * StorageSystem.cellSize / 2),
                            new Vector3(size.x * StorageSystem.cellSize, 0.05f, size.y * StorageSystem.cellSize));

            Gizmos.color = new Color(1, 1, 0, 0.1f);
            Gizmos.DrawWireCube(new Vector3(size.x * StorageSystem.cellSize / 2, 0.025f, size.y * StorageSystem.cellSize / 2),
                            new Vector3(size.x * StorageSystem.cellSize, 0.05f, size.y * StorageSystem.cellSize));

            Gizmos.color = new Color(1, 0, 0, 0.1f);
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    if (Application.isPlaying && cells[x, z] == true)
                        Gizmos.DrawCube(new Vector3(StorageSystem.cellSize / 2 + x * StorageSystem.cellSize, 0.025f, StorageSystem.cellSize / 2 + z * StorageSystem.cellSize),
                                        new Vector3(StorageSystem.cellSize, 0.05f, StorageSystem.cellSize));
                }
            }
        }


        //unsubscribe from events
        void OnDestroy()
        {
            StorageSystem.onBuildActivated -= OnBuildActivate; 
        }
    }
}