using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class OptionsPanelController : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    public GameObject firstSelectedOption;

    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);

        EventSystem.current.SetSelectedGameObject(firstSelectedOption);
        //UAP_AccessibilityManager.Say("Options Menu... Navigate the menu with the up and down arrow keys. Press enter to select and then up and down arrow keys to increase or decrease the volume.");
    }

    public void CloseOptions()
    {
        // Byt paneler först
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        // Starta uppläsningen med en mikropaus
        StartCoroutine(PlayMenuAnnouncer());
    }

    private IEnumerator PlayMenuAnnouncer()
    {
        // Låt Unity uppdatera en bildruta (frame) så att Start-knappen hinner köras och markeras
        yield return null;

        // Töm ljudkön på knappens standardljud
        UAP_AccessibilityManager.StopSpeaking();

        // Lägg in din långa text ifred
        UAP_AccessibilityManager.Say("Main Menu... Navigate the menu with the up and down arrow keys. Press enter to interact");
    }
}
