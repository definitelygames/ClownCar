# ClownCar Documentation

Technical documentation for the ClownCar Unity project.

## Documents

### Input & Steering

- **[Vehicle Input System](VehicleInputSystem.md)** - Base vehicle input handling using Unity's new Input System, including external override support
- **[Multiplayer Steering](MultiplayerSteering.md)** - Cooperative/competitive steering system for 1-4 players sharing vehicle control

## Quick Reference

### Key Bindings (Multiplayer Steering)

| Player | Left | Right | Toggle |
|--------|------|-------|--------|
| P1 | A | D | 1 |
| P2 | LeftArrow | RightArrow | 2 |
| P3 | J | K | 3 |
| P4 | O | P | 4 |

| Action | Key |
|--------|-----|
| Toggle Steering UI | U |
| Toggle Telemetry | Y |

### Component Setup

```
Vehicle GameObject
├── VehicleController (EVP)
├── VehicleNewInput
├── MultiplayerSteeringManager  ◄── Add for multiplayer
└── MultiplayerSteeringUI       ◄── Add for multiplayer
```

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│                        Input Layer                               │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────┐         ┌──────────────────────────────┐   │
│  │ VehicleNewInput │         │ MultiplayerSteeringManager   │   │
│  │                 │         │                              │   │
│  │ • Keyboard      │         │ • Player 1-4 inputs          │   │
│  │ • Gamepad       │ ──────► │ • Analog ramping             │   │
│  │ • Throttle      │ override│ • Input combining            │   │
│  │ • Brake         │         │                              │   │
│  └────────┬────────┘         └──────────────┬───────────────┘   │
│           │                                 │                    │
│           │ steerInput                      │ steerInput         │
│           │ (when no override)              │ (when override)    │
│           │                                 │                    │
│           └─────────────┬───────────────────┘                    │
│                         ▼                                        │
│              ┌─────────────────────┐                             │
│              │  VehicleController  │                             │
│              │                     │                             │
│              │  • steerInput       │                             │
│              │  • throttleInput    │                             │
│              │  • brakeInput       │                             │
│              │  • handbrakeInput   │                             │
│              └─────────────────────┘                             │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

## File Locations

```
Assets/
├── _Scripts/
│   ├── Input/
│   │   └── VehicleNewInput.cs
│   └── MultiplayerSteering/
│       ├── MultiplayerSteeringPlayer.cs
│       ├── MultiplayerSteeringManager.cs
│       └── MultiplayerSteeringUI.cs
└── Plugins/
    └── EVP5/
        └── Scripts/
            ├── VehicleController.cs
            └── VehicleTelemetry.cs
```
