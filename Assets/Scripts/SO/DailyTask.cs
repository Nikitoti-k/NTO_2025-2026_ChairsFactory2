using UnityEngine;

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
[CreateAssetMenu(fileName = "DayTask_", menuName = "Game/Day Task", order = 1)]
public class DailyTaskSO : ScriptableObject
{
    public int dayNumber;
    public int depositsToBreak = 5;
    public int mineralsToResearch = 3;
    public Vector3 caveEntrancePosition = Vector3.zero;
    public string caveSceneName = "";
}


