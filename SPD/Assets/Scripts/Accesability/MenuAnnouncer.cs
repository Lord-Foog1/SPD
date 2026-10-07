using UnityEngine;

public class MenuAnnouncer : MonoBehaviour
{
    void Start()
    {
        // Läser upp texten direkt när menyn laddas, men går inte att cykla tillbaka till
        UAP_AccessibilityManager.Say("Main Menu... Navigate the menu with the up and down arrow keys. Press space to interact");
    }
}