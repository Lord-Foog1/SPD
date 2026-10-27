using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public void StartGame()
    {
        SettingsManager.playthrough++;
        LogSystem.ResetCount();
        // Byt ut "Level1" mot exakt det namn din spelscen har i projektet
        SceneManager.LoadScene("Level1");
    }

    public void StartLevel2()
    {
        SettingsManager.playthrough++;
        LogSystem.ResetCount();

        SceneManager.LoadScene("Level2");
    }

    public void StartTutorial()
    {
        // No playthrough++ / LogSystem.ResetCount() here: TutorialManager does both
        // when it hands off to Level1, so a tutorial + Level1 run counts as one playthrough.
        SceneManager.LoadScene("Tutorial");
    }

    public void QuitGame()
    {
        // Detta stänger spelet i den färdiga Builden
        Application.Quit();
    }
}