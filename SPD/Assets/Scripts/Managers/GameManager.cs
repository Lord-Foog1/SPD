using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem.Controls;

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
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();

        if (newEnemyTutorial)
        {
            source.PlayOneShot(tutorialClip);
        }

        pointTotal = 0;
        timeStamp = Time.time;
        timeToNextSpawn = gracePeriod;

        LogSystem log = new LogSystem();
        log.SaveEvent("#RoundStart", gameTimeInSeconds);

        // play tutorial soundclip
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSoundVolume();

        if (timeStamp + timeToNextSpawn <= Time.time && !gameOver)
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
            gameTimeInSeconds -= Time.deltaTime;
        }
        else if (!gameOver)
        {
            gameOver = true;
            ShowText();
            Debug.Log("Time over");
            LogSystem log = new LogSystem();
            log.SaveEvent("#RoundEnd", gameTimeInSeconds, pointTotal.ToString());
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
         */
    }

    private System.Collections.IEnumerator ReturnToMenuTimer()
    {
        yield return new WaitForSeconds(5f);

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
}
