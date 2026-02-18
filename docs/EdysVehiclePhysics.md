# Edy's Vehicle Physics (EVP) – Documentation

Source: https://www.edy.es/dev/vehicle-physics/user-guide/

## Overview

Edy's Vehicle Physics (EVP) is a Unity vehicle physics simulation package providing realistic and fun vehicles for gameplay. It uses a single `VehicleController` component for core physics with optional add-on components.

## Example Scenes

- **"The City – Simple Scene"**: Basic setup with a single vehicle and camera
- **"The City – Vehicle Manager"**: Uses Vehicle Manager component for controlling multiple vehicles

## Control Keys

| Key | Function |
|-----|----------|
| WSAD/Arrows | Throttle, brake, steering |
| Space | Handbrake |
| Enter | Reset vehicle |
| C | Change camera |
| Tab/Page Up/Down | Select vehicle (Vehicle Manager) |
| E | Make stone jump (load test) |
| R | Repair vehicle damage |
| T | Slow time mode |
| P | Pause vehicles |
| Y | Show/hide telemetry |
| Esc | Reset scene |

## Core Components

### Main Components

- **VehicleController**: Provides vehicle physics. The central component.
- **VehicleCameraController**: Camera control and modes
- **VehicleManager**: Manages multiple user-controlled vehicles
- **GroundMaterialManager**: Manages ground materials and properties

### Vehicle Add-on Components

- **VehicleAudio**: Audio effects (engine, turbo, transmission, tire skid, body impacts, body scratches)
- **VehicleDamage**: Deformation and handling deterioration
- **VehicleRandomInput**: Simple AI example
- **VehicleStandardInput**: Standard Unity Input handling
- **VehicleTelemetry**: Physics value exposure via UI and gizmos
- **VehicleTireEffects**: Tire marks and smoke effects
- **VehicleViewConfig**: Visual information for camera controller

### Additional Components

- **TireMarksRenderer**: Draws tire marks on surfaces
- **TireParticleEmitter**: Particle effects (smoke, dust)
- **RigidbodyPause**: Pause and resume functionality (without setting timeScale to zero)
- **VehicleNitro**: Impulse or acceleration boost
- **SceneTools**: Generic functions (slow-time, quit on Escape)

## VehicleController Settings

### Wheels Configuration

Reference WheelCollider components and define roles:
- **steer**: Wheel turns with steering input
- **drive**: Wheel receives motor force
- **brake**: Wheel receives brake force
- **handbrake**: Wheel locks with handbrake input

Provide optional visual mesh transforms for each wheel.

**Tip:** Set wheels in Vehicle Controller, then use context menu "Adjust WheelColliders to their meshes" for automatic adjustment.

### Center of Mass (CoM)

Two modes:
- **Transform**: Use a transform reference directly
- **Parametric**: Use parameters:
  - `centerOfMassPosition` [0.1–0.9]: Front (1.0) to rear (0.0) position
  - `centerOfMassHeightOffset` [-1.0 to 1.0]: Height adjustment

Position affects vehicle reactions to acceleration and braking. Height combines with Anti-Roll setting.

### Handling & Behavior

| Parameter | Description |
|-----------|-------------|
| **Tire Friction** | Coefficient (1.0 standard). Higher for sport vehicles, lower for trucks. |
| **Anti-Roll** | Controls sideways banking (0–1). Higher = flatter steering response. |
| **Max Steer Angle** | Steering wheel rotation in degrees. |
| **Aero Drag** | Force = coefficient × speed². Reduces maximum speed. |
| **Aero Downforce** | Force = coefficient × speed². Increases wheel grip at speed. |
| **Aero App Point Offset** | Longitudinal distance (m) from CoM where aero forces apply. Positive = ahead of CoM, negative = behind. |

### Motor Settings

| Parameter | Description |
|-----------|-------------|
| **Max Speed Forward/Reverse** | Maximum velocity (m/s). |
| **Max Drive Force** | Maximum force (N) at full throttle, decreased by Force Curve Shape. |
| **Max Drive Slip** | Maximum wheel slide rate (m/s) while receiving drive force. |
| **Drive Force To Max Slip** | Excess throttle force (N) causing maximum slip. |
| **Force Curve Shape** | How force decreases with speed. <0.5 = realistic, >0.5 = arcade-style. |

### Brakes

