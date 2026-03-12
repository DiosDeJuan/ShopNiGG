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

## Technical Details

- **Engine:** Unity 6 (6000.0.37f1)
- **Language:** C#
- **Target:** Desktop PCs (2 cores, 4GB RAM minimum)
- **Performance:** Supports 100+ simultaneous customers at 30+ FPS

