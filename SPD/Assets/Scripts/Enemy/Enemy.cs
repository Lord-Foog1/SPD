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
    private BoxCollider2D boxCollider;

    [Header("Sound")]
    [Tooltip("Idle ljuden går här")]
    [SerializeField] AudioClip[] enemyGroans;

    [SerializeField] AudioClip spawnSound;
    [SerializeField] AudioClip deathSound;
    [SerializeField] AudioClip attackSound;
    [SerializeField] AudioClip muffledSound;
    [SerializeField] AudioClip escapeSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();
        SetSoundPosition(transform.position.x);

        // Make sound
        MakeSound(spawnSound, 0.35f);

        gameManager = FindObjectsOfType<GameManager>()[0];

        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(new Vector2(0, -speed));

        source.PlayOneShot(enemyGroans[UnityEngine.Random.Range(0, enemyGroans.Length)]);
        source.loop = true;

        boxCollider = GetComponent<BoxCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Bullet" && enemyHealth == 1)
        {
            Destroy(collision.gameObject);
            gameManager.ChangePoints(enemyReward);
            source.Stop();
            MakeSound(deathSound, 0.5f);
            source.loop = false;
            boxCollider.enabled = false;
            Destroy(this.gameObject, 1.5f);
        }
        if (collision.gameObject.tag == "Bullet" && enemyHealth > 1)
        {
            enemyHealth--;
            Destroy(collision.gameObject);
        }
        if (collision.gameObject.tag == "Killbox" && this.gameObject.tag == "Enemy")
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

    private void OnTriggerExit2D(Collider2D collision)
    {
        if( collision.gameObject.tag == "LastChance" && this.gameObject.tag == "Civillian")
        {
            source.Stop();

            MakeSound(escapeSound, 1);

            Destroy(this.gameObject, 2);
        }
    }

    void MakeSound(AudioClip sound, float vol)
    {
        source.PlayOneShot(sound, vol);
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
