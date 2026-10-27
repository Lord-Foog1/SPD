using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private Slider playerSlider;
    [SerializeField] private Slider monsterSlider;
    [SerializeField] private Slider menuSlider;
    [SerializeField] private Slider ambienceSlider;
    [SerializeField] private Slider operatorSlider;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip monsterExample;
    [SerializeField] private AudioClip playerExample;
    [SerializeField] private AudioClip ambienceExample;
    [SerializeField] private AudioClip operatorExample;
    [SerializeField] private AudioClip menuExample;

    [Header("Sound Duration")]
    [SerializeField] private float soundDuration = 2f;
    [SerializeField] private float playCooldown = 0.0f;
    private float nextPlayTime;

    public static float monsterSoundLevel { get; set; }
    public static float playerSoundLevel { get; set; }
    public static float menuSoundLevel { get; set; }
    public static float ambienceSoundLevel { get; set; }
    public static float operatorSoundLevel { get; set; }
    public static int playthrough {  get; set; }
    private static bool soundSet = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!soundSet)
        {
            playerSoundLevel = playerSlider.value;
            menuSoundLevel = menuSlider.value;
            monsterSoundLevel = monsterSlider.value;
            operatorSoundLevel = operatorSlider.value;
            ambienceSoundLevel = ambienceSlider.value;

            soundSet = true;
        }

        playerSlider.onValueChanged.AddListener(val => 
        {
            playerSoundLevel = val;
            PlayExample(playerExample, val);
        });
        monsterSlider.onValueChanged.AddListener(val => 
        {
            monsterSoundLevel = val;
            PlayExample(monsterExample, val);
        });
        ambienceSlider.onValueChanged.AddListener(val => 
        { 
            ambienceSoundLevel = val;
            PlayExample(ambienceExample, val);
        });
        operatorSlider.onValueChanged.AddListener(val =>
        {
            operatorSoundLevel = val;
            PlayExample(operatorExample, val);
        });
        menuSlider.onValueChanged.AddListener(val => 
        { 
            menuSoundLevel = val;
            PlayExample(menuExample, val);
        });
    }

    private void PlayExample(AudioClip clip, float vol)
    {
        if(audioSource != null && clip != null)
        {
            if (Time.time < nextPlayTime) return;

            nextPlayTime = Time.time + playCooldown;

            StopAllCoroutines();
            audioSource.Stop();
            audioSource.PlayOneShot(clip, vol);

            StartCoroutine(StopSoundAfterDelay(soundDuration));
        }
    }

    public static void SetSoundLevels()
    {
        playerSoundLevel = 0.25f;
        monsterSoundLevel = 0.40f;
        ambienceSoundLevel = 0.05f;
    }

    private IEnumerator StopSoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.Stop();
    }
}
