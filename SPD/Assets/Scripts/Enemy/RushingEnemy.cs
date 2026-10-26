using System.Collections;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class RushingEnemy : MonoBehaviour
{
    [SerializeField] float chargeTime = 10;
    [Tooltip("Amount of points you lose when hit")]
    [SerializeField] int enemyDamage = -200;
    [SerializeField] private float enemySpeed = 1000;

    [Header("Sound")]
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private AudioClip hitPlayerSound;

    [Header("Sound Duration")]
    [SerializeField] private float soundDuration = 2.0f;
    [SerializeField] private float playCooldown = 0.5f;
    private float nextPlayTime;

    private GameManager gameManager;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    
    private bool isRunning = false;
    private bool hitPlayer = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LogSystem log = new LogSystem();
        log.SaveEvent("#RushingEnemySpawned", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());

        gameManager = FindObjectOfType<GameManager>();
        rb = GetComponent<Rigidbody2D>();
        
        audioSource = GetComponent<AudioSource>();
        SetSoundPosition(transform.position.x);
        
        MakeSound(chargeSound);
    }

    // Update is called once per frame
    void Update()
    {
        if (chargeTime <= 0 && !isRunning)
        {
            isRunning = true;
            rb.AddForce(new Vector2(0, -enemySpeed));
        }
        else
        {
            chargeTime -= Time.deltaTime;
        }
    }
    
    public void SetSoundPosition(float lane)
    {
        if (lane == 0)
        {
            audioSource.panStereo = 0;
        }
        else if (lane > 0)
        {
            audioSource.panStereo = 1;
        }
        else if (lane < 0)
        {
            audioSource.panStereo = -1;
        }
    }

    void MakeSound(AudioClip sound)
    {
        if (audioSource && sound)
        {
            if (Time.time < nextPlayTime) return;

            nextPlayTime = Time.time + playCooldown;

            StopAllCoroutines();
            audioSource.Stop();
            audioSource.PlayOneShot(sound, SettingsManager.monsterSoundLevel);

            StartCoroutine(StopSoundAfterDelay(soundDuration));
        }
    }

    private IEnumerator StopSoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.Stop();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            gameManager.ChangePoints(enemyDamage);

            hitPlayer = true;
            MakeSound(hitPlayerSound);
        }

        if((collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Civillian")) && isRunning)
        {
            // source.Stop();
            // MakeSound(deathSound);
            // source.loop = false;
            // boxCollider.enabled = false;
            // spriteRenderer.enabled = false;

            Debug.Log("Crushed by rushing enemy");

            Destroy(collision.gameObject);
        }

        if (collision.CompareTag("Killbox"))
        {
            Destroy(gameObject, 2);
        }
    }
}
