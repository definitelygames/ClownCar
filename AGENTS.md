# ClownCar

Unity 6.3 multiplayer game where multiple players control a single vehicle simultaneously.

## Tech Stack
- Unity 6.3
- Universal Render Pipeline (URP)
- New Input System (for universal controls: handbrake, reset)
- Legacy `Input.GetKey` (for per-player keyboard input in steering methods)
- EVP5 (Edy's Vehicle Physics)

## Project Structure
```
Assets/
├── Plugins/EVP5/       # Vehicle physics package
├── _Scripts/
│   ├── Input/          # New Input System vehicle controls
│   ├── Steering/       # Multiplayer steering system
│   │   ├── Configs/    # ScriptableObject configs per mode
│   │   ├── Data/       # Player data classes
│   │   └── Methods/    # SteeringMethod implementations
│   └── *.cs            # Ragdoll passenger system
├── _Data/              # ScriptableObject assets
├── _Prefabs/           # Vehicle prefabs
└── _Scenes/            # Game scenes
```

## Key Systems

### Multiplayer Steering (Strategy Pattern)
- `VehicleMultiplayerSteering.cs` - Manager: mode switching, player toggles, universal input
- `SteeringMethod.cs` / `SteeringMethodConfig.cs` - Abstract base classes
- Modes: Single Player, Discrete Multiplayer, Lean Multiplayer, Per-Wheel Multiplayer
- Each mode is a ScriptableObject config + plain C# method class

### Vehicle Input
- `VehicleNewInput.cs` - New Input System wrapper for EVP5
- `VehicleInputActions.inputactions` - Input bindings (keyboard + gamepad)

### Ragdoll Passengers
- `TruckPassengerAnchor.cs` - Spawns/anchors ragdolls to vehicle
- `RagdollBalance.cs` - Active ragdoll balance forces
- `RagdollSetup.cs` - Helper to create ragdolls from humanoids

## Conventions
- C# scripts use EVP namespace for vehicle-related code
- Shader Graphs for custom materials (URP compatible)
- New Input System for action-based input (handbrake, reset)
- Legacy `Input.GetKey`/`Input.GetKeyDown` for direct keyboard polling in steering methods (acceptable — Unity 6 supports both via "Both" input handling mode)

## Unity 6+ API Notes
- Use `Rigidbody.linearVelocity` not `Rigidbody.velocity` (deprecated, auto-fixed by Script Updater)
- Use `rb.AddForceAtPosition` / `rb.AddTorque` for physics forces (unchanged)
- `WheelCollider.steerAngle` direct writes are valid for per-wheel control
- Do NOT use `WheelCollider.motorTorque` with EVP — EVP zeroes friction stiffness, so PhysX ignores motorTorque. Use `rb.AddForceAtPosition` instead.
