using UnityEngine;

public class OptionsAnnouncer : MonoBehaviour
{
    void Start()
    {
        // Läser upp texten direkt när menyn laddas, men går inte att cykla tillbaka till
        UAP_AccessibilityManager.Say("Options Menu... Navigate the menu with the up and down arrow keys. Press enter to select and then up and down arrow keys to increase or decrease the volume.");
    }
}
