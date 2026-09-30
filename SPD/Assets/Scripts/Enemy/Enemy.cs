using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Tooltip("Speed of the enemy")]
    [SerializeField] float speed = 100.0f;

    [Tooltip("The amount of points you lose")]
    [SerializeField] float EnemyDamage = 100.0f;

    [Tooltip("The amount of points you gain when killing")]
    [SerializeField] float enemyReward = 100.0f;

    [Tooltip("Enemy health")]
    [SerializeField] float enemyHealth = 2.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Make sound

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
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
            /*
             * Code to add points
             */
            Destroy(this.gameObject);
        }
    }
}
