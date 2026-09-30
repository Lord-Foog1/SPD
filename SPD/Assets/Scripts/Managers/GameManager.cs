using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    private float pointTotal;

    [SerializeField] int maxWaitTime = 10;
    [SerializeField] int minwaitTime = 1;

    [SerializeField] GameObject[] enemyLanes;
    [SerializeField] GameObject[] enemies;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pointTotal = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangePoints(float pointAmount)
    {
        pointTotal += pointAmount;
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

    }

    void SavePoints()
    {

    }
}
