# Multiplayer Control System

A party-game system that splits vehicle control into 4 discrete single-button actions randomly distributed among 1-4 players. Players must cooperate, each only controlling a piece of the vehicle.

## Overview

The system takes 4 vehicle actions (steer left, steer right, accelerate, brake) and randomly deals them to active players. With 1 player, they get all 4 actions. With 4 players, each gets exactly 1 action. Toggling players on/off triggers a reshuffle.

## Actions

| Action | Effect |
|--------|--------|
| **SteerLeft** | Steers the vehicle left (0 to -1) |
| **SteerRight** | Steers the vehicle right (0 to +1) |
| **Accelerate** | Throttle input (0 to 1) |
| **Brake** | Brake input (0 to 1) |

## Control Distribution

When players are toggled, all 4 actions are shuffled (Fisher-Yates) and dealt round-robin:

| Players | Distribution |
|---------|-------------|
| 1 | 4 controls |
| 2 | 2 each |
| 3 | 2+1+1 |
| 4 | 1 each |

Each assigned action maps to the next available key from the player's key pool.

## Default Key Pools

| Player | Key 1 | Key 2 | Key 3 | Key 4 | Default State |
|--------|-------|-------|-------|-------|---------------|
| P1 | A | D | W | S | Enabled |
| P2 | Left | Right | Up | Down | Disabled |
| P3 | J | L | I | K | Disabled |
| P4 | Num4 | Num6 | Num8 | Num5 | Disabled |

Keys are assigned in pool order to the player's controls. For example, if P1 receives SteerRight and Brake, they'd use A for SteerRight and D for Brake.

## Components

### MultiplayerSteeringPlayer

Per-player input state. Holds the key pool, assigned controls, and analog ramping parameters.

**Location:** `Assets/_Scripts/MultiplayerSteering/MultiplayerSteeringPlayer.cs`

**Key types:**
- `VehicleControlAction` enum: `SteerLeft`, `SteerRight`, `Accelerate`, `Brake`
- `ControlBinding`: maps an action to a key with ramped `currentValue`

### MultiplayerSteeringManager

Main controller. Distributes controls, reads input, combines values, applies to vehicle.

**Location:** `Assets/_Scripts/MultiplayerSteering/MultiplayerSteeringManager.cs`

**Properties:**
- `CombinedSteer` — net steering (-1 to 1)
- `CombinedThrottle` — throttle (0 to 1)
- `CombinedBrake` — brake (0 to 1)

### MultiplayerSteeringUI

OnGUI display showing per-player control panels with key assignments and fill indicators, plus combined steer/throttle/brake bars.

**Location:** `Assets/_Scripts/MultiplayerSteering/MultiplayerSteeringUI.cs`

## Setup

1. Add `MultiplayerSteeringManager` and `MultiplayerSteeringUI` to your vehicle GameObject
2. Play — P1 starts with all 4 controls on WASD
3. Press 2, 3, or 4 to enable additional players and redistribute
4. Press U to toggle UI visibility

## Integration with VehicleNewInput

The manager sets `externalSteeringOverride`, `externalThrottleOverride`, and `externalBrakeOverride` on `VehicleNewInput` when enabled. This prevents the normal input system from overwriting multiplayer-controlled values. All three overrides are cleared when the manager is disabled.

## API

```csharp
// Redistribute controls (called automatically on player toggle)
steeringManager.DistributeControls();

// Get combined values
float steer = steeringManager.CombinedSteer;
float throttle = steeringManager.CombinedThrottle;
float brake = steeringManager.CombinedBrake;

// Enable/disable player programmatically (auto-redistributes)
steeringManager.SetPlayerEnabled(1, true);

// Get a player's current value for a specific action
float val = player.GetControlValue(VehicleControlAction.Accelerate);
```
