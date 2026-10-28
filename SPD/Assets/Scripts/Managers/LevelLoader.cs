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
        // No playthrough++ / LogSystem.ResetCount() here.
        // 2026-10-28 (Alfred): efter tutorialen går spelaren tillbaka till huvudmenyn i stället för Level 1,
        // så TutorialManager räknar inte upp playthrough längre. Det gör StartGame() när spelaren sen startar Level 1.
        SceneManager.LoadScene("Tutorial");
    }

    public void QuitGame()
    {
        // Detta stänger spelet i den färdiga Builden
        Application.Quit();
    }
}