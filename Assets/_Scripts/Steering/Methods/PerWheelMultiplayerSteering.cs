using UnityEngine;
using UnityEngine.InputSystem;

namespace EVP
{
    /// <summary>
    /// Per-wheel multiplayer steering: each player controls one wheel's steering angle
    /// and drive force independently. Bypasses EVP's global steer/drive by disabling
    /// per-wheel flags and writing directly to WheelColliders + AddForceAtPosition.
    /// </summary>
    public class PerWheelMultiplayerSteering : SteeringMethod
    {
        private readonly PerWheelSteeringConfig config;

        // Per-player data
        private PerWheelPlayerData[] players;

        // Saved EVP wheel flags for restore on deactivate
        private bool[] originalSteerFlags;
        private bool[] originalDriveFlags;

        // Per-wheel runtime state
        private float[] wheelSteerAngles;

        // Resolved max steer angle for front wheels
        private float frontMaxSteerAngle;

        // UI texture
        private Texture2D uiTexture;

        public PerWheelMultiplayerSteering(PerWheelSteeringConfig config)
        {
            this.config = config;
        }

        public override void Initialize(VehicleMultiplayerSteering manager, VehicleController vehicle)
        {
            base.Initialize(manager, vehicle);

            players = new PerWheelPlayerData[4];
            for (int i = 0; i < 4; i++)
            {
                players[i] = new PerWheelPlayerData
                {
                    isEnabled = manager.PlayerEnabled[i],
                    wheelIndex = -1,
                    inputType = config.GetDefaultInputType(i)
                };
            }

            int wheelCount = vehicle.wheels.Length;
            originalSteerFlags = new bool[wheelCount];
            originalDriveFlags = new bool[wheelCount];
            wheelSteerAngles = new float[wheelCount];
        }

        public override void Activate()
        {
            // Resolve front max steer angle
            frontMaxSteerAngle = config.useVehicleSteerAngle ? vehicle.maxSteerAngle : config.maxSteerAngle;

            // Save original flags and disable EVP steer/drive
            for (int i = 0; i < vehicle.wheels.Length; i++)
            {
                originalSteerFlags[i] = vehicle.wheels[i].steer;
                originalDriveFlags[i] = vehicle.wheels[i].drive;
                vehicle.wheels[i].steer = false;
                vehicle.wheels[i].drive = false;
                // Keep brake = true so EVP braking still works
            }

            // Reset wheel steer angles
            for (int i = 0; i < wheelSteerAngles.Length; i++)
                wheelSteerAngles[i] = 0f;

            // Reset player inputs
            foreach (var p in players)
                p.inputPosition = Vector2.zero;

            // Assign wheels to players
            AssignWheels();

            // Create UI texture
            if (uiTexture == null)
            {
                uiTexture = new Texture2D(1, 1);
                uiTexture.SetPixel(0, 0, Color.white);
                uiTexture.Apply();
            }
        }

        public override void Deactivate()
        {
            // Restore original EVP wheel flags
            for (int i = 0; i < vehicle.wheels.Length; i++)
            {
                vehicle.wheels[i].steer = originalSteerFlags[i];
                vehicle.wheels[i].drive = originalDriveFlags[i];
            }

            // Zero steer angles on both WheelColliders and EVP visual data
            var wd = vehicle.wheelData;
            if (wd != null)
            {
                for (int i = 0; i < wd.Length; i++)
                {
                    wd[i].collider.steerAngle = 0f;
                    wd[i].steerAngle = 0f;
                }
            }

            if (uiTexture != null)
            {
                Object.Destroy(uiTexture);
                uiTexture = null;
            }
        }

        public override void OnPlayersChanged()
        {
            SyncPlayerEnabled();
            AssignWheels();
        }

        public override void ReadInput(float deltaTime)
        {
            SyncPlayerEnabled();

            for (int i = 0; i < 4; i++)
            {
                var player = players[i];
                if (!player.isEnabled) continue;

                switch (player.inputType)
                {
                    case LeanInputType.WASD:
                        UpdateKeyboardInput(player, KeyCode.A, KeyCode.D, KeyCode.S, KeyCode.W, deltaTime);
                        break;
                    case LeanInputType.ArrowKeys:
                        UpdateKeyboardInput(player, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.UpArrow, deltaTime);
                        break;
                    case LeanInputType.Gamepad1:
                        UpdateGamepadInput(player, 0);
                        break;
                    case LeanInputType.Gamepad2:
                        UpdateGamepadInput(player, 1);
                        break;
                    case LeanInputType.Gamepad3:
                        UpdateGamepadInput(player, 2);
                        break;
                    case LeanInputType.Gamepad4:
                        UpdateGamepadInput(player, 3);
                        break;
                    case LeanInputType.Mouse:
                        UpdateMouseInput(player);
                        break;
                }
            }
        }

