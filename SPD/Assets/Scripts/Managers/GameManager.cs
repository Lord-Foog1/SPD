using System;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    private float pointTotal;

    [SerializeField] int maxWaitTime = 10;
    [SerializeField] int minwaitTime = 1;
    [SerializeField] int spawnAmount = 1;
    [SerializeField] float gracePeriod = 0;
    [SerializeField] float gameTimeInSeconds = 120;

    [SerializeField] GameObject[] enemyLanes;
    [SerializeField] GameObject[] enemies;
    [SerializeField] Text finishText;

    private float timeStamp;
    private float timeToNextSpawn;
    private bool gameOver = false;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pointTotal = 0;
        timeStamp = Time.time;
        timeToNextSpawn = 0;

        // play tutorial soundclip
    }

    // Update is called once per frame
    void Update()
    {
        if (timeStamp + timeToNextSpawn <= Time.time && !gameOver)
        {
            timeStamp = Time.time;
            SpawnEnemy(ChooseEnemy(), ChooseLane());
            timeToNextSpawn = GetSpawnTime();
            Debug.Log("Hello");
        }

        if (gameTimeInSeconds > 0 && !gameOver)
        {
            gameTimeInSeconds -= Time.deltaTime;
        }
        else
        {
            gameOver = true;
            Debug.Log("Time over");
        }

        /*
         * OM DET FINNS TID ATT GÖRA
         * 
         * vid 30 sekunder ändra till mer suspensfylld musik och varna att det är 30 sekunder kvar
         */
    }

    public void ChangePoints(float pointAmount)
    {
        pointTotal += pointAmount;
        Debug.Log(pointTotal);
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

    void SpawnEnemy(GameObject enemy, GameObject lane)
    {
        Instantiate(enemy, lane.transform);
    }

    void SavePoints()
    {

    }

    void UpdateText(float points)
    {

    }

    void ShowText()
    {

    }
}
