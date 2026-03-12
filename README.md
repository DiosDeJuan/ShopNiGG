# ShopNiGG - ShopMaster

A 3D supermarket business simulator built with Unity where the player manages and expands their own store.

## Game Overview

The player starts with:
- **$2,500** starting capital
- No employees
- No security
- A small supermarket

The goal is to expand the business and dominate the market through strategic decisions about pricing, inventory, security, and employee management.

## Systems

### Core Systems (from Store Simulator Template)
- **Customer System** – 50-75 customers per day with AI shopping behavior
- **Cash Desk** – Manual checkout supporting card and cash payments with change
- **Day Cycle** – Time progression with opening/closing hours and auto-save
- **Pricing System** – Buy price, store price, and market price with purchase probability
- **Delivery & Placement** – Product ordering, delivery, and shelf stocking
- **Storage System** – Warehouse grid management
- **Expansion System** – Sales space ($1,750/16m²) and storage space ($2,500/32m²)
- **Upgrade System** – Licenses, decorations, and daily boosters

### New Systems (ShopMaster Requirements)
- **Entrepreneur Skill Tree (RQF3-4)** – Progression system with skills, products, employees, and upgrades. Each node has a cost and may require prerequisites.
- **Employee System (RQF8-9)** – Hire and assign employees as Cashiers (auto-checkout) or Stockers (auto-restock). Employees unlock via the skill tree.
- **Security System (RQF10-12)** – Three security levels with increasing auto-arrest chances (33%, 66%, 99%). Each level requires prior skill tree nodes.
- **Shoplifter System (RQF13-20)** – 1 shoplifter per 25 customers. Three types: Common, Expert, Fast. Visual and audio detection alerts. Daily robbery report in statistics. Difficulty scales with store expansion.
- **Map Layout System** – Zone management for the supermarket layout with modular expansion tracking, area validation, and runtime zone queries.

## Level Design

### Terrain Specifications

| Property | Value |
|---|---|
| Total terrain area | 900 m² (30m × 30m) |
| Initial sales floor | 192 m² (16m × 12m) |
| Initial storage | 32 m² (8m × 4m) |
| Player office | 32 m² (8m × 4m) |
| Sales expansion module | 16 m² per module |
| Storage expansion module | 32 m² per module |

### Map Zones

The supermarket map is divided into the following functional zones (defined in `MapZone` enum and configured via `MapZoneConfig` ScriptableObjects):

1. **Main Entrance** – Automatic sliding doors, customer flow area, cart staging zone
2. **Sales Floor** – Gondolas, refrigerators, freezers, and product displays arranged in navigable aisles (minimum 2m wide)
3. **Cash Registers** – Queue lanes with space for multiple cash desks and NPC cashiers
4. **Player Office** – 32 m² room with desk, laptop (store management), and tablet (build mode)
5. **Storage** – Warehouse with grid-based shelving for product packages
6. **Loading Dock** – Secondary door for product deliveries
7. **Parking** – Front parking lot for customer vehicles
8. **Access Road** – Connecting road and sidewalks in a suburban/rural setting

### Expansion System

The supermarket grows through modular expansion modules:
- **Sales Floor Modules**: +16 m² per module, adds aisles and gondola space
- **Storage Modules**: +32 m² per module, adds warehouse grid area
- Each module activates a `StorageGrid` and animates walls below ground via `ExpansionObject`
- Expansions are sequential (each requires the previous one) with increasing level and cost requirements
- The `MapLayoutSystem` tracks total built area and prevents expansion beyond the 900 m² terrain limit

### Layout Design Principles

- **Customer Flow**: Entrance → Sales Floor → Cash Registers → Exit. Aisles are ≥2m wide for AI navigation without bottlenecks
- **Modular Growth**: Expansion direction is along positive X (sales) and positive Z (storage), keeping the building rectangle clean
- **Visual Integration**: New modules share wall materials (via `DecorationScriptableObject`) and blend seamlessly with the base structure
- **Optimization**: Baked lighting, occlusion culling, simple box colliders, and modular prefabs minimize draw calls and physics overhead

