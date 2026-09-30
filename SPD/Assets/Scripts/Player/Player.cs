using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.VirtualTexturing;

public class Player : MonoBehaviour
{
    [SerializeField] InputAction middleLane;
    [SerializeField] InputAction leftLane;
    [SerializeField] InputAction rightLane;
    [SerializeField] InputAction shoot;
    [SerializeField] InputAction weaponSwap;

    [SerializeField] GameObject[] playerLanes;

    [SerializeField] GameObject bullet;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        middleLane.Enable();
        leftLane.Enable();
        rightLane.Enable();
        shoot.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (middleLane.IsPressed())
        {
            middleLane.Disable();
            transform.position = playerLanes[1].transform.position;
            leftLane.Enable();
            rightLane.Enable();
        }
        if (leftLane.IsPressed())
        {
            leftLane.Disable();
            transform.position = playerLanes[0].transform.position;
            middleLane.Enable();
            rightLane.Enable();
        }
        if (rightLane.IsPressed())
        {
            rightLane.Disable();
            transform.position = playerLanes[2].transform.position;
            middleLane.Enable();
            leftLane.Enable();
        }
        if (shoot.IsPressed())
        {
            //shoot.Disable();
            Instantiate(bullet, this.transform);
            
        }
    }
}
