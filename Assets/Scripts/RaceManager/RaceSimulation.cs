using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceSimulation : GameLoop
{
    private Map Map;
    public int TickNumber { get; private set; }

    // private NavigationPath RaceLine;
    public Point StartPoint { get; private set; }
    public Point EndPoint { get; private set; }

    public List<Racer> Racers;
    public List<Racer> Standings; // Current in-race standings

    private List<Point> NetworkPoints;

    int NumFinishers = 0;

    // Path cache
    private Dictionary<Point, NavigationPath> BestPathsToFin; // Caches the non-entity-specific best paths from specific points to the finish.

    // UI
    private Racer SelectedRacer;
    public UI_Race UI;

    private void Start()
    {
        DefDatabaseRegistry.AddAllDefs();

        Racers = new List<Racer>();
        BestPathsToFin = new Dictionary<Point, NavigationPath>();

        Map = Map.LoadMap("racingworld");

        Map.SetDisplayToViewMode();

        /*
        RaceStart = Map.PointFeatures.Values.First(p => p.Label == "TestRaceStart").Point;
        RaceEnd = Map.PointFeatures.Values.First(p => p.Label == "TestRaceEnd").Point;
        */

        NetworkPoints = Map.GetNavigationNetworkPointsNoWater();

        StartPoint = NetworkPoints.RandomElement();
        EndPoint = NetworkPoints.RandomElement();

        Map.AddPointFeature(StartPoint, PointFeatureDefOf.Pin, "Start");
        Map.AddPointFeature(EndPoint, PointFeatureDefOf.Pin, "End");

        CameraHandler.Instance.SetPosition(StartPoint.Position);

        for(int i = 0; i < 1000; i++)
        {
            Racer testRacer = new Racer(this, Map, "TestRacer" + (i + 1), new Color(Random.value, Random.value, Random.value), StartPoint);
            foreach (SurfaceDef surface in DefDatabase<SurfaceDef>.AllDefs) testRacer.SetSurfaceSpeedModififer(surface, Random.Range(0.5f, 3f));
            // testRacer.GeneralSpeedModifier = Random.Range(0.5f, 25f);
            Racers.Add(testRacer);
            Map.RegisterEntity(testRacer);
        }

        foreach (Racer racer in Racers)
        {
            NavigationPath racePath = Pathfinder.GetCheapestPath(Map, racer, StartPoint, EndPoint);
            racer.SetPath(racePath);
        }

        Standings = new List<Racer>(Racers);

        SetSimulationSpeed(1f);

        // UI
        UI.Init(this);
        UI.RacerInfo.Hide();

        // Immediately start
        StartRace();
    }

    public void StartRace()
    {
        foreach (Racer racer in Racers) racer.OnRaceStart();
    }

    /// <summary>
    /// Returns the general (non-entity-specific) cheapest path from any point to the race finish.
    /// <br/>Caches every path ever queried for performance.
    /// </summary>
    public NavigationPath GetBestPathToFin(Point from)
    {
        // Check cache
        if (BestPathsToFin.ContainsKey(from)) return new NavigationPath(BestPathsToFin[from]);

        NavigationPath bestPath = Pathfinder.GetCheapestPath(Map, from, EndPoint);
        BestPathsToFin[from] = bestPath;
        return bestPath;
    }

    #region Loop

    protected override void Tick()
    {
        TickNumber++;
        Map.Tick();

        // Current ranking
        Standings = Standings.OrderBy(r => r.CurrentDistanceToFinish).ToList();
        for(int i = 0; i < Standings.Count; i++)
        {
            if (!Standings[i].IsFinished) Standings[i].CurrentRank = i + 1;
        }
    }

    protected override void HandleInputs()
    {
        Map.UpdateHoverInfo();

        if (Input.GetKey(KeyCode.Period)) SetSimulationSpeed(SimulationSpeed + 1f);
        if (Input.GetKey(KeyCode.Comma)) SetSimulationSpeed(Mathf.Max(0f, SimulationSpeed - 1f));
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (SimulationSpeed > 0f) SetSimulationSpeed(0f); // Pause
            else SetSimulationSpeed(1f); // Unpause
        }

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
            ShowRacerAsSelected(SelectedRacer, false);
            SelectedRacer = null;
            UI.RacerInfo.Hide();
        }

        // Select new
        if (racer != null)
        {
            UI.RacerInfo.Show(racer);
            SelectedRacer = racer;
            ShowRacerAsSelected(SelectedRacer, true);
        }
    }

    protected override void OnFrame() { }

    protected override void Render(float alpha)
    {
        Map.Render(alpha);
    }




    #endregion

    private void ShowRacerAsSelected(Racer racer, bool value)
    {
        UI.Standings.ShowRacerAsSelected(racer, value);
        racer.ShowAsSelected(value);
    }
    
    public void PanToAndFollowRacer(Racer racer)
    {
        CameraHandler.Instance.PanTo(racer.CurrentWorldPosition, postPanFollowEntity: racer);
    }
}
