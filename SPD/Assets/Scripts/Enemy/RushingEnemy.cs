using UnityEngine;

public class RushingEnemy : MonoBehaviour
{
    [SerializeField] float chargeTime = 10;
    [Tooltip("Amount of points you lose when hit")]
    [SerializeField] float enemyDamage = -200.0f;
    [SerializeField] private float enemySpeed = 1000;

    [Header("Sound")]
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private AudioClip hitPlayerSound;
    
    private GameManager gameManager;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    
    private bool isRunning = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        rb = GetComponent<Rigidbody2D>();
        
        audioSource = GetComponent<AudioSource>();
        SetSoundPosition(transform.position.x);
        
        // MakeSound(chargeSound, 1);
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

    public void MakeSound(AudioClip sound, float vol)
    {
        audioSource.PlayOneShot(sound, vol);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            gameManager.ChangePoints(enemyDamage);
            // MakeSound(hitPlayerSound, 1)
        }

        if (collision.CompareTag("Killbox"))
        {
            Destroy(gameObject);
        }
    }
}
