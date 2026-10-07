using UnityEngine;

public class KillafterTime : MonoBehaviour
{

    [SerializeField] float lifeTime = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(this.gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