### Building the Map in Unity (Step-by-Step)

1. **Terrain Setup**: Create a flat 30m × 30m plane. Place the store building prefab at center with parking in front and loading dock at the rear.
2. **Zone Layout**: Use `MapZoneConfig` ScriptableObjects to define each zone's dimensions, position offset, and expansion rules. Assign them to `MapLayoutSystem.zoneConfigs` in the inspector.
3. **Sales Floor**: Place `StorageGrid` components for the initial 192 m² area. Add shelf prefabs (`Shelf_A`, `Shelf_B`, `Shelf_C`) in rows with 2m aisle spacing. Use `StorageGridBlocker` for walls and pillars.
4. **Cash Registers**: Place `CashDesk` prefabs near the exit with queue lanes. Leave space for `SelfCheckout` prefabs as the store expands.
5. **Player Office**: Place the `Computer` prefab (shop desktop) and configure the 32 m² room with desk and furniture.
6. **Storage Area**: Place a `StorageGrid` for the initial 32 m² warehouse. Position the `DeliverySystem.deliveryStart` transform at the loading dock.
7. **Entrance**: Add `SlidingDoorTrigger` on the main door. Position `CustomerSystem` spawn locations outside along the access road.
8. **Expansions**: For each expansion module, create an `ExpansionObject` with its `ExpansionScriptableObject` and a new `StorageGrid`. Place removable walls that animate below ground on purchase.
9. **NavMesh**: Bake the NavMesh for the entire walkable area including expansion zones (set inactive grids to NavMesh walkable).
10. **Lighting**: Use baked lighting for all static elements. Limit dynamic lights to the sun (via `DayCycleSystem`) and any interactive light sources.

### Optimization Best Practices

- **Occlusion Culling**: Enable occlusion culling in the Game scene. Mark all static store geometry as Occluder Static and Occludee Static.
- **Baked Lighting**: Use the Baked GI lightmapper for all non-moving lights. Only the sun (rotating via `DayCycleSystem`) should be realtime.
- **Simple Colliders**: Use box colliders on shelves, walls, and furniture. Avoid mesh colliders for static geometry.
- **Modular Prefabs**: Build walls, floors, and roof sections as reusable prefabs (`Store_Wall`, `Store_Floor`, `Store_Door`) for consistent batching.
- **LOD Groups**: Apply LOD to environment buildings (`Building_2A`, `Building_3A`, etc.) visible from parking and road areas.
- **Texture Atlasing**: Combine product textures into atlases to reduce material switches on shelves.
- **Streaming Mipmaps**: Enable streaming mipmaps in QualitySettings for large textures to reduce memory pressure.

### Template Prefabs Reference

| Category | Prefabs | Usage |
|---|---|---|
| Store Elements | `CashDesk`, `SelfCheckout`, `OpenSign`, `Computer`, `TrashCan` | Core interactive store objects |
| Storage | `Shelf_A/B/C`, `PlacementArea`, `PriceTag`, `GroundLine`, `GridBlocker` | Sales floor and warehouse placement |
| Environment | `Building_2A/2B`, `Building_3A-3F`, `Store_Wall/Door/Floor`, `Roof_*`, `Road_*` | Exterior and structural elements |
| Customers | `Customer_A` through `Customer_E` | Customer AI prefabs with NavMeshAgent |
| Systems | `AudioSystem`, `CustomerSystem`, `DayCycleSystem`, `DeliverySystem`, etc. | Singleton system managers |

## Technical Details

- **Engine:** Unity 6 (6000.0.37f1)
- **Language:** C#
- **Target:** Desktop PCs (2 cores, 4GB RAM minimum)
- **Performance:** Supports 100+ simultaneous customers at 30+ FPS

