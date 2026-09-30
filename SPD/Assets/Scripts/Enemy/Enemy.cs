using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Tooltip("Speed of the enemy")]
    [SerializeField] float speed = 100.0f;

    [Tooltip("The amount of points you lose")]
    [SerializeField] float enemyDamage = -100.0f;

    [Tooltip("The amount of points you gain when killing")]
    [SerializeField] float enemyReward = 100.0f;

    [Tooltip("Enemy health")]
    [SerializeField] float enemyHealth = 2.0f;

    private GameManager gameManager;
    private Rigidbody2D rb;
    private AudioSource source;

    [Header("Sound")]
    [Tooltip("Idle ljuden går här")]
    [SerializeField] AudioClip[] enemyGroans;

    [SerializeField] AudioClip spawnSound;
    [SerializeField] AudioClip deathSoubnd;
    [SerializeField] AudioClip attackSound;
    [SerializeField] AudioClip muffledSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();
        SetSoundPosition(transform.position.x);

        // Make sound
        // MakeSound(spawnSound);

        gameManager = FindObjectsOfType<GameManager>()[0];

        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(new Vector2(0, -speed));
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Bullet")
        {
            Destroy(collision.gameObject);
            gameManager.ChangePoints(enemyReward);
            // MakeSound(deathSound)
            Destroy(this.gameObject);
        }
        if(collision.gameObject.tag == "Killbox")
        {
            gameManager.ChangePoints(enemyDamage);
            Destroy(this.gameObject);
        }
        if(collision.gameObject.tag == "LastChance")
        {
            // rb.AddForce(new Vector2(0, speed));

        }
        if(collision.gameObject.tag == "SoundTrigger")
        {
            // MakeSound(attackSound)
        }
    }

    void MakeSound(AudioClip aound)
    {

    }

    public void SetSoundPosition(float lane)
    {
        if (lane == 0)
        {
            source.panStereo = 0;
        }
        else if (lane > 0)
        {
            source.panStereo = 1;
        }
        else if (lane < 0)
        {
            source.panStereo = -1;
        }
    }
}
