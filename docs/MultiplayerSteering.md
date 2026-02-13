# Multiplayer Steering System

A system that allows 1-4 players to simultaneously steer the same vehicle using dedicated keyboard keys, with onscreen steering wheel indicators.

## Overview

The multiplayer steering system enables chaotic cooperative (or competitive) vehicle control where multiple players share steering responsibility. Each player has their own analog steering input that gets combined into a single steering value applied to the vehicle.

## Components

### MultiplayerSteeringPlayer

A serializable data class that handles per-player input state and analog ramping.

**Location:** `Assets/_Scripts/MultiplayerSteering/MultiplayerSteeringPlayer.cs`

**Properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `playerIndex` | int | - | Player number (0-3) |
| `leftKey` | KeyCode | varies | Key to steer left |
| `rightKey` | KeyCode | varies | Key to steer right |
| `isEnabled` | bool | varies | Whether this player is active |
| `rampUpSpeed` | float | 3.0 | Speed at which steering ramps up when key is held |
| `rampDownSpeed` | float | 5.0 | Speed at which steering returns to center |
| `currentSteer` | float | 0 | Current analog steering value (-1 to 1) |
| `targetSteer` | float | 0 | Immediate binary input target |

**Analog Ramping:**

The system converts binary keyboard input to smooth analog steering using `Mathf.MoveTowards`:
- **Tap** = small turn (quick press releases before reaching full deflection)
- **Hold** = full turn (ramps to -1 or +1 over time)
- **Release** = smooth return to center (faster than ramp-up for responsive feel)

---

### MultiplayerSteeringManager

The main controller that combines all player inputs and applies them to the vehicle.

**Location:** `Assets/_Scripts/MultiplayerSteering/MultiplayerSteeringManager.cs`

**Inspector Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `vehicle` | VehicleController | Target vehicle (auto-detected if null) |
| `vehicleInput` | VehicleNewInput | Input component to override (auto-detected if null) |
| `combineMode` | CombineMode | How to combine multiple inputs |
| `player1-4ToggleKey` | KeyCode | Keys to toggle each player (default: 1-4) |
| `players` | MultiplayerSteeringPlayer[] | Array of 4 player configurations |

**Combine Modes:**

| Mode | Behavior | Use Case |
|------|----------|----------|
| **Average** | Sum / enabled player count | Balanced, predictable steering |
| **Sum** | All inputs added, clamped to [-1, 1] | Chaotic, encourages cooperation/competition |

**Default Key Bindings:**

| Player | Left Key | Right Key | Default State |
|--------|----------|-----------|---------------|
| P1 | A | D | Enabled |
| P2 | LeftArrow | RightArrow | Disabled |
| P3 | J | K | Disabled |
| P4 | O | P | Disabled |

**Runtime Toggle:**
Press number keys 1-4 to toggle each player on/off during gameplay.

---

### MultiplayerSteeringUI

OnGUI-based display showing steering wheel indicators for each player.

**Location:** `Assets/_Scripts/MultiplayerSteering/MultiplayerSteeringUI.cs`

**Inspector Properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `steeringManager` | MultiplayerSteeringManager | - | Manager reference (auto-detected) |
| `show` | bool | true | Whether UI is visible |
| `toggleKey` | KeyCode | U | Key to toggle visibility |
| `wheelSize` | float | 80 | Size of each wheel indicator |
| `wheelSpacing` | float | 20 | Gap between wheels |
| `bottomMargin` | float | 30 | Distance from screen bottom |
| `maxRotationAngle` | float | 90 | Maximum wheel rotation in degrees |
| `player1-4Color` | Color | R/B/G/Y | Color for each player's wheel |
| `disabledColor` | Color | gray | Color for disabled players |

**UI Elements:**
- Horizontal row of 4 steering wheel indicators at screen bottom
- Each wheel rotates based on player's `currentSteer` value
- Player labels show "P1 [ON]" or "P2 [OFF]" status
- Combined steering indicator bar at panel bottom
- Title text: "Multiplayer Steering (Press 1-4 to toggle)"

---

## Setup Instructions

1. **Add Components to Vehicle:**
   - Select your vehicle GameObject in the Hierarchy
   - Add Component > `MultiplayerSteeringManager`
   - Add Component > `MultiplayerSteeringUI`

2. **Configure (Optional):**
   - Adjust key bindings in the Inspector
   - Choose combine mode (Average or Sum)
   - Customize UI colors and sizing

3. **Play:**
   - P1 starts enabled with A/D keys
   - Press 2, 3, or 4 to enable additional players
   - Press U to toggle the UI display

---

## How It Works

```
┌─────────────────────────────────────────────────────────────┐
│                    Update() Loop                            │
├─────────────────────────────────────────────────────────────┤
│  1. Handle player toggle keys (1-4)                         │
│  2. Each player reads keyboard input → targetSteer          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  FixedUpdate() Loop                         │
├─────────────────────────────────────────────────────────────┤
│  1. Each player: MoveTowards(currentSteer, targetSteer)     │
│  2. Combine all enabled players' currentSteer values        │
│  3. Apply combined value to VehicleController.steerInput    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     OnGUI() Loop                            │
├─────────────────────────────────────────────────────────────┤
│  1. Draw background panel                                   │
│  2. Draw each player's rotated steering wheel               │
│  3. Draw combined steering indicator                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Integration with VehicleNewInput

The `MultiplayerSteeringManager` sets `VehicleNewInput.externalSteeringOverride = true` when enabled. This prevents the normal input system from overwriting the multiplayer steering value.

When `MultiplayerSteeringManager` is disabled, it automatically clears the override flag, restoring normal single-player steering.

---

## API Reference

### MultiplayerSteeringManager

```csharp
// Get combined steering value
float steer = steeringManager.CombinedSteer;

// Get number of active players
int count = steeringManager.GetEnabledPlayerCount();

// Enable/disable a player programmatically
steeringManager.SetPlayerEnabled(1, true);  // Enable player 2
```

### MultiplayerSteeringPlayer

```csharp
// Toggle player on/off
player.Toggle();

// Reset steering to center
player.Reset();

// Check if steering left or right
bool isSteeringLeft = player.currentSteer < -0.1f;
```

---

## Customization Ideas

- **Different vehicle actions:** Extend the system to share throttle/brake control
- **Weighted players:** Give certain players more steering influence
- **Input deadzone:** Add minimum threshold before steering registers
- **Network multiplayer:** Replace keyboard input with networked player inputs
