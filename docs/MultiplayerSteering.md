# Multiplayer Steering System

A unified steering system using a strategy pattern. A single manager (`VehicleMultiplayerSteering`) delegates to pluggable steering method implementations via ScriptableObject configs. Supports 1-4 players with hot-swappable modes at runtime.

## Architecture

```
SteeringMethodConfig (ScriptableObject)     SteeringMethod (plain C# class)
├── SinglePlayerSteeringConfig        ───►  SinglePlayerSteering
├── DiscreteSteeringConfig            ───►  DiscreteMultiplayerSteering
├── LeanSteeringConfig                ───►  LeanMultiplayerSteering
└── PerWheelSteeringConfig            ───►  PerWheelMultiplayerSteering
```

Each config is a ScriptableObject with tuning parameters and a `CreateMethod()` factory. The manager holds an array of configs and instantiates methods at `Awake()`.

## VehicleMultiplayerSteering (Manager)

**Location:** `Assets/_Scripts/Steering/VehicleMultiplayerSteering.cs`
**Execution order:** 100

Central controller that:
- Creates `SteeringMethod` instances from config assets
- Delegates input reading, vehicle input, physics, and GUI to the active method
- Handles universal controls: handbrake (Input System), vehicle reset, player toggles (1-4 keys), UI toggle (U key), mode switching (M key)

### Inspector

| Property | Type | Description |
|----------|------|-------------|
| `vehicle` | VehicleController | Target vehicle (auto-detected) |
| `modes` | SteeringMethodConfig[] | Available steering mode configs |
| `activeModeIndex` | int | Initially active mode index |
| `universalInputActions` | InputActionAsset | For handbrake and reset actions |
| `player1-4ToggleKey` | KeyCode | Toggle keys (default: 1-4) |
| `uiToggleKey` | KeyCode | UI visibility toggle (default: U) |
| `nextModeKey` | KeyCode | Cycle modes (default: M) |

### Lifecycle

```
Awake:   configs[i].CreateMethod() → methods[i].Initialize(manager, vehicle)
OnEnable:  SetMode(activeModeIndex) → method.Activate()
Update:    HandlePlayerToggles(), HandleModeSwitch(), method.ReadInput()
FixedUpdate: method.GetVehicleInput() → apply to vehicle, method.ApplyPhysics()
LateUpdate:  method.LateUpdate()
OnGUI:       method.DrawGUI()
OnDisable:   method.Deactivate()
```

### API

```csharp
manager.SetMode(1);                    // Switch steering mode
manager.SetPlayerEnabled(2, true);     // Enable player 3
manager.ActiveModeName;                // Current mode display name
manager.GetEnabledPlayerCount();       // Number of active players
manager.PlayerEnabled[i];              // Per-player enabled state
manager.HandbrakeInput;                // Universal handbrake value
manager.ShowUI;                        // UI visibility flag
```

## Steering Methods

### 1. Single Player (`SinglePlayerSteering`)

Standard Input System-based steering for one player. Reads steer/throttle/brake/reverse from a `Vehicle` action map.

**Config:** `SinglePlayerSteeringConfig` (menu: Vehicle/Steering/Single Player)

| Setting | Default | Description |
|---------|---------|-------------|
| `inputActions` | - | InputActionAsset with Vehicle action map |
| `continuousForwardAndReverse` | true | Auto forward/reverse based on velocity |
| `handbrakeOverridesThrottle` | false | Handbrake reduces throttle |
| `combineMode` | TakeHighestMagnitude | How to combine multiple bindings |

### 2. Discrete Multiplayer (`DiscreteMultiplayerSteering`)

Distributes 4 vehicle actions (steer left, steer right, accelerate, brake) randomly among active players. Each player presses assigned keys to control their actions with analog ramping.

**Config:** `DiscreteSteeringConfig` (menu: Vehicle/Steering/Discrete Multiplayer)

| Setting | Default | Description |
|---------|---------|-------------|
| `player1-4Keys` | WASD, Arrows, IJKL, Numpad | 4-key pools per player |
| `rampUpSpeed` | 3.0 | Input ramp-up rate |
| `rampDownSpeed` | 5.0 | Input ramp-down rate |
| `player1-4Color` | R/B/G/Y | UI colors per player |

**Distribution:** Fisher-Yates shuffle, then round-robin deal:

| Players | Distribution |
|---------|-------------|
| 1 | 4 actions |
| 2 | 2 each |
| 3 | 2+1+1 |
| 4 | 1 each |

### 3. Lean Multiplayer (`LeanMultiplayerSteering`)

2D dot-position controls per player. Combined dot positions apply roll/pitch torques and optionally steer the vehicle. Includes a "pop" mechanic — synchronized edge hits from all players produce an impulse.

**Config:** `LeanSteeringConfig` (menu: Vehicle/Steering/Lean Multiplayer)