        public override VehicleInput GetVehicleInput(float fixedDeltaTime)
        {
            // We handle steer/throttle per-wheel in ApplyPhysics, so return zero for EVP's global inputs.
            // Compute brake from players' negative vertical input when moving forward.
            float maxBrake = 0f;
            float forwardSpeed = Vector3.Dot(vehicle.cachedRigidbody.linearVelocity, vehicle.transform.forward);

            for (int i = 0; i < 4; i++)
            {
                if (!players[i].isEnabled) continue;
                float y = players[i].inputPosition.y;
                // Negative Y while moving forward = braking
                if (y < 0f && forwardSpeed > 1f)
                    maxBrake = Mathf.Max(maxBrake, -y);
            }

            return new VehicleInput
            {
                steer = 0f,
                throttle = 0f,
                brake = maxBrake
            };
        }

        public override void ApplyPhysics(Rigidbody rb, float fixedDeltaTime)
        {
            if (rb == null) return;

            var wd = vehicle.wheelData;
            if (wd == null) return;

            float speed = vehicle.speed;
            float speedRatio = config.maxSpeed > 0f ? Mathf.Clamp01(Mathf.Abs(speed) / config.maxSpeed) : 0f;
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, vehicle.transform.forward);

            // Process each wheel
            for (int wi = 0; wi < wd.Length; wi++)
            {
                // Find which player (if any) controls this wheel
                int ownerPlayer = -1;
                for (int pi = 0; pi < 4; pi++)
                {
                    if (players[pi].isEnabled && players[pi].wheelIndex == wi)
                    {
                        ownerPlayer = pi;
                        break;
                    }
                }

                float maxAngle = IsRearWheel(wi) ? config.rearMaxSteerAngle : frontMaxSteerAngle;

                if (ownerPlayer >= 0)
                {
                    var player = players[ownerPlayer];

                    // 1. Steer angle
                    float targetAngle = player.inputPosition.x * maxAngle;
                    wheelSteerAngles[wi] = targetAngle;
                    wd[wi].collider.steerAngle = targetAngle;
                    wd[wi].steerAngle = targetAngle; // EVP reads this for visual wheel rotation

                    // 2. Drive force
                    float inputY = player.inputPosition.y;
                    if (Mathf.Abs(inputY) > 0.01f)
                    {
                        float forceMag = Mathf.Abs(inputY) * config.maxDriveForce;
                        forceMag *= (1f - config.forceFalloffWithSpeed * speedRatio);

                        // Determine direction: positive Y = forward, negative Y = reverse (when slow) or brake (when fast)
                        Vector3 forceDir;
                        if (inputY > 0f)
                        {
                            // Forward drive
                            forceDir = GetWheelForward(wd[wi]) * forceMag;
                        }
                        else
                        {
                            // Reverse only when slow; when moving forward fast, braking is handled by GetVehicleInput
                            if (forwardSpeed < 2f)
                                forceDir = GetWheelForward(wd[wi]) * -forceMag;
                            else
                                forceDir = Vector3.zero; // braking handled by EVP
                        }

                        if (forceDir.sqrMagnitude > 0.001f)
                        {
                            Vector3 forcePoint = wd[wi].grounded ? wd[wi].hit.point : wd[wi].collider.transform.position;
                            rb.AddForceAtPosition(forceDir, forcePoint);
                        }
                    }
                }
                else
                {
                    // Uncontrolled wheel: gradually center steering
                    if (config.centerUncontrolledWheels)
                    {
                        wheelSteerAngles[wi] = Mathf.MoveTowards(wheelSteerAngles[wi], 0f, config.centeringSpeed * fixedDeltaTime);
                        wd[wi].collider.steerAngle = wheelSteerAngles[wi];
                        wd[wi].steerAngle = wheelSteerAngles[wi];
                    }
                    // No drive force for uncontrolled wheels
                }
            }
        }

