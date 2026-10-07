using UnityEngine;

public class ReaderLock : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UAP_AccessibilityManager.Say("Press the right Arrow to go to the right lane, press the left Arrow to go to the left lane, press the down Arrow to go to the middle lane, press spacebar to shoot Listen for the zombie sounds and then press the coresponding button to go to the lane the zombie is in and shoot it.You have six bullets after you have shot all of them you will need to reload");
    }
}
