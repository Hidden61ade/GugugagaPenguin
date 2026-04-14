# Leap Motion Overlay Proposal

## Goal

In the current Unity project, add a non-invasive debug overlay in the bottom-left corner that shows:

1. A Leap Motion / Ultraleap device image preview
2. A hand tracking preview

This should be implemented without changing the main gameplay flow in `Assets/SampleScene`.

## Current Project State

The project already contains the core pieces needed for this:

- `Service Provider (Desktop)` is present in `Assets/SampleScene`
- `LeapWingInputController` already reads hand tracking data from `LeapProvider`
- Ultraleap packages are already installed
- The SDK includes:
  - `LeapImageRetriever` for device image access
  - `LeapDistortImage` for corrected image rendering

This means the work is an integration task, not a new SDK setup.

## Proposed Implementation

### 1. Bottom-left debug overlay container

Add a lightweight screen-space overlay anchored to the bottom-left corner.

The overlay should be isolated from gameplay UI and easy to disable later.

### 2. Hand tracking preview

Render Ultraleap hand models into a dedicated preview camera and display them in the overlay via a `RenderTexture`.

Preferred approach:

- Use a separate preview rig
- Reuse Ultraleap hand model prefabs already available in the project or samples
- Keep this preview independent from the main game camera

### 3. Leap image preview

Use `LeapImageRetriever` to acquire the device image stream and display it in the overlay.

Two possible display modes:

- Raw infrared image preview
- Distortion-corrected preview using `LeapDistortImage`

Recommended first pass:

- Start with the simplest stable preview path
- If image quality or readability is poor, switch to corrected rendering

### 4. Combined layout

Support one of these overlay layouts:

- Single combined panel with image and hand preview stacked
- Two adjacent mini-panels in the lower-left corner

Recommended first pass:

- Two stacked mini-panels, because it is simpler to tune and debug

## Constraints And Risks

### 1. Image access depends on Ultraleap settings

The device image preview requires Ultraleap image streaming to be enabled.

Specifically:

- `Allow Images` must be enabled in Ultraleap settings

Without that, hand tracking can still work while the image panel remains empty.

### 2. Device image is not a normal RGB webcam feed

The preview is typically the Ultraleap infrared camera image, not a conventional color camera view.

That affects how polished or intuitive the overlay will look.

### 3. Scene stability

The overlay should not interfere with:

- Existing penguin controls
- Current Leap input flow
- Main camera rendering

To keep risk low, the overlay should remain purely observational.

## Recommended Delivery Order

### Phase 1

- Add bottom-left overlay container
- Add hand tracking preview only
- Verify it works with the current `Service Provider (Desktop)`

### Phase 2

- Add Leap image preview through `LeapImageRetriever`
- Verify image stream availability on the target machine

### Phase 3

- Improve presentation
- Tune panel sizing, materials, and image correction if needed
- Add a simple on/off toggle if useful

## Acceptance Criteria

The proposal is considered implemented successfully when:

1. Running `Assets/SampleScene` shows a bottom-left overlay
2. The overlay displays live hand tracking preview
3. The overlay displays Leap device imagery when image streaming is enabled
4. Main gameplay behavior remains unchanged

## Recommendation

This is feasible in the current project.

The lowest-risk path is to build the hand preview first, then connect the device image stream as a second step.
