using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// The Entrepreneur Skill Tree is a progression system that shows skills, products, employees,
    /// and upgrades. Each node costs 1 progress point and may require prior nodes to be unlocked.
    /// Progress points are awarded on each level-up and via AddProgressPoints().
    /// Addresses RQF3 and RQF4.
    /// </summary>
    public class EntrepreneurTreeSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static EntrepreneurTreeSystem Instance { get; private set; }

        /// <summary>
        /// Event fired when a skill tree node is unlocked.
        /// </summary>
        public static event Action<SkillTreeNode> onNodeUnlocked;

        /// <summary>
        /// Event fired when the available progress points change.
        /// </summary>
        public static event Action<int> onProgressPointsChanged;
        /// <summary>
        /// Pascal-case alias for external integrations.
        /// </summary>
        public static event Action<int> OnProgressPointsChanged;

        /// <summary>
        /// Fired whenever points are added with a reason (source telemetry/debug).
        /// </summary>
        public static event Action<int, string> onProgressPointsAdded;

        /// <summary>
        /// All nodes in the entrepreneur skill tree. (RQF3)
        /// Populated automatically by InitializeDefaultNodes() if empty at startup.
        /// </summary>
        public List<SkillTreeNode> nodes = new List<SkillTreeNode>();

        /// <summary>
        /// Progress points available to spend on tree nodes.
        /// Awarded by level-ups or external systems via AddProgressPoints().
        /// </summary>
        public int progressPoints { get; private set; }

        /// <summary>
        /// Fallback source: add 1 point on level-up.
        /// </summary>
        public bool useLevelUpFallback = true;

        /// <summary>
        /// Optional point source components implementing IProgressPointSource.
        /// </summary>
        public List<MonoBehaviour> progressPointSourceBehaviours = new List<MonoBehaviour>();
        private readonly List<IProgressPointSource> pointSources = new List<IProgressPointSource>();


        //initialize references
        void Awake()
        {
            Instance = this;
            if (useLevelUpFallback)
            {
                StoreDatabase.onLevelUpdate -= OnLevelUp;
                StoreDatabase.onLevelUpdate += OnLevelUp;
            }
            RegisterPointSources();
        }


        //on scene load, build default nodes if none are set
        void Start()
        {
            if (nodes.Count == 0)
                InitializeDefaultNodes();
        }


        //award 1 progress point each time the player levels up
        private void OnLevelUp(int level)
        {
            AddProgressPoints(1, "LevelUp");
        }


        //register optional point source adapters implementing IProgressPointSource
        private void RegisterPointSources()
        {
            pointSources.Clear();
            for (int i = 0; i < progressPointSourceBehaviours.Count; i++)
            {
                MonoBehaviour behaviour = progressPointSourceBehaviours[i];
                if (behaviour == null)
                {
                    Debug.LogWarning("[EntrepreneurTree] progressPointSourceBehaviours contiene una referencia nula.");
                    continue;
                }

                if (behaviour is IProgressPointSource source)
                {
                    source.onProgressPointsGranted += OnExternalProgressPointsGranted;
                    pointSources.Add(source);
                }
            }
        }


        private void OnExternalProgressPointsGranted(int amount, string reason)
        {
            AddProgressPoints(amount, reason);
        }


        private void NotifyProgressPointsChanged()
        {
            onProgressPointsChanged?.Invoke(progressPoints);
            OnProgressPointsChanged?.Invoke(progressPoints);
        }


        /// <summary>
        /// Awards progress points. Use this to integrate with achievement systems or other events.
        /// Connect to: AddProgressPoints(int amount, string reason)
        /// </summary>
        public static void AddProgressPoints(int amount)
        {
            AddProgressPoints(amount, "Unspecified");
        }

        /// <summary>
        /// Awards progress points with a reason tag.
        /// </summary>
        public static void AddProgressPoints(int amount, string reason)
        {
            if (Instance == null || amount <= 0) return;

            long updated = (long)Instance.progressPoints + amount;
            if (updated < 0) updated = 0;
            if (updated > int.MaxValue) updated = int.MaxValue;
            Instance.progressPoints = (int)updated;
            onProgressPointsAdded?.Invoke(amount, reason);
            Instance.NotifyProgressPointsChanged();
        }

        /// <summary>
        /// Spends progress points if available.
        /// </summary>
        public static bool SpendProgressPoints(long amount)
        {
            if (Instance == null || amount < 0) return false;
            if (Instance.progressPoints < amount) return false;

            Instance.progressPoints -= (int)amount;
            Instance.NotifyProgressPointsChanged();
            return true;
        }


        /// <summary>
        /// Checks whether a specific node (by ID) has been unlocked.
        /// </summary>
        public static bool IsNodeUnlocked(string nodeId)
        {
            if (Instance == null) return false;

            foreach (SkillTreeNode node in Instance.nodes)
            {
                if (node.id == nodeId)
                    return node.isUnlocked;
            }

            return false;
        }


        /// <summary>
        /// Returns a message describing why a node cannot be unlocked, or null if it can.
        /// Useful for the UI to display user-friendly feedback.
        /// </summary>
        public static string GetUnlockBlockReason(string nodeId)
        {
            SkillTreeNode node = GetNode(nodeId);
            if (node == null) return "Nodo no encontrado.";
            if (node.isUnlocked) return "Ya está desbloqueado.";

            //collect missing prerequisites
            List<string> missing = new List<string>();
            foreach (string prereqId in node.prerequisites)
            {
                if (!string.IsNullOrEmpty(prereqId) && !IsNodeUnlocked(prereqId))
                {
                    SkillTreeNode prereq = GetNode(prereqId);
                    missing.Add(prereq != null ? prereq.title : prereqId);
                }
            }
            if (missing.Count > 0)
                return "Requisitos faltantes:\n• " + string.Join("\n• ", missing);

            if (node.pointCost > 0 && Instance.progressPoints < node.pointCost)
                return "No tienes suficientes puntos de progreso. Necesitas " + node.pointCost + " punto(s).";

            return null;
        }


        /// <summary>
        /// Attempt to unlock a node. Validates prerequisites and point cost. (RQF4)
        /// </summary>
        public static bool TryUnlockNode(string nodeId)
        {
            SkillTreeNode node = GetNode(nodeId);
            if (node == null)
            {
                UIGame.AddNotification("Nodo del árbol no encontrado.");
                return false;
            }

            if (node.isUnlocked)
            {
                UIGame.AddNotification("Nodo ya desbloqueado.");
                return false;
            }

            //check prerequisites (RQF4)
            List<string> missingPrereqs = new List<string>();
            foreach (string prereqId in node.prerequisites)
            {
                if (!string.IsNullOrEmpty(prereqId) && !IsNodeUnlocked(prereqId))
                {
                    SkillTreeNode prereq = GetNode(prereqId);
                    missingPrereqs.Add(prereq != null ? prereq.title : prereqId);
                }
            }

            if (missingPrereqs.Count > 0)
            {
                UIGame.AddNotification("Requisitos faltantes:\n• " + string.Join("\n• ", missingPrereqs));
                return false;
            }

            //check progress points
            if (node.pointCost > 0 && Instance.progressPoints < node.pointCost)
            {
                UIGame.Instance.ShowMessage("No tienes suficientes puntos de progreso.");
                return false;
            }

            //deduct points and unlock
            if (!SpendProgressPoints(node.pointCost))
            {
                UIGame.Instance.ShowMessage("No tienes suficientes puntos de progreso.");
                return false;
            }
            node.isUnlocked = true;
            onNodeUnlocked?.Invoke(node);
            UIGame.AddNotification("¡Desbloqueado: " + node.title + "!", otherColor: Color.green);

            //notify game systems of the unlock effect
            ApplyNodeEffect(node);

            return true;
        }


        //apply the in-game effect of an unlocked node
        private static void ApplyNodeEffect(SkillTreeNode node)
        {
            switch (node.category)
            {
                case SkillTreeCategory.Employee:
                    //TODO: connect to employee availability in EmployeeSystem
                    //EmployeeSystem will already check IsNodeUnlocked(node.id) via requiredSkillNode
                    break;

                case SkillTreeCategory.Upgrade:
                    //security nodes are stored as Upgrade category and identified by seg_ prefix
                    if (!string.IsNullOrEmpty(node.id) && node.id.StartsWith("seg_"))
                    {
                        //SecuritySystem.TryUpgradeLevel validates required nodes + applies percentages
                        if (SecuritySystem.Instance != null)
                            SecuritySystem.TryUpgradeLevel();
                        break;
                    }

                    if (node.id == "cafeina" && EmployeeSystem.Instance != null)
                    {
                        //+10% employee speed: shorter stocker restock time and cashier attend time
                        EmployeeSystem.Instance.stockerRestockTime *= 0.9f;
                        EmployeeSystem.Instance.cashierAttendTime *= 0.9f;
                    }
                    else if (node.id == "carismatico" && CustomerSystem.Instance != null)
                    {
                        //+5% customer spawn rate as a proxy for sales boost
                        CustomerSystem.Instance.spawnRate = Mathf.CeilToInt(CustomerSystem.Instance.spawnRate * 1.05f);
                    }
                    break;

                case SkillTreeCategory.Product:
                    //TODO: connect to product catalog/inventory system
                    //Call: OnProductCategoryUnlocked(node.id) to unlock products in the shop
                    break;
            }
        }


        /// <summary>
        /// Hook point for product category unlock events.
        /// Connect this to your catalog/inventory system when available.
        /// </summary>
        public static void OnProductCategoryUnlocked(string nodeId)
        {
            //intentionally left open: wire to product catalog system here
        }


        /// <summary>
        /// Returns a node by its ID, or null if not found.
        /// </summary>
        public static SkillTreeNode GetNode(string nodeId)
        {
            if (Instance == null) return null;

            foreach (SkillTreeNode node in Instance.nodes)
            {
                if (node.id == nodeId)
                    return node;
            }

            return null;
        }


        /// <summary>
        /// Returns all nodes by category. (RQF3)
        /// </summary>
        public static List<SkillTreeNode> GetNodesByCategory(SkillTreeCategory category)
        {
            List<SkillTreeNode> result = new List<SkillTreeNode>();
            if (Instance == null) return result;

            foreach (SkillTreeNode node in Instance.nodes)
            {
                if (node.category == category)
                    result.Add(node);
            }

            return result;
        }


        /// <summary>
        /// Returns all unlocked nodes.
        /// </summary>
        public static List<SkillTreeNode> GetUnlockedNodes()
        {
            List<SkillTreeNode> result = new List<SkillTreeNode>();
            if (Instance == null) return result;

            foreach (SkillTreeNode node in Instance.nodes)
            {
                if (node.isUnlocked)
                    result.Add(node);
            }

            return result;
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode.
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
            data["progressPoints"] = progressPoints;

            JSONNode nodeArray = new JSONArray();
            foreach (SkillTreeNode node in nodes)
            {
                if (!node.isUnlocked) continue;

                JSONNode element = new JSONObject();
                element["id"] = node.id;
                element["isUnlocked"] = node.isUnlocked;
                nodeArray.Add(element);
            }

            data["nodes"] = nodeArray;
            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            if (nodes.Count == 0)
                InitializeDefaultNodes();

            progressPoints = data["progressPoints"].AsInt;

            JSONArray nodeArray = data["nodes"].AsArray;
            for (int i = 0; i < nodeArray.Count; i++)
            {
                string id = nodeArray[i]["id"].Value;
                foreach (SkillTreeNode node in nodes)
                {
                    if (node.id == id)
                    {
                        node.isUnlocked = nodeArray[i]["isUnlocked"].AsBool;
                        break;
                    }
                }
            }

            NotifyProgressPointsChanged();
        }


        //unsubscribe from events
        void OnDestroy()
        {
            if (useLevelUpFallback)
                StoreDatabase.onLevelUpdate -= OnLevelUp;

            for (int i = 0; i < pointSources.Count; i++)
                pointSources[i].onProgressPointsGranted -= OnExternalProgressPointsGranted;
            pointSources.Clear();
        }


        /// <summary>
        /// Populates the default full Entrepreneur Tree with all 36 nodes and their dependencies.
        /// Called automatically in Start() when the nodes list is empty.
        /// Assign your own nodes in the Inspector to override this default tree.
        /// </summary>
        public void InitializeDefaultNodes()
        {
            nodes.Clear();

            //unlock Productos Básicos 1 for free at game start
            progressPoints = 0;

            //────────────────────────────────────────────────
            // PRODUCTS
            //────────────────────────────────────────────────
            AddNode("prod_basicos_1", "Productos Básicos 1",
                "Acceso a productos esenciales: pan, agua, arroz y aceite. Punto de partida del árbol.",
                SkillTreeCategory.Product, 0, new string[0], true);

            AddNode("prod_basicos_2", "Productos Básicos 2",
                "Amplía la gama de productos básicos: pastas, conservas y condimentos.",
                SkillTreeCategory.Product, 1, new[] { "prod_basicos_1" });

            AddNode("prod_basicos_3", "Productos Básicos 3",
                "Completa el catálogo básico: snacks, galletas y cereales.",
                SkillTreeCategory.Product, 1, new[] { "prod_basicos_2" });

            AddNode("lacteos_1", "Lácteos 1",
                "Desbloquea leche, mantequilla y queso fresco.",
                SkillTreeCategory.Product, 1, new[] { "prod_basicos_2" });

            AddNode("lacteos_2", "Lácteos 2",
                "Amplía la sección láctea: yogur, quesos curados y nata.",
                SkillTreeCategory.Product, 1, new[] { "lacteos_1" });

            AddNode("especias_1", "Especias 1",
                "Desbloquea sal, pimienta, hierbas y especias de cocina.",
                SkillTreeCategory.Product, 1, new[] { "prod_basicos_2" });

            AddNode("prod_frescos_1", "Productos Frescos 1",
                "Sección de frutas y verduras frescas.",
                SkillTreeCategory.Product, 1, new[] { "prod_basicos_2" });

            AddNode("prod_frescos_2", "Productos Frescos 2",
                "Amplía los frescos: carnes, pescados y mariscos.",
                SkillTreeCategory.Product, 1, new[] { "prod_frescos_1" });

            AddNode("prod_higiene", "Productos de Higiene",
                "Desbloquea jabones, champú, papel higiénico y artículos de limpieza.",
                SkillTreeCategory.Product, 1, new[] { "prod_basicos_3" });

            AddNode("proteina_1", "Proteína 1",
                "Suplementos proteicos, batidos y alimentos deportivos.",
                SkillTreeCategory.Product, 1, new[] { "prod_frescos_2" });

            AddNode("sodas", "Sodas",
                "Refrescos, zumos y bebidas carbonatadas.",
                SkillTreeCategory.Product, 1, new[] { "prod_basicos_3" });

            AddNode("prod_lujo_1", "Productos de Lujo 1",
                "Vinos, quesos premium, chocolate gourmet y delicatessen.",
                SkillTreeCategory.Product, 1, new[] { "proteina_1" });

            AddNode("electro_1", "Electrodomésticos 1",
                "Pequeños electrodomésticos: cafeteras, licuadoras y tostadoras.",
                SkillTreeCategory.Product, 1, new[] { "prod_lujo_1" });

            //────────────────────────────────────────────────
            // EMPLOYEES
            //────────────────────────────────────────────────
            AddNode("emp_1", "Empleado 1",
                "Primer empleado disponible. Rol: Cajero. Requiere tienda con especias.",
                SkillTreeCategory.Employee, 1, new[] { "especias_1" });

            AddNode("emp_2", "Empleado 2",
                "Empleado especializado en higiene. Rol: Surtidor.",
                SkillTreeCategory.Employee, 1, new[] { "prod_higiene" });

            AddNode("emp_3", "Empleado 3",
                "Empleado para la sección de bebidas. Rol: Surtidor.",
                SkillTreeCategory.Employee, 1, new[] { "sodas" });

            AddNode("emp_4", "Empleado 4",
                "Empleado lácteo. Rol: Cajero. Requiere sección de lácteos.",
                SkillTreeCategory.Employee, 1, new[] { "lacteos_1" });

            AddNode("emp_5", "Empleado 5",
                "Segundo empleado de lácteos. Rol: Surtidor.",
                SkillTreeCategory.Employee, 1, new[] { "lacteos_1" });

            AddNode("emp_6", "Empleado 6",
                "Empleado de especias. Rol: Cajero.",
                SkillTreeCategory.Employee, 1, new[] { "especias_1" });

            AddNode("emp_7", "Empleado 7",
                "Supervisor de área. Rol: Cajero. Requiere Empleado 5.",
                SkillTreeCategory.Employee, 1, new[] { "emp_5" });

            AddNode("emp_8", "Empleado 8",
                "Asistente de bebidas. Rol: Surtidor.",
                SkillTreeCategory.Employee, 1, new[] { "sodas" });

            AddNode("emp_9", "Empleado 9",
                "Auxiliar de limpieza. Rol: Surtidor.",
                SkillTreeCategory.Employee, 1, new[] { "prod_higiene" });

            AddNode("emp_10", "Empleado 10",
                "Asistente de caja. Rol: Cajero. Requiere Empleado 1.",
                SkillTreeCategory.Employee, 1, new[] { "emp_1" });

            AddNode("emp_11", "Empleado 11",
                "Guardia asistente. Rol: Cajero. Requiere Seguridad Nivel 1.",
                SkillTreeCategory.Employee, 1, new[] { "seg_1" });

            AddNode("emp_12", "Empleado 12",
                "Asistente de lujo. Rol: Cajero. Requiere Empleado 13.",
                SkillTreeCategory.Employee, 1, new[] { "emp_13" });

            AddNode("emp_13", "Empleado 13",
                "Especialista de lujo. Rol: Surtidor. Requiere Productos de Lujo 1.",
                SkillTreeCategory.Employee, 1, new[] { "prod_lujo_1" });

            AddNode("emp_14", "Empleado 14",
                "Técnico de electrodomésticos. Rol: Surtidor. Requiere Electrodomésticos 1.",
                SkillTreeCategory.Employee, 1, new[] { "electro_1" });

            AddNode("emp_15", "Empleado 15",
                "Supervisor de seguridad. Rol: Cajero. Requiere Seguridad Nivel 2.",
                SkillTreeCategory.Employee, 1, new[] { "seg_2" });

            AddNode("emp_16", "Empleado 16",
                "Especialista en proteínas. Rol: Surtidor.",
                SkillTreeCategory.Employee, 1, new[] { "proteina_1" });

            AddNode("emp_17", "Empleado 17",
                "Empleado de frescos premium. Rol: Cajero.",
                SkillTreeCategory.Employee, 1, new[] { "prod_frescos_2" });

            AddNode("emp_18", "Empleado 18",
                "Jefe de seguridad. Rol: Cajero. Requiere Seguridad Nivel 3.",
                SkillTreeCategory.Employee, 1, new[] { "seg_3" });

            //────────────────────────────────────────────────
            // SECURITY
            //────────────────────────────────────────────────
            AddNode("seg_1", "Seguridad Nivel 1 — Cámaras (33%)",
                "Instala cámaras de seguridad. 33% de probabilidad de arresto automático.",
                SkillTreeCategory.Upgrade, 1, new[] { "emp_7" });

            AddNode("seg_2", "Seguridad Nivel 2 — Guardias (66%)",
                "Contrata guardias de seguridad. 66% de probabilidad de arresto automático.",
                SkillTreeCategory.Upgrade, 1, new[] { "emp_8" });

            AddNode("seg_3", "Seguridad Nivel 3 — Alarmas (99%)",
                "Sistema de alarmas avanzado. 99% de probabilidad de arresto automático.",
                SkillTreeCategory.Upgrade, 1, new[] { "emp_14" });

            //────────────────────────────────────────────────
            // UPGRADES / IMPROVEMENTS
            //────────────────────────────────────────────────
            AddNode("cafeina", "Cafeína (+10% velocidad empleados)",
                "Los empleados trabajan un 10% más rápido gracias a la mejora de bienestar.",
                SkillTreeCategory.Upgrade, 1, new[] { "prod_frescos_2" });

            AddNode("carismatico", "Carismático (+5% ventas)",
                "La actitud positiva del equipo aumenta las ventas un 5%.",
                SkillTreeCategory.Upgrade, 1, new[] { "emp_15" });

            NotifyProgressPointsChanged();
        }


        //helper to add a node cleanly
        private void AddNode(string id, string title, string description, SkillTreeCategory category,
                             int cost, string[] prereqs, bool startUnlocked = false)
        {
            SkillTreeNode node = new SkillTreeNode
            {
                id = id,
                title = title,
                description = description,
                category = category,
                pointCost = cost,
                prerequisites = new List<string>(prereqs),
                isUnlocked = startUnlocked
            };
            nodes.Add(node);
        }
    }


    /// <summary>
    /// Represents a single node in the Entrepreneur Skill Tree. (RQF3)
    /// Each node has a category, cost, prerequisites, and unlock state.
    /// </summary>
    [Serializable]
    public class SkillTreeNode
    {
        /// <summary>
        /// Unique identifier for this node.
        /// </summary>
        public string id;

        /// <summary>
        /// Display name of the node.
        /// </summary>
        public string title;

        /// <summary>
        /// Description of what this node unlocks or improves.
        /// </summary>
        [TextArea]
        public string description;

        /// <summary>
        /// Category this node belongs to: Skill, Product, Employee, or Upgrade. (RQF3)
        /// </summary>
        public SkillTreeCategory category;

        /// <summary>
        /// Cost in progress points to unlock this node. (RQF3)
        /// Set to 0 for free/starting nodes.
        /// </summary>
        public long pointCost;

        /// <summary>
        /// IDs of prerequisite nodes that must be unlocked first. (RQF4)
        /// </summary>
        public List<string> prerequisites = new List<string>();

        /// <summary>
        /// Whether this node has been unlocked.
        /// </summary>
        public bool isUnlocked;

        /// <summary>
        /// Optional icon for the node in the UI.
        /// </summary>
        public Sprite icon;
    }
}
