using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class LaneJumper : MonoBehaviour
{
    [SerializeField] float speed = 100.0f;
    [SerializeField] int enemyDamage = -200;
    [SerializeField] int enemyReward = 100;
    [SerializeField] float enemyHealth = 2.0f;

    private GameManager gameManager;
    private Rigidbody2D rb;
    private AudioSource source;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private AudioLowPassFilter filter;

    [SerializeField] AudioClip[] idleSounds;

    [SerializeField] AudioClip spawnSound;
    [SerializeField] AudioClip deathSound;
    [SerializeField] AudioClip attackSound;
    [SerializeField] AudioClip muffledSound;
    [SerializeField] AudioClip escapeSound;


    [SerializeField] int maxTimeBetweenJumps = 5;
    [SerializeField] int minTimeBetweenJumps = 2;

    private float jumpTimeStamp;
    private float timeToNextJump;
    private bool canJump;

    [Header("Sound Duration")]
    [SerializeField] private float soundDuration = 2.0f;
    [SerializeField] private float playCooldown = 0.5f;
    private float nextPlayTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindObjectsOfType<GameManager>()[0];

        LogSystem log = new LogSystem();
        log.SaveEvent("#laneJumperSpawned", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());

        source = GetComponent<AudioSource>();
        SetSoundPosition(transform.position.x);
        filter = GetComponent<AudioLowPassFilter>();

        filter.enabled = false;

        spriteRenderer = GetComponent<SpriteRenderer>();

        // Make spawn sound
        if (spawnSound)
        {
            MakeSound(spawnSound);

            source.loop = true;

            StartCoroutine(StartIdleSounds(1.6f));
        }

        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(new Vector2(0, -speed));

        boxCollider = GetComponent<BoxCollider2D>();

        jumpTimeStamp = Time.time;
        timeToNextJump = UnityEngine.Random.Range(minTimeBetweenJumps, maxTimeBetweenJumps + 1);
        canJump = true;
    }

    // Update is called once per frame
    void Update()
    {
        SetSoundPosition(transform.position.x);

        if (jumpTimeStamp + timeToNextJump <= Time.time && canJump)
        {
            jumpTimeStamp = Time.time;

            Jump(transform.position.x);
            Debug.Log("Jumped lanes");

            timeToNextJump = UnityEngine.Random.Range(minTimeBetweenJumps, maxTimeBetweenJumps + 1);
        }

        Debug.DrawRay(new Vector2(transform.position.x, transform.position.y - 0.6f), new Vector2(0, -3.5f));
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - 0.6f), new Vector2(0, -3.5f));

        if (hit && (hit.collider.gameObject.CompareTag("Civillian") || hit.collider.gameObject.CompareTag("Enemy")))
        {
            filter.enabled = true;
            Debug.Log("Hit");
        }
        else
        {
            filter.enabled = false;
            Debug.Log("No Hit");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Bullet" && enemyHealth == 1)
        {
            canJump = false;
            Destroy(collision.gameObject);
            gameManager.ChangePoints(enemyReward);

            if (gameObject.CompareTag("Enemy"))
            {
                LogSystem log = new LogSystem();
                log.SaveEvent("#LaneJumperKilled", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());
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

            jumpTimeStamp = Time.time;

            Jump(transform.position.x);
            Debug.Log("Jumped lanes");

            timeToNextJump = UnityEngine.Random.Range(minTimeBetweenJumps, maxTimeBetweenJumps + 1);
        }
        if (collision.gameObject.tag == "Killbox")
        {
            canJump = false;
            gameManager.ChangePoints(enemyDamage);

            MakeSound(escapeSound);
            Destroy(this.gameObject, 2.5f);
        }
        if (collision.gameObject.tag == "LastChance")
        {
            canJump = false;
            // rb.AddForce(new Vector2(0, speed));

        }
        if (collision.gameObject.tag == "SoundTrigger")
        {
            canJump = false;
            MakeSound(attackSound);
        }
    }

    void Jump(float currentLane)
    {
        float linePicker = UnityEngine.Random.Range(0, 2);
        if (currentLane == 0) //middle
        {
            if (linePicker < 0.5)
                transform.position = new Vector3(-5, transform.position.y, transform.position.z);
            else
                transform.position = new Vector3(5, transform.position.y, transform.position.z);
        }
        else if (currentLane > 0) //right
        {
            if (linePicker < 0.5)
                transform.position = new Vector3(-5, transform.position.y, transform.position.z);
            else
                transform.position = new Vector3(0, transform.position.y, transform.position.z);
        }
        else if (currentLane < 0) //left
        {
            if (linePicker < 0.5)
                transform.position = new Vector3(0, transform.position.y, transform.position.z);
            else
                transform.position = new Vector3(5, transform.position.y, transform.position.z);
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
            
            source.PlayOneShot(sound, SettingsManager.monsterSoundLevel);

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
