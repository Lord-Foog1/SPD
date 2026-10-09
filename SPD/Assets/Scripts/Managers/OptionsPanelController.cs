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
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}
