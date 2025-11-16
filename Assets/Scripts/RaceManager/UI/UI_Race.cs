using UnityEngine;

public class UI_Race : MonoBehaviour
{
    private RaceSimulation Race;

    [Header("Elements")]
    public UI_RacerInfo RacerInfo;
    public UI_RaceRanking Standings;

    public void Init(RaceSimulation race)
    {
        Race = race;
        Standings.Init(Race);
    }

    private void Update()
    {
        Standings.UpdateStandings(Race);
    }
}
