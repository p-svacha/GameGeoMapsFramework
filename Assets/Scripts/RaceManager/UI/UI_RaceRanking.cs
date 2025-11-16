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

    private List<UI_RaceRankingRow> Rows;
    
    public void Init(RaceSimulation race)
    {
        HelperFunctions.DestroyAllChildredImmediately(Container);

        Race = race;
        Rows = new List<UI_RaceRankingRow>();
        for (int i = 0; i < 20; i++)
        {
            UI_RaceRankingRow elem = GameObject.Instantiate(RowPrefab, Container.transform);
            elem.Init();
            Rows.Add(elem);
        }
    }

    /// <summary>
    /// Costs around 8 FPS for 20 racers.
    /// </summary>
    public void UpdateStandings(RaceSimulation race)
    {
        // Show
        for (int i = 0; i < 20; i++)
        {
            Racer racer = race.Standings[i];
            Rows[i].UpdateValues(racer);
            Rows[i].ShowAsSelected(Race.SelectedRacer == racer);
        }
    }
}
