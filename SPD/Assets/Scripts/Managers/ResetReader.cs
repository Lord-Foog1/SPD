using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
public class ResetReader : MonoBehaviour
{
    void Update() 
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
