/*  SupermarketSceneBuilder.cs
 *  ─────────────────────────────────────────────────────────────────────────
 *  Editor-only tool that reorganises the Game scene hierarchy and builds
 *  the full supermarket layout described in the ShopMaster design doc.
 *
 *  USAGE
 *  ─────
 *  1. Open  Assets/StoreSimulator/Scenes/Game.unity  in the Unity Editor.
 *  2. Go to  Window ▸ Store Simulator ▸ Build Supermarket Scene.
 *  3. The tool will:
 *       • reorganise the Hierarchy into a clean, modular structure,
 *       • create every required zone (sales floor, office, storage, etc.),
 *       • instantiate prefabs where available and primitive placeholders
 *         where assets are still missing,
 *       • create all spawn / anchor / socket Transforms,
 *       • leave the scene ready for first-person walkthrough.
 *  4. Save the scene (Ctrl+S) after running the tool.
 *
 *  NOTE: The tool is non-destructive — it parents existing root objects
 *  rather than deleting them, so no data is lost.
 *  ─────────────────────────────────────────────────────────────────────── */

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FLOBUK.StoreSimulator
{
    public static class SupermarketSceneBuilder
    {
        // ───────────── constants ────────────────────────────────────────
        // All measurements in Unity-units (1 unit = 1 metre).
        // Total terrain ≈ 30 × 30 = 900 m².

        // Sales floor initial: 16 × 12 = 192 m²
        const float SalesW = 16f, SalesD = 12f;
        // Storage: 8 × 4 = 32 m²
        const float StorageW = 8f, StorageD = 4f;
        // Office: 8 × 4 = 32 m²
        const float OfficeW = 8f, OfficeD = 4f;
        // Wall height
        const float WallH = 3f;
        // Aisle width for comfortable first-person movement
        const float AisleW = 2.0f;

        // ───────────── menu entry ───────────────────────────────────────

        [MenuItem("Window/Store Simulator/Build Supermarket Scene", false, 200)]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build Supermarket Scene",
                    "This will reorganise the current scene hierarchy, add zones, " +
                    "placeholders and spawn points.\n\nMake sure you are in the " +
                    "Game scene and have a backup.\n\nContinue?",
                    "Build", "Cancel"))
                return;

            Undo.SetCurrentGroupName("Build Supermarket Scene");
            int undoGroup = Undo.GetCurrentGroup();

            // ─── Step 1: create top-level organisational roots ──────────
            Transform sceneRoot      = GetOrCreateRoot("SceneRoot");
            Transform systems        = GetOrCreateChild(sceneRoot, "Systems");
            Transform environment    = GetOrCreateChild(sceneRoot, "Environment");
            Transform gameplay       = GetOrCreateChild(sceneRoot, "Gameplay");
            Transform uiWorld        = GetOrCreateChild(sceneRoot, "UIWorld");
            Transform lighting       = GetOrCreateChild(sceneRoot, "Lighting");

            // Systems sub-groups
            Transform gameManagers    = GetOrCreateChild(systems, "GameManagers");
            Transform interactionSys  = GetOrCreateChild(systems, "InteractionSystems");
            Transform audioSys        = GetOrCreateChild(systems, "AudioSystems");
            Transform spawnPoints     = GetOrCreateChild(systems, "SpawnPoints");
            Transform debug           = GetOrCreateChild(systems, "Debug");

            // Environment sub-groups
            Transform terrain         = GetOrCreateChild(environment, "Terrain");
            Transform exterior        = GetOrCreateChild(environment, "Exterior");
            Transform parkingFront    = GetOrCreateChild(environment, "ParkingOrFrontArea");
            Transform building        = GetOrCreateChild(environment, "SupermarketBuilding");
            Transform office          = GetOrCreateChild(environment, "Office");
            Transform storage         = GetOrCreateChild(environment, "Storage");
            Transform salesFloor      = GetOrCreateChild(environment, "SalesFloor");

            // Building sub-groups
            Transform structure       = GetOrCreateChild(building, "Structure");
            Transform doors           = GetOrCreateChild(building, "Doors");
            Transform windows         = GetOrCreateChild(building, "Windows");
            Transform lights          = GetOrCreateChild(building, "Lights");
            Transform securitySockets = GetOrCreateChild(building, "SecuritySockets");

            // Office sub-groups
            Transform officeFurniture = GetOrCreateChild(office, "Furniture");
            Transform officeLaptop    = GetOrCreateChild(office, "Laptop");
            Transform officeTablet    = GetOrCreateChild(office, "Tablet");
            Transform reportBoard     = GetOrCreateChild(office, "ReportBoard");

            // Storage sub-groups
            Transform storageShelves  = GetOrCreateChild(storage, "Shelves");
            Transform deliveryZone    = GetOrCreateChild(storage, "DeliveryZone");
            Transform stockArea       = GetOrCreateChild(storage, "StockArea");

            // Sales floor sub-groups
            Transform checkoutArea    = GetOrCreateChild(salesFloor, "CheckoutArea");
            Transform gondolas        = GetOrCreateChild(salesFloor, "Gondolas");
            Transform refrigerators   = GetOrCreateChild(salesFloor, "Refrigerators");
            Transform freezers        = GetOrCreateChild(salesFloor, "Freezers");
            Transform promoDisplays   = GetOrCreateChild(salesFloor, "PromoDisplays");
            Transform navPaths        = GetOrCreateChild(salesFloor, "NavigationPaths");

            // Gameplay sub-groups
            Transform player          = GetOrCreateChild(gameplay, "Player");
            Transform customerSpawn   = GetOrCreateChild(gameplay, "CustomerSpawn");
            Transform employeeSpawn   = GetOrCreateChild(gameplay, "EmployeeSpawn");
            Transform thiefSpawn      = GetOrCreateChild(gameplay, "ThiefSpawn");
            Transform queuePoints     = GetOrCreateChild(gameplay, "QueuePoints");
            Transform interactPoints  = GetOrCreateChild(gameplay, "InteractionPoints");

            // ─── Step 2: parent existing root objects ───────────────────
            ParentIfExists("GameSystems", gameManagers);
            ParentIfExists("Store", structure);
            ParentIfExists("StoreObjects", salesFloor);
            ParentIfExists("Canvas", uiWorld);
            ParentIfExists("EventSystem", uiWorld);
            ParentIfExists("Directional Light", lighting);
            ParentIfExists("DynamicCamera", gameplay);
            ParentIfExists("SkinnedMeshRenderer", gameplay);

            // Move existing Player to gameplay
            ParentIfExists("Player", player);

            // Move roads/terrain quads to exterior or terrain
            MoveObjectsByPrefix("Road_", exterior);
            MoveObjectsByPrefix("Quad", terrain);

            // ─── Step 3: build zones and placeholders ───────────────────

            // Origins — store front faces +Z (north), entrance at Z=0, X=0 centre
            Vector3 storeOrigin = Vector3.zero;

            // === FLOOR ===
            CreatePlaceholderCube(terrain, "MainFloor",
                storeOrigin + new Vector3(0, -0.05f, SalesD * 0.5f),
                new Vector3(SalesW + 4, 0.1f, SalesD + StorageD + OfficeD + 4),
                "Floor_Placeholder", HexColor("#C8C8C8"));

            // === SALES FLOOR (192 m², 16×12) ===
            // Place gondola rows (4 double-sided aisles)
            float gondolaStartX = -SalesW * 0.5f + 1.5f;
            float gondolaEndX   =  SalesW * 0.5f - 1.5f;
            float gondolaZ      = 3f; // start after checkout zone
            int aisleCount       = 4;
            float aisleSpacing   = (gondolaEndX - gondolaStartX) / aisleCount;

            for (int i = 0; i < aisleCount; i++)
            {
                float x = gondolaStartX + aisleSpacing * 0.5f + i * aisleSpacing;
                // Each gondola is a long shelf running along Z
                string gondolaName = $"Gondola_Aisle{i + 1:D2}";
                CreatePlaceholderCube(gondolas, gondolaName,
                    new Vector3(x, 0.6f, gondolaZ + 3.5f),
                    new Vector3(0.6f, 1.2f, 7f),
                    "Gondola_Placeholder", HexColor("#D2B48C"));
            }

            // Refrigerators along right wall
            for (int i = 0; i < 3; i++)
            {
                CreatePlaceholderCube(refrigerators, $"Refrigerator_{i + 1:D2}",
                    new Vector3(SalesW * 0.5f - 0.4f, 0.8f, 4f + i * 3f),
                    new Vector3(0.8f, 1.6f, 2.5f),
                    "Refrigerator_Placeholder", HexColor("#ADD8E6"));
            }

            // Freezers along back wall
            for (int i = 0; i < 2; i++)
            {
                CreatePlaceholderCube(freezers, $"Freezer_{i + 1:D2}",
                    new Vector3(-SalesW * 0.25f + i * 4f, 0.5f, SalesD - 0.5f),
                    new Vector3(3f, 1f, 0.8f),
                    "Freezer_Placeholder", HexColor("#87CEEB"));
            }

            // Promo display near entrance
            CreatePlaceholderCube(promoDisplays, "PromoDisplay_Entrance",
                new Vector3(0, 0.4f, 1.5f),
                new Vector3(2f, 0.8f, 1.5f),
                "PromoDisplay_Placeholder", HexColor("#FFD700"));

            // === CHECKOUT AREA ===
            // Two checkout desks near entrance (Z ≈ 1)
            for (int i = 0; i < 2; i++)
            {
                float cx = -2f + i * 4f;
                string deskName = $"CheckoutDesk_{i + 1:D2}";

                // Try to instantiate CashDesk prefab, fall back to placeholder
                GameObject desk = TryInstantiatePrefab("Assets/StoreSimulator/Prefabs/Store/CashDesk.prefab");
                if (desk != null)
                {
                    desk.name = deskName;
                    desk.transform.SetParent(checkoutArea, false);
                    desk.transform.localPosition = new Vector3(cx, 0, 1f);
                    desk.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    Undo.RegisterCreatedObjectUndo(desk, "Create " + deskName);
                }
                else
                {
                    CreatePlaceholderCube(checkoutArea, deskName,
                        new Vector3(cx, 0.5f, 1f),
                        new Vector3(1.5f, 1f, 0.8f),
                        "CashDesk_Placeholder", HexColor("#8B4513"));
                }

                // Queue line anchor
                CreateAnchor(queuePoints, $"CheckoutQueuePoint_{i + 1:D2}",
                    new Vector3(cx, 0, -0.5f));
                CreateAnchor(interactPoints, $"CheckoutInteractionPoint_{i + 1:D2}",
                    new Vector3(cx + 0.8f, 0.9f, 1f));
            }

            // === MAIN ENTRANCE (double door) ===
            for (int i = 0; i < 2; i++)
            {
                float dx = -1.0f + i * 2.0f;
                string doorName = $"MainEntrance_Door_{(i == 0 ? "L" : "R")}";

                GameObject door = TryInstantiatePrefab("Assets/StoreSimulator/Prefabs/Environment/Store_Door.prefab");
                if (door != null)
                {
                    door.name = doorName;
                    door.transform.SetParent(doors, false);
                    door.transform.localPosition = new Vector3(dx, 0, 0);
                    door.transform.localRotation = Quaternion.identity;
                    Undo.RegisterCreatedObjectUndo(door, "Create " + doorName);
                }
                else
                {
                    CreatePlaceholderCube(doors, doorName,
                        new Vector3(dx, WallH * 0.5f, 0),
                        new Vector3(1.2f, WallH, 0.15f),
                        "Door_Placeholder", HexColor("#A0522D"));
                }
            }

            // Alarm gate sockets at entrance
            AddSecuritySocket(securitySockets, "AlarmGateSocket_Entrance_A",
                new Vector3(-1.5f, 0, 0.3f), SecuritySocket.SecurityDeviceType.AlarmGate);
            AddSecuritySocket(securitySockets, "AlarmGateSocket_Entrance_B",
                new Vector3(1.5f, 0, 0.3f), SecuritySocket.SecurityDeviceType.AlarmGate);

            // === SECONDARY ENTRANCE (left side) ===
            {
                string sideDoor = "SecondaryEntrance_Door";
                GameObject sDoor = TryInstantiatePrefab("Assets/StoreSimulator/Prefabs/Environment/Store_Door.prefab");
                if (sDoor != null)
                {
                    sDoor.name = sideDoor;
                    sDoor.transform.SetParent(doors, false);
                    sDoor.transform.localPosition = new Vector3(-SalesW * 0.5f, 0, SalesD * 0.5f);
                    sDoor.transform.localRotation = Quaternion.Euler(0, -90, 0);
                    Undo.RegisterCreatedObjectUndo(sDoor, "Create " + sideDoor);
                }
                else
                {
                    CreatePlaceholderCube(doors, sideDoor,
                        new Vector3(-SalesW * 0.5f, WallH * 0.5f, SalesD * 0.5f),
                        new Vector3(0.15f, WallH, 1.2f),
                        "Door_Placeholder", HexColor("#A0522D"));
                }
            }

            // === WALLS (placeholder) ===
            // Front wall (with gap for entrance)
            CreatePlaceholderCube(structure, "Wall_Front_L",
                new Vector3(-SalesW * 0.25f - 1f, WallH * 0.5f, 0),
                new Vector3(SalesW * 0.5f - 2f, WallH, 0.15f),
                "Wall_Placeholder", HexColor("#F5F5DC"));
            CreatePlaceholderCube(structure, "Wall_Front_R",
                new Vector3(SalesW * 0.25f + 1f, WallH * 0.5f, 0),
                new Vector3(SalesW * 0.5f - 2f, WallH, 0.15f),
                "Wall_Placeholder", HexColor("#F5F5DC"));

            // Back wall
            CreatePlaceholderCube(structure, "Wall_Back",
                new Vector3(0, WallH * 0.5f, SalesD + StorageD),
                new Vector3(SalesW + 4, WallH, 0.15f),
                "Wall_Placeholder", HexColor("#F5F5DC"));

            // Right wall
            CreatePlaceholderCube(structure, "Wall_Right",
                new Vector3(SalesW * 0.5f, WallH * 0.5f, (SalesD + StorageD) * 0.5f),
                new Vector3(0.15f, WallH, SalesD + StorageD),
                "Wall_Placeholder", HexColor("#F5F5DC"));

            // Left wall (with gap for secondary entrance)
            CreatePlaceholderCube(structure, "Wall_Left_Front",
                new Vector3(-SalesW * 0.5f, WallH * 0.5f, SalesD * 0.25f - 0.5f),
                new Vector3(0.15f, WallH, SalesD * 0.5f - 1.5f),
                "Wall_Placeholder", HexColor("#F5F5DC"));
            CreatePlaceholderCube(structure, "Wall_Left_Back",
                new Vector3(-SalesW * 0.5f, WallH * 0.5f, SalesD * 0.75f + 0.5f + StorageD * 0.5f),
                new Vector3(0.15f, WallH, SalesD * 0.5f + StorageD - 1.5f),
                "Wall_Placeholder", HexColor("#F5F5DC"));

            // === OFFICE (32 m², 8×4) behind/beside storage, left side ===
            Vector3 officeOrigin = new Vector3(-SalesW * 0.5f + OfficeW * 0.5f,
                                                0, SalesD + 0.5f);
            // Office floor marker
            CreatePlaceholderCube(office, "OfficeFloor",
                officeOrigin + new Vector3(0, 0.01f, OfficeD * 0.5f),
                new Vector3(OfficeW, 0.02f, OfficeD),
                "Office_Floor", HexColor("#DEB887"));

            // Desk
            CreatePlaceholderCube(officeFurniture, "Desk",
                officeOrigin + new Vector3(0, 0.4f, OfficeD - 1f),
                new Vector3(2f, 0.8f, 0.8f),
                "Desk_Placeholder", HexColor("#8B4513"));

            // Chair
            CreatePlaceholderCube(officeFurniture, "Chair",
                officeOrigin + new Vector3(0, 0.25f, OfficeD - 2f),
                new Vector3(0.5f, 0.5f, 0.5f),
                "Chair_Placeholder", HexColor("#2F4F4F"));

            // Laptop on desk
            GameObject laptop = TryInstantiatePrefab("Assets/StoreSimulator/Prefabs/Store/Computer.prefab");
            if (laptop != null)
            {
                laptop.name = "OfficeLaptop";
                laptop.transform.SetParent(officeLaptop.transform, false);
                laptop.transform.localPosition = officeOrigin + new Vector3(-0.3f, 0.82f, OfficeD - 1f);
                Undo.RegisterCreatedObjectUndo(laptop, "Create OfficeLaptop");
            }
            else
            {
                CreatePlaceholderCube(officeLaptop, "OfficeLaptop",
                    officeOrigin + new Vector3(-0.3f, 0.85f, OfficeD - 1f),
                    new Vector3(0.35f, 0.02f, 0.25f),
                    "Laptop_Placeholder", HexColor("#333333"));
            }

            // Tablet on desk
            CreatePlaceholderCube(officeTablet, "OfficeTablet",
                officeOrigin + new Vector3(0.4f, 0.85f, OfficeD - 1f),
                new Vector3(0.2f, 0.01f, 0.15f),
                "Tablet_Placeholder", HexColor("#444444"));

            // Report board on back wall
            CreatePlaceholderCube(reportBoard, "ReportBoard",
                officeOrigin + new Vector3(0, 1.5f, OfficeD - 0.05f),
                new Vector3(1.5f, 1f, 0.05f),
                "ReportBoard_Placeholder", HexColor("#FFFFFF"));

            // Interaction anchors
            CreateAnchor(interactPoints, "OfficeLaptopInteractionPoint",
                officeOrigin + new Vector3(-0.3f, 0.85f, OfficeD - 1.5f));
            CreateAnchor(interactPoints, "OfficeTabletInteractionPoint",
                officeOrigin + new Vector3(0.4f, 0.85f, OfficeD - 1.5f));

            // === STORAGE / WAREHOUSE (32 m², 8×4) ===
            Vector3 storageOrigin = new Vector3(SalesW * 0.5f - StorageW * 0.5f,
                                                 0, SalesD + 0.5f);
            // Storage floor
            CreatePlaceholderCube(storage, "StorageFloor",
                storageOrigin + new Vector3(0, 0.01f, StorageD * 0.5f),
                new Vector3(StorageW, 0.02f, StorageD),
                "Storage_Floor", HexColor("#A9A9A9"));

            // Storage shelves (tall racks)
            for (int i = 0; i < 3; i++)
            {
                float sx = storageOrigin.x - StorageW * 0.3f + i * (StorageW * 0.3f);
                string shelfName = $"StorageShelf_{i + 1:D2}";

                GameObject shelf = TryInstantiatePrefab("Assets/StoreSimulator/Prefabs/Storage/Shelf_Boxed.prefab");
                if (shelf != null)
                {
                    shelf.name = shelfName;
                    shelf.transform.SetParent(storageShelves, false);
                    shelf.transform.localPosition = new Vector3(sx, 0, storageOrigin.z + StorageD * 0.5f);
                    Undo.RegisterCreatedObjectUndo(shelf, "Create " + shelfName);
                }
                else
                {
                    CreatePlaceholderCube(storageShelves, shelfName,
                        new Vector3(sx, 1f, storageOrigin.z + StorageD * 0.5f),
                        new Vector3(1.2f, 2f, 0.5f),
                        "StorageShelf_Placeholder", HexColor("#696969"));
                }
            }

            // === DELIVERY ZONE ===
            Vector3 deliveryPos = new Vector3(-SalesW * 0.5f - 1.5f, 0, SalesD * 0.5f + 2f);
            CreatePlaceholderCube(deliveryZone, "DeliveryPlatform",
                deliveryPos + new Vector3(0, 0.05f, 0),
                new Vector3(3f, 0.1f, 3f),
                "DeliveryPlatform_Placeholder", HexColor("#808080"));

            // Spawn point for deliveries
            AddSpawnPoint(spawnPoints, "DeliverySpawnPoint",
                deliveryPos + new Vector3(0, 0.5f, 0),
                Color.magenta, "Delivery");

            // === OPEN SIGN ===
            GameObject openSign = TryInstantiatePrefab("Assets/StoreSimulator/Prefabs/Store/OpenSign.prefab");
            if (openSign != null)
            {
                openSign.name = "OpenSign";
                openSign.transform.SetParent(structure, false);
                openSign.transform.localPosition = new Vector3(0, WallH + 0.5f, -0.2f);
                Undo.RegisterCreatedObjectUndo(openSign, "Create OpenSign");
            }

            // === TRASH CANS ===
            for (int i = 0; i < 2; i++)
            {
                float tx = -3f + i * 6f;
                GameObject trash = TryInstantiatePrefab("Assets/StoreSimulator/Prefabs/Store/TrashCan.prefab");
                if (trash != null)
                {
                    trash.name = $"TrashCan_{i + 1:D2}";
                    trash.transform.SetParent(salesFloor, false);
                    trash.transform.localPosition = new Vector3(tx, 0, 0.5f);
                    Undo.RegisterCreatedObjectUndo(trash, "Create TrashCan");
                }
            }

            // === INTERIOR LIGHTING ===
            for (int i = 0; i < 3; i++)
            {
                float lz = 3f + i * 4f;
                GameObject lightObj = new GameObject($"CeilingLight_{i + 1:D2}");
                lightObj.transform.SetParent(lights, false);
                lightObj.transform.localPosition = new Vector3(0, WallH - 0.1f, lz);
                Light lc = lightObj.AddComponent<Light>();
                lc.type = LightType.Point;
                lc.intensity = 1.5f;
                lc.range = 10f;
                lc.color = new Color(1f, 0.97f, 0.9f); // warm white
                Undo.RegisterCreatedObjectUndo(lightObj, "Create CeilingLight");
            }

            // Office light
            {
                GameObject offLight = new GameObject("OfficeLight");
                offLight.transform.SetParent(lights, false);
                offLight.transform.localPosition = officeOrigin + new Vector3(0, WallH - 0.1f, OfficeD * 0.5f);
                Light ol = offLight.AddComponent<Light>();
                ol.type = LightType.Point;
                ol.intensity = 1.2f;
                ol.range = 6f;
                ol.color = new Color(1f, 0.95f, 0.85f);
                Undo.RegisterCreatedObjectUndo(offLight, "Create OfficeLight");
            }

            // Storage light
            {
                GameObject stLight = new GameObject("StorageLight");
                stLight.transform.SetParent(lights, false);
                stLight.transform.localPosition = storageOrigin + new Vector3(0, WallH - 0.1f, StorageD * 0.5f);
                Light sl = stLight.AddComponent<Light>();
                sl.type = LightType.Point;
                sl.intensity = 1.0f;
                sl.range = 8f;
                sl.color = new Color(1f, 1f, 1f);
                Undo.RegisterCreatedObjectUndo(stLight, "Create StorageLight");
            }

            // ─── Step 4: spawn / anchor / socket points ────────────────

            // Player
            AddSpawnPoint(spawnPoints, "PlayerSpawn",
                new Vector3(0, 0, -2f), Color.green, "PlayerSpawn");

            // Customer
            AddSpawnPoint(customerSpawn, "CustomerEntrySpawn",
                new Vector3(0, 0, -4f), new Color(0, 0.8f, 0.8f), "CustomerEntry");
            CreateAnchor(customerSpawn, "CustomerExitPoint",
                new Vector3(0, 0, -5f));

            // Employee
            AddSpawnPoint(employeeSpawn, "EmployeeSpawn",
                officeOrigin + new Vector3(1f, 0, 0), Color.blue, "EmployeeSpawn");

            // Thief
            AddSpawnPoint(thiefSpawn, "ThiefSpawn",
                new Vector3(0, 0, -6f), Color.red, "ThiefSpawn");

            // Security cameras
            AddSecuritySocket(securitySockets, "SecurityCameraSocket_Main",
                new Vector3(0, WallH - 0.3f, 0.5f),
                SecuritySocket.SecurityDeviceType.Camera);
            AddSecuritySocket(securitySockets, "SecurityCameraSocket_Sales",
                new Vector3(0, WallH - 0.3f, SalesD * 0.5f),
                SecuritySocket.SecurityDeviceType.Camera);

            // Security guards
            AddSecuritySocket(securitySockets, "SecurityGuardPoint_01",
                new Vector3(-SalesW * 0.5f + 1f, 0, 0.5f),
                SecuritySocket.SecurityDeviceType.GuardPost);
            AddSecuritySocket(securitySockets, "SecurityGuardPoint_02",
                new Vector3(SalesW * 0.5f - 1f, 0, 0.5f),
                SecuritySocket.SecurityDeviceType.GuardPost);

            // ─── Step 5: expansion zones ────────────────────────────────

            Transform expansions = GetOrCreateChild(environment, "ExpansionZones");

            // Sales floor expansions (right side of the building)
            for (int i = 0; i < 3; i++)
            {
                AddExpansionZone(expansions, $"Expansion_Sales_{i + 1:D2}",
                    new Vector3(SalesW * 0.5f + 2f + i * 4.5f, 0, SalesD * 0.5f),
                    ExpansionType.SalesFloor,
                    new Vector3(4f, WallH, SalesD));
            }

            // Storage expansions (behind storage)
            for (int i = 0; i < 2; i++)
            {
                AddExpansionZone(expansions, $"Expansion_Storage_{i + 1:D2}",
                    new Vector3(storageOrigin.x, 0, SalesD + StorageD + 1f + i * 5f),
                    ExpansionType.Storage,
                    new Vector3(StorageW, WallH, 4f));
            }

            // ─── Step 6: parking / front area ───────────────────────────
            CreatePlaceholderCube(parkingFront, "ParkingLot",
                new Vector3(0, -0.02f, -8f),
                new Vector3(20f, 0.04f, 8f),
                "ParkingLot_Placeholder", HexColor("#555555"));

            // ─── Done ───────────────────────────────────────────────────
            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(sceneRoot.gameObject);

            Debug.Log("[SupermarketSceneBuilder] Scene build complete. " +
                      "Remember to save the scene (Ctrl+S).");
            EditorUtility.DisplayDialog("Done",
                "Supermarket scene has been built successfully.\n\n" +
                "• Hierarchy reorganised\n" +
                "• Zones created (Sales, Office, Storage, Checkout)\n" +
                "• Spawn/anchor/socket points placed\n" +
                "• Expansion zones marked\n\n" +
                "Save the scene now (Ctrl+S).", "OK");
        }

        // ───────────── helper methods ───────────────────────────────────

        static Transform GetOrCreateRoot(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }
            return go.transform;
        }

        static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                child = go.transform;
            }
            return child;
        }

        static void ParentIfExists(string objectName, Transform newParent)
        {
            // Search all root objects in the active scene
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    Undo.SetTransformParent(root.transform, newParent, "Reparent " + objectName);
                    return;
                }
            }
        }

        static void MoveObjectsByPrefix(string prefix, Transform newParent)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name.StartsWith(prefix))
                {
                    Undo.SetTransformParent(root.transform, newParent, "Reparent " + root.name);
                }
            }
        }

        static GameObject TryInstantiatePrefab(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return null;
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        static void CreatePlaceholderCube(Transform parent, string name,
            Vector3 position, Vector3 scale, string description, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.isStatic = true;

            // Apply colour via a simple unlit material
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat != null)
                {
                    mat.color = color;
                    rend.sharedMaterial = mat;
                }
            }

            PlaceholderMarker marker = go.AddComponent<PlaceholderMarker>();
            marker.description = description;

            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        }

        static void CreateAnchor(Transform parent, string name, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            InteractionAnchor anchor = go.AddComponent<InteractionAnchor>();
            anchor.anchorId = name;

            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        }

        static void AddSpawnPoint(Transform parent, string name,
            Vector3 position, Color color, string label)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            SpawnPointMarker sp = go.AddComponent<SpawnPointMarker>();
            sp.label = label;
            sp.gizmoColor = color;

            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        }

        static void AddSecuritySocket(Transform parent, string name,
            Vector3 position, SecuritySocket.SecurityDeviceType deviceType)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            SecuritySocket socket = go.AddComponent<SecuritySocket>();
            socket.deviceType = deviceType;

            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        }

        static void AddExpansionZone(Transform parent, string name,
            Vector3 position, ExpansionType type, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            ExpansionZone ez = go.AddComponent<ExpansionZone>();
            ez.expansionType = type;
            ez.slotSize = size;

            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        }

        static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
    }
}
#endif
