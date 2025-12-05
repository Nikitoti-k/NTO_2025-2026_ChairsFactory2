using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DailyTasksDatabase", menuName = "Game/Daily Tasks Database", order = 2)]
public class DailyTasksDatabase : ScriptableObject
{
    public List<DailyTaskSO> tasks = new List<DailyTaskSO>();

    public DailyTaskSO GetTaskForDay(int day)
    {
        var task = tasks.FirstOrDefault(t => t.dayNumber == day);
        if (task != null) return task;

        var last = tasks.Where(t => t.dayNumber < day).OrderByDescending(t => t.dayNumber).FirstOrDefault();
        return last != null ? last : tasks.FirstOrDefault();
    }
}