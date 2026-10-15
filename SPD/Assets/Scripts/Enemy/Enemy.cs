using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Tooltip("Speed of the enemy")]
    [SerializeField] float speed = 100.0f;

    [Tooltip("The amount of points you lose")]
    [SerializeField] int enemyDamage = -100;

    [Tooltip("The amount of points you gain when killing")]
    [SerializeField] int enemyReward = 100;

    [Tooltip("Enemy health")]
    [SerializeField] float enemyHealth = 2.0f;

    private GameManager gameManager;
    private Rigidbody2D rb;
    private AudioSource source;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

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

        spriteRenderer = GetComponent<SpriteRenderer>();

        // Make sound
        MakeSound(spawnSound);

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

            if (gameObject.CompareTag("Civillian"))
            {
                LogSystem log = new LogSystem();
                log.SaveEvent("#CivilianKilled", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());
            }
            else if (gameObject.CompareTag("Enemy"))
            {
                LogSystem log = new LogSystem();
                log.SaveEvent("#EnemyKilled", gameManager.GetTimeLeft(),gameManager.GetPoints().ToString(), transform.position.x.ToString());
            }

            source.Stop();
            MakeSound(deathSound);
            source.loop = false;
            boxCollider.enabled = false;
            spriteRenderer.enabled = false;
            Destroy(this.gameObject, 1.5f);
        }
        if (collision.gameObject.tag == "Bullet" && enemyHealth > 1)
        {
            enemyHealth--;
            Destroy(collision.gameObject);
        }
        if (collision.gameObject.tag == "Killbox")
        {
            gameManager.ChangePoints(enemyDamage);

            if (gameObject.CompareTag("Enemy"))
            {
                LogSystem log = new LogSystem();
                log.SaveEvent("#EnemyEscaped", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());
            }
            else if (gameObject.CompareTag("Civillian"))
            {
                LogSystem log = new LogSystem();
                log.SaveEvent("#CivilianEscaped", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());
            }

            source.Stop();
            MakeSound(escapeSound);
            Destroy(this.gameObject, 1.5f);
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

    void MakeSound(AudioClip sound)
    {
        source.PlayOneShot(sound, SettingsManager.monsterSoundLevel);
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