| Parameter | Description |
|-----------|-------------|
| **Max Brake Force** | Maximum braking force (N). |
| **Brake Force To Max Slip** | Excess force causing maximum slip while braking. |
| **Brake Mode / Hand Brake Mode** | Two options: **Slip** (m/s limit) or **Ratio** (percentage of actual speed). |
| **Max Brake Slip / Max Handbrake Slip** | m/s limit in Slip mode. |
| **Max Brake Ratio / Max Handbrake Ratio** | Percentage (0–1) in Ratio mode. |

### Driving Aids

Configurable from disabled to fully engaged:

- **TC (Traction Control)**: Prevents wheel slide during acceleration
- **ABS**: Prevents wheel slide during braking
- **ESP**: Limits steering angle based on speed

### Visual Wheels

- **Spin Update Rate**: `OnUpdate` for quality, `OnFixedUpdate` for performance
- **WheelPositionMode**: `Accurate` uses additional RayCast, `Fast` uses physics-reported position

### Optimization

- **Disallow Runtime Changes**: Performance optimization if WheelCollider parameters won't change at runtime
- Garbage-Collector friendly — no GC allocations at runtime
- Supports physics timesteps up to 0.06 seconds for CPU efficiency

## WheelCollider Configuration

### Required Properties

| Property | Unit |
|----------|------|
| Radius | meters |
| Suspension Distance | meters |
| Spring | N/m |
| Damper | Ns/m |

### Optional Properties

- Force App Point Distance (m)
- Center

Other WheelCollider properties are handled internally by EVP or unused.

## Key VehicleController API

From the source code (`Assets/Plugins/EVP5/Scripts/VehicleController.cs`):

### Input Properties (set these to control the vehicle)

```csharp
vehicle.steerInput    // -1 (left) to 1 (right)
vehicle.throttleInput // -1 (reverse) to 1 (forward)
vehicle.brakeInput    // 0 to 1
vehicle.handbrakeInput // 0 to 1
```

### Read-only Properties

```csharp
vehicle.speed              // Current speed (m/s), positive = forward
vehicle.cachedRigidbody    // Access to the Rigidbody component
```

### Methods

```csharp
vehicle.ResetVehicle()     // Reset vehicle to upright position
```

### Physics Access

```csharp
Rigidbody rb = vehicle.cachedRigidbody;
rb.centerOfMass            // Center of mass (local space)
rb.AddForceAtPosition(force, worldPoint);
rb.AddTorque(torque, ForceMode.Force);
```

### Aerodynamic Forces (applied internally in FixedUpdate)

- Drag: `force = -aeroDrag * speed² * velocityDirection`
- Downforce: `force = -aeroDownforce * speed² * forwardFactor * vehicleUp`
- Applied at aero balance point between front and rear axles

### Vehicle Frame Data

```csharp
// Internal struct with axle positions (used for force application points)
m_vehicleFrame.frontPosition   // Average forward position of front wheels
m_vehicleFrame.rearPosition    // Average forward position of rear wheels
m_vehicleFrame.baseHeight      // Average vertical position of wheels
m_vehicleFrame.frontWidth      // Front axle half-width
m_vehicleFrame.rearWidth       // Rear axle half-width
```

## Upgrade Notes (Unity 4 → Unity 5)

### Script Correspondence

| EVP 4 | EVP 5 |
|-------|-------|
| CameraControl, CamMouseOrbit, CamSmoothFollow | VehicleCameraController |
| CarAntiRollBar | Anti Roll property in VehicleController |
| CarCameras | VehicleViewConfig |
| CarControl | VehicleController |
| CarDamage | VehicleDamage |
| CarExternalInputRandom | VehicleRandomInput |
| CarMain | VehicleManager, SceneTools |
| CarSound | VehicleAudio |
| CarTelemetry | VehicleTelemetry |
| CarVisuals | VehicleTireEffects |

### WheelCollider Migration

When migrating from Unity 4:
- **spring** = 4 × unity4spring
- **damper** = 15 × unity4damper

## Features Summary

- GTA-style physics configured for gameplay
- Complete C# commented source code
- Single VehicleController for core physics + optional add-ons
- Vehicle damage with handling deterioration
- Multiple ground materials with per-material grip, drag, marks, smoke, dust
- Built-in pause without timeScale
- Custom particle shader with configurable shadows
- No GC allocations at runtime
- Extensible via exposed properties and delegates
