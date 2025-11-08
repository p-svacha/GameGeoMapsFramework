using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceSimulation : GameLoop
{
    private Map Map;

    // private NavigationPath RaceLine;
    private Point RaceStart;
    private Point RaceEnd;

    private List<Entity> Racers;

    private void Start()
    {
        DefDatabaseRegistry.AddAllDefs();
        Map = Map.LoadMap("racingworld");

        Map.SetDisplayToViewMode();

        /*
        RaceStart = Map.PointFeatures.Values.First(p => p.Label == "TestRaceStart").Point;
        RaceEnd = Map.PointFeatures.Values.First(p => p.Label == "TestRaceEnd").Point;
        */

        List<Point> racePointCandidates = Map.Points.Values.Where(p => p.HasLineFeature).ToList();

        RaceStart = racePointCandidates.RandomElement();
        RaceEnd = racePointCandidates.RandomElement();

        Map.AddPointFeature(RaceStart, PointFeatureDefOf.Pin, "Start");
        Map.AddPointFeature(RaceEnd, PointFeatureDefOf.Pin, "End");

        CameraHandler.Instance.SetPosition(RaceStart.Position);

        Racers = new List<Entity>();

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
            Entity testRacer = new Entity(Map, "TestRacer" + (i + 1), new Color(Random.value, Random.value, Random.value), RaceStart);
            foreach (SurfaceDef surface in DefDatabase<SurfaceDef>.AllDefs) testRacer.SetSurfaceSpeedModififer(surface, Random.Range(0.5f, 3f));
            // testRacer.GeneralSpeedModifier = Random.Range(0.5f, 25f);
            Racers.Add(testRacer);
        }

        foreach (Entity racer in Racers)
        {
            NavigationPath racePath = Pathfinder.GetCheapestPath(Map, racer, RaceStart, RaceEnd);
            racer.SetPath(racePath);
        }

        SetSimulationSpeed(1f);
    }

    #region Loop

    protected override void HandleInputs()
    {
        if (Input.GetKey(KeyCode.Period)) SetSimulationSpeed(SimulationSpeed += 1f);
        if (Input.GetKey(KeyCode.Comma)) SetSimulationSpeed(Mathf.Max(0f, SimulationSpeed -= 1f));
        if (Input.GetKeyDown(KeyCode.Space)) SetSimulationSpeed(1f);
    }

    protected override void OnFrame() { }

    protected override void Render(float alpha)
    {
        Map.Render();
        foreach (Entity racer in Racers) racer.Render(alpha);
    }


    protected override void Tick()
    {
        foreach(Entity racer in Racers) racer.Tick();
    }

    #endregion
}
