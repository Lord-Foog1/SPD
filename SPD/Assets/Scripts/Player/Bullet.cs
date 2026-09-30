using UnityEngine;

public class Bullet : MonoBehaviour
{

    [SerializeField] bool isSpceial = false;
    [SerializeField] float bulletSpeed = 500.0f;
    [SerializeField] float lifeTime = 5.0f;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.AddForce(new Vector2(0, bulletSpeed));

        Destroy(this.gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
