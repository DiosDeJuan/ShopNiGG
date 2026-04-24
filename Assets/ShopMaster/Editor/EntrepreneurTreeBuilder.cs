// ShopMaster - Entrepreneur Skill Tree System
// Editor-only utility that auto-creates all 36 EntrepreneurNodeData ScriptableObjects
// and one EntrepreneurTreeData asset containing them all.
//
// How to run:
//   Unity menu  →  ShopMaster  →  Build Entrepreneur Tree
//
// Output (created inside the project):
//   Assets/ShopMaster/ScriptableObjects/EntrepreneurTree/
//     EntrepreneurTreeData.asset       ← assign this to EntrepreneurTreeManager.treeData
//     Nodes/
//       (36 × NodeData_<id>.asset)
//
// Running the tool a second time REPLACES existing assets (safe to re-run after edits).

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ShopMaster.Editor
{
    public static class EntrepreneurTreeBuilder
    {
        private const string RootFolder  = "Assets/ShopMaster/ScriptableObjects/EntrepreneurTree";
        private const string NodesFolder = "Assets/ShopMaster/ScriptableObjects/EntrepreneurTree/Nodes";
        private const string TreeAsset   = "Assets/ShopMaster/ScriptableObjects/EntrepreneurTree/EntrepreneurTreeData.asset";


        // ── Menu entry ────────────────────────────────────────────────────────────

        [MenuItem("ShopMaster/Build Entrepreneur Tree")]
        public static void BuildTree()
        {
            EnsureFolders();

            List<EntrepreneurNodeData> allNodes = new List<EntrepreneurNodeData>();

            // ── Products ─────────────────────────────────────────────────────────

            allNodes.Add(MakeNode(
                id:          "products_basic_1",
                displayName: "Productos Básicos 1",
                description: "Desbloquea: leche, sal, agua, pasta, azúcar.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(50f, -200f),
                cost:        1,
                requires:    new string[0],
                gameplayKey: "products_basic_1"
            ));

            allNodes.Add(MakeNode(
                id:          "products_basic_2",
                displayName: "Productos Básicos 2",
                description: "Desbloquea: harina, arroz, frijoles, pan, aceite.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(300f, -200f),
                cost:        1,
                requires:    new[] { "products_basic_1" },
                gameplayKey: "products_basic_2"
            ));

            allNodes.Add(MakeNode(
                id:          "dairy_1",
                displayName: "Lácteos 1",
                description: "Desbloquea: cheddar, yogurt natural, mantequilla.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(550f, -100f),
                cost:        1,
                requires:    new[] { "products_basic_2" },
                gameplayKey: "dairy_1"
            ));

            allNodes.Add(MakeNode(
                id:          "spices_1",
                displayName: "Especias 1",
                description: "Desbloquea: pimienta negra, canela.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(550f, -200f),
                cost:        1,
                requires:    new[] { "products_basic_2" },
                gameplayKey: "spices_1"
            ));

            allNodes.Add(MakeNode(
                id:          "fresh_1",
                displayName: "Productos Frescos 1",
                description: "Desbloquea: manzana, plátanos, jitomate, cebollas.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(550f, -300f),
                cost:        1,
                requires:    new[] { "products_basic_2" },
                gameplayKey: "fresh_1"
            ));

            allNodes.Add(MakeNode(
                id:          "dairy_2",
                displayName: "Lácteos 2",
                description: "Desbloquea: queso americano, queso crema.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(800f, -100f),
                cost:        1,
                requires:    new[] { "dairy_1" },
                gameplayKey: "dairy_2"
            ));

            allNodes.Add(MakeNode(
                id:          "products_basic_3",
                displayName: "Productos Básicos 3",
                description: "Desbloquea: café, huevo.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(800f, -200f),
                cost:        1,
                requires:    new[] { "spices_1" },
                gameplayKey: "products_basic_3"
            ));

            allNodes.Add(MakeNode(
                id:          "fresh_2",
                displayName: "Productos Frescos 2",
                description: "Desbloquea: uvas, zanahorias, ajo.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(800f, -300f),
                cost:        1,
                requires:    new[] { "fresh_1" },
                gameplayKey: "fresh_2"
            ));

            allNodes.Add(MakeNode(
                id:          "hygiene",
                displayName: "Productos de Higiene",
                description: "Desbloquea: jabón, papel higiénico, detergente, pasta de dientes.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(1050f, -150f),
                cost:        1,
                requires:    new[] { "products_basic_3" },
                gameplayKey: "hygiene"
            ));

            allNodes.Add(MakeNode(
                id:          "sodas",
                displayName: "Sodas",
                description: "Desbloquea: cola, cola sin azúcar, refresco de limón.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(1050f, -250f),
                cost:        1,
                requires:    new[] { "products_basic_3" },
                gameplayKey: "sodas"
            ));

            allNodes.Add(MakeNode(
                id:          "protein_1",
                displayName: "Proteína 1",
                description: "Desbloquea: res, pollo, cerdo, pescado.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(1050f, -350f),
                cost:        1,
                requires:    new[] { "fresh_2" },
                gameplayKey: "protein_1"
            ));

            allNodes.Add(MakeNode(
                id:          "luxury_1",
                displayName: "Productos de Lujo 1",
                description: "Desbloquea: trufa, chocolate importado, caviar.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(1300f, -200f),
                cost:        1,
                requires:    new[] { "sodas" },
                gameplayKey: "luxury_1"
            ));

            allNodes.Add(MakeNode(
                id:          "appliances_1",
                displayName: "Electrodomésticos 1",
                description: "Desbloquea: refrigeradores, microondas, hornos, licuadora.",
                nodeType:    EntrepreneurNodeType.Product,
                uiPosition:  new Vector2(1300f, -350f),
                cost:        1,
                requires:    new[] { "protein_1" },
                gameplayKey: "appliances_1"
            ));

            // ── Employees ────────────────────────────────────────────────────────
            // NOTE: Empleado 12 depends on Empleado 13 (as documented; may be a design
            //       inconsistency but is intentionally respected here).

            allNodes.Add(MakeNode(
                id:          "employee_1",
                displayName: "Empleado 1",
                description: "Primer empleado. Requiere haber desbloqueado Especias 1.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(600f, -450f),
                cost:        1,
                requires:    new[] { "spices_1" },
                gameplayKey: "employee_1"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_2",
                displayName: "Empleado 2",
                description: "Requiere Productos de Higiene.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1150f, -100f),
                cost:        1,
                requires:    new[] { "hygiene" },
                gameplayKey: "employee_2"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_3",
                displayName: "Empleado 3",
                description: "Requiere Sodas.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1200f, -300f),
                cost:        1,
                requires:    new[] { "sodas" },
                gameplayKey: "employee_3"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_4",
                displayName: "Empleado 4",
                description: "Requiere Lácteos 1.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(600f, -50f),
                cost:        1,
                requires:    new[] { "dairy_1" },
                gameplayKey: "employee_4"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_5",
                displayName: "Empleado 5",
                description: "Requiere Lácteos 1.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(700f, -50f),
                cost:        1,
                requires:    new[] { "dairy_1" },
                gameplayKey: "employee_5"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_6",
                displayName: "Empleado 6",
                description: "Requiere Especias 1.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(700f, -450f),
                cost:        1,
                requires:    new[] { "spices_1" },
                gameplayKey: "employee_6"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_7",
                displayName: "Empleado 7",
                description: "Requiere Empleado 5.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(850f, -50f),
                cost:        1,
                requires:    new[] { "employee_5" },
                gameplayKey: "employee_7"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_8",
                displayName: "Empleado 8",
                description: "Requiere Sodas.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1200f, -400f),
                cost:        1,
                requires:    new[] { "sodas" },
                gameplayKey: "employee_8"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_9",
                displayName: "Empleado 9",
                description: "Requiere Productos de Higiene.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1150f, -200f),
                cost:        1,
                requires:    new[] { "hygiene" },
                gameplayKey: "employee_9"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_10",
                displayName: "Empleado 10",
                description: "Requiere Empleado 1.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(750f, -450f),
                cost:        1,
                requires:    new[] { "employee_1" },
                gameplayKey: "employee_10"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_11",
                displayName: "Empleado 11",
                description: "Requiere Seguridad 1.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1150f, 50f),
                cost:        1,
                requires:    new[] { "security_1" },
                gameplayKey: "employee_11"
            ));

            // ⚠ NOTE: Empleado 12 depends on Empleado 13 (documented dependency, potentially
            //   a design-time reversal — kept as-is per specification).
            allNodes.Add(MakeNode(
                id:          "employee_12",
                displayName: "Empleado 12",
                description: "Requiere Empleado 13.\n⚠ Dependencia inversa (ver especificación).\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1650f, -200f),
                cost:        1,
                requires:    new[] { "employee_13" },
                gameplayKey: "employee_12"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_13",
                displayName: "Empleado 13",
                description: "Requiere Productos de Lujo 1.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1550f, -200f),
                cost:        1,
                requires:    new[] { "luxury_1" },
                gameplayKey: "employee_13"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_14",
                displayName: "Empleado 14",
                description: "Requiere Electrodomésticos 1.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1550f, -450f),
                cost:        1,
                requires:    new[] { "appliances_1" },
                gameplayKey: "employee_14"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_15",
                displayName: "Empleado 15",
                description: "Requiere Seguridad 2.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1550f, -350f),
                cost:        1,
                requires:    new[] { "security_2" },
                gameplayKey: "employee_15"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_16",
                displayName: "Empleado 16",
                description: "Requiere Proteína 1.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1100f, -450f),
                cost:        1,
                requires:    new[] { "protein_1" },
                gameplayKey: "employee_16"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_17",
                displayName: "Empleado 17",
                description: "Requiere Productos Frescos 2.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(900f, -450f),
                cost:        1,
                requires:    new[] { "fresh_2" },
                gameplayKey: "employee_17"
            ));

            allNodes.Add(MakeNode(
                id:          "employee_18",
                displayName: "Empleado 18",
                description: "Requiere Seguridad 3.\nTODO: asignar rol en EmployeeSystem.",
                nodeType:    EntrepreneurNodeType.Employee,
                uiPosition:  new Vector2(1850f, -450f),
                cost:        1,
                requires:    new[] { "security_3" },
                gameplayKey: "employee_18"
            ));

            // ── Security ─────────────────────────────────────────────────────────

            allNodes.Add(MakeNode(
                id:          "security_1",
                displayName: "Seguridad 1",
                description: "Cámaras de seguridad. Arresto automático 33%.\nRequiere Empleado 7.",
                nodeType:    EntrepreneurNodeType.Security,
                uiPosition:  new Vector2(1000f, 50f),
                cost:        1,
                requires:    new[] { "employee_7" },
                gameplayKey: "1"
            ));

            allNodes.Add(MakeNode(
                id:          "security_2",
                displayName: "Seguridad 2",
                description: "Guardias de seguridad. Arresto automático 66%.\nRequiere Empleado 8.",
                nodeType:    EntrepreneurNodeType.Security,
                uiPosition:  new Vector2(1400f, -400f),
                cost:        1,
                requires:    new[] { "employee_8" },
                gameplayKey: "2"
            ));

            allNodes.Add(MakeNode(
                id:          "security_3",
                displayName: "Seguridad 3",
                description: "Alarmas y arcos antihurto. Arresto automático 99%.\nRequiere Empleado 14.",
                nodeType:    EntrepreneurNodeType.Security,
                uiPosition:  new Vector2(1700f, -450f),
                cost:        1,
                requires:    new[] { "employee_14" },
                gameplayKey: "3"
            ));

            // ── Upgrades ─────────────────────────────────────────────────────────

            allNodes.Add(MakeNode(
                id:          "upgrade_caffeine",
                displayName: "Cafeína",
                description: "+10% velocidad de empleados.\nRequiere Productos Frescos 2.",
                nodeType:    EntrepreneurNodeType.Upgrade,
                uiPosition:  new Vector2(900f, -600f),
                cost:        1,
                requires:    new[] { "fresh_2" },
                gameplayKey: "caffeine"
            ));

            allNodes.Add(MakeNode(
                id:          "upgrade_charismatic",
                displayName: "Carismático",
                description: "+5% ventas del cajero.\nRequiere Empleado 15.",
                nodeType:    EntrepreneurNodeType.Upgrade,
                uiPosition:  new Vector2(1700f, -350f),
                cost:        1,
                requires:    new[] { "employee_15" },
                gameplayKey: "charismatic"
            ));

            // ── Create / Update TreeData asset ────────────────────────────────────

            EntrepreneurTreeData treeData = AssetDatabase.LoadAssetAtPath<EntrepreneurTreeData>(TreeAsset);
            if (treeData == null)
            {
                treeData = ScriptableObject.CreateInstance<EntrepreneurTreeData>();
                AssetDatabase.CreateAsset(treeData, TreeAsset);
            }

            treeData.nodes = allNodes;
            EditorUtility.SetDirty(treeData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EntrepreneurTreeBuilder] Tree built successfully with {allNodes.Count} nodes.\n" +
                      $"Asset: {TreeAsset}");

            // Select the asset in the Project window for convenience.
            Selection.activeObject = treeData;
        }


        // ── Helpers ───────────────────────────────────────────────────────────────

        private static EntrepreneurNodeData MakeNode(
            string   id,
            string   displayName,
            string   description,
            EntrepreneurNodeType nodeType,
            Vector2  uiPosition,
            int      cost,
            string[] requires,
            string   gameplayKey)
        {
            string assetPath = $"{NodesFolder}/NodeData_{id}.asset";

            EntrepreneurNodeData node = AssetDatabase.LoadAssetAtPath<EntrepreneurNodeData>(assetPath);
            if (node == null)
            {
                node = ScriptableObject.CreateInstance<EntrepreneurNodeData>();
                AssetDatabase.CreateAsset(node, assetPath);
            }

            node.id           = id;
            node.displayName  = displayName;
            node.description  = description;
            node.nodeType     = nodeType;
            node.uiPosition   = uiPosition;
            node.cost         = cost;
            node.gameplayKey  = gameplayKey;
            node.requiredNodeIds = new System.Collections.Generic.List<string>(requires);
            // icon left null — assign in Inspector after importing sprites.

            EditorUtility.SetDirty(node);
            return node;
        }


        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                string parent = "Assets/ShopMaster/ScriptableObjects";
                if (!AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder("Assets/ShopMaster", "ScriptableObjects");

                AssetDatabase.CreateFolder(parent, "EntrepreneurTree");
            }

            if (!AssetDatabase.IsValidFolder(NodesFolder))
                AssetDatabase.CreateFolder(RootFolder, "Nodes");
        }
    }
}
#endif
