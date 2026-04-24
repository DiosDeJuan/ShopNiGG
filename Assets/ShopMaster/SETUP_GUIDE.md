# ShopMaster – Árbol del Emprendedor: Guía de Montaje en Unity

## Resumen del sistema

| Capa | Archivos |
|------|----------|
| Datos (ScriptableObjects) | `EntrepreneurNodeData`, `EntrepreneurTreeData` |
| Runtime | `PlayerProgressPoints`, `EntrepreneurTreeManager` |
| UI | `EntrepreneurTreeUIController`, `EntrepreneurNodeUI`, `EntrepreneurConnectionUI`, `EntrepreneurTooltipUI` |
| Editor | `EntrepreneurTreeBuilder` (auto-crea los 36 nodos) |

---

## Paso 1 – Crear los ScriptableObjects

1. Abre Unity.
2. En el menú superior: **ShopMaster → Build Entrepreneur Tree**.
3. El builder crea automáticamente:
   - `Assets/ShopMaster/ScriptableObjects/EntrepreneurTree/EntrepreneurTreeData.asset`
   - `Assets/ShopMaster/ScriptableObjects/EntrepreneurTree/Nodes/NodeData_<id>.asset` (×36)

---

## Paso 2 – Agregar los sistemas al objeto de escena

1. Selecciona el GameObject `Systems` (o el que contenga `UpgradeSystem`, `EmployeeSystem`, etc.).
2. Agrega los componentes:
   - `PlayerProgressPoints`
   - `EntrepreneurTreeManager`  
     → Asigna `EntrepreneurTreeData.asset` al campo **Tree Data**.

> Estos dos MonoBehaviours se suscriben automáticamente a `SaveGameSystem.dataSaveEvent` y `dataLoadEvent` para persistir datos con PlayerPrefs.

---

## Paso 3 – Crear los prefabs de UI

### 3a. Prefab de Nodo (`NodePrefab`)

```
[Panel] (120 × 60 px, Image – Color blanco para tinting)
  ├─ [Icon] Image (40 × 40 px, opcional)
  ├─ [NameText] TMP_Text (nombre del nodo)
  ├─ [CostText] TMP_Text (costo o "✓")
  └─ [TypeText] TMP_Text (tipo – opcional)
```
- Añade el componente `EntrepreneurNodeUI`.
- Conecta los campos `backgroundImage`, `iconImage`, `nameText`, `costText`, `typeText`.
- Guarda como: `Assets/ShopMaster/Prefabs/UI/Upgrades/NodePrefab.prefab`

### 3b. Prefab de Conexión (`ConnectionPrefab`)

```
[Image] (cualquier tamaño, solo necesita Image + RectTransform)
```
- Añade el componente `EntrepreneurConnectionUI`.
- Color de imagen base: blanco (el script lo tinta en tiempo de ejecución).
- Guarda como: `Assets/ShopMaster/Prefabs/UI/Upgrades/ConnectionPrefab.prefab`

### 3c. Prefab de Tooltip (`TooltipPrefab`)

```
[Panel] (300 × 200 px, CanvasGroup)
  ├─ [TitleText]        TMP_Text
  ├─ [DescriptionText]  TMP_Text
  ├─ [TypeText]         TMP_Text
  ├─ [CostText]         TMP_Text
  ├─ [RequirementsText] TMP_Text
  └─ [StatusText]       TMP_Text
```
- Añade el componente `EntrepreneurTooltipUI`.
- Conecta todos los campos.
- Guarda como: `Assets/ShopMaster/Prefabs/UI/Upgrades/TooltipPrefab.prefab`

---

## Paso 4 – Integrar en la pestaña UPGRADES existente

El asset incluye un prefab `UIShopDesktop` con una pestaña **UPGRADES**. No la elimines.

### Jerarquía recomendada dentro del panel UPGRADES:

```
[UpgradesPanel]  ← Panel existente del asset (activado por el botón UPGRADES)
  ├─ Header
  │    ├─ PointsText    TMP_Text  ← "Puntos: X"
  │    └─ FeedbackText  TMP_Text  ← mensajes de feedback
  ├─ Scroll View        ScrollRect
  │    └─ Viewport
  │         └─ Content  RectTransform  ← contentContainer
  └─ Tooltip            TooltipPrefab instance (FUERA del Scroll View)
```

1. Añade el componente `EntrepreneurTreeUIController` al GameObject `[UpgradesPanel]`.
2. Conecta los campos:
   | Campo | Qué asignar |
   |-------|-------------|
   | `treeData` | `EntrepreneurTreeData.asset` (Paso 1) |
   | `nodePrefab` | `NodePrefab.prefab` |
   | `connectionPrefab` | `ConnectionPrefab.prefab` |
   | `contentContainer` | RectTransform del `Content` del Scroll View |
   | `pointsText` | TMP_Text del header |
   | `feedbackText` | TMP_Text del header |
   | `tooltip` | Instancia de `TooltipPrefab` en la escena |

---

## Paso 5 – Dar puntos por logros

En cualquier script de logros, llama:
```csharp
PlayerProgressPoints.AddPoints(1);
```
Por ejemplo, cuando el jugador sube de nivel:
```csharp
// En un script custom (no modificar StoreDatabase.cs)
StoreDatabase.onLevelUpdate += level => PlayerProgressPoints.AddPoints(1);
```

---

## Paso 6 – Conectar gameplay (TODOs)

Abre `EntrepreneurTreeManager.cs` y busca los comentarios `// TODO:` en el método `ApplyGameplayEffect`. Ahí están los hooks para:

- **Productos** → Conectar con el sistema de delivery/tienda del asset.
- **Empleados** → Conectar con `EmployeeSystem.Instance.employees`.
- **Seguridad** → Llamar a `SecuritySystem.TryUpgradeLevel()`.
- **Mejoras** → Aplicar multiplicadores a `EmployeeSystem` o `CashDesk`.

---

## Paso 7 – Verificación rápida

1. Entra en Play Mode.
2. Navega a la computadora → pestaña UPGRADES.
3. Deberías ver los nodos del árbol en gris (bloqueados).
4. Usa el **Context Menu** en el componente `PlayerProgressPoints`: _"Debug: Add 10 Points"_.
5. El primer nodo (`Productos Básicos 1`) debería cambiar a azul (disponible).
6. Haz clic → debe cambiar a verde y el resto del árbol se actualiza.

---

## Notas de diseño

- **Empleado 12 depende de Empleado 13** – dependencia inversa documentada en la especificación. Está implementada tal cual. Si es un error de diseño, editar el campo `requiredNodeIds` del ScriptableObject `NodeData_employee_12` en el inspector.
- Los puntos se guardan en **PlayerPrefs** (clave `ShopMaster_ProgressPoints`). Los nodos desbloqueados se guardan en **PlayerPrefs** (clave `ShopMaster_UnlockedNodes`).
- Para migrar a el sistema de save del asset (JSON), sustituye el cuerpo de `OnSave`/`OnLoad` en `PlayerProgressPoints` y `EntrepreneurTreeManager` por llamadas a `SimpleJSON`.
