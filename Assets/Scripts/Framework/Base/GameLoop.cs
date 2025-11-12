using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core logic of the tick-based game loop. Provides a fixed tick-interval for the simulation and a render interval (which is every frame). 
/// <br/> This is intended to be the only MonoBehaviour used, so that the rest of the game systems can remain independent of Unity-specific classes.
/// </summary>
public abstract class GameLoop : MonoBehaviour
{
    // ------------------------------------------------------------
    // Constants & Fields
    // ------------------------------------------------------------

    /// <summary>
    /// The target Ticks Per Second (TPS). 60 ticks per second = 16.67 ms per tick.
    /// </summary>
    public const float TPS = 60f;

    /// <summary>
    /// Seconds per tick.
    /// </summary>
    public const float TickDeltaTime = 1f / TPS;

    /// <summary>
    /// Maximum number of real time delta to be accumulated in a single frame to avoid massive spikes. 
    /// </summary>
    public const float MaxDeltaTime = 0.25f;

    /// <summary>
    /// The maximum nuber of ticks that can be processed in a single frame.
    /// </summary>
    public const int MaxTicksPerFrame = 20;

    /// <summary>
    /// Maximum amount of real-time allowed to be in the accumulator before the real-time gets shed. Used to avoid spiraling tick backlog.
    /// </summary>
    public const float MaxAccumulator = TickDeltaTime * MaxTicksPerFrame * 4f;

    /// <summary>
    /// Allows to speed up or slow down the simulation as desired.
    /// For example, 2.0 would run simulation at double speed, 0.5 would run it at half speed.
    /// </summary>
    public float SimulationSpeed { get; private set; }

    /// <summary>
    /// Accumulates real time (scaled by SimulationSpeed) to know when to run ticks.
    /// </summary>
    private float Accumulator = 0f;

    /// <summary>
    /// Keeps track of the last frame's real time (via Time.realtimeSinceStartup).
    /// </summary>
    private float LastFrameTime = 0f;

    // ------------------------------------------------------------
    // Unity Lifecycle Methods
    // ------------------------------------------------------------

    private void Awake()
    {
        MouseHoverInfo.AwakeReset();

        ResourceManager.ClearCache();
        DefDatabaseRegistry.AddAllDefs();
        DefDatabaseRegistry.ResolveAllReferences();
        DefDatabaseRegistry.OnLoadingDone();

        LastFrameTime = Time.realtimeSinceStartup;
        SimulationSpeed = 1f;
    }

    private void Update()
    {
        // Register inputs for this frame
        HandleInputs();

        // Calculate how much real time has passed since the last frame.
        float currentFrameTime = Time.realtimeSinceStartup;
        float deltaTime = currentFrameTime - LastFrameTime;
        LastFrameTime = currentFrameTime;

        if (deltaTime > MaxDeltaTime) deltaTime = MaxDeltaTime; // Clamp to avoid spikes

        // Scale the real time by SimulationSpeed to allow slow-mo or fast-forward effects.
        deltaTime *= SimulationSpeed;

        // Accumulate time to know how many discrete ticks we need to simulate.
        Accumulator += deltaTime;
        if (Accumulator > MaxAccumulator) Accumulator = MaxAccumulator; // Shed excess acumulated time if too much gets accumulated

        // Run as many "ticks" as needed to catch up with real time. Multiple ticks may be run in a single frame.
        int ticksProcessed = 0;
        while (Accumulator >= TickDeltaTime && ticksProcessed < MaxTicksPerFrame)
        {
            Tick();
            Accumulator -= TickDeltaTime;
            ticksProcessed++;
        }

        // General stuff called every frame
        OnFrame();

        // Render the game, using interpolation alpha (0..1) between last and next tick.
        float alpha = Accumulator / TickDeltaTime;
        Render(alpha);
    }

    // ------------------------------------------------------------
    // Simulation + Rendering
    // ------------------------------------------------------------

    /// <summary>
    /// Called once per frame at the very beginning of the frame.
    /// </summary>
    protected abstract void HandleInputs();

    /// <summary>
    /// Main function to advance the simulation.
    /// </summary>
    protected abstract void Tick();

    /// <summary>
    /// Gets called every frame after handling inputs and tick processing and before rendering.
    /// </summary>
    protected abstract void OnFrame();

    /// <summary>
    /// Called once per frame after everything else has been processed. 'alpha' tells the ratio [0,1] how far between the last tick and the next tick.
    /// Use 'alpha' to interpolate positions, rotations, or animation states for smooth rendering.
    /// </summary>
    protected abstract void Render(float alpha);

    // ------------------------------------------------------------
    // Actions
    // ------------------------------------------------------------

    /// <summary>
    /// Function to set the simulation speed. 0 is paused, 1 is to set the speed as defined in TPS.
    /// </summary>
    protected void SetSimulationSpeed(float newSpeed)
    {
        SimulationSpeed = Mathf.Max(0f, newSpeed);
    }
}
