using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceSimulation : GameLoop
{
    private Map Map;
    private int Ticks;

    // private NavigationPath RaceLine;
    private Point RaceStart;
    private Point RaceEnd;

    private List<Racer> Racers;

    private List<Point> NetworkPoints;

    int NumFinishers = 0;

    // UI
    private Racer SelectedRacer;
    public UI_Race UI;

    private void Start()
    {
        DefDatabaseRegistry.AddAllDefs();
        Map = Map.LoadMap("racingworld");

        Map.SetDisplayToViewMode();

        /*
        RaceStart = Map.PointFeatures.Values.First(p => p.Label == "TestRaceStart").Point;
        RaceEnd = Map.PointFeatures.Values.First(p => p.Label == "TestRaceEnd").Point;
        */

        NetworkPoints = Map.GetNavigationNetworkPointsNoWater();

        RaceStart = NetworkPoints.RandomElement();
        RaceEnd = NetworkPoints.RandomElement();

        Map.AddPointFeature(RaceStart, PointFeatureDefOf.Pin, "Start");
        Map.AddPointFeature(RaceEnd, PointFeatureDefOf.Pin, "End");

        CameraHandler.Instance.SetPosition(RaceStart.Position);

        Racers = new List<Racer>();

        /*
        Entity testRacer = new Entity(Map, "TestRacer", Color.yellow, RaceStart);
        Racers.Add(testRacer);

        Entity testRacerSwim = new Entity(Map, "TestRacer", Color.blue, RaceStart);
        testRacerSwim.SetSurfaceSpeedModififer(SurfaceDefOf.Asphalt, 0.99f);
        testRacerSwim.SetSurfaceSpeedModififer(SurfaceDefOf.Water, 2f);
        Racers.Add(testRacerSwim);

        Entity testRacerTrail = new Entity(Map, "Trail", Color.red, RaceStart);
        testRacerTrail.SetSurfaceSpeedModififer(SurfaceDefOf.Asphalt, 0.985f);
        testRacerTrail.SetSurfaceSpeedModififer(SurfaceDefOf.Trail, 2f);
        Racers.Add(testRacerTrail);
        */
        for(int i = 0; i < 1000; i++)
        {
            Racer testRacer = new Racer(Map, "TestRacer" + (i + 1), new Color(Random.value, Random.value, Random.value), RaceStart);
            foreach (SurfaceDef surface in DefDatabase<SurfaceDef>.AllDefs) testRacer.SetSurfaceSpeedModififer(surface, Random.Range(0.5f, 3f));
            // testRacer.GeneralSpeedModifier = Random.Range(0.5f, 25f);
            Racers.Add(testRacer);
            Map.RegisterEntity(testRacer);
        }

        foreach (Racer racer in Racers)
        {
            NavigationPath racePath = Pathfinder.GetCheapestPath(Map, racer, RaceStart, RaceEnd);
            racer.SetPath(racePath);
        }

        SetSimulationSpeed(1f);

        // UI
        UI.RacerInfo.Hide();
    }

    #region Loop

    protected override void Tick()
    {
        Ticks++;
        Map.Tick();

        foreach (Racer racer in Racers)
        {
            if (racer.CurrentPath == null)
            {
                // Racer has reached the end
                if (!racer.IsFinished)
                {
                    racer.IsFinished = true;
                    NumFinishers++;
                    float time = Ticks * GameLoop.TickDeltaTime;
                    string timeString = HelperFunctions.GetDurationString(time, includeMilliseconds: true);
                    Debug.Log($"{racer.Name} has reached the finish on rank {NumFinishers} in {timeString}.");
                }

                /*
                // Make racers move randomly around map after having reached target
                Point newTarget = NetworkPoints.RandomElement();
                NavigationPath newPath = Pathfinder.GetCheapestPath(Map, racer, racer.Point, newTarget);
                racer.SetPath(newPath);
                */
            }
        }
    }

    protected override void HandleInputs()
    {
        Map.UpdateHoverInfo();

        if (Input.GetKey(KeyCode.Period)) SetSimulationSpeed(SimulationSpeed += 1f);
        if (Input.GetKey(KeyCode.Comma)) SetSimulationSpeed(Mathf.Max(0f, SimulationSpeed -= 1f));
        if (Input.GetKeyDown(KeyCode.Space)) SetSimulationSpeed(1f);

        if(Input.GetMouseButtonDown(0))
        {
            if (MouseHoverInfo.HoveredEntity != null)
            {
                UI.RacerInfo.Show(MouseHoverInfo.HoveredEntity as Racer);
            }
            else UI.RacerInfo.Hide();
        }
    }

    protected override void OnFrame() { }

    protected override void Render(float alpha)
    {
        Map.Render(alpha);
    }


    

    #endregion
}
