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

    public void QuitGame()
    {
        // Detta stänger spelet i den färdiga Builden
        Application.Quit();
    }
}