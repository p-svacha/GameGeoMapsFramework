using System.Collections.Generic;
using UnityEngine;

public class UI_RaceRanking : MonoBehaviour
{
    private RaceSimulation Race;
    public const int NUM_ROWS = 20;

    [Header("Elements")]
    public GameObject Container;

    [Header("Prefabs")]
    public UI_RaceRankingRow RowPrefab;

    private Dictionary<Racer, UI_RaceRankingRow> Rows;
    private List<Racer> ShownRacers;
    
    public void Init(RaceSimulation race)
    {
        HelperFunctions.DestroyAllChildredImmediately(Container);

        Race = race;
        Rows = new Dictionary<Racer, UI_RaceRankingRow>();
        ShownRacers = new List<Racer>();
        foreach(Racer racer in Race.Racers)
        {
            UI_RaceRankingRow elem = GameObject.Instantiate(RowPrefab, Container.transform);
            elem.Init(racer);
            Rows[racer] = elem;
            elem.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Costs around 8 FPS for 20 racers.
    /// </summary>
    public void UpdateStandings(RaceSimulation race)
    {
        // Hide 
        foreach (Racer racer in ShownRacers)
        {
            if (racer.CurrentRank > NUM_ROWS) Rows[racer].gameObject.SetActive(false);
        }
        ShownRacers.Clear();
        
        // Show
        for (int i = 0; i < 20; i++)
        {
            Racer racer = race.Standings[i];
            if (!Rows[racer].gameObject.activeSelf) Rows[racer].gameObject.SetActive(true);
            Rows[racer].UpdateValues();
            Rows[racer].transform.SetSiblingIndex(i);

            ShownRacers.Add(racer);
        }
    }

    public void ShowRacerAsSelected(Racer racer, bool value)
    {
        Rows[racer].ShowAsSelected(value);
    }
}
