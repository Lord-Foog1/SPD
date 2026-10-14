using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EventListWrapper
{
    public List<LoggedEvent> events = new List<LoggedEvent>();
}
