using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceSimulation : GameLoop
{
    private Map Map;
    public int TickNumber { get; private set; }

    // private NavigationPath RaceLine;
    private Point RaceStart;
    private Point RaceEnd;

    private List<Racer> Racers;
    private List<Racer> CurrentRanking;

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

        for(int i = 0; i < 1000; i++)
        {
            Racer testRacer = new Racer(this, Map, "TestRacer" + (i + 1), new Color(Random.value, Random.value, Random.value), RaceStart);
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

        CurrentRanking = new List<Racer>(Racers);

        SetSimulationSpeed(1f);

        // UI
        UI.RacerInfo.Hide();
    }

    #region Loop

    protected override void Tick()
    {
        TickNumber++;
        Map.Tick();

        // Current ranking
        CurrentRanking = CurrentRanking.OrderBy(r => r.CurrentDistanceToFinish).ToList();
        for(int i = 0; i < CurrentRanking.Count; i++)
        {
            if (!CurrentRanking[i].IsFinished) CurrentRanking[i].CurrentRank = i + 1;
        }
    }

    protected override void HandleInputs()
    {
        Map.UpdateHoverInfo();

        if (Input.GetKey(KeyCode.Period)) SetSimulationSpeed(SimulationSpeed + 1f);
        if (Input.GetKey(KeyCode.Comma)) SetSimulationSpeed(Mathf.Max(0f, SimulationSpeed - 1f));
        if (Input.GetKeyDown(KeyCode.Space)) SetSimulationSpeed(1f);

        if(Input.GetMouseButtonDown(0) && !HelperFunctions.IsMouseOverUi())
        {
            SelectRacer(MouseHoverInfo.HoveredEntity as Racer);
        }
    }

    public void SelectRacer(Racer racer)
    {
        // Deselect previous
        if (SelectedRacer != null)
        {
            SelectedRacer.ShowAsSelected(false);
            SelectedRacer = null;
            UI.RacerInfo.Hide();
        }

        // Select new
        if (racer != null)
        {
            UI.RacerInfo.Show(racer);
            SelectedRacer = racer;
            SelectedRacer.ShowAsSelected(true);
        }
    }

    protected override void OnFrame() { }

    protected override void Render(float alpha)
    {
        Map.Render(alpha);
    }


    

    #endregion
}
