using UnityEngine;
using UnityEngine.EventSystems;

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
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        UAP_AccessibilityManager.Say("Main Menu... Navigate the menu with the up and down arrow keys. Press enter to interact");
    }
}
