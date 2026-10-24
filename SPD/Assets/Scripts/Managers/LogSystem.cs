using System;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;

public class LogSystem : MonoBehaviour
{
    private string path;
    private GameManager gameManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        path = Path.Combine(Application.persistentDataPath, "playerEvents.json");
    }

    public void SaveEvent(string eventType, float gameTimeLeft, string gamePoints = "Null", string lane = "Null")
    {
        path = Path.Combine(Application.persistentDataPath, "playerEvents.json");

        LoggedEvent newEvent = new LoggedEvent
        {
            eventType = eventType,
            dateAndTime = DateTime.Now.ToString(),
            playthrough = SettingsManager.playthrough,
            points = gamePoints,
            timeLeftOnRound = gameTimeLeft,
            occupiedLane = lane
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
