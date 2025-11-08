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
    /// Allows to speed up or slow down the simulation as desired.
    /// For example, 2.0 would run simulation at double speed, 0.5 would run it at half speed.
    /// </summary>
    public float SimulationSpeed = 1f;

    /// <summary>
    /// Accumulates real time (scaled by SimulationSpeed) to know when to run ticks.
    /// </summary>
    private float Accumulator = 0f;

    /// <summary>
    /// Keeps track of the last frame's real time (via Time.realtimeSinceStartup).
    /// </summary>
    private float LastFrameTime = 0f;

    /// <summary>
    /// Derived from TPS. This is how many seconds each discrete simulation tick spans.
    /// For example, at 60 TPS, each tick is 1/60 = 0.01666... seconds.
    /// </summary>
    public const float TickDeltaTime = 1f / TPS;

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
    }

    private void Update()
    {
        // Register inputs for this frame
        HandleInputs();

        // Calculate how much real time has passed since the last frame.
        float currentFrameTime = Time.realtimeSinceStartup;
        float deltaTime = currentFrameTime - LastFrameTime;
        LastFrameTime = currentFrameTime;

        // Scale the real time by SimulationSpeed to allow slow-mo or fast-forward effects.
        deltaTime *= SimulationSpeed;

        // Accumulate time to know how many discrete ticks we need to simulate.
        Accumulator += deltaTime;

        // Run as many "ticks" as needed to catch up with real time. Multiple ticks may be run in a single frame.
        while (Accumulator >= TickDeltaTime)
        {
            Tick();
            Accumulator -= TickDeltaTime;
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
