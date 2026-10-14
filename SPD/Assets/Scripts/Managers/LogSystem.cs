using System;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;

public class LogSystem : MonoBehaviour
{
    private string path;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        path = Path.Combine(Application.persistentDataPath, "playerEvents.json");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SaveEvent(string eventType)
    {
        path = Path.Combine(Application.persistentDataPath, "playerEvents.json");

        LoggedEvent newEvent = new LoggedEvent
        {
            eventType = eventType,
            dateAndTime = DateTime.Now.ToString()
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

        Debug.Log(finalJson);
    }
}
