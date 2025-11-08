using UnityEngine;

public class RaceSimulation : GameLoop
{
    private Map Map;

    private const int TEST_RACE_LINE_FEATURE_ID = 7;

    private void Start()
    {
        DefDatabaseRegistry.AddAllDefs();
        Map = Map.LoadMap("racingworld");

        Map.SetDisplayToViewMode();


    }

    #region Loop

    protected override void HandleInputs() { }

    protected override void OnFrame() { }

    protected override void Render(float alpha)
    {
        Map.Render();
    }


    protected override void Tick() { }

    #endregion
}
