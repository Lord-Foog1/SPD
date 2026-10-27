using JetBrains.Annotations;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;

public class LogSystem : MonoBehaviour
{
    private string path;
    private GameManager gameManager;
    private static int totalEnemySpawned;
    private static int totalEnemyKilled;
    private static int totalCivilianSpwaned;
    private static int totalCivilianKilled;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "playerEvents.json");
    }

    public static void ResetCount()
    {
        totalEnemySpawned = 0;
        totalEnemyKilled = 0;
        totalCivilianKilled = 0;
        totalCivilianSpwaned = 0;
    }

    public void SaveEvent(string eventType, float gameTimeLeft, string gamePoints = "Null", string lane = "Null")
    {
        path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "playerEvents.json");

        if (eventType == "#EnemyKilled") totalEnemyKilled++;
        if (eventType == "#EnemySpawned") totalEnemySpawned++;
        if (eventType == "#CivilianKilled") totalCivilianKilled++;
        if (eventType == "#CivilianSpawned") totalCivilianSpwaned++;

        LoggedEvent newEvent = new LoggedEvent
        {
            eventType = eventType,
            dateAndTime = DateTime.Now.ToString(),
            playthrough = SettingsManager.playthrough,
            points = gamePoints,
            timeLeftOnRound = gameTimeLeft,
            occupiedLane = lane,
            totalEnemyKilled = totalEnemyKilled,
            totalCivilianKilled = totalCivilianKilled,
            totalCivilianSpwaned = totalCivilianSpwaned,
            totalEnemySpawned = totalEnemySpawned
        };

        EventListWrapper wrapper = new EventListWrapper();

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            wrapper = JsonUtility.FromJson<EventListWrapper>(json);

            if(wrapper == null)
            {
                wrapper = new EventListWrapper();
            }
        }

        wrapper.events.Add(newEvent);

        string finalJson = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(path, finalJson);

        //Debug.Log(finalJson);
    }
}
