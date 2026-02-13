# Vehicle Input System

Documentation for the vehicle input system in ClownCar, including the base input handler and external override capabilities.

## Overview

The vehicle input system is built on Unity's new Input System and provides:
- Multiple simultaneous input methods (keyboard, gamepad)
- Multiple key bindings per action
- Configurable input combining modes
- External override support for multiplayer steering

## Components

### VehicleNewInput

The primary input handler that reads player input and applies it to the vehicle.

**Location:** `Assets/_Scripts/Input/VehicleNewInput.cs`

**Namespace:** `EVP`

---

## Inspector Properties

### Target Vehicle

| Property | Type | Description |
|----------|------|-------------|
| `target` | VehicleController | The vehicle to control (auto-detected if null) |

### Input Asset

| Property | Type | Description |
|----------|------|-------------|
| `inputActions` | InputActionAsset | Unity Input System asset (VehicleInputActions) |

### Behavior Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `continuousForwardAndReverse` | bool | true | Auto-switch between forward/reverse based on velocity |
| `handbrakeOverridesThrottle` | bool | false | Reduce throttle when handbrake is held |

### Input Combining

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `combineMode` | InputCombineMode | TakeHighestMagnitude | How to combine multiple inputs |

**Combine Modes:**

| Mode | Behavior |
|------|----------|
| `TakeHighestMagnitude` | Use whichever input has the largest absolute value |
| `Sum` | Add all inputs together (clamped to -1, 1) |
| `Average` | Average all active inputs |

### External Override

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `externalSteeringOverride` | bool | false | When true, steering is controlled externally |

---

## External Steering Override

The `externalSteeringOverride` flag allows external systems (like multiplayer steering) to take control of the vehicle's steering input.

### Behavior

When `externalSteeringOverride = true`:
- `VehicleNewInput` **skips** setting `VehicleController.steerInput`
- Throttle, brake, and handbrake inputs continue to work normally
- External systems can set `steerInput` directly on the `VehicleController`

When `externalSteeringOverride = false`:
- Normal input processing resumes
- `steerInput` is set from keyboard/gamepad input

### Usage Example

```csharp
// Take control of steering
vehicleNewInput.externalSteeringOverride = true;
vehicleController.steerInput = myCustomSteeringValue;

// Return control to normal input
vehicleNewInput.externalSteeringOverride = false;
```

### Integration with MultiplayerSteeringManager

The `MultiplayerSteeringManager` automatically manages this flag:
- Sets `externalSteeringOverride = true` in `OnEnable()`
- Sets `externalSteeringOverride = false` in `OnDisable()`

---

## Input Actions

The system expects an InputActionAsset with a "Vehicle" action map containing:

| Action | Type | Description |
|--------|------|-------------|
| `Steer` | Axis | Left/right steering (-1 to 1) |
| `Throttle` | Axis | Forward/reverse throttle |
| `Brake` | Button/Axis | Brake input |
| `Handbrake` | Button | Handbrake/parking brake |
| `ResetVehicle` | Button | Reset vehicle to upright position |
| `ReverseModifier` | Button | Manual reverse mode toggle |

---

## Public API

### Properties (Read-Only)

```csharp
float steer = vehicleInput.SteerInput;      // Current steer value
float throttle = vehicleInput.ThrottleInput; // Current throttle
float brake = vehicleInput.BrakeInput;       // Current brake
float handbrake = vehicleInput.HandbrakeInput; // Current handbrake
```

### Methods

```csharp
// Add custom steering binding at runtime
vehicleInput.AddSteerBinding("<Keyboard>/q", "<Keyboard>/e");

// Add custom throttle binding
vehicleInput.AddThrottleBinding("<Keyboard>/i", "<Keyboard>/k");

// Add handbrake binding
vehicleInput.AddHandbrakeBinding("<Keyboard>/space");

// Add reset binding
vehicleInput.AddResetBinding("<Keyboard>/r");
```

---

## Input Processing Flow

```
┌─────────────────────────────────────────────────────────────┐
│                       Update()                              │
├─────────────────────────────────────────────────────────────┤
│  ReadInputs()                                               │
│  ├── Read steer axis (with combine mode)                    │
│  ├── Read throttle axis                                     │
│  ├── Read brake axis                                        │
│  ├── Read handbrake                                         │
│  └── Read reverse modifier                                  │
│                                                             │
│  TranslateToVehicleInput()                                  │
│  └── Convert forward/reverse to throttle/brake              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     FixedUpdate()                           │
├─────────────────────────────────────────────────────────────┤
│  if (!externalSteeringOverride)                             │
│      target.steerInput = steerInput    ◄── Skip if override │
│                                                             │
│  target.throttleInput = throttleInput                       │
│  target.brakeInput = brakeInput                             │
│  target.handbrakeInput = handbrakeInput                     │
│                                                             │
│  if (doReset) target.ResetVehicle()                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Continuous Forward and Reverse Mode

When `continuousForwardAndReverse = true`:

| Vehicle State | Forward Input | Reverse Input | Result |
|---------------|---------------|---------------|--------|
| Moving forward | Throttle | Brake | Accelerate / Brake |
| Stationary | Throttle | Reverse | Move forward / backward |
| Moving backward | Brake | Throttle | Brake / Accelerate backward |

When `continuousForwardAndReverse = false`:
- Use `ReverseModifier` key to toggle between forward and reverse modes
- More like a traditional manual transmission

---

## VehicleController Integration

The input system sets these properties on `VehicleController`:

| Property | Range | Description |
|----------|-------|-------------|
| `steerInput` | -1 to 1 | Steering angle (left/right) |
| `throttleInput` | -1 to 1 | Throttle (negative = reverse) |
| `brakeInput` | 0 to 1 | Brake force |
| `handbrakeInput` | 0 to 1 | Handbrake force |

---

## Extending the System

### Adding New Override Types

To add a new type of external override (e.g., AI control):

```csharp
public class AIVehicleController : MonoBehaviour
{
    public VehicleNewInput vehicleInput;
    public VehicleController vehicle;

    void OnEnable()
    {
        vehicleInput.externalSteeringOverride = true;
    }

    void OnDisable()
    {
        vehicleInput.externalSteeringOverride = false;
    }

    void FixedUpdate()
    {
        vehicle.steerInput = CalculateAISteering();
    }
}
```

### Future Considerations

The override system could be extended to include:
- `externalThrottleOverride` for throttle control
- `externalBrakeOverride` for brake control
- A combined `externalControlOverride` flag for full AI takeover
