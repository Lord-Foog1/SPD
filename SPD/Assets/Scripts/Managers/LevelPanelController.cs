using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class LevelPanelController : MonoBehaviour
{
   public GameObject levelPanel;
    public GameObject mainMenuPanel;

    public GameObject firstSelectedOption;

    public void OpenLevels() {
        mainMenuPanel.SetActive(false);
        levelPanel.SetActive(true);

        EventSystem.current.SetSelectedGameObject(firstSelectedOption);
    }

    public void CloseLevels()
    {
        levelPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(firstSelectedOption);

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