        public override void LateUpdate()
        {
            // EVP's Update() calls UpdateSteering which resets wd.steerAngle to 0
            // when wheel.steer is false, then UpdateTransform uses that for visuals.
            // We re-apply our steer angles here after EVP is done.
            var wd = vehicle.wheelData;
            if (wd == null) return;

            for (int wi = 0; wi < wd.Length && wi < wheelSteerAngles.Length; wi++)
            {
                wd[wi].steerAngle = wheelSteerAngles[wi];
                wd[wi].collider.steerAngle = wheelSteerAngles[wi];

                // Re-apply visual transform rotation with our steer angle
                if (wd[wi].wheel.caliperTransform != null)
                {
                    wd[wi].wheel.caliperTransform.rotation = wd[wi].transform.rotation
                        * Quaternion.Euler(0f, wheelSteerAngles[wi], 0f);
                }

                if (wd[wi].wheel.wheelTransform != null && !wd[wi].isWheelChildOfCaliper)
                {
                    wd[wi].wheel.wheelTransform.rotation = wd[wi].transform.rotation
                        * Quaternion.Euler(wd[wi].angularPosition * Mathf.Rad2Deg, wheelSteerAngles[wi], 0f);
                }
            }
        }

        // --- Helpers ---

        private Vector3 GetWheelForward(WheelData wd)
        {
            // Wheel forward direction accounting for steer angle
            return Quaternion.AngleAxis(wd.collider.steerAngle, vehicle.transform.up) * vehicle.transform.forward;
        }

        private bool IsRearWheel(int wheelIndex)
        {
            // Convention: first half of wheels are front, second half are rear
            return wheelIndex >= vehicle.wheels.Length / 2;
        }

        private void AssignWheels()
        {
            // Fixed mapping: P1→wheel[0], P2→wheel[1], P3→wheel[2], P4→wheel[3]
            int wheelCount = vehicle.wheels.Length;
            for (int i = 0; i < 4; i++)
            {
                players[i].wheelIndex = (i < wheelCount && players[i].isEnabled) ? i : -1;
            }
        }

        private void SyncPlayerEnabled()
        {
            for (int i = 0; i < 4; i++)
                players[i].isEnabled = manager.PlayerEnabled[i];
        }

        // --- Input Methods ---

        private void UpdateKeyboardInput(PerWheelPlayerData player, KeyCode left, KeyCode right, KeyCode down, KeyCode up, float dt)
        {
            float hInput = 0f;
            float vInput = 0f;

            if (Input.GetKey(left)) hInput -= 1f;
            if (Input.GetKey(right)) hInput += 1f;
            if (Input.GetKey(down)) vInput -= 1f;
            if (Input.GetKey(up)) vInput += 1f;

            bool hasH = Mathf.Abs(hInput) > 0.01f;
            bool hasV = Mathf.Abs(vInput) > 0.01f;

            if (hasH)
                player.inputPosition.x = Mathf.MoveTowards(player.inputPosition.x, hInput, config.keyboardMoveSpeed * dt);
            else if (config.inputReturnToCenter)
                player.inputPosition.x = Mathf.MoveTowards(player.inputPosition.x, 0f, config.keyboardReturnSpeed * dt);

            if (hasV)
                player.inputPosition.y = Mathf.MoveTowards(player.inputPosition.y, vInput, config.keyboardMoveSpeed * dt);
            else if (config.inputReturnToCenter)
                player.inputPosition.y = Mathf.MoveTowards(player.inputPosition.y, 0f, config.keyboardReturnSpeed * dt);

            player.inputPosition.x = Mathf.Clamp(player.inputPosition.x, -1f, 1f);
            player.inputPosition.y = Mathf.Clamp(player.inputPosition.y, -1f, 1f);
        }

        private void UpdateGamepadInput(PerWheelPlayerData player, int gamepadIndex)
        {
            var gamepads = Gamepad.all;
            if (gamepadIndex >= gamepads.Count)
            {
                player.inputPosition = Vector2.zero;
                return;
            }

            Vector2 stick = gamepads[gamepadIndex].leftStick.ReadValue();
            bool hasInput = stick.sqrMagnitude > 0.04f; // ~0.2 deadzone
            if (hasInput || config.inputReturnToCenter)
            {
                player.inputPosition.x = Mathf.Clamp(stick.x, -1f, 1f);
                player.inputPosition.y = Mathf.Clamp(stick.y, -1f, 1f);
            }
        }

        private void UpdateMouseInput(PerWheelPlayerData player)
        {
            // Mouse: horizontal = steer (-1 to 1 mapped from screen), vertical = throttle
            float normalizedX = (Input.mousePosition.x / Screen.width) * 2f - 1f;
            float normalizedY = (Input.mousePosition.y / Screen.height) * 2f - 1f;
            player.inputPosition.x = Mathf.Clamp(normalizedX, -1f, 1f);
            player.inputPosition.y = Mathf.Clamp(normalizedY, -1f, 1f);
        }

