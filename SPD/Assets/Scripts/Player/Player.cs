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
    [SerializeField] GameObject archingBullet;
    [SerializeField] float weaponCooldown = 0.3f;
    [SerializeField] float reloadTime = 1f;
    [SerializeField] int maxAmmo = 6;
    [SerializeField] float equipWeaponCooldown;

    private float weaponCooldownTimeStamp;
    private float reloadTimeStamp;
    private bool isReloading;
    private int ammo;
    public float equippedWeapon = 1;
    private float equippedWeaponTimeStamp;

    [Header("Sound")]
    [SerializeField] AudioClip playerShoot;
    [SerializeField] AudioClip[] playerMove;
    [SerializeField] AudioClip changeWeapon;
    [SerializeField] AudioClip reload;

    private AudioSource source;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        middleLane.Enable();
        leftLane.Enable();
        rightLane.Enable();
        shoot.Enable();
        weaponSwap.Enable();

        weaponCooldownTimeStamp = Time.time;

        source = GetComponent<AudioSource>();
        ammo = maxAmmo;
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
            MakeSound(playerMove[Random.Range(0, playerMove.Length)], 1);
        }
        if (leftLane.IsPressed())
        {
            leftLane.Disable();
            transform.position = playerLanes[0].transform.position;
            middleLane.Enable();
            rightLane.Enable();
            MakeSound(playerMove[Random.Range(0, playerMove.Length)], 1);
        }
        if (rightLane.IsPressed())
        {
            rightLane.Disable();
            transform.position = playerLanes[2].transform.position;
            middleLane.Enable();
            leftLane.Enable();
            MakeSound(playerMove[Random.Range(0, playerMove.Length)], 1);
        }
        if (weaponSwap.IsPressed() && equippedWeaponTimeStamp + equipWeaponCooldown <= Time.time)
        {
            SwapWeapon();
            equippedWeaponTimeStamp = Time.time;
        }
        if (ammo <= 0 && !isReloading)
        {
            reloadTimeStamp = Time.time;
            isReloading = true;
            MakeSound(reload, 1);
        }
        if (isReloading)
        {
            if (reloadTimeStamp + reloadTime <= Time.time)
            {
                ammo = maxAmmo;
                isReloading = false;
            }
        }
        if (shoot.IsPressed() && weaponCooldownTimeStamp + weaponCooldown <= Time.time && !isReloading && ammo > 0)
        {
            weaponCooldownTimeStamp = Time.time;
            MakeSound(playerShoot, 0.1f);
            ammo--;
            if (equippedWeapon == 1)
            {
                Instantiate(bullet, this.transform.position, this.transform.rotation);
            }
            if (equippedWeapon == 2)
            {
                Instantiate(archingBullet, this.transform.position, this.transform.rotation);
            }
        }
    }

    void SwapWeapon()
    {
        if (equippedWeapon == 1)
        {
            equippedWeapon = 2;
            Debug.Log("swapped to 2nd weapon");
            return;
        }
        if (equippedWeapon == 2)
        {
            equippedWeapon = 1;
            Debug.Log("swapped to 1st weapon");
            return;
        }
    }

    void MakeSound(AudioClip sound, float vol)
    {
        source.PlayOneShot(sound, vol);
    }
}
