
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource guideSource;
    [SerializeField] private AudioSource dummySource;

    [Header("Introduction")]
    [SerializeField] private AudioClip[] introduction;

    [Header("Right Calibration")]
    [SerializeField] private AudioClip[] askRight;
    [SerializeField] private AudioClip[] wrongRight;
    [SerializeField] private AudioClip[] correctRight;

    [Header("Left Calibration")]
    [SerializeField] private AudioClip[] askLeft;
    [SerializeField] private AudioClip[] wrongLeft;
    [SerializeField] private AudioClip[] correctLeft;

    [Header("Dummy Practice")]
    [SerializeField] private AudioClip[] explainDummy;
    [SerializeField] private AudioClip dummySound;

    [Header("Ending")]
    [SerializeField] private AudioClip[] tutorialComplete;
    [SerializeField] private string nextScene = "prototype";

    private IEnumerator Start()
    {
        if (guideSource == null)
        {
            Debug.LogError("TutorialManager: assign Guide Source in the Inspector.", this);
            yield break;
        }
        guideSource.loop = false;

        // STEP 1: Introduction
        yield return PlayAudioList(introduction);

        // STEP 2: Face left first, matching the recorded instructions.
        yield return PlayAudioList(askLeft);
        yield return WaitForDirection(
            Key.LeftArrow,
            Key.RightArrow,
            wrongLeft,
            askLeft
        );
        yield return PlayAudioList(correctLeft);

        // STEP 3: correctLeft may already say "now right";
        // leave askRight empty in that case to avoid repeating it.
        yield return PlayAudioList(askRight);
        yield return WaitForDirection(
            Key.RightArrow,
            Key.LeftArrow,
            wrongRight,
            askRight
        );
        yield return PlayAudioList(correctRight);

        // STEP 4: Explain dummy
        yield return PlayAudioList(explainDummy);

        // STEP 5: Deploy dummy sound
        if (dummySource != null && dummySound != null)
        {
            dummySource.clip = dummySound;
            dummySource.loop = true;
            dummySource.Play();
        }

        // Wait for player to shoot
        yield return WaitForShoot();

        // Stop dummy sound
        if (dummySource != null)
        {
            dummySource.Stop();
        }

        // STEP 6: Tutorial completed
        yield return PlayAudioList(tutorialComplete);

        // STEP 7: Load actual game
        SceneManager.LoadScene(nextScene);
    }

    // Play every recording in a list, in order
    private IEnumerator PlayAudioList(AudioClip[] clips)
    {
        if (clips == null || guideSource == null)
            yield break;

        foreach (AudioClip clip in clips)
        {
            if (clip == null)
                continue;

            guideSource.clip = clip;
            guideSource.Play();

            // Wait until the current recording finishes
            while (guideSource.isPlaying)
            {
                yield return null;
            }
        }
    }

    // Wait for the correct movement
    private IEnumerator WaitForDirection(
        Key correctKey,
        Key wrongKey,
        AudioClip[] wrongAudio,
        AudioClip[] repeatInstruction)
    {
        // Avoid accepting a key held during narration.
        yield return WaitForKeysReleased();

        while (true)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard[wrongKey].wasPressedThisFrame)
                {
                    // Play correction
                    yield return PlayAudioList(wrongAudio);

                    // Repeat the instruction
                    yield return PlayAudioList(repeatInstruction);

                    // Wait for fresh input
                    yield return WaitForKeysReleased();
                }
                else if (keyboard[correctKey].wasPressedThisFrame)
                {
                    // Correct input
                    yield break;
                }
            }

            yield return null;
        }
    }

    // Wait until movement keys are released
    private IEnumerator WaitForKeysReleased()
    {
        while (Keyboard.current != null &&
              (Keyboard.current.leftArrowKey.isPressed ||
               Keyboard.current.rightArrowKey.isPressed))
        {
            yield return null;
        }

        // Start listening on the following frame
        yield return null;
    }

    // Wait until Space is pressed
    private IEnumerator WaitForShoot()
    {
        // Require a new Space press
        while (Keyboard.current != null &&
               Keyboard.current.spaceKey.isPressed)
        {
            yield return null;
        }

        yield return null;

        while (true)
        {
            if (Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                yield break;
            }

            yield return null;
        }
    }
}

