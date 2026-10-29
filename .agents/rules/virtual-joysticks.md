# Virtual Joystick and Mobile Touch Input Guidelines

This rule establishes technical standards for integrating on-screen touch joysticks using the **Virtual Joystick Pack** (`Terresquall`).

## Core Directives

1. **Deadzone & Jitter Prevention**:
   - Always set `Deadzone` to at least `0.1f` to prevent micro-tremors from resting thumbs from drifting the player character.
   - Clamp output magnitudes to `1.0f` to prevent diagonal speed boosting.

2. **Canvas & Resolution Scaler**:
   - Virtual joysticks must be parented under a Canvas set to `Screen Space - Overlay` with `Canvas Scaler: Scale With Screen Size` (reference resolution e.g. 1920x1080) to maintain uniform physical touch radius across various mobile screen densities.

3. **Multi-Touch Finger Separation**:
   - Ensure the joystick handle component consumes its specific `PointerEventData` touch ID so that simultaneous camera panning or button tapping with other fingers does not steal the joystick focus.
