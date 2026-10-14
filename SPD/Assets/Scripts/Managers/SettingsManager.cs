using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private Slider playerSlider;
    [SerializeField] private Slider monsterSlider;
    [SerializeField] private Slider menuSlider;
    [SerializeField] private Slider ambienceSlider;
    [SerializeField] private Slider operatorSlider;

    private static float monsterSoundLevel { get; set; }
    private static float playerSoundLevel { get; set; }
    private static float menuSoundLevel { get; set; }
    private static float ambienceSoundLevel { get; set; }
    private static float operatorSoundLevel { get; set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerSoundLevel = playerSlider.value;
        menuSoundLevel = menuSlider.value;
        monsterSoundLevel = monsterSlider.value;
        operatorSoundLevel = operatorSlider.value;
        ambienceSoundLevel = ambienceSlider.value;
    }

    public void UpdateVolume(float val)
    {

    }
}