        // --- GUI ---

        public override void DrawGUI()
        {
            if (uiTexture == null || vehicle.wheelData == null) return;

            // Draw a top-down vehicle silhouette with 4 wheel indicators
            float panelWidth = 160f;
            float panelHeight = 200f;
            float margin = 20f;
            float panelX = (Screen.width - panelWidth) / 2f;
            float panelY = Screen.height - margin - panelHeight;

            // Panel background
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
            GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), uiTexture);

            // Vehicle body silhouette (centered rectangle)
            float bodyW = 60f;
            float bodyH = 120f;
            float bodyX = panelX + (panelWidth - bodyW) / 2f;
            float bodyY = panelY + (panelHeight - bodyH) / 2f;

            GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            GUI.DrawTexture(new Rect(bodyX, bodyY, bodyW, bodyH), uiTexture);

            // Wheel positions relative to body (top-down: up = forward)
            // FL=0, FR=1, RL=2, RR=3
            float wheelW = 20f;
            float wheelH = 36f;
            float[][] wheelOffsets = new float[][]
            {
                new float[] { bodyX - wheelW - 4f, bodyY + 10f },           // FL - left front
                new float[] { bodyX + bodyW + 4f, bodyY + 10f },            // FR - right front
                new float[] { bodyX - wheelW - 4f, bodyY + bodyH - wheelH - 10f }, // RL - left rear
                new float[] { bodyX + bodyW + 4f, bodyY + bodyH - wheelH - 10f },  // RR - right rear
            };

            int wheelCount = Mathf.Min(vehicle.wheelData.Length, 4);
            for (int wi = 0; wi < wheelCount; wi++)
            {
                float wx = wheelOffsets[wi][0];
                float wy = wheelOffsets[wi][1];

                // Find owner
                int owner = -1;
                for (int pi = 0; pi < 4; pi++)
                {
                    if (players[pi].isEnabled && players[pi].wheelIndex == wi)
                    {
                        owner = pi;
                        break;
                    }
                }

                // Wheel rectangle color
                Color wheelColor = owner >= 0 ? config.GetPlayerColor(owner) : config.uncontrolledColor;
                GUI.color = wheelColor;
                GUI.DrawTexture(new Rect(wx, wy, wheelW, wheelH), uiTexture);

                if (owner >= 0)
                {
                    var player = players[owner];
                    float maxAngle = IsRearWheel(wi) ? config.rearMaxSteerAngle : frontMaxSteerAngle;

                    // Steer indicator: vertical line offset by steer angle
                    float steerNorm = player.inputPosition.x; // -1 to 1
                    float lineX = wx + wheelW / 2f + steerNorm * (wheelW / 2f - 2f);
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(lineX - 1f, wy + 2f, 2f, wheelH - 4f), uiTexture);

                    // Drive bar: green up (throttle) or red down (brake/reverse)
                    float driveY = player.inputPosition.y; // -1 to 1
                    float barMaxH = wheelH / 2f - 4f;
                    float barW = 4f;
                    float barX = wx + wheelW + 2f;
                    float barCenterY = wy + wheelH / 2f;

                    if (driveY > 0.01f)
                    {
                        float barH = driveY * barMaxH;
                        GUI.color = new Color(0.2f, 0.9f, 0.2f, 0.9f);
                        GUI.DrawTexture(new Rect(barX, barCenterY - barH, barW, barH), uiTexture);
                    }
                    else if (driveY < -0.01f)
                    {
                        float barH = -driveY * barMaxH;
                        GUI.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);
                        GUI.DrawTexture(new Rect(barX, barCenterY, barW, barH), uiTexture);
                    }

                    // Player label
                    GUI.color = Color.white;
                    GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    labelStyle.fontSize = 10;
                    labelStyle.normal.textColor = wheelColor;
                    GUI.Label(new Rect(wx - 4f, wy + wheelH, wheelW + 8f, 16f), $"P{owner + 1}", labelStyle);
                }
            }

            // Mode label at top of panel
            GUI.color = Color.white;
            GUIStyle modeStyle = new GUIStyle(GUI.skin.label);
            modeStyle.alignment = TextAnchor.MiddleCenter;
            modeStyle.fontSize = 11;
            modeStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(panelX, panelY - 18f, panelWidth, 16f), "Per-Wheel", modeStyle);

            GUI.color = Color.white;
        }
    }
}
