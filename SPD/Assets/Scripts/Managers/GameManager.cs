using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem.Controls;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{

    private int pointTotal;

    [SerializeField] int maxWaitTime = 10;
    [SerializeField] int minwaitTime = 1;
    [SerializeField] int spawnAmount = 1;
    [SerializeField] float gameTimeInSeconds = 120;

    [Header("New Enemy Tutorial")]
    [SerializeField] bool newEnemyTutorial = false;
    [SerializeField] AudioClip tutorialClip;
    [SerializeField] float gracePeriod = 0;

    [Header("Enemy Setup")]
    [SerializeField] GameObject[] enemyLanes;
    [SerializeField] GameObject[] enemies;
    [SerializeField] TMP_Text finishText;

    [SerializeField] AudioSource[] ambientAudioSources;

    private float timeStamp;
    private float timeToNextSpawn;
    private bool gameOver = false;
    private bool hasBeenRead;
    private AudioSource source;

    // 2026-10-28 (Alfred): operatörens repliker i levlarna. Klippen ligger i Assets/Resources/LevelLines och laddas
    // med Resources.Load, så jag slipper koppla in dem i scenerna. De spelas en i taget på en egen AudioSource med
    // Operator-reglagets volym, precis som rösten i tutorialen. Nya fiender medan en replik spelas väntar tills den är klar.
    private const string LineFolder = "LevelLines/";
    private AudioSource voiceSource;
    private readonly Queue<AudioClip> voiceQueue = new Queue<AudioClip>();
    private bool voiceBusy;
    private bool introPlaying;      // klockan står still medan introt (L04/L11 eller L05) spelas
    private bool thirtyWarned;
    private float roundLength;
    private static GameManager current;
    // Repliker som bara ska höras första gången under en spelomgång (static = gäller tills spelet stängs).
    private static bool saidCharger, saidLaneJumper, saidFastOne, saidOutOfAmmo, saidCivilianThrough, saidScream, saidPointRules;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // SettingsManager.SetSoundLevels();

        source = GetComponent<AudioSource>();

        // 2026-10-28 (Alfred): startar man en level direkt (utan menyn) var alla volymer 0 och man hörde ingenting.
        // Har menyn inte satt reglagen tar jag menyns standardvärden (samma som tutorialen gör).
        SettingsManager.EnsureDefaultSoundLevels();

        // 2026-10-28 (Alfred): egen ljudkälla för operatören och introt för den här leveln.
        current = this;
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
        roundLength = gameTimeInSeconds;
        PlayLevelIntro();

        // play tutorial soundclip
        if (newEnemyTutorial)
        {
            source.PlayOneShot(tutorialClip);
        }

        pointTotal = 0;
        timeStamp = Time.time;
        timeToNextSpawn = gracePeriod;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSoundVolume();

        // 2026-10-28 (Alfred): "&& !voiceBusy" så att ingen ny fiende spawnar mitt i en operatörsreplik.
        if (timeStamp + timeToNextSpawn <= Time.time && !gameOver && !voiceBusy)
        {
            timeStamp = Time.time;
            for (int i = 0; i < spawnAmount; i++)
            {
                SpawnEnemy(ChooseEnemy(), ChooseLane());
            }
            timeToNextSpawn = GetSpawnTime();
        }

        if (gameTimeInSeconds > 0 && !gameOver)
        {
            // 2026-10-28 (Alfred): klockan börjar först när introt är klart, så "sextio sekunder" stämmer.
            if (!introPlaying) gameTimeInSeconds -= Time.deltaTime;

            // 2026-10-28 (Alfred): TODO:n nedan, varningen när 30 sekunder är kvar (L06). Bara om rundan är längre än 30 s.
            if (!thirtyWarned && roundLength > 30f && gameTimeInSeconds <= 30f)
            {
                thirtyWarned = true;
                Say("L06_ThirtySecondsLeft");
            }
        }
        else if (!gameOver)
        {
            gameOver = true;
            // 2026-10-28 (Alfred): rundan är slut, repliker som inte hunnit spelas slängs.
            voiceQueue.Clear();
            ShowText();
            Debug.Log("Time over");
            GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civillian");
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach(GameObject toDestroy in civilians)
            {
                Destroy(toDestroy);
            }

            foreach(GameObject toDestroy in enemies)
            {
                Destroy(toDestroy);
            }

            StartCoroutine(ReturnToMenuTimer());
        }

        /*
         * OM DET FINNS TID ATT GÖRA
         * 
         * vid 30 sekunder ändra till mer suspensfylld musik och varna att det är 30 sekunder kvar
         *
         * 2026-10-28 (Alfred): varningen är gjord (L06, se ovan). Musikbytet är inte gjort.
         */
    }

    private System.Collections.IEnumerator ReturnToMenuTimer()
    {
        yield return new WaitForSeconds(5f);

        // 2026-10-28 (Alfred): efter Level 1 kommer Level 2 (flödet är meny -> tutorial -> Level 1 -> Level 2).
        // Förut gick Level 1 tillbaka till menyn. Efter Level 2 (och andra scener) går det till menyn som förut.
        if (SceneManager.GetActiveScene().name == "Level1")
            SceneManager.LoadScene("Level2");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void ChangePoints(int pointAmount)
    {
        pointTotal += pointAmount;
        //Debug.Log(pointTotal);
    }

    public int GetPoints()
    {
        return pointTotal;
    }

    GameObject ChooseEnemy()
    {
        return enemies[UnityEngine.Random.Range(0, enemies.Length)];
    }

    GameObject ChooseLane()
    {
        return enemyLanes[UnityEngine.Random.Range(0, enemyLanes.Length)];
    }

    int GetSpawnTime()
    {
        return UnityEngine.Random.Range(minwaitTime, maxWaitTime + 1);
    }

    public float GetTimeLeft()
    {
        return gameTimeInSeconds;
    }

    void SpawnEnemy(GameObject enemy, GameObject lane)
    {
        Instantiate(enemy, lane.transform);

        // 2026-10-28 (Alfred): första gången en ny fiendetyp dyker upp säger operatören vad det är (en gång per spelomgång).
        if (enemy.GetComponent<RushingEnemy>() != null && !saidCharger) { saidCharger = true; Say("L01_Charger"); }
        else if (enemy.GetComponent<LaneJumper>() != null && !saidLaneJumper) { saidLaneJumper = true; Say("L02_LaneJumper"); }
        else if (enemy.name == "FastEnemy" && !saidFastOne) { saidFastOne = true; Say("L03_FastOne"); }
    }

    void UpdateText(float points)
    {
        finishText.text = "Round over, points earned: " + pointTotal.ToString();
    }

    void ShowText()
    {
        if (!hasBeenRead)
        {
            UpdateText(pointTotal);
            finishText.enabled = true;
            UAP_AccessibilityManager.Say("Round over, points earned: " + pointTotal.ToString());
            hasBeenRead = true;
        }
    }

    void UpdateSoundVolume()
    {
        foreach(AudioSource audio in ambientAudioSources)
        {
            audio.volume = SettingsManager.ambienceSoundLevel;
        }
    }

    // ---------------------------------------------------------------- operatörens repliker (2026-10-28, Alfred)

    // Introt: Level 1 = "First shift..." och (första gången) poängreglerna, Level 2 = "Second shift...".
    private void PlayLevelIntro()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Level1")
        {
            Say("L04_FirstShift");
            if (!saidPointRules) { saidPointRules = true; Say("L11_PointRules"); }
        }
        else if (scene == "Level2")
        {
            Say("L05_SecondShift");
        }
        introPlaying = voiceBusy; // Say() har redan startat uppspelningen om något klipp hittades
    }

    // Lägger en replik i kön. Finns inte klippet loggar jag en varning och spelet fortsätter som vanligt.
    private void Say(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>(LineFolder + clipName);
        if (clip == null)
        {
            Debug.LogWarning("[GameManager] Level line not found: Resources/" + LineFolder + clipName);
            return;
        }
        voiceQueue.Enqueue(clip);
        if (!voiceBusy) StartCoroutine(PlayVoiceQueue());
    }

    // Spelar kön en replik i taget. När kön är tom får fienderna spawna igen och klockan gå.
    private IEnumerator PlayVoiceQueue()
    {
        voiceBusy = true;
        while (voiceQueue.Count > 0)
        {
            AudioClip clip = voiceQueue.Dequeue();
            voiceSource.volume = SettingsManager.operatorSoundLevel;
            voiceSource.clip = clip;
            voiceSource.Play();
            yield return new WaitForSeconds(clip.length + 0.3f);
        }
        voiceBusy = false;
        introPlaying = false;
        // Nästa fiende kommer tidigast en vanlig spawn-paus efter repliken, inte i samma sekund.
        timeStamp = Time.time;
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    // Anropas från Player.cs: första gången man klickar med tomt magasin (L08).
    public static void NotifyOutOfAmmo()
    {
        if (current == null || current.gameOver || saidOutOfAmmo) return;
        saidOutOfAmmo = true;
        current.Say("L08_OutOfAmmo");
    }

    // Anropas från Enemy.cs: första civila som når slutet utan att bli skjuten (L10).
    public static void NotifyCivilianThrough()
    {
        if (current == null || current.gameOver || saidCivilianThrough) return;
        saidCivilianThrough = true;
        current.Say("L10_CivilianThrough");
    }

    // Anropas från Enemy.cs: första gången en zombie skriker nära spelaren (L13).
    public static void NotifyZombieScream()
    {
        if (current == null || current.gameOver || saidScream) return;
        saidScream = true;
        current.Say("L13_HearThatScream");
    }
}
