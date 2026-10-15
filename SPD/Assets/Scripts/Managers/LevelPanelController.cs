using UnityEngine;
using UnityEngine.EventSystems;

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
    }
}
