using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.VirtualTexturing;

public class Player : MonoBehaviour
{
    [SerializeField] InputAction moveLeft;
    [SerializeField] InputAction moveRight;
    [SerializeField] InputAction shoot;
    [SerializeField] InputAction reload;
    [SerializeField] InputAction chargeSpecial;

    [SerializeField] GameObject[] playerLanes;

    [SerializeField] GameObject bullet;
    [SerializeField] GameObject archingBullet;
    [SerializeField] float weaponCooldown = 0.3f;
    [SerializeField] float reloadTime = 1f;
    [SerializeField] int maxAmmo = 6;
    [SerializeField] float equipWeaponCooldown;
    //[SerializeField] float specialShotTime; controlled in inputAction
    [SerializeField] private float moveCooldown = 0.5f;
    [SerializeField] float bulletSpawnOffset;

    private float weaponCooldownTimeStamp;
    private float reloadTimeStamp;
    private bool isReloading;
    private int ammo;
    public float equippedWeapon = 1;
    private float equippedWeaponTimeStamp;
    private int currentPos = 1;
    private float moveTimeStamp;
    private float shootPressTimestamp;
    private bool specialShotReady;

    [Header("Sound")]
    [SerializeField] AudioClip playerShoot;
    [SerializeField] AudioClip[] playerMove;
    [SerializeField] AudioClip changeWeapon;
    [SerializeField] AudioClip reloadSound;

    private AudioSource source;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveLeft.Enable();
        moveRight.Enable();
        shoot.Enable();
        reload.Enable();
        chargeSpecial.Enable();

        weaponCooldownTimeStamp = Time.time;
        moveTimeStamp = Time.time;

        source = GetComponent<AudioSource>();
        ammo = maxAmmo;
    }

    // Update is called once per frame
    void Update()
    {
        if (moveLeft.IsPressed() && moveTimeStamp + moveCooldown <= Time.time)
        {
            if (transform.position == playerLanes[0].transform.position)
            {
                // MakeSound(wallHit);
            }
            else
            {
                transform.position = playerLanes[currentPos-1].transform.position;
                currentPos--;
                MakeSound(playerMove[Random.Range(0, playerMove.Length)], 1);
            }
            
            moveTimeStamp = Time.time;
        }
        if (moveRight.IsPressed() && moveTimeStamp + moveCooldown <= Time.time)
        {
            if (transform.position == playerLanes[2].transform.position)
            {
                // MakeSound(wallHit);
            }
            else
            {
                transform.position = playerLanes[currentPos+1].transform.position;
                currentPos++;
                MakeSound(playerMove[Random.Range(0, playerMove.Length)], 1);
            }
            
            moveTimeStamp = Time.time;
        }
        /*if (weaponSwap.IsPressed() && equippedWeaponTimeStamp + equipWeaponCooldown <= Time.time)
        {
            SwapWeapon();
            equippedWeaponTimeStamp = Time.time;
        }*/
        if (reload.IsPressed() && !isReloading && ammo != maxAmmo)
        {
            reloadTimeStamp = Time.time;
            isReloading = true;
            MakeSound(reloadSound, 1);
            reload.Disable();
            Debug.Log("reloading");
        }
        if (isReloading)
        {
            if (reloadTimeStamp + reloadTime <= Time.time)
            {
                ammo = maxAmmo;
                isReloading = false;
                reload.Enable();
                Debug.Log("reloaded");
            }
        }
        /*if (shoot.IsPressed() && weaponCooldownTimeStamp + weaponCooldown <= Time.time && !isReloading && ammo > 0)
        {
            LogSystem log = new LogSystem();
            log.SaveEvent("#Shot");
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
        }*/

        if (shoot.WasPressedThisFrame() && weaponCooldownTimeStamp + weaponCooldown <= Time.time && !isReloading)
        {
            Debug.Log("shoot was pressed");
            if (ammo > 0)
            {
                ammo--;
                weaponCooldownTimeStamp = Time.time;
                Debug.Log("shot");
                MakeSound(playerShoot, 0.1f);
                Instantiate(bullet, this.transform.position + new Vector3(0, bulletSpawnOffset), this.transform.rotation);
            }
            else
            {
                //play outOfAmmo sound
                Debug.Log("play outOfAmmo sound");
                //shoot.Reset();
            }
        }

        if (chargeSpecial.WasPressedThisFrame())
        {
            //make chargeSpecialSound
            Debug.Log("started charging");
            moveLeft.Disable();
            moveRight.Disable();
            shoot.Disable();
            reload.Disable();
        }

        if (chargeSpecial.WasReleasedThisFrame())
        {
            moveLeft.Enable();
            moveRight.Enable();
            shoot.Enable();
            reload.Enable();

            if (specialShotReady)
            {
                specialShotReady = false;
                Debug.Log("special shot");
                Instantiate(archingBullet, this.transform.position + new Vector3(0, bulletSpawnOffset), this.transform.rotation);
            }
            else
            {
                Debug.Log("stopped charging");
                source.Stop();
            }
        }

        if (chargeSpecial.WasPerformedThisFrame() && !isReloading && ammo > 0) // 1s
        {
            //play specialShotReady sound
            Debug.Log("play specialShotReady sound");
            specialShotReady = true;
        }
    }

    /*void SwapWeapon()
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
    }*/

    void MakeSound(AudioClip sound, float vol)
    {
        source.PlayOneShot(sound, vol);
    }
}
