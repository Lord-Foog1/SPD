using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public void StartGame()
    {
        // Byt ut "Level1" mot exakt det namn din spelscen har i projektet
        SceneManager.LoadScene("Prototype");
    }

    public void QuitGame()
    {
        // Detta stänger spelet i den färdiga Builden
        Application.Quit();
    }
}