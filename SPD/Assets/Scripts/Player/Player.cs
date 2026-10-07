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
    [SerializeField] float weaponCooldown = 0.3f;
    [SerializeField] float reloadTime = 1f;
    [SerializeField] int ammo = 6;

    private float weaponCooldownTimeStamp;
    private float reloadTimeStamp;
    private bool isReloading;

    [Header("Sound")]
    [SerializeField] AudioClip playerShoot;
    [SerializeField] AudioClip playerMove;
    [SerializeField] AudioClip changeWeapon;
    [SerializeField] AudioClip reload;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        middleLane.Enable();
        leftLane.Enable();
        rightLane.Enable();
        shoot.Enable();

        weaponCooldownTimeStamp = Time.time;
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
            // MakeSound(playeMove);
        }
        if (leftLane.IsPressed())
        {
            leftLane.Disable();
            transform.position = playerLanes[0].transform.position;
            middleLane.Enable();
            rightLane.Enable();
            // MakeSound(playeMove);
        }
        if (rightLane.IsPressed())
        {
            rightLane.Disable();
            transform.position = playerLanes[2].transform.position;
            middleLane.Enable();
            leftLane.Enable();
            // MakeSound(playeMove);
        }
        if (ammo == 0 && !isReloading)
        {
            reloadTimeStamp = Time.time;
            isReloading = true;
        }
        if (isReloading)
        {
            if (reloadTimeStamp + reloadTime <= reloadTime.time)
            {

            }
        }
        if (shoot.IsPressed() && weaponCooldownTimeStamp + weaponCooldown <= Time.time && !isReloading)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        Instantiate(bullet, this.transform.position, this.transform.rotation);
        weaponCooldownTimeStamp = Time.time;
        // MakeSound(playerShoot);
        ammo--;
    }

    void Reload()
    {
        
    }

    void MakeSound(AudioClip sound)
    {

    }
}
