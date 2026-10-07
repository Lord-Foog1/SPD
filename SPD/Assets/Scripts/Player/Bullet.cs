using UnityEngine;

public class Bullet : MonoBehaviour
{

    [SerializeField] bool isSpceial = false;
    [SerializeField] float bulletSpeed = 500.0f;
    [SerializeField] float lifeTime = 5.0f;
    [SerializeField] float specialBulletOffset = 1;

    [SerializeField] GameObject bullet;



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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("test");
        if (isSpceial && (collision.gameObject.tag == "Civillian" || collision.gameObject.tag == "Enemy"))
        {
            Instantiate(bullet, this.transform.position + new Vector3(0, specialBulletOffset), this.transform.rotation);
            Debug.Log("Test");
            Destroy(this.gameObject);
        }
    }
}
