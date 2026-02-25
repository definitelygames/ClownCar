# ClownCar Documentation

Technical documentation for the ClownCar Unity project.

## Documents

### Input & Steering

- **[Vehicle Input System](VehicleInputSystem.md)** - Base vehicle input handling using Unity's new Input System
- **[Multiplayer Steering](MultiplayerSteering.md)** - Unified steering system with pluggable modes (single player, discrete multiplayer, lean, per-wheel)

## Quick Reference

### Key Bindings

| Action | Key |
|--------|-----|
| Toggle P1-P4 | 1, 2, 3, 4 |
| Toggle Steering UI | U |
| Cycle Steering Mode | M |

### Component Setup

```
Vehicle GameObject
├── VehicleController (EVP)
├── VehicleMultiplayerSteering       ◄── Unified steering manager
└── PerWheelVisualEffects            ◄── Optional: per-wheel steering wheel visuals
```

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│                     VehicleMultiplayerSteering                       │
│                      (execution order: 100)                          │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │  SteeringMethodConfig[] modes (ScriptableObject assets)       │  │
│  │                                                                │  │
│  │  ┌──────────────┐ ┌──────────────┐ ┌───────────┐ ┌─────────┐ │  │
│  │  │ SinglePlayer │ │   Discrete   │ │   Lean    │ │PerWheel │ │  │
│  │  │   Steering   │ │  Multiplayer │ │Multiplayer│ │Multiplay│ │  │
│  │  └──────────────┘ └──────────────┘ └───────────┘ └─────────┘ │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                              │                                       │
│              activeMethod.GetVehicleInput()                          │
│              activeMethod.ApplyPhysics()                             │
│                              │                                       │
│                              ▼                                       │
│                 ┌─────────────────────┐                              │
│                 │  VehicleController  │                              │
│                 │  (EVP physics)      │                              │
│                 └─────────────────────┘                              │
│                              │                                       │
│                              ▼                                       │
│               ┌───────────────────────────┐                          │
│               │  PerWheelVisualEffects    │                          │
│               │  (execution order: 200)   │                          │
│               │  Reads wheelData[i] for   │                          │
│               │  steering wheel visuals   │                          │
│               └───────────────────────────┘                          │
└──────────────────────────────────────────────────────────────────────┘
```

## File Locations

```
Assets/
├── _Scripts/
│   ├── Steering/
│   │   ├── VehicleMultiplayerSteering.cs    ◄── Manager (exec order 100)
│   │   ├── SteeringMethod.cs                ◄── Abstract base + VehicleInput struct
│   │   ├── SteeringMethodConfig.cs          ◄── Abstract ScriptableObject base
│   │   ├── PerWheelVisualEffects.cs         ◄── Per-wheel steering wheel visuals (exec order 200)
│   │   ├── Configs/
│   │   │   ├── SinglePlayerSteeringConfig.cs
│   │   │   ├── DiscreteSteeringConfig.cs
│   │   │   ├── LeanSteeringConfig.cs
│   │   │   └── PerWheelSteeringConfig.cs
│   │   ├── Methods/
│   │   │   ├── SinglePlayerSteering.cs
│   │   │   ├── DiscreteMultiplayerSteering.cs
│   │   │   ├── LeanMultiplayerSteering.cs
│   │   │   └── PerWheelMultiplayerSteering.cs
│   │   └── Data/
│   │       └── MultiplayerSteeringPlayer.cs ◄── Shared data types
│   └── Input/
│       └── VehicleNewInput.cs               ◄── Legacy single-player input
└── Plugins/
    └── EVP5/
        └── Scripts/
            ├── VehicleController.cs
            ├── VehicleVisualEffects.cs       ◄── EVP built-in (single steering wheel)
            └── VehicleTelemetry.cs
```
