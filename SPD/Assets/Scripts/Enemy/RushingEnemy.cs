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
    
    private GameManager gameManager;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    
    private bool isRunning = false;
    private bool hitPlayer = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        rb = GetComponent<Rigidbody2D>();
        
        audioSource = GetComponent<AudioSource>();
        SetSoundPosition(transform.position.x);
        
        // MakeSound(chargeSound);
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

    public void MakeSound(AudioClip sound)
    {
        audioSource.PlayOneShot(sound, SettingsManager.monsterSoundLevel);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            gameManager.ChangePoints(enemyDamage);

            if (gameObject.CompareTag("Enemy"))
            {
                LogSystem log = new LogSystem();
                log.SaveEvent("#HitByRushingEnemy", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());
            }

            hitPlayer = true;
            // MakeSound(hitPlayerSound)
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
            if (gameObject.CompareTag("Enemy") && !hitPlayer)
            {
                LogSystem log = new LogSystem();
                log.SaveEvent("#AvoidedRushingEnemy", gameManager.GetTimeLeft(), gameManager.GetPoints().ToString(), transform.position.x.ToString());
            }

            Destroy(gameObject, 2);
        }
    }
}
