using System;
using System.IO;
using UnityEngine;

[Serializable]
public class LoggedEvent
{
    public string eventType;
    public string dateAndTime;
    public int playthrough;
    public string points;
    public float timeLeftOnRound;
    public string occupiedLane;
}
