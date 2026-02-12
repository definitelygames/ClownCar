# ClownCar

Unity 6.3 multiplayer game where multiple players control a single vehicle simultaneously.

## Tech Stack
- Unity 6.3
- Universal Render Pipeline (URP)
- New Input System
- EVP5 (Edy's Vehicle Physics)

## Project Structure
```
Assets/
├── EVP5/           # Vehicle physics package
├── Scripts/
│   ├── Input/      # New Input System vehicle controls
│   └── *.cs        # Ragdoll passenger system
```

## Key Systems

### Vehicle Input
- `VehicleNewInput.cs` - New Input System wrapper for EVP5
- `VehicleInputActions.inputactions` - Input bindings (keyboard + gamepad)
- Supports multiple simultaneous input sources

### Ragdoll Passengers
- `TruckPassengerAnchor.cs` - Spawns/anchors ragdolls to vehicle
- `RagdollBalance.cs` - Active ragdoll balance forces
- `RagdollSetup.cs` - Helper to create ragdolls from humanoids

## Conventions
- C# scripts use EVP namespace for vehicle-related code
- Shader Graphs for custom materials (URP compatible)
- New Input System only (no legacy Input Manager)
