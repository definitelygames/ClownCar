# Vehicle Input System

Documentation for the base vehicle input handling in ClownCar.

## Overview

The vehicle input system is built on Unity's new Input System. In the current architecture, input handling is primarily done through `VehicleMultiplayerSteering` and its pluggable steering methods. The legacy `VehicleNewInput` component is still available for standalone use.

## Current Architecture

The primary input path is:

```
VehicleMultiplayerSteering (manager)
  └── SteeringMethod.ReadInput()           ◄── Each mode reads its own inputs
  └── SteeringMethod.GetVehicleInput()     ◄── Returns VehicleInput struct
  └── Manager applies to VehicleController ◄── steerInput, throttleInput, brakeInput
```

See [Multiplayer Steering](MultiplayerSteering.md) for full details on the steering system.

## Input Actions

The system expects an InputActionAsset with a `Vehicle` action map containing:

| Action | Type | Description |
|--------|------|-------------|
| `Steer` | Axis | Left/right steering (-1 to 1) |
| `Throttle` | Axis | Forward/reverse throttle |
| `Brake` | Button/Axis | Brake input |
| `Handbrake` | Button | Handbrake/parking brake |
| `ResetVehicle` | Button | Reset vehicle to upright position |
| `ReverseModifier` | Button | Manual reverse mode toggle |

The manager reads `Handbrake` and `ResetVehicle` universally. Individual steering methods (like `SinglePlayerSteering`) read `Steer`, `Throttle`, `Brake`, and `ReverseModifier` from their own config's InputActionAsset.

## VehicleController Integration

All steering methods ultimately set these properties on `VehicleController`:

| Property | Range | Description |
|----------|-------|-------------|
| `steerInput` | -1 to 1 | Steering angle (left/right) |
| `throttleInput` | -1 to 1 | Throttle (negative = reverse) |
| `brakeInput` | 0 to 1 | Brake force |
| `handbrakeInput` | 0 to 1 | Handbrake force (set by manager) |

## Input Combine Modes

Used by `SinglePlayerSteering` when multiple bindings exist for the same action:

| Mode | Behavior |
|------|----------|
| `TakeHighestMagnitude` | Use whichever input has the largest absolute value |
| `Sum` | Add all inputs together (clamped to -1, 1) |
| `Average` | Average all active inputs |

## Continuous Forward and Reverse

When `continuousForwardAndReverse = true` (in `SinglePlayerSteeringConfig`):

| Vehicle State | Forward Input | Reverse Input | Result |
|---------------|---------------|---------------|--------|
| Moving forward | Throttle | Brake | Accelerate / Brake |
| Stationary | Throttle | Reverse | Move forward / backward |
| Moving backward | Brake | Throttle | Brake / Accelerate backward |

When false, the `ReverseModifier` key toggles between forward and reverse modes.
