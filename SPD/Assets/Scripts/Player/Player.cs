using System.Collections;
using System.Linq;
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
    [SerializeField] private float moveCooldown = 0.5f;
    [SerializeField] float bulletSpawnOffset;

    private float weaponCooldownTimeStamp;
    private float reloadTimeStamp;
    private bool isReloading;
    private int ammo;
    public float equippedWeapon = 1;
    private int currentPos = 1;
    private float moveTimeStamp;
    private bool specialShotReady;

    [Header("Sound")]
    [SerializeField] AudioClip playerShoot;
    [SerializeField] AudioClip[] playerMove;
    [SerializeField] AudioClip chargeWeapon;
    [SerializeField] AudioClip firedChargedWeapon;
    [SerializeField] AudioClip releaseChargingWeapon;
    [SerializeField] AudioClip reloadSound;
    [SerializeField] AudioClip wallHit;
    [SerializeField] AudioClip noAmmoSound;

    [Header("Sound Duration")]
    [SerializeField] private float soundDuration = 1f;
    [SerializeField] private float playCooldown = 0.2f;
    private float nextPlayTime;

    private AudioSource[] sources;


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

        sources = GetComponents<AudioSource>();
        ammo = maxAmmo;
    }

    // Update is called once per frame
    void Update()
    {
        if (moveLeft.IsPressed() && moveTimeStamp + moveCooldown <= Time.time)
        {
            if (transform.position == playerLanes[0].transform.position)
            {
                MakeSound(wallHit);
            }
            else
            {
                transform.position = playerLanes[currentPos-1].transform.position;
                currentPos--;
                MakeSound(playerMove[Random.Range(0, playerMove.Length)]);
            }
            
            moveTimeStamp = Time.time;
        }
        if (moveRight.IsPressed() && moveTimeStamp + moveCooldown <= Time.time)
        {
            if (transform.position == playerLanes[2].transform.position)
            {
                MakeSound(wallHit);
            }
            else
            {
                transform.position = playerLanes[currentPos+1].transform.position;
                currentPos++;
                MakeSound(playerMove[Random.Range(0, playerMove.Length)]);
            }
            
            moveTimeStamp = Time.time;
        }
 
        if (reload.IsPressed() && !isReloading && ammo != maxAmmo)
        {
            reloadTimeStamp = Time.time;
            isReloading = true;
            MakeSound(reloadSound);
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

        if (shoot.WasPressedThisFrame() && weaponCooldownTimeStamp + weaponCooldown <= Time.time && !isReloading)
        {
            Debug.Log("shoot was pressed");
            if (ammo > 0)
            {
                ammo--;
                weaponCooldownTimeStamp = Time.time;
                Debug.Log("shot");
                MakeSound(playerShoot);
                Instantiate(bullet, this.transform.position + new Vector3(0, bulletSpawnOffset), this.transform.rotation);
            }
            else
            {
                //play outOfAmmo sound
                Debug.Log("play outOfAmmo sound");
                MakeSound(noAmmoSound);
            }
        }

        if (chargeSpecial.WasPressedThisFrame())
        {
            //make chargeSpecialSound
            MakeSound(chargeWeapon);
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
                MakeSound(firedChargedWeapon);
                specialShotReady = false;
                Debug.Log("special shot");
                Instantiate(archingBullet, this.transform.position + new Vector3(0, bulletSpawnOffset), this.transform.rotation);
            }
            else
            {
                Debug.Log("stopped charging");
                sources[0].Stop();
                MakeSound(releaseChargingWeapon);
            }
        }

        if (chargeSpecial.WasPerformedThisFrame() && !isReloading && ammo > 0) // 1s
        {
            //play specialShotReady sound
            Debug.Log("play specialShotReady sound");
            specialShotReady = true;
        }
    }

    void MakeSound(AudioClip sound)
    {
        if (sources[1] && playerMove.Contains(sound))
        {
            if (Time.time < nextPlayTime) return;

            nextPlayTime = Time.time + playCooldown;

            StopAllCoroutines();
            sources[1].Stop();
            sources[1].PlayOneShot(sound, SettingsManager.playerSoundLevel);

            StartCoroutine(StopSoundAfterDelay(soundDuration));
        }
        else
        {
            sources[0].PlayOneShot(sound, SettingsManager.playerSoundLevel);
        }
    }

    private IEnumerator StopSoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        sources[1].Stop();
    }
}