| Setting | Default | Description |
|---------|---------|-------------|
| `leanAffectsSteering` | true | Horizontal lean controls steering |
| `steeringMultiplier` | 1.0 | Per-player steering contribution |
| `leanTorqueLateral` | 5000 | Roll torque magnitude |
| `leanTorqueLongitudinal` | 3000 | Pitch torque magnitude |
| `popWindow` | 0.1s | Pop timing window |
| `popForce` | 10000 | Pop impulse torque |
| `keyboardMoveSpeed` | 2.0 | Dot move speed (keyboard) |
| `keyboardReturnSpeed` | 3.0 | Dot return-to-center speed |

**Input types:** WASD, ArrowKeys, Gamepad1-4, Mouse

### 4. Per-Wheel Multiplayer (`PerWheelMultiplayerSteering`)

Each player controls one wheel's steering angle and drive force independently. Bypasses EVP's global steer/drive by disabling per-wheel flags and writing directly to WheelColliders + `AddForceAtPosition`.

**Config:** `PerWheelSteeringConfig` (menu: Vehicle/Steering/Per-Wheel Multiplayer)

| Setting | Default | Description |
|---------|---------|-------------|
| `useVehicleSteerAngle` | true | Use EVP's maxSteerAngle for front |
| `maxSteerAngle` | 35 | Custom front max angle (if above is false) |
| `rearMaxSteerAngle` | 15 | Max steer angle for rear wheels |
| `maxDriveForce` | 1500 | Max force per wheel (Newtons) |
| `forceFalloffWithSpeed` | 0.5 | Force reduction at high speed |
| `maxSpeed` | 30 | Speed (m/s) for full force falloff |
| `centerUncontrolledWheels` | true | Unowned wheels center steering |
| `centeringSpeed` | 45 | Centering rate (degrees/sec) |

**Wheel assignment:** Fixed mapping P1→wheel[0], P2→wheel[1], P3→wheel[2], P4→wheel[3].

**Input types:** Same as Lean (WASD, ArrowKeys, Gamepad1-4, Mouse). X-axis = steer, Y-axis = throttle/brake.

## Visual Effects

### PerWheelVisualEffects

**Location:** `Assets/_Scripts/Steering/PerWheelVisualEffects.cs`
**Execution order:** 200 (runs after VehicleMultiplayerSteering)

Rotates up to 4 interior steering wheel meshes based on each wheel's individual `steerAngle`. Works in all steering modes — in normal EVP mode, `wheelData[i].steerAngle` is set by EVP for steered wheels and 0 for non-steered.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `steeringWheels` | Transform[4] | - | Steering wheel mesh per player slot |
| `degreesOfRotation` | float | 420 | Visual rotation range for full lock |
| `brakesRenderer` | Renderer | - | Brake light mesh renderer |
| `brakesMaterialIndex` | int | - | Material slot for brake lights |
| `brakesOnMaterial` | Material | - | Lit brake material |
| `brakesOffMaterial` | Material | - | Unlit brake material |

**How it works:**
- `LateUpdate`: reads `vehicle.wheelData[i].steerAngle`, computes `z = -0.5 * degreesOfRotation * steerAngle / maxSteerAngle`, applies to each steering wheel transform
- `Update`: swaps brake light material based on `vehicle.brakeInput`
- Null steering wheel slots are safely skipped

## Data Types

**Location:** `Assets/_Scripts/Steering/Data/MultiplayerSteeringPlayer.cs`

| Type | Used By | Description |
|------|---------|-------------|
| `VehicleControlAction` | Discrete | Enum: SteerLeft, SteerRight, Accelerate, Brake |
| `ControlBinding` | Discrete | Maps action → key with ramped value |
| `MultiplayerSteeringPlayer` | Discrete | Per-player input state and key pool |
| `LeanInputType` | Lean, PerWheel | Enum: WASD, ArrowKeys, Gamepad1-4, Mouse |
| `LeanPlayerData` | Lean | Per-player dot position and pop state |
| `PerWheelPlayerData` | PerWheel | Per-player wheel index and input position |

## Setup

1. Add `VehicleMultiplayerSteering` to the vehicle GameObject
2. Create ScriptableObject configs via `Assets > Create > Vehicle > Steering > ...`
3. Assign configs to the `modes` array
4. Assign `universalInputActions` (VehicleInputActions asset)
5. Optionally add `PerWheelVisualEffects` and assign steering wheel transforms
6. Play — P1 starts enabled, press 2-4 to toggle players, M to cycle modes, U for UI

## Key Bindings

| Action | Key | Description |
|--------|-----|-------------|
| Toggle P1-P4 | 1, 2, 3, 4 | Enable/disable players |
| Toggle UI | U | Show/hide mode-specific UI |
| Next Mode | M | Cycle through steering modes |
| Handbrake | (Input System) | Universal handbrake |
| Reset | (Input System) | Reset vehicle upright |
