using TMPro;
using UnityEngine;

public class LaneJumper : MonoBehaviour
{
    [SerializeField] float speed = 100.0f;
    [SerializeField] float enemyDamage = -200.0f;
    [SerializeField] float enemyReward = 100.0f;
    [SerializeField] float enemyHealth = 2.0f;

    private GameManager gameManager;
    private Rigidbody2D rb;
    private AudioSource source;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

    [SerializeField] AudioClip[] enemyGroans;

    [SerializeField] AudioClip spawnSound;
    [SerializeField] AudioClip deathSound;
    [SerializeField] AudioClip attackSound;
    [SerializeField] AudioClip muffledSound;
    [SerializeField] AudioClip escapeSound;


    [SerializeField] int maxTimeBetweenJumps = 5;
    [SerializeField] int minTimeBetweenJumps = 2;

    private float jumpTimeStamp;
    private float timeToNextJump;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    /*void Start()
    {
        jumpTimeStamp = Time.time;
        timeToNextJump = 0;

    // Update is called once per frame
    /*void Update()
    {
        if (timeStamp + timeToNextSpawn <= Time.time && canJump)
        {
            timeStamp = Time.time;
            for (int i = 0; i < spawnAmount; i++)
            {
                SpawnEnemy(ChooseEnemy(), ChooseLane());
            }
            timeToNextSpawn = GetSpawnTime();
        }*/

        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();
        SetSoundPosition(transform.position.x);

        spriteRenderer = GetComponent<SpriteRenderer>();

        // Make spawn sound
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
        SetSoundPosition(transform.position.x);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Bullet" && enemyHealth == 1)
        {
            Destroy(collision.gameObject);
            gameManager.ChangePoints(enemyReward);
            source.Stop();
            MakeSound(deathSound, 0.5f);
            source.loop = false;
            boxCollider.enabled = false;
            spriteRenderer.enabled = false;
            Destroy(this.gameObject, 1.5f);
        }
        if (collision.gameObject.tag == "Bullet" && enemyHealth > 1)
        {
            enemyHealth--;
            Destroy(collision.gameObject);
            //make enemy hit sound
        }
        if (collision.gameObject.tag == "Killbox")
        {
            gameManager.ChangePoints(enemyDamage);
            //play escape sound
            Destroy(this.gameObject, 1.5f);
        }
        if (collision.gameObject.tag == "LastChance")
        {
            // rb.AddForce(new Vector2(0, speed));

        }
        if (collision.gameObject.tag == "SoundTrigger")
        {
            // MakeSound(attackSound)
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
