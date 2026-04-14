# GugugagaPenguin Progress Notes

注意！请使用Assets/SampleScene这个scene为基础scene 和 Prefabs/FinalFlyingPenguin这个prefab，不然可能会出现leap motion不对的问题。

如果连不上leapmotion且确定驱动已经安装，请调整Service Provider (Desktop)这个game object里面 Advanced子菜单的Service Connection input, IP和Port到正确的版本。macOS请使用IP_Port, 127.0.0.1, 12345.


## Done

### 1. Stopped tracking local Plastic/VS config files

- `.plastic` and `.vsconfig` were removed from Git tracking.
- `.gitignore` includes ignore rules for `.plastic` and `.vsconfig`.
- That change was committed and pushed earlier as commit `2be7b88`.

### 2. Refactored wing flap logic out of direct keyboard input

- `Assets/Scripts/FlyingPenguinController.cs`
  - No longer reads keyboard input directly.
  - Now exposes flap action methods:
    - `FlapLeft(float upwardStrength = 1f, float sidewaysStrength = 1f)`
    - `FlapRight(float upwardStrength = 1f, float sidewaysStrength = 1f)`
  - Default force behavior remains the same as before:
    - `upwardForce = 10f`
    - `sidewaysForce = 5f`

- `Assets/Scripts/KeyboardWingInputController.cs`
  - Added as a temporary input source for testing.
  - Keeps current controls unchanged:
    - `A` triggers left flap
    - `L` triggers right flap

### 3. Added initial Ultraleap input integration

- `Assets/Scripts/LeapWingInputController.cs`
  - Added to read hand data from Ultraleap `LeapProvider`.
  - Detects left and right palm downward motion independently.
  - Uses downward palm velocity to trigger flaps.
  - Maps velocity to flap strength.
  - Includes:
    - confidence threshold
    - optional palm orientation gate
    - cooldown
    - re-arm threshold
    - configurable strength curve

- `Assets/Editor/Coplay/SetupFlyingPenguin.cs`
  - Updated so auto-setup can also add:
    - `KeyboardWingInputController`
    - `LeapWingInputController`

### 4. Package status

- Ultraleap packages are already present in the project.
- Current `Packages/manifest.json` already includes:
  - `com.ultraleap.tracking`
  - `com.ultraleap.tracking.preview`
- No new package installation was required for today’s work.

## Not Done Yet

### Real device testing has not been done (Finished)

- Not yet verified with actual Leap/Ultraleap hardware input.
- Not yet verified that hand tracking data is reaching `LeapWingInputController`.
- Not yet tuned for real-world gesture thresholds.

### Gesture quality tuning is still pending (Finished)

- Current flap detection is a first-pass implementation.
- Likely needs tuning for:
  - `flapStartDownwardSpeed`
  - `flapResetDownwardSpeed`
  - `flapCooldown`
  - `minConfidence`
  - `maxTrackedDownwardSpeed`
  - `speedToStrengthCurve`

### Animation intensity is not yet linked to flap strength (Do we need to do this?)

- Right now strength changes the physics impulse.
- Animator trigger flow is unchanged.
- If needed later, add an Animator float parameter to make stronger flaps also look stronger/faster.

### Keyboard fallback policy (Finished)

- Keyboard input is still kept for testing.
- Later we should decide whether to:
  - keep both keyboard and Leap enabled
  - disable keyboard when Leap is active
  - switch input sources with a toggle
