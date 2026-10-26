using TMPro;
using UnityEngine;

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
    private bool canJump;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindObjectsOfType<GameManager>()[0];

        LogSystem log = new LogSystem();
        log.SaveEvent("#laneJumperSpawned", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());

        source = GetComponent<AudioSource>();
        SetSoundPosition(transform.position.x);

        spriteRenderer = GetComponent<SpriteRenderer>();

        // Make spawn sound
        MakeSound(spawnSound, 0.35f);

        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(new Vector2(0, -speed));

        //source.PlayOneShot(enemyGroans[UnityEngine.Random.Range(0, enemyGroans.Length)]);
        source.loop = true;

        boxCollider = GetComponent<BoxCollider2D>();

        jumpTimeStamp = Time.time;
        timeToNextJump = Random.Range(minTimeBetweenJumps, maxTimeBetweenJumps + 1);
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

            timeToNextJump = Random.Range(minTimeBetweenJumps, maxTimeBetweenJumps + 1);
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
            canJump = false;
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

    void Jump(float currentLane)
    {
        float linePicker = Random.Range(0, 2);
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
