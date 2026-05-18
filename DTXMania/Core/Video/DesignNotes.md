# New Video Player Architecture Design Notes

## Core Requirements & Invariants
- **Atomic Continuity**: The currently displayed video frame must NEVER mismatch with the reported time or frame number. This is enforced by encapsulating the visual texture and its metadata (Timestamp, Frame Index) together into a single atomic struct/class (`DisplayedFrame`). 
- **Decoupled Responsibilities**:
  - `VideoPlayerController`: Handles time progression, scaling (Playback Speed), state (Pause, Loop), seeking requests, and houses the Inspector UI logic for controlling playback (Browse file, scrub, pause, etc.).
  - `VideoDecoder` (Backend): Purely responsible for interacting with FFmpeg, extracting packets, decoding frames, skipping intermediary frames during seeks, and surfacing the resulting image block to the Controller.
  - `UINewVideoRenderer`: `UIDrawable` that takes a Controller and efficiently renders its `CurrentFrame.Texture`.

## Async Decoding Considerations
- While the initial backend will be synchronous `SoftwareVideoDecoder` for simplicity, the `VideoDecoder` base class/interface must be designed to support threading/hardware queues without refactoring the controller.
- The `VideoDecoder` should provide a non-blocking frame retrieval method (`TryGetNextFrame`) for regular playback, and a blocking/synchronous enforcement method (`GetFrameBlocking`) for atomic seeking/scrubbing to ensure immediate visual updates.

## Structural Components

### 1. `DisplayedFrame` (Struct/Record)
```csharp
public record struct DisplayedFrame(
    BaseTexture Texture, 
    double TimeSeconds, 
    long FrameNumber,
    long TotalFrames,
    double TotalDurationSeconds
);
```
*Locks time and visual representation at an architectural level. Everything the UI needs to display (current time, total time, current frame, total frames) is tied directly to the exact texture currently being rendered.*

### 2. `VideoDecoder` (Abstract Base Class / Interface)
A simplified, state-agnostic interface/base class. Designed to be easily implemented asynchronously later.
- Methods:
  - `bool TryOpen(string path)`: Allocates FFmpeg contexts.
  - `void SeekTo(double targetSeconds)`: Flushes buffers and sets up the demuxer stream to decode the target time.
  - `bool TryGetDecodedFrame(out DecodedFrameData data)`: Non-blocking way to get the next frame (used during active playback). For software decoder, this just runs a small synchronous decode cycle. For threaded, it pops from a queue.
  - `DecodedFrameData GetNextFrameBlocking()`: Blocks until the frame is resolved. Used when seeking/scrubbing so the UI can instantly snap to the target.

### 3. `VideoPlayerController`
Manages the Stopwatch (or scaled time accumulator).
- Properties:
  - `public DisplayedFrame CurrentFrame { get; private set; }`
  - `public bool IsPaused { get; set; }`
  - `public float PlaybackSpeed { get; set; }`
- Methods:
  - `public void Update()`: Advances elapsed time by DeltaTime * PlaybackSpeed. Checks `Decoder.TryGetDecodedFrame()`. Updates `CurrentFrame` atomically from the result.
  - `public void SeekToSeconds(double seconds)`
  - `public void SeekRoundRelative(int frameOffset)`: Computes the precise PTS of `currentFrame + offset` based on the media framerate, calls Decoder `SeekTo` and `GetNextFrameBlocking()`, then updates `CurrentFrame` immediately.
  - `public void DrawInspector()`: 
    - Displays `CurrentFrame.TimeSeconds` / `CurrentFrame.TotalDurationSeconds`
    - Displays `CurrentFrame.FrameNumber` / `CurrentFrame.TotalFrames`
    - "Browse..." button to load a new video path seamlessly.
    - Play/Pause toggle
    - Frame stepping (< Frame / Frame >)
    - Scrubbing slider (tied to SeekToSeconds).

### 4. `UINewVideoRenderer` (extends UIDrawable)
- Must have `[AddChildMenu]` so it can be cleanly constructed from the generic UI.
- Contains a member: `public VideoPlayerController Controller { get; }`
- In `Draw(Matrix4x4)`: Simply calls `Controller.Update()` then draws `Controller.CurrentFrame.Texture`.
- In `DrawInspector()`: Calls `Controller.DrawInspector()`.

## Implementation Strategy
1. Create `DisplayedFrame` struct.
2. Build a highly stripped down `SoftwareVideoDecoder`. Remove the complex queues, custom thread timers, and factory bloat.
3. Build the `VideoPlayerController` using simple update loop mechanics.
4. Implement `UINewVideoRenderer`.
5. Iterate through tests and debug.
