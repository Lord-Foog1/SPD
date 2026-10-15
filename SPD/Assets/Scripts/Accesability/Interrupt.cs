using UnityEngine;
using UnityEngine.EventSystems;

public class ForceInterrupt : MonoBehaviour, ISelectHandler
{
    public void OnSelect(BaseEventData eventData)
    {
        // Avbryter screenreadern
        UAP_AccessibilityManager.StopSpeaking();
    }
}