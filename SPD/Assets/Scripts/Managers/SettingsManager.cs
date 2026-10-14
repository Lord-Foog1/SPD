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

    private static float monsterSoundLevel { get; set; }
    private static float playerSoundLevel { get; set; }
    private static float menuSoundLevel { get; set; }
    private static float ambienceSoundLevel { get; set; }
    private static float operatorSoundLevel { get; set; }

    static private bool soundSet = false;

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
            audioSource.PlayOneShot(clip, vol);
        }
    }
}
