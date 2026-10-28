using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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
    private AudioLowPassFilter filter;

    [Header("Sound")]
    [Tooltip("Idle ljuden g�r h�r")]
    [SerializeField] AudioClip[] idleSounds;

    [SerializeField] AudioClip spawnSound;
    [SerializeField] AudioClip deathSound;
    [SerializeField] AudioClip attackSound;
    [SerializeField] AudioClip muffledSound;
    [SerializeField] AudioClip escapeSound;

    [Header("Sound Duration")]
    [SerializeField] private float soundDuration = 2.0f;
    [SerializeField] private float playCooldown = 0.5f;
    private float nextPlayTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindObjectsOfType<GameManager>()[0];

        if (gameObject.CompareTag("Civillian"))
        {
            LogSystem log = new LogSystem();
            log.SaveEvent("#CivilianSpawned", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());
        }
        if (gameObject.CompareTag("Enemy"))
        {
            LogSystem log = new LogSystem();
            log.SaveEvent("#EnemySpawned", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());
        }

        source = GetComponent<AudioSource>();
        SetSoundPosition(transform.position.x);
        filter = GetComponent<AudioLowPassFilter>();

        // 2026-10-28 (Alfred): "if (filter)" så att en prefab utan LowPass-filter inte kraschar (FastEnemy saknade det).
        if (filter) filter.enabled = false;

        spriteRenderer = GetComponent<SpriteRenderer>();

        // Make sound
        if (spawnSound)
        {
            MakeSound(spawnSound);

            source.loop = true;

            StartCoroutine(StartIdleSounds(1.6f));
        }

        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(new Vector2(0, -speed));

        // source.PlayOneShot(enemyGroans[UnityEngine.Random.Range(0, enemyGroans.Length)]);

        boxCollider = GetComponent<BoxCollider2D>();

        if(idleSounds.Length > 0)
        {

        }
    }

    // Update is called once per frame
    void Update()
    {
        
        Debug.DrawRay(new Vector2(transform.position.x, transform.position.y - 0.6f), new Vector2(0, -3.5f));
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - 0.6f), new Vector2(0, -3.5f));

        // 2026-10-28 (Alfred): samma koll här, filtret används bara om det finns.
        if (hit && (hit.collider.gameObject.CompareTag("Civillian") || hit.collider.gameObject.CompareTag("Enemy")))
        {
            if (filter) filter.enabled = true;
            Debug.Log("Hit");
        }
        else
        {
            if (filter) filter.enabled = false;
            Debug.Log("No Hit");
        }

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

            // 2026-10-28 (Alfred): första civila som tar sig igenom: "Good, you let the civilian through" (L10).
            if (gameObject.CompareTag("Civillian")) GameManager.NotifyCivilianThrough();

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
            MakeSound(attackSound);

            // 2026-10-28 (Alfred): Jag tog bort anropet till GameManager.NotifyZombieScream() (operatörsraden L13 "Hear that scream"), för att jag inte ville ha raden Hear that scream. Själva skriket ovan spelas som förut.
        }
    }

    void MakeSound(AudioClip sound)
    {
        if (source && sound)
        {
            if (Time.time < nextPlayTime) return;

            nextPlayTime = Time.time + playCooldown;

            StopAllCoroutines();
            source.Stop();
            if (gameObject.CompareTag("Civillian"))
            {
                source.PlayOneShot(sound, SettingsManager.monsterSoundLevel);
            }
            else
            {
                source.PlayOneShot(sound, SettingsManager.monsterSoundLevel);
            }

            StartCoroutine(StopSoundAfterDelay(soundDuration));
        }
    }

    private IEnumerator StopSoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        source.Stop();
    }

    private IEnumerator StartIdleSounds(float delay)
    {
        yield return new WaitForSeconds(delay);
        // 2026-10-28 (Alfred): utan idle-ljud (som FastEnemy hade) blev det IndexOutOfRange här, så då hoppar jag över det.
        if (idleSounds == null || idleSounds.Length == 0) yield break;
        source.resource = idleSounds[0];
        source.Play();
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
