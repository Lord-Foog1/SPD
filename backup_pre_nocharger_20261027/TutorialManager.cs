using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Audio-only tutorial. Built our own way (scripted steps, recorded narrator lines only),
// but every game sound, rule and timing mirrors Level 1/2 (Enemy.cs, RushingEnemy.cs,
// Player.cs, Bullet.cs and the level prefabs/scenes), so the player hears the same game.
public class TutorialManager : MonoBehaviour
{
    private enum Kind { Zombie, Civilian, Rusher }
    private enum Act { None, Ctrl, Move, Wall, Shot, Click, ShotIgnored, ReloadStart, ReloadDone, ChargeStart, LobFired, LobEarly }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource guideSource;
    [Tooltip("Multiply the narrator by the menu's Operator volume slider (SettingsManager.operatorSoundLevel).")]
    [SerializeField] private bool guideUsesOperatorVolume = true;

    [Header("Repeat Help")]
    [SerializeField] private AudioClip[] repeatHelp;

    [Header("Introduction")]
    [SerializeField] private AudioClip[] introduction;

    [Header("Right Calibration")]
    [SerializeField] private AudioClip[] askRight;
    [SerializeField] private AudioClip[] wrongRight;
    [SerializeField] private AudioClip[] correctRight;

    [Header("Left Calibration")]
    [SerializeField] private AudioClip[] askLeft;
    [SerializeField] private AudioClip[] wrongLeft;
    [SerializeField] private AudioClip[] correctLeft;

    [Header("Step 4: Dummy in front")]
    [SerializeField] private AudioClip[] explainDummy;
    [SerializeField] private AudioClip[] dummyHit;

    [Header("Step 5: Zombie right, civilian left")]
    [SerializeField] private AudioClip[] askZombieRight;
    [SerializeField] private AudioClip[] zombieRightHit;

    [Header("Step 6: Real zombie, level timing")]
    [SerializeField] private AudioClip[] askRealZombie;
    [SerializeField] private AudioClip[] explainSilence;        // NEW (record): before the zombie spawns
    [SerializeField] private AudioClip[] explainScream;         // NEW (record): when the first zombie screams
    [SerializeField] private AudioClip[] realZombieHit;
    [SerializeField] private AudioClip[] realZombieReachedYou;

    [Header("Step 7: Ammo and reload")]
    [SerializeField] private AudioClip[] explainAmmo;           // NEW (record): six shots, empty gun clicks
    [SerializeField] private AudioClip[] askReload;             // NEW (record): press R to reload
    [SerializeField] private AudioClip[] reloadReminder;        // "Remember to reload." (also in the mini-wave on an empty click)
    [SerializeField] private AudioClip[] reloadDone;            // "All right, good to go."

    [Header("Step 8: Lob")]
    [SerializeField] private AudioClip[] askLobber;
    [SerializeField] private AudioClip[] lobHowTo;              // NEW (record): hold until the charge sound fades, then let go
    [SerializeField] private AudioClip[] lobberHit;
    [SerializeField] private AudioClip[] tappedCivilian;
    [SerializeField] private AudioClip[] holdLonger;            // re-record: the old take says "wait for the click"
    [SerializeField] private float chargeHoldSeconds = 1.5f;    // Level1/2 scene override: Hold(duration=1.5)

    [Header("Step 9: Civilian walking past")]
    [SerializeField] private AudioClip[] explainCivilianWalk;   // NEW (record)
    [SerializeField] private AudioClip[] civilianPassed;

    [Header("Step 10: Rusher")]
    [SerializeField] private AudioClip[] explainRusher;         // NEW (record)
    [SerializeField] private AudioClip[] rusherDodged;          // NEW (record)
    [SerializeField] private AudioClip[] rusherHit;             // NEW (record)

    [Header("Step 11: Mini-wave (Level 1 pace)")]
    [SerializeField] private AudioClip[] explainWave;
    [SerializeField] private AudioClip[] explainScore;          // NEW (record)
    [SerializeField] private AudioClip[] explainRound;          // NEW (record)
    [SerializeField] private AudioClip[] waveMultiple;
    [SerializeField] private AudioClip[] waveCivilianShot;
    [SerializeField] private AudioClip[] waveTimeUp;
    [SerializeField] private float waveSeconds = 30f;

    [Header("Reactions")]
    [SerializeField] private AudioClip[] hintGoLeft;
    [SerializeField] private AudioClip[] hintGoRight;
    [SerializeField] private AudioClip[] hintShootHere;
    [SerializeField] private AudioClip[] hintLobHere;
    [SerializeField] private AudioClip[] wrongWay;
    [SerializeField] private AudioClip[] hitWall;
    [SerializeField] private AudioClip[] nearCivilian;
    [SerializeField] private AudioClip[] missedEmptyLane;
    [SerializeField] private AudioClip[] shotCivilian;

    [Header("Level sounds (synced from the level prefabs by 'Assign default tutorial sounds')")]
    [SerializeField] private AudioClip zombieSpawn;
    [SerializeField] private AudioClip zombieWalkLoop;
    [SerializeField] private AudioClip zombieScream;
    [SerializeField] private AudioClip zombieDeath;
    [SerializeField] private AudioClip zombieEscape;
    [SerializeField] private AudioClip civilianSpawn;
    [SerializeField] private AudioClip civilianWalkLoop;
    [SerializeField] private AudioClip civilianHit;
    [SerializeField] private AudioClip civilianEscape;          // empty in Civillian.prefab today (silent, like the levels)
    [SerializeField] private AudioClip rusherCharge;
    [SerializeField] private AudioClip rusherHitPlayer;
    [SerializeField] private AudioClip gunShot;
    [SerializeField] private AudioClip gunEmptyClick;
    [SerializeField] private AudioClip gunReload;
    [SerializeField] private AudioClip lobCharge;
    [SerializeField] private AudioClip lobFire;
    [SerializeField] private AudioClip lobRelease;
    [SerializeField] private AudioClip wallThud;
    [SerializeField] private AudioClip footstep;
    [SerializeField] private AudioClip fireAmbience;

    [Header("Ending")]
    [SerializeField] private AudioClip[] tutorialComplete;
    [SerializeField] private string nextScene = "Level1";

    [Header("Extras (optional)")]
    // Optional narrator lines. An empty slot is skipped silently (no delay, no warning).
    [SerializeField] private AudioClip[] explainLanes;          // after correctRight, before Step 4
    [SerializeField] private AudioClip[] explainSoundDirection; // start of Step 5, before askZombieRight
    [SerializeField] private AudioClip[] explainLob;            // Step 8, before askLobber
    [SerializeField] private AudioClip[] inRightLane;           // first arrival in the target lane per step (non-lob steps)
    [SerializeField] private AudioClip[] tryAgain;              // after missedEmptyLane
    [SerializeField] private AudioClip[] praiseShort;           // after zombieRightHit and realZombieHit
    [SerializeField] private AudioClip[] lobKeepInMind;         // after lobberHit
    [SerializeField] private AudioClip[] patience;              // second idle repeat in a row during a shooting step
    [SerializeField] private AudioClip[] closing;               // after tutorialComplete, before the next scene

    [Header("Timing")]
    [SerializeField] private float idleRepeatSeconds = 8f;
    [SerializeField] private int maxAttemptsNewSteps = 3;       // new steps move on after this many failed tries

    // ---- Level values (Level1/2 scenes, prefabs, Enemy.cs, RushingEnemy.cs, Player.cs, Bullet.cs) ----
    private const float LaneX = 5f;              // EnemyLane/PlayerLane x = -5 / 0 / +5
    private const float SpawnY = 4f;             // EnemyLane y
    private const float PlayerY = -4f;           // PlayerLane y
    private const float WalkSpeed = 1f;          // speed 50 force * 0.02 fixed step = 1 unit/s
    private const float IdleStartAge = 1.6f;     // Enemy.StartIdleSounds(1.6f)
    private const float MobSoundDuration = 1.5f; // StandardEnemy/Civillian soundDuration
    private const float MobPlayCooldown = 0.5f;  // Enemy playCooldown
    private const float ScreamAge = 5.3f;        // enters SoundTrigger (y = -2.3)
    private const float KillBoxAge = 9.7f;       // reaches KillBox (y = -6.69)
    private const float KillBoxY = SpawnY - WalkSpeed * KillBoxAge;
    private const float DestroyDelay = 1.5f;     // Destroy(gameObject, 1.5f)
    private const float MuffleRay = 3.5f;        // Enemy.Update raycast length
    private const float LowPassCurveValue = 0.09049568f; // StandardEnemy/Civillian lowpassLevelCustomCurve
    private const float RusherChargeTime = 10f;  // RushingEnemy chargeTime
    private const float RusherSpeed = 30f;       // enemySpeed 1500 * 0.02
    private const float RusherHitAge = 10.23f;   // reaches the player's collider
    private const float RusherSoundDuration = 15f;
    private const float BulletSpeed = 10f;       // bulletSpeed 500 * 0.02
    private const float WeaponCooldown = 0.3f;   // Player.prefab
    private const float ReloadTime = 2.5f;       // Level1/2 scene override
    private const int MaxAmmo = 6;
    private const float MoveCooldown = 0.1f;
    private const float StepSoundDuration = 2f;  // Player.prefab soundDuration (footstep source)
    private const float StepPlayCooldown = 0.2f;
    private const int ZombieReward = 100, ZombieEscapePenalty = -100, CivilianShotPenalty = -100, CivilianEscapeReward = 50, RusherPenalty = -200;
    private const float DefaultOperatorLevel = 0.5f; // MainMenu Operator slider default

    // Narrator takes that contradict the level behaviour (no "click" when the lob is ready,
    // almost no "louder when closer"). They are never played, even if still assigned.
    private static readonly string[] RetiredClips =
    {
        "Testing mortar - Press dowm MK1",
        "Released to early",
        "Zombie gets loader the closer they get",
    };

    private class Mob
    {
        public Kind kind;
        public int lane;
        public float y;
        public bool walking;
        public float age;
        public bool alive = true;
        public bool screamed;
        public bool running;
        public bool hitChecked;
        public float idleAt = -1f;
        public float stopAt = -1f;
        public float destroyAt = -1f;
        public float nextPlayTime;
        public AudioSource source;
        public AudioLowPassFilter filter;
    }

    private class Shot
    {
        public Mob victim;
        public float hitTime;
        public bool done;
        public Mob killed;
    }

    [System.Serializable]
    private class StepLog
    {
        public string step;
        public float seconds;
        public int attempts;
        public bool skipped;
        public int wrongPresses;
        public int wallHits;
        public int ctrlRepeats;
        public int idleRepeats;
        public int shotsFired;
        public int shotsMissed;
        public int zombiesKilled;
        public int zombiesEscaped;
        public int civiliansShot;
        public int civiliansPassed;
        public int emptyClicks;
        public int reloads;
        public int lobsFired;
        public int lobsEarly;
        public int rusherHits;
        public int score;
    }

    [System.Serializable]
    private class SessionLog
    {
        public string startedAt;
        public int playthrough;
        public bool completed;
        public float totalSeconds;
        public int totalScore;
        public List<StepLog> steps = new List<StepLog>();
    }

    [System.Serializable]
    private class TutorialLogFile
    {
        public List<SessionLog> sessions = new List<SessionLog>();
    }

    private int lane = 1; // 0 = left, 1 = middle, 2 = right
    private int previousLane = 1;
    private Vector3 origin;
    private readonly List<Mob> mobs = new List<Mob>();
    private readonly List<Shot> shots = new List<Shot>();
    private AudioSource playerSource;  // Player source 0: 3D at the player (shots, click, reload, charge, thud)
    private AudioSource stepSource;    // Player source 1: 2D footsteps
    private readonly List<AudioSource> ambienceSources = new List<AudioSource>();
    private float stepStopAt = -1f;
    private float nextStepTime;
    private float guideBaseVolume = 1f;

    private int ammo = MaxAmmo;
    private bool infiniteAmmo = true;   // steps 4-6: the operator tops you up; real ammo from step 7
    private bool reloading;
    private float reloadEnd;
    private float lastShotTime = -10f;
    private float lastMoveTime = -10f;
    private bool charging;
    private bool chargeChecked;
    private bool lobReady;
    private float chargeStart;
    private Shot lastShot;

    private bool pauseMobsWhileTalking = true;
    private int score;
    private int zombieEscapes;
    private int civilianPasses;
    private int rusherHits;
    private int civiliansShotTotal;

    private float lastInputTime;
    private bool stepDone;
    private AudioClip[] lastClips; // last guide line, for Ctrl repeat
    private readonly HashSet<string> warnedSlots = new HashSet<string>();
    private bool inRightLaneSaid; // inRightLane plays once per shooting step

    private TutorialLogFile logFile;
    private SessionLog session;
    private StepLog current = new StepLog { step = "setup" };
    private float stepStartTime;
    private float sessionStartTime;
    private string logPath;

    private IEnumerator Start()
    {
        if (guideSource == null)
        {
            Debug.LogError("TutorialManager: assign Guide Source in the Inspector.", this);
            yield break;
        }
        guideSource.loop = false;

#if UNITY_EDITOR
        FillMissingSounds();
#endif
        RetireContradictingClips();
        InitVolumes();
        CreateSources();
        StartLog();
        lane = 1;
        PlacePlayer();

        // STEP 0: How to repeat (Ctrl)
        BeginStep("0_repeatHelp");
        yield return Say(repeatHelp, nameof(repeatHelp));
        EndStep();

        // STEP 1: Introduction (the levels' fire ambience starts here)
        BeginStep("1_intro");
        StartAmbience();
        yield return PlayAudioList(introduction);
        EndStep();

        // STEP 2: Face left first, matching the recorded instructions.
        BeginStep("2_left");
        yield return PlayAudioList(askLeft);
        yield return WaitForDirection(Key.LeftArrow, Key.RightArrow, wrongLeft, askLeft);
        MoveLane(-1); // middle -> left
        yield return PlayAudioList(correctLeft);
        EndStep();

        // STEP 3: correctLeft may already say "now right";
        // leave askRight empty in that case to avoid repeating it.
        BeginStep("3_right");
        yield return PlayAudioList(askRight);
        yield return WaitForDirection(Key.RightArrow, Key.LeftArrow, wrongRight, askRight);
        MoveLane(1); // left -> middle
        yield return PlayAudioList(correctRight);
        yield return SayExtra(explainLanes);
        EndStep();

        // STEP 4: Dummy zombie in front (middle lane). Level zombie sounds, standing still.
        BeginStep("4_dummy");
        infiniteAmmo = true;
        System.Action dummy = () => { ClearMobs(); SpawnMob(Kind.Zombie, 1, SpawnY, false); };
        dummy();
        yield return Say(explainDummy, nameof(explainDummy));
        yield return ShootStep(1, explainDummy, nameof(explainDummy), false, dummy);
        ClearMobs();
        yield return Say(dummyHit, nameof(dummyHit));
        EndStep();

        // STEP 5: Zombie right, civilian left. Pan is fixed per lane, like the levels.
        BeginStep("5_direction");
        System.Action pair = () => { ClearMobs(); SpawnMob(Kind.Civilian, 0, SpawnY, false); SpawnMob(Kind.Zombie, 2, SpawnY, false); };
        pair();
        yield return SayExtra(explainSoundDirection);
        yield return Say(askZombieRight, nameof(askZombieRight));
        yield return ShootStep(2, askZombieRight, nameof(askZombieRight), false, pair);
        ClearMobs();
        yield return Say(zombieRightHit, nameof(zombieRightHit));
        yield return SayExtra(praiseShort);
        EndStep();

        // STEP 6: Real zombie walking down the middle lane on level timing:
        // groan -> walking loop -> scream at 5.3 s -> silence -> escape laugh at 9.7 s.
        BeginStep("6_realZombie");
        yield return Say(askRealZombie, nameof(askRealZombie));
        yield return SayExtra(explainSilence);
        System.Action walker = () => { ClearMobs(); SpawnMob(Kind.Zombie, 1, SpawnY, true); };
        walker();
        yield return ShootStep(1, askRealZombie, nameof(askRealZombie), false, walker, true);
        ClearMobs();
        yield return Say(realZombieHit, nameof(realZombieHit));
        yield return SayExtra(praiseShort);
        EndStep();

        // STEP 7 (new): Six shots, empty click, R to reload (2.5 s).
        BeginStep("7_ammoReload");
        yield return AmmoStep();
        EndStep();

        // STEP 8: Civilian in front, zombie behind it (muffled): lob over.
        BeginStep("8_lob");
        System.Action lobSetup = () => { ClearMobs(); SpawnMob(Kind.Civilian, 1, 1f, false); SpawnMob(Kind.Zombie, 1, 3f, false); };
        lobSetup();
        yield return SayExtra(explainLob);
        yield return Say(askLobber, nameof(askLobber));
        yield return SayExtra(lobHowTo);
        yield return ShootStep(1, askLobber, nameof(askLobber), true, lobSetup);
        ClearMobs();
        yield return Say(lobberHit, nameof(lobberHit));
        yield return SayExtra(lobKeepInMind);
        EndStep();

        // STEP 9 (new): A civilian walks down a side lane. Let it pass (+50, silent like the levels).
        BeginStep("9_civilianWalk");
        yield return CivilianWalkStep();
        EndStep();

        // STEP 10 (new): Rusher. 10 s charge sound in your lane, cannot be shot: move out.
        BeginStep("10_rusher");
        yield return RusherStep();
        EndStep();

        // STEP 11 (new): ~30 s scripted wave at Level 1 pace, real ammo, no help from the narrator.
        BeginStep("11_miniWave");
        yield return MiniWave();
        EndStep();

        // STEP 12: Tutorial completed
        BeginStep("12_end");
        yield return Say(tutorialComplete, nameof(tutorialComplete));
        yield return SayExtra(closing);
        EndStep();
        FinishLog();

        // STEP 13: Load the first level, the same way the menu's Level 1 button does.
        LoadNextScene();
    }

    // ---------------------------------------------------------------- guided shooting steps

    // One shooting exercise. Loops until a zombie in targetLane is hit.
    private IEnumerator ShootStep(int targetLane, AudioClip[] ask, string askSlot, bool lobStep, System.Action respawn, bool walking = false)
    {
        stepDone = false;
        int idleRepeats = 0; // idle repeats in a row, for the patience line
        bool screamExplained = false;
        int escapesBefore = zombieEscapes;
        inRightLaneSaid = false;
        pauseMobsWhileTalking = true;

        yield return WaitForKeysReleased();
        lastInputTime = Time.time;

        while (!stepDone)
        {
            if (walking)
            {
                if (!screamExplained && AnyScreamed())
                {
                    screamExplained = true;
                    yield return SayExtra(explainScream);
                }
                if (zombieEscapes != escapesBefore)
                {
                    // The escape laugh has already played from the zombie (like the levels).
                    escapesBefore = zombieEscapes;
                    current.attempts++;
                    yield return WaitWhileGuideOrSeconds(1.0f);
                    yield return Say(realZombieReachedYou, nameof(realZombieReachedYou));
                    respawn();
                    yield return Say(ask, askSlot);
                    yield return WaitForKeysReleased();
                    lastInputTime = Time.time;
                    continue;
                }
            }

            Act act = ReadAction();
            if (act != Act.None) idleRepeats = 0;

            switch (act)
            {
                case Act.Ctrl:
                    yield return RepeatLast();
                    lastInputTime = Time.time;
                    continue;
                case Act.Wall:
                    yield return Say(hitWall, nameof(hitWall));
                    yield return LaneHint(targetLane, lobStep);
                    yield return WaitForKeysReleased();
                    lastInputTime = Time.time;
                    continue;
                case Act.Move:
                    yield return OnMoved(targetLane, lobStep);
                    continue;
                case Act.Shot:
                case Act.LobFired:
                    yield return OnShot(lastShot, targetLane, ask, askSlot, lobStep, respawn);
                    continue;
                case Act.LobEarly:
                    if (Time.time - chargeStart >= chargeHoldSeconds)
                        yield return SayExtra(reloadReminder); // held long enough but the gun was empty/reloading
                    else
                    {
                        yield return Say(holdLonger, nameof(holdLonger));
                        yield return SayExtra(lobHowTo);
                    }
                    yield return LaneHint(targetLane, lobStep);
                    yield return WaitForKeysReleased();
                    lastInputTime = Time.time;
                    continue;
                case Act.Click:
                    yield return SayExtra(reloadReminder);
                    lastInputTime = Time.time;
                    continue;
                case Act.None:
                    break;
                default:
                    lastInputTime = Time.time;
                    break;
            }

            // Idle: repeat the instruction (not while a real zombie is walking)
            if (!walking && !charging && Time.time - lastInputTime > idleRepeatSeconds)
            {
                current.idleRepeats++;
                yield return Say(ask, askSlot);
                yield return LaneHint(targetLane, lobStep);
                idleRepeats++;
                if (idleRepeats == 2)
                    yield return SayExtra(patience);
                lastInputTime = Time.time;
            }

            yield return null;
        }
    }

    private IEnumerator OnMoved(int targetLane, bool lobStep)
    {
        int before = Mathf.Abs(previousLane - targetLane);
        int after = Mathf.Abs(lane - targetLane);

        if (lane == targetLane)
        {
            // First arrival this step: inRightLane replaces the "shoot here" hint.
            // Empty slot, lob step or later arrivals: the normal hint, as before.
            if (!lobStep && !inRightLaneSaid && HasClips(inRightLane))
            {
                inRightLaneSaid = true;
                yield return SayExtra(inRightLane);
            }
            else
                yield return LaneHint(targetLane, lobStep);
        }
        else
        {
            Mob front = FirstInLane(lane, 0);
            if (front != null && front.kind == Kind.Civilian)
                yield return Say(nearCivilian, nameof(nearCivilian));
            else if (after > before)
            {
                current.wrongPresses++;
                yield return Say(wrongWay, nameof(wrongWay));
            }
            yield return LaneHint(targetLane, lobStep);
        }

        yield return WaitForKeysReleased();
        lastInputTime = Time.time;
    }

    private IEnumerator OnShot(Shot shot, int targetLane, AudioClip[] ask, string askSlot, bool lobStep, System.Action respawn)
    {
        // Wait for the bullet to travel (10 units/s, like Bullet.cs).
        while (shot != null && !shot.done) yield return null;
        Mob hit = shot != null ? shot.killed : null;

        if (hit != null && hit.kind == Kind.Zombie && hit.lane == targetLane)
        {
            stepDone = true;
            yield return WaitWhileGuideOrSeconds(0.8f); // let the death groan be heard
            yield break;
        }

        if (hit != null && hit.kind == Kind.Civilian)
        {
            current.wrongPresses++;
            yield return WaitWhileGuideOrSeconds(0.8f);
            if (lobStep && lane == targetLane)
                yield return Say(tappedCivilian, nameof(tappedCivilian));
            else
                yield return Say(shotCivilian, nameof(shotCivilian));
            respawn();
            yield return Say(ask, askSlot);
        }
        else if (hit == null)
        {
            current.wrongPresses++;
            yield return Say(missedEmptyLane, nameof(missedEmptyLane));
            yield return SayExtra(tryAgain);
            yield return LaneHint(targetLane, lobStep);
        }
        else
        {
            // Killed a zombie in another lane (no such setup today): carry on.
            yield return LaneHint(targetLane, lobStep);
        }

        yield return WaitForKeysReleased();
        lastInputTime = Time.time;
    }

    private IEnumerator LaneHint(int targetLane, bool lobStep)
    {
        if (lane > targetLane) yield return Say(hintGoLeft, nameof(hintGoLeft));
        else if (lane < targetLane) yield return Say(hintGoRight, nameof(hintGoRight));
        else if (lobStep) yield return Say(hintLobHere, nameof(hintLobHere));
        else yield return Say(hintShootHere, nameof(hintShootHere));
    }

    // ---------------------------------------------------------------- new steps

    // Step 7: shoot until the gun clicks, then press R and wait for the reload.
    private IEnumerator AmmoStep()
    {
        infiniteAmmo = false;
        ammo = MaxAmmo;
        pauseMobsWhileTalking = true;
        System.Action target = () => { ClearMobs(); SpawnMob(Kind.Zombie, 1, SpawnY, false); };
        target();

        yield return Say(explainAmmo, nameof(explainAmmo));
        yield return LaneHint(1, false);
        yield return WaitForKeysReleased();
        lastInputTime = Time.time;

        bool clicked = false;
        int idleAfterClick = 0;
        float respawnAt = -1f;

        while (true)
        {
            if (respawnAt >= 0f && Time.time >= respawnAt)
            {
                respawnAt = -1f;
                target(); // a new dummy groans in
            }

            Act act = ReadAction();
            if (act == Act.Ctrl)
            {
                yield return RepeatLast();
                lastInputTime = Time.time;
                continue;
            }
            if (act == Act.Wall)
            {
                yield return Say(hitWall, nameof(hitWall));
                yield return LaneHint(1, false);
                lastInputTime = Time.time;
                continue;
            }
            if (act == Act.Move)
            {
                if (lane != 1) yield return LaneHint(1, false);
                lastInputTime = Time.time;
                continue;
            }
            if (act == Act.Shot)
            {
                Shot shot = lastShot;
                while (shot != null && !shot.done) yield return null;
                if (shot != null && shot.killed != null) respawnAt = Time.time + 1f;
                lastInputTime = Time.time;
                continue;
            }
            if (act == Act.Click)
            {
                clicked = true;
                yield return WaitWhileGuideOrSeconds(0.6f);
                yield return Say(askReload, nameof(askReload));
                yield return SayExtra(reloadReminder);
                lastInputTime = Time.time;
                continue;
            }
            if (act == Act.ReloadStart)
            {
                while (reloading)
                {
                    ReadAction(); // runs the reload timer; Space is ignored, like the levels
                    yield return null;
                }
                if (clicked)
                {
                    yield return Say(reloadDone, nameof(reloadDone));
                    break;
                }
                lastInputTime = Time.time;
                continue;
            }
            if (act != Act.None) lastInputTime = Time.time;

            if (!charging && Time.time - lastInputTime > idleRepeatSeconds)
            {
                current.idleRepeats++;
                if (!clicked)
                {
                    yield return Say(explainAmmo, nameof(explainAmmo));
                    yield return LaneHint(1, false);
                }
                else
                {
                    idleAfterClick++;
                    if (idleAfterClick > maxAttemptsNewSteps)
                    {
                        current.skipped = true;
                        ammo = MaxAmmo; // move on with a full gun
                        break;
                    }
                    yield return Say(askReload, nameof(askReload));
                    yield return SayExtra(reloadReminder);
                }
                lastInputTime = Time.time;
            }
            yield return null;
        }
        ClearMobs();
    }

    // Step 9: a civilian walks the whole lane (cry, then walking loop). Do not shoot it.
    private IEnumerator CivilianWalkStep()
    {
        pauseMobsWhileTalking = true;
        yield return Say(explainCivilianWalk, nameof(explainCivilianWalk));

        int tries = 0;
        while (true)
        {
            int civLane = lane != 0 ? 0 : 2;
            ClearMobs();
            int passedBefore = civilianPasses;
            int shotBefore = civiliansShotTotal;
            Mob civ = SpawnMob(Kind.Civilian, civLane, SpawnY, true);
            yield return WaitForKeysReleased();

            bool shotIt = false;
            while (civ.alive)
            {
                Act act = ReadAction();
                if (act == Act.Ctrl) yield return RepeatLast();
                else if (act == Act.Wall) yield return Say(hitWall, nameof(hitWall));
                else if (act == Act.Shot || act == Act.LobFired)
                {
                    Shot shot = lastShot;
                    while (shot != null && !shot.done) yield return null;
                }
                else if (act == Act.Click) yield return SayExtra(reloadReminder);
                if (civiliansShotTotal != shotBefore) { shotIt = true; break; }
                yield return null;
            }

            yield return WaitWhileGuideOrSeconds(0.8f);
            if (!shotIt && civilianPasses != passedBefore)
            {
                yield return Say(civilianPassed, nameof(civilianPassed));
                break;
            }

            tries++;
            current.attempts++;
            current.wrongPresses++;
            yield return Say(shotCivilian, nameof(shotCivilian));
            if (tries >= maxAttemptsNewSteps)
            {
                current.skipped = true;
                break;
            }
            yield return Say(explainCivilianWalk, nameof(explainCivilianWalk));
        }
        ClearMobs();
    }

    // Step 10: rusher in your lane (2D, hard pan), 10 s charge, rush in ~0.23 s, -200 + thud if you stay.
    private IEnumerator RusherStep()
    {
        pauseMobsWhileTalking = true;
        yield return Say(explainRusher, nameof(explainRusher));

        int tries = 0;
        while (true)
        {
            ClearMobs();
            int hitsBefore = rusherHits;
            Mob rusher = SpawnMob(Kind.Rusher, lane, SpawnY, false);
            yield return WaitForKeysReleased();

            while (!rusher.hitChecked)
            {
                Act act = ReadAction();
                if (act == Act.Ctrl) yield return RepeatLast();
                else if (act == Act.Wall) yield return Say(hitWall, nameof(hitWall));
                yield return null;
            }

            yield return WaitWhileGuideOrSeconds(1.2f);
            if (rusherHits == hitsBefore)
            {
                if (HasClips(rusherDodged)) yield return Say(rusherDodged, nameof(rusherDodged));
                else
                {
                    WarnEmpty(nameof(rusherDodged));
                    yield return SayExtra(praiseShort);
                }
                break;
            }

            tries++;
            current.attempts++;
            yield return Say(rusherHit, nameof(rusherHit));
            if (tries >= maxAttemptsNewSteps)
            {
                current.skipped = true;
                break;
            }
            yield return Say(explainRusher, nameof(explainRusher));
        }
        ClearMobs();
    }

    // Step 11: scripted wave, spawns 3-4 s apart (Level 1: 3-5 s), 2-3 alive at once,
    // two zombies stacked in one lane, a zombie behind a civilian, real ammo. Ends like a round:
    // everything is removed at once when time is up.
    private IEnumerator MiniWave()
    {
        yield return Say(explainWave, nameof(explainWave));
        yield return Say(explainScore, nameof(explainScore));
        yield return Say(explainRound, nameof(explainRound));

        float[] times = { 0f, 4f, 8f, 11f, 15f, 18f, 22f, 25f };
        Kind[] kinds = { Kind.Zombie, Kind.Zombie, Kind.Civilian, Kind.Zombie, Kind.Zombie, Kind.Zombie, Kind.Civilian, Kind.Zombie };
        int[] spawnLanes = { 0, 2, 1, 1, 0, 0, 2, 1 };

        ClearMobs();
        pauseMobsWhileTalking = false; // like a real round: nothing waits for the narrator
        int scoreBefore = score;
        int next = 0;
        bool saidMultiple = false;
        int civShotBefore = civiliansShotTotal;
        yield return WaitForKeysReleased();
        float start = Time.time;

        while (Time.time - start < waveSeconds)
        {
            float t = Time.time - start;
            while (next < times.Length && t >= times[next])
            {
                SpawnMob(kinds[next], spawnLanes[next], SpawnY, true);
                next++;
            }

            Act act = ReadAction();
            if (act == Act.Ctrl && !guideSource.isPlaying) StartCoroutine(RepeatLast());
            else if (act == Act.Click && !guideSource.isPlaying) StartCoroutine(SayExtra(reloadReminder));

            if (civiliansShotTotal != civShotBefore)
            {
                civShotBefore = civiliansShotTotal;
                if (!guideSource.isPlaying) StartCoroutine(SayExtra(waveCivilianShot));
            }
            if (!saidMultiple && AliveCount(Kind.Zombie) >= 3 && !guideSource.isPlaying)
            {
                saidMultiple = true;
                StartCoroutine(SayExtra(waveMultiple));
            }
            yield return null;
        }

        // Time over: like GameManager, every zombie and civilian is removed at once.
        ClearMobs();
        charging = false;
        lobReady = false;
        pauseMobsWhileTalking = true;
        current.score = score - scoreBefore;
        while (guideSource.isPlaying) yield return null;
        yield return Say(waveTimeUp, nameof(waveTimeUp));
    }

    // ---------------------------------------------------------------- player (mirrors Player.cs)

    private Act ReadAction()
    {
        Keyboard keyboard = Keyboard.current;

        if (reloading && Time.time >= reloadEnd)
        {
            reloading = false;
            ammo = MaxAmmo; // no "reload done" sound in the levels
            return Act.ReloadDone;
        }
        if (keyboard == null) return Act.None;

        // While charging, move/shoot/reload are disabled (like Player.cs).
        if (charging)
        {
            if (!chargeChecked && Time.time - chargeStart >= chargeHoldSeconds)
            {
                chargeChecked = true;
                lobReady = !reloading && ammo > 0; // no ready sound in the levels
            }
            if (!keyboard.upArrowKey.isPressed)
            {
                charging = false;
                if (lobReady)
                {
                    lobReady = false;
                    PlayPlayer(lobFire); // the lob uses no ammo, like the levels
                    lastShot = Fire(true);
                    current.lobsFired++;
                    return Act.LobFired;
                }
                playerSource.Stop();
                PlayPlayer(lobRelease);
                current.lobsEarly++;
                current.wrongPresses++;
                return Act.LobEarly;
            }
            return Act.None;
        }

        if (CtrlPressed())
        {
            current.ctrlRepeats++;
            return Act.Ctrl;
        }

        int dir = 0;
        if (keyboard.leftArrowKey.wasPressedThisFrame) dir = -1;
        else if (keyboard.rightArrowKey.wasPressedThisFrame) dir = 1;
        if (dir != 0 && Time.time >= lastMoveTime + MoveCooldown)
        {
            lastMoveTime = Time.time;
            int newLane = lane + dir;
            if (newLane < 0 || newLane > 2)
            {
                PlayPlayer(wallThud);
                current.wallHits++;
                current.wrongPresses++;
                return Act.Wall;
            }
            MoveLane(dir);
            return Act.Move;
        }

        if (keyboard.rKey.isPressed && !reloading && ammo != MaxAmmo)
        {
            reloading = true;
            reloadEnd = Time.time + ReloadTime;
            PlayPlayer(gunReload);
            current.reloads++;
            return Act.ReloadStart;
        }

        if (keyboard.spaceKey.wasPressedThisFrame && Time.time >= lastShotTime + WeaponCooldown)
        {
            if (reloading) return Act.ShotIgnored; // silently ignored, like the levels
            if (ammo > 0)
            {
                if (!infiniteAmmo) ammo--;
                lastShotTime = Time.time;
                PlayPlayer(gunShot);
                lastShot = Fire(false);
                return Act.Shot;
            }
            PlayPlayer(gunEmptyClick);
            current.emptyClicks++;
            return Act.Click;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            charging = true;
            chargeChecked = false;
            lobReady = false;
            chargeStart = Time.time;
            PlayPlayer(lobCharge);
            return Act.ChargeStart;
        }

        return Act.None;
    }

    private void MoveLane(int dir)
    {
        previousLane = lane;
        lane = Mathf.Clamp(lane + dir, 0, 2);
        PlacePlayer();
        PlayStep();
    }

    private void PlacePlayer()
    {
        if (playerSource != null)
            playerSource.transform.position = origin + new Vector3((lane - 1) * LaneX, PlayerY, 0f);
    }

    private void PlayPlayer(AudioClip clip)
    {
        if (clip != null && playerSource != null)
            playerSource.PlayOneShot(clip, PlayerLevel);
    }

    private void PlayStep()
    {
        if (footstep == null || stepSource == null || Time.time < nextStepTime) return;
        nextStepTime = Time.time + StepPlayCooldown;
        stepSource.Stop();
        stepSource.PlayOneShot(footstep, PlayerLevel);
        stepStopAt = Time.time + StepSoundDuration;
    }

    // Normal bullet: first zombie/civilian in the lane. Lob (arching bullet): skips the first
    // one and kills the next one in the lane; nothing if there is no second one.
    // Bullets pass through the rusher.
    private Shot Fire(bool lob)
    {
        current.shotsFired++;
        Mob victim = FirstInLane(lane, lob ? 1 : 0);
        Shot shot = new Shot { victim = victim };
        shot.hitTime = Time.time + (victim != null ? Mathf.Max(0f, victim.y - PlayerY) / BulletSpeed : 0f);
        shots.Add(shot);
        return shot;
    }

    private void ResolveShots()
    {
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            Shot s = shots[i];
            if (Time.time < s.hitTime) continue;
            if (s.victim != null && s.victim.alive)
            {
                Kill(s.victim);
                s.killed = s.victim;
            }
            else
                current.shotsMissed++;
            s.done = true;
            shots.RemoveAt(i);
        }
    }

    // ---------------------------------------------------------------- enemies (mirrors Enemy.cs / RushingEnemy.cs)

    private Mob SpawnMob(Kind kind, int mobLane, float y, bool walking)
    {
        Mob m = new Mob { kind = kind, lane = mobLane, y = y, walking = walking };
        GameObject go = new GameObject(kind + "_Lane" + mobLane);
        go.transform.SetParent(transform, false);
        m.source = go.AddComponent<AudioSource>();
        m.source.playOnAwake = false;
        m.source.volume = 1f;
        m.source.priority = 128;
        m.source.panStereo = mobLane == 1 ? 0f : (mobLane > 1 ? 1f : -1f); // Enemy.SetSoundPosition
        if (kind == Kind.Rusher)
        {
            m.source.spatialBlend = 0f; // RushingEnemy prefab is 2D: hard pan, no distance change
        }
        else
        {
            m.source.spatialBlend = 1f;
            m.source.rolloffMode = AudioRolloffMode.Linear;
            m.source.minDistance = 2f;
            m.source.maxDistance = 15f;
            m.source.dopplerLevel = 1f;
            m.source.spread = 0f;
            m.filter = go.AddComponent<AudioLowPassFilter>();
            m.filter.customCutoffCurve = AnimationCurve.Constant(0f, 1f, LowPassCurveValue);
            m.filter.cutoffFrequency = LowPassCurveValue * 22000f;
            m.filter.lowpassResonanceQ = 1f;
            m.filter.enabled = false;
        }
        mobs.Add(m);
        PlaceMob(m);

        if (kind == Kind.Rusher)
            MobSound(m, rusherCharge);
        else
        {
            MobSound(m, kind == Kind.Zombie ? zombieSpawn : civilianSpawn);
            m.idleAt = IdleStartAge;
        }
        return m;
    }

    private void PlaceMob(Mob m)
    {
        if (m.source != null)
            m.source.transform.position = origin + new Vector3((m.lane - 1) * LaneX, m.y, 0f);
    }

    // Enemy.MakeSound: cooldown, stop, one-shot at monster level (+0.5 for civilians), stop after soundDuration.
    private void MobSound(Mob m, AudioClip clip)
    {
        if (m.source == null || clip == null) return;
        if (Time.time < m.nextPlayTime) return;
        m.nextPlayTime = Time.time + MobPlayCooldown;
        m.idleAt = -1f; // StopAllCoroutines
        m.source.Stop();
        m.source.PlayOneShot(clip, m.kind == Kind.Civilian ? MonsterLevel + 0.5f : MonsterLevel);
        m.stopAt = m.age + (m.kind == Kind.Rusher ? RusherSoundDuration : MobSoundDuration);
    }

    private void Kill(Mob m)
    {
        m.alive = false;
        m.source.Stop();
        MobSound(m, m.kind == Kind.Zombie ? zombieDeath : civilianHit);
        m.source.loop = false;
        m.destroyAt = Time.time + DestroyDelay;
        if (m.kind == Kind.Zombie)
        {
            score += ZombieReward;
            current.zombiesKilled++;
        }
        else
        {
            score += CivilianShotPenalty;
            civiliansShotTotal++;
            current.civiliansShot++;
        }
    }

    private void Escape(Mob m)
    {
        m.alive = false;
        m.destroyAt = Time.time + DestroyDelay;
        if (m.kind == Kind.Zombie)
        {
            score += ZombieEscapePenalty;
            zombieEscapes++;
            current.zombiesEscaped++;
            MobSound(m, zombieEscape);
        }
        else
        {
            score += CivilianEscapeReward;
            civilianPasses++;
            current.civiliansPassed++;
            MobSound(m, civilianEscape); // empty in the levels: the walking loop just carries on
        }
    }

    private void UpdateRusher(Mob m)
    {
        if (!m.running && m.age >= RusherChargeTime) m.running = true;
        if (!m.running) return;

        m.y = SpawnY - RusherSpeed * (m.age - RusherChargeTime);

        // Crushes zombies and civilians in its lane, silently and with no points.
        foreach (Mob o in mobs)
        {
            if (o == m || !o.alive || o.kind == Kind.Rusher || o.lane != m.lane || o.y < m.y) continue;
            o.alive = false;
            o.source.Stop();
            o.destroyAt = Time.time;
        }

        if (!m.hitChecked && m.age >= RusherHitAge)
        {
            m.hitChecked = true;
            if (lane == m.lane)
            {
                MobSound(m, rusherHitPlayer);
                score += RusherPenalty;
                rusherHits++;
                current.rusherHits++;
            }
        }

        if (m.y <= KillBoxY)
        {
            m.alive = false;
            m.destroyAt = Time.time + 2f; // Destroy(gameObject, 2)
        }
    }

    private void Update()
    {
        if (stepStopAt >= 0f && Time.time >= stepStopAt)
        {
            stepStopAt = -1f;
            if (stepSource != null) stepSource.Stop();
        }
        foreach (AudioSource a in ambienceSources)
            if (a != null) a.volume = AmbienceLevel; // GameManager.UpdateSoundVolume

        bool paused = pauseMobsWhileTalking && guideSource != null && guideSource.isPlaying;
        float dt = paused ? 0f : Time.deltaTime;

        for (int i = mobs.Count - 1; i >= 0; i--)
        {
            Mob m = mobs[i];
            if (m.destroyAt >= 0f && Time.time >= m.destroyAt)
            {
                RemoveMob(m);
                continue;
            }
            m.age += dt;
            if (m.stopAt >= 0f && m.age >= m.stopAt)
            {
                m.stopAt = -1f;
                m.source.Stop();
            }
            if (m.idleAt >= 0f && m.age >= m.idleAt)
            {
                m.idleAt = -1f;
                AudioClip loop = m.kind == Kind.Zombie ? zombieWalkLoop : civilianWalkLoop;
                if (loop != null)
                {
                    // Enemy.StartIdleSounds: Play() at the source volume (1), not scaled by the slider.
                    m.source.clip = loop;
                    m.source.loop = true;
                    m.source.Play();
                }
            }
            if (m.kind == Kind.Rusher)
            {
                if (m.alive) UpdateRusher(m);
                continue;
            }
            if (!m.alive) continue;
            if (m.walking)
            {
                m.y = SpawnY - WalkSpeed * m.age;
                if (m.kind == Kind.Zombie && !m.screamed && m.age >= ScreamAge)
                {
                    m.screamed = true;
                    MobSound(m, zombieScream); // stops the loop; silent ~1.5 s later until the KillBox
                }
                if (m.age >= KillBoxAge) Escape(m);
            }
            PlaceMob(m);
        }

        // Muffle: low-pass when another zombie/civilian is within 3.5 units in front (Enemy.Update raycast).
        foreach (Mob m in mobs)
        {
            if (m.filter == null) continue;
            bool blocked = false;
            if (m.alive)
            {
                foreach (Mob o in mobs)
                {
                    if (o == m || !o.alive || o.kind == Kind.Rusher || o.lane != m.lane || o.y >= m.y) continue;
                    if ((m.y - 0.6f) - (o.y + 0.5f) <= MuffleRay) { blocked = true; break; }
                }
            }
            m.filter.enabled = blocked;
        }

        ResolveShots();
    }

    private Mob FirstInLane(int l, int skip)
    {
        List<Mob> inLane = new List<Mob>();
        foreach (Mob m in mobs)
            if (m.alive && m.kind != Kind.Rusher && m.lane == l && m.y > PlayerY) inLane.Add(m);
        inLane.Sort((a, b) => a.y.CompareTo(b.y));
        return skip < inLane.Count ? inLane[skip] : null;
    }

    private bool AnyScreamed()
    {
        foreach (Mob m in mobs) if (m.screamed) return true;
        return false;
    }

    private int AliveCount(Kind kind)
    {
        int n = 0;
        foreach (Mob m in mobs) if (m.alive && m.kind == kind) n++;
        return n;
    }

    private void RemoveMob(Mob m)
    {
        mobs.Remove(m);
        if (m.source != null) Destroy(m.source.gameObject);
    }

    private void ClearMobs()
    {
        for (int i = mobs.Count - 1; i >= 0; i--) RemoveMob(mobs[i]);
        foreach (Shot s in shots) s.done = true; // a step waiting on a bullet must not hang
        shots.Clear();
    }

    // ---------------------------------------------------------------- audio setup and volumes

    private static float MonsterLevel => SettingsManager.monsterSoundLevel;
    private static float PlayerLevel => SettingsManager.playerSoundLevel;
    private static float AmbienceLevel => SettingsManager.ambienceSoundLevel;

    private void InitVolumes()
    {
        // The statics are 0 until the menu has set them (tutorial opened directly in the editor).
        // Then use the menu defaults (SettingsManager.SetSoundLevels: player 0.25, monster 0.4,
        // ambience 0.05), so the tutorial and the Level 1 that follows are audible.
        if (SettingsManager.monsterSoundLevel <= 0f && SettingsManager.playerSoundLevel <= 0f &&
            SettingsManager.ambienceSoundLevel <= 0f)
            SettingsManager.SetSoundLevels();

        guideBaseVolume = guideSource.volume;
        if (guideUsesOperatorVolume)
        {
            float op = SettingsManager.operatorSoundLevel > 0f ? SettingsManager.operatorSoundLevel : DefaultOperatorLevel;
            guideSource.volume = guideBaseVolume * op;
        }
    }

    private void CreateSources()
    {
        // Same listener as the levels: Main Camera at (0, 0, -10). Lanes sit 10 units in front of it.
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener == null)
        {
            Camera cam = Camera.main;
            listener = (cam != null ? cam.gameObject : gameObject).AddComponent<AudioListener>();
            Debug.LogWarning("[Tutorial] No AudioListener in the scene; added one.", this);
        }
        origin = listener.transform.position + new Vector3(0f, 0f, 10f);

        playerSource = NewSource("PlayerSounds");
        playerSource.spatialBlend = 1f; // Player.prefab source 0: 3D, logarithmic 1-500
        playerSource.rolloffMode = AudioRolloffMode.Logarithmic;
        playerSource.minDistance = 1f;
        playerSource.maxDistance = 500f;
        stepSource = NewSource("PlayerSteps"); // Player.prefab source 1: 2D
        PlacePlayer();
    }

    private void StartAmbience()
    {
        if (fireAmbience == null || ambienceSources.Count > 0) return;
        // Level1/2 LightLeft / LightRight: looping FireSound, 3D logarithmic, volume = ambience slider.
        Vector3[] positions = { new Vector3(-9.1f, -4f, 0f), new Vector3(9.2f, -4f, 0f) };
        for (int i = 0; i < 2; i++)
        {
            AudioSource a = NewSource(i == 0 ? "FireLeft" : "FireRight");
            a.transform.position = origin + positions[i];
            a.spatialBlend = 1f;
            a.rolloffMode = AudioRolloffMode.Logarithmic;
            a.minDistance = 3.15f;
            a.maxDistance = 500f;
            a.panStereo = i == 0 ? -1f : 1f;
            a.clip = fireAmbience;
            a.loop = true;
            a.volume = AmbienceLevel;
            a.Play();
            ambienceSources.Add(a);
        }
    }

    private AudioSource NewSource(string sourceName)
    {
        GameObject go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private void RetireContradictingClips()
    {
        askLobber = WithoutRetired(askLobber);
        holdLonger = WithoutRetired(holdLonger);
        hintLobHere = WithoutRetired(hintLobHere);
        explainLob = WithoutRetired(explainLob);
        explainSoundDirection = WithoutRetired(explainSoundDirection);
        explainDummy = WithoutRetired(explainDummy);
        lobHowTo = WithoutRetired(lobHowTo);
    }

    private static AudioClip[] WithoutRetired(AudioClip[] clips)
    {
        if (clips == null) return null;
        List<AudioClip> keep = new List<AudioClip>();
        foreach (AudioClip c in clips)
        {
            if (c == null) continue;
            if (System.Array.IndexOf(RetiredClips, c.name) < 0) keep.Add(c);
        }
        return keep.ToArray();
    }

    private void LoadNextScene()
    {
        string target = nextScene;
        if (string.IsNullOrEmpty(target) || string.Equals(target, "prototype", System.StringComparison.OrdinalIgnoreCase))
            target = "Level1";
        if (!Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogError("[Tutorial] Scene '" + target + "' is not in Build Settings; trying Level1.", this);
            target = "Level1";
        }
        if (target == "Level1" || target == "Level2")
        {
            // Same as LevelLoader.StartGame(): new playthrough number for playerEvents.json.
            SettingsManager.playthrough++;
            LogSystem.ResetCount();
        }
        SceneManager.LoadScene(target);
    }

    // ---------------------------------------------------------------- narrator

    // Plays the recorded clips of a slot. No text-to-speech: an empty slot stays
    // silent and logs one warning with the slot name, so missing lines are visible.
    private IEnumerator Say(AudioClip[] clips, string slot)
    {
        if (HasClips(clips))
        {
            yield return PlayAudioList(clips);
            yield break;
        }
        WarnEmpty(slot);
    }

    private void WarnEmpty(string slot)
    {
        if (!string.IsNullOrEmpty(slot) && warnedSlots.Add(slot))
            Debug.LogWarning("[Tutorial] No recorded clip in slot '" + slot + "', skipped.", this);
    }

    // Play every recording in a list, in order
    private IEnumerator PlayAudioList(AudioClip[] clips)
    {
        if (!HasClips(clips) || guideSource == null)
            yield break;

        lastClips = clips;

        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null)
                continue;

            guideSource.clip = clip;
            guideSource.Play();

            // Wait until the current recording finishes
            bool restart = false;
            while (guideSource.isPlaying)
            {
                if (CtrlPressed())
                {
                    restart = true;
                    break;
                }
                yield return null;
            }

            if (restart)
            {
                // Ctrl while speaking: restart this line from the beginning
                current.ctrlRepeats++;
                guideSource.Stop();
                i--;
                yield return null;
            }
        }
    }

    // Optional extra line: silent skip when empty (no delay, no log).
    // Does not become the "last guide line", so Ctrl still repeats the main instruction.
    private IEnumerator SayExtra(AudioClip[] clips)
    {
        if (!HasClips(clips))
            yield break;

        AudioClip[] keepClips = lastClips;
        yield return PlayAudioList(clips);
        lastClips = keepClips;
    }

    private static bool HasClips(AudioClip[] clips)
    {
        if (clips == null) return false;
        foreach (AudioClip c in clips) if (c != null) return true;
        return false;
    }

    // Ctrl: replay the last recorded guide line (nothing yet: no-op)
    private IEnumerator RepeatLast()
    {
        if (lastClips != null)
            yield return PlayAudioList(lastClips);
    }

    private IEnumerator WaitWhileGuideOrSeconds(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end || (guideSource != null && guideSource.isPlaying))
            yield return null;
    }

    private bool CtrlPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.rightCtrlKey.wasPressedThisFrame);
    }

    // Wait for the correct movement
    private IEnumerator WaitForDirection(
        Key correctKey,
        Key wrongKey,
        AudioClip[] wrongAudio,
        AudioClip[] repeatInstruction)
    {
        // Avoid accepting a key held during narration.
        yield return WaitForKeysReleased();
        lastInputTime = Time.time;

        while (true)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (CtrlPressed())
                {
                    current.ctrlRepeats++;
                    yield return RepeatLast();
                    lastInputTime = Time.time;
                }
                else if (keyboard[wrongKey].wasPressedThisFrame)
                {
                    current.wrongPresses++;
                    // Play correction
                    yield return PlayAudioList(wrongAudio);

                    // Repeat the instruction
                    yield return PlayAudioList(repeatInstruction);

                    // Wait for fresh input
                    yield return WaitForKeysReleased();
                    lastInputTime = Time.time;
                }
                else if (keyboard[correctKey].wasPressedThisFrame)
                {
                    // Correct input
                    yield break;
                }
            }

            // Idle: repeat the instruction
            if (Time.time - lastInputTime > idleRepeatSeconds)
            {
                current.idleRepeats++;
                yield return PlayAudioList(repeatInstruction);
                lastInputTime = Time.time;
            }

            yield return null;
        }
    }

    // Wait until movement, shoot, lob and reload keys are released
    private IEnumerator WaitForKeysReleased()
    {
        while (Keyboard.current != null &&
              (Keyboard.current.leftArrowKey.isPressed ||
               Keyboard.current.rightArrowKey.isPressed ||
               Keyboard.current.upArrowKey.isPressed ||
               Keyboard.current.spaceKey.isPressed ||
               Keyboard.current.rKey.isPressed))
        {
            yield return null;
        }

        // Start listening on the following frame
        yield return null;
    }

    // ---------------------------------------------------------------- playtest log (tutorialEvents.json)

    // Written next to playerEvents.json (project folder in the editor, next to the _Data folder in a build),
    // with JsonUtility like LogSystem. One session per run, one entry per step, rewritten at each step end.
    private void StartLog()
    {
        try
        {
            logPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "tutorialEvents.json");
            if (File.Exists(logPath))
                logFile = JsonUtility.FromJson<TutorialLogFile>(File.ReadAllText(logPath));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Tutorial] Could not read tutorialEvents.json: " + e.Message, this);
        }
        if (logFile == null || logFile.sessions == null) logFile = new TutorialLogFile();
        session = new SessionLog
        {
            startedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            playthrough = SettingsManager.playthrough,
        };
        logFile.sessions.Add(session);
        sessionStartTime = Time.time;
    }

    private void BeginStep(string stepName)
    {
        current = new StepLog { step = stepName };
        stepStartTime = Time.time;
    }

    private void EndStep()
    {
        current.seconds = Time.time - stepStartTime;
        session.steps.Add(current);
        session.totalSeconds = Time.time - sessionStartTime;
        session.totalScore = score;
        WriteLog();
    }

    private void FinishLog()
    {
        session.completed = true;
        session.totalSeconds = Time.time - sessionStartTime;
        session.totalScore = score;
        WriteLog();
    }

    private void WriteLog()
    {
        if (string.IsNullOrEmpty(logPath)) return;
        try
        {
            File.WriteAllText(logPath, JsonUtility.ToJson(logFile, true));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Tutorial] Could not write tutorialEvents.json: " + e.Message, this);
        }
    }

#if UNITY_EDITOR
    // Inspector: TutorialManager component menu (three dots) > "Assign default tutorial sounds", then save the scene.
    [ContextMenu("Assign default tutorial sounds")]
    private void AssignDefaultSounds()
    {
        Undo.RecordObject(this, "Assign default tutorial sounds");
        FillMissingSounds();
        RetireContradictingClips();
        if (string.IsNullOrEmpty(nextScene) || string.Equals(nextScene, "prototype", System.StringComparison.OrdinalIgnoreCase))
            nextScene = "Level1";
        EditorUtility.SetDirty(this);
    }

    private const string NewClips = "Assets/Sounds/Newtutorialmaterial/";
    private const string IntroClips = "Assets/Sounds/Tutorial.intro/";
    private const string Ucs = "Assets/Sounds/Ljudklipp_UCS_Remastered/";
    private const string ZombiePrefab = "Assets/Prefabs/Enemies/StandardEnemy.prefab";
    private const string CivilianPrefab = "Assets/Prefabs/Enemies/Civillian.prefab";
    private const string RusherPrefab = "Assets/Prefabs/Enemies/RushingEnemy.prefab";
    private const string PlayerPrefab = "Assets/Prefabs/Player/Player.prefab";

    // Level sounds are always re-read from the level prefabs (read only), so the tutorial follows
    // any change the team makes there. Voice slots: fills only EMPTY slots (PickSet keeps Inspector choices).
    private void FillMissingSounds()
    {
        // Level sounds, same clips as Level 1/2
        zombieSpawn = LevelClip(ZombiePrefab, "Enemy", "spawnSound", "Assets/Sounds/Monster/Standard/ZombieSpawn_2.wav");
        zombieWalkLoop = LevelClip(ZombiePrefab, "Enemy", "idleSounds.Array.data[0]", "Assets/Sounds/Monster/Standard/ZombieAttack_2.wav");
        zombieScream = LevelClip(ZombiePrefab, "Enemy", "attackSound", "Assets/Sounds/Monster/Standard/ZombieAttack_3.wav");
        zombieDeath = LevelClip(ZombiePrefab, "Enemy", "deathSound", "Assets/Sounds/Monster/Standard/ZombieDeath_2.wav");
        zombieEscape = LevelClip(ZombiePrefab, "Enemy", "escapeSound", "Assets/Sounds/Monster/Standard/ZombieEscape_1.wav");
        civilianSpawn = LevelClip(CivilianPrefab, "Enemy", "spawnSound", "Assets/Sounds/Civilllian/CivilianOld_idle2.wav");
        civilianWalkLoop = LevelClip(CivilianPrefab, "Enemy", "idleSounds.Array.data[0]", "Assets/Sounds/Civilllian/CivilianOld_idle3.wav");
        civilianHit = LevelClip(CivilianPrefab, "Enemy", "deathSound", "Assets/Sounds/Civilllian/CivilianOld_hit1.wav");
        civilianEscape = LevelClip(CivilianPrefab, "Enemy", "escapeSound", null);
        rusherCharge = LevelClip(RusherPrefab, "RushingEnemy", "chargeSound", "Assets/Sounds/Monster/Rushing/HeavyRunwav.wav");
        rusherHitPlayer = LevelClip(RusherPrefab, "RushingEnemy", "hitPlayerSound", "Assets/Sounds/Thud.wav");
        gunShot = LevelClip(PlayerPrefab, "Player", "playerShoot", "Assets/Sounds/Player/gunShotSound.mp3");
        gunEmptyClick = LevelClip(PlayerPrefab, "Player", "noAmmoSound", "Assets/Sounds/Player/GunClick.mp3");
        gunReload = LevelClip(PlayerPrefab, "Player", "reloadSound", "Assets/Sounds/Player/ReloadRevolver.wav");
        lobCharge = LevelClip(PlayerPrefab, "Player", "chargeWeapon", "Assets/Sounds/Player/ChargeWeapon.wav");
        lobFire = LevelClip(PlayerPrefab, "Player", "firedChargedWeapon", "Assets/Sounds/Player/ChargeShot.wav");
        lobRelease = LevelClip(PlayerPrefab, "Player", "releaseChargingWeapon", "Assets/Sounds/Player/ReleaseChargingWeapon.wav");
        wallThud = LevelClip(PlayerPrefab, "Player", "wallHit", "Assets/Sounds/Thud.wav");
        footstep = LevelClip(PlayerPrefab, "Player", "playerMove.Array.data[0]", "Assets/Sounds/Player/step_sound.mp3");
        fireAmbience = Pick(fireAmbience, "Assets/Sounds/Ambience/FireSound.mp3");

        // New recordings (Newtutorialmaterial, file name = spoken line)
        repeatHelp = PickSet(repeatHelp, NewClips + "If you want me to repeat myself press ctrl and ill repeat the last thing i said.wav");
        askRealZombie = PickSet(askRealZombie, NewClips + "Here comes a real zombie in the middle lane.wav");
        realZombieReachedYou = PickSet(realZombieReachedYou, NewClips + "The zombie reached you.wav");
        tappedCivilian = PickSet(tappedCivilian, NewClips + "Space shots toward the front. hold the up arrow to lob.wav");
        hintGoLeft = PickSet(hintGoLeft, NewClips + "Target on the left, press left.wav");
        hintGoRight = PickSet(hintGoRight, NewClips + "Test on the right, press right.wav");
        hintShootHere = PickSet(hintShootHere, NewClips + "Target in this lane.wav", NewClips + "Press space to shoot.wav");
        hintLobHere = PickSet(hintLobHere, NewClips + "The target is in this lane, hold the up arrow.wav");
        wrongWay = PickSet(wrongWay, NewClips + "Thats the wrong way.wav");
        hitWall = PickSet(hitWall, NewClips + "Ouch that a wall.wav");
        missedEmptyLane = PickSet(missedEmptyLane, NewClips + "Nothing in this lane.wav");

        // New recordings: extras ("Zombie gets loader the closer they get" is retired: the levels barely get louder)
        explainLanes = PickSet(explainLanes, NewClips + "PRess the left or right arrow to move a lane at a time.wav");
        explainSoundDirection = PickSet(explainSoundDirection, NewClips + "Zombies makes sound from the lane they come from, move there.wav");
        explainLob = PickSet(explainLob, NewClips + "Lob shots is the only way to shot awhats behind a civilian.wav");
        inRightLane = PickSet(inRightLane, NewClips + "Great you are in the right lane now, take the shot.wav");
        tryAgain = PickSet(tryAgain, NewClips + "Almost, try again.wav");
        praiseShort = PickSet(praiseShort, NewClips + "Nice oneee.wav");
        lobKeepInMind = PickSet(lobKeepInMind, NewClips + "thats the lob.wav");
        patience = PickSet(patience, NewClips + "Take your time.wav", NewClips + "Im not goping anywhere.wav", NewClips + "I still get payed on the clock.wav");
        closing = PickSet(closing, NewClips + "I taught you all i know, lets go.wav");

        // Earlier recordings (Tutorial.intro)
        explainDummy = PickSet(explainDummy, IntroClips + "Testing you weapomn.wav", IntroClips + "Dont move to the sides.wav");
        dummyHit = PickSet(dummyHit, IntroClips + "Well Done.wav", IntroClips + "Shoot the dead Dead.wav");
        askZombieRight = PickSet(askZombieRight, IntroClips + "Civilian and zombie dummy.wav", IntroClips + "Dont shoot civilians.wav");
        zombieRightHit = PickSet(zombieRightHit, IntroClips + "you shot the correct target.wav");
        // "Press dowm MK1" says "until you hear a click": the levels have no click, so it is left out.
        askLobber = PickSet(askLobber, IntroClips + "Testing mortar function.wav", IntroClips + "Once released - MK 1.wav");
        lobberHit = PickSet(lobberHit, IntroClips + "Released button success.wav");
        // holdLonger: "Released to early" says "wait for the click", so the slot stays empty until re-recorded.
        shotCivilian = PickSet(shotCivilian, IntroClips + "Dont shoot the civilian.wav");
        nearCivilian = PickSet(nearCivilian, IntroClips + "Dont shoot civilians.wav");

        // Operator clips from Ljudklipp_UCS_Remastered (machine-transcribed; check by ear)
        realZombieHit = PickSet(realZombieHit, Ucs + "Hit/Hit_Remaster_3.wav");                       // "Target neutralized."
        reloadReminder = PickSet(reloadReminder, Ucs + "Reload/Reload_Remaster_2.wav");               // "Remember to reload."
        reloadDone = PickSet(reloadDone, Ucs + "Reload/Reload_Remaster_5.wav");                       // "All right, good to go."
        civilianPassed = PickSet(civilianPassed, Ucs + "Humor/Humour_Remaster_6.wav");                // "Good news, no innocent bystanders were harmed."
        explainWave = PickSet(explainWave, Ucs + "Guidance/Guidance_Remaster_15.wav");                // "Targets approaching from multiple directions."
        waveMultiple = PickSet(waveMultiple, Ucs + "Intense/Intense_Remaster_3.wav");                 // "...multiple contacts closing in."
        waveCivilianShot = PickSet(waveCivilianShot, Ucs + "CivilianKill/CivilianKill_Remaster_5.wav"); // "I will pretend that I didn't see that."
        waveTimeUp = PickSet(waveTimeUp, Ucs + "Ljud1/Ljud1_Remaster_1.wav");                         // "Alright, that's time."
        tutorialComplete = PickSet(tutorialComplete, Ucs + "Ljud1/Ljud1_Remaster_4.wav");             // "Alright, it looks like we're finished here."

        // Lines still to be recorded (Voice_lines_to_record_20261027). Save them in Newtutorialmaterial with
        // exactly these names and they are picked up here; until then the slots stay empty (no warning).
        explainAmmo = PickIfRecorded(explainAmmo, "Your gun holds six shots, when its empty youll hear a click.wav");
        askReload = PickIfRecorded(askReload, "Press R to reload, it takes a couple of seconds.wav");
        lobHowTo = PickIfRecorded(lobHowTo, "Hold the up arrow until the charge sound fades, then let go.wav");
        holdLonger = PickIfRecorded(holdLonger, "Too early, the lob wasnt charged yet.wav");
        explainSilence = PickIfRecorded(explainSilence, "Right before they reach you they go quiet, dont wait for them.wav");
        explainScream = PickIfRecorded(explainScream, "Hear that scream, that zombie is right on top of you, shoot now.wav");
        explainCivilianWalk = PickIfRecorded(explainCivilianWalk, "Civilians walk down the lanes too, let them pass.wav");
        explainRusher = PickIfRecorded(explainRusher, "Listen for heavy running, thats a charger, get out of its lane.wav");
        rusherDodged = PickIfRecorded(rusherDodged, "Good dodge, chargers flatten anything in their lane.wav");
        rusherHit = PickIfRecorded(rusherHit, "Ouch, it ran you over, move out of its lane next time.wav");
        explainScore = PickIfRecorded(explainScore, "A zombie down is a hundred points.wav");
        explainRound = PickIfRecorded(explainRound, "This is a short practice shift, thirty seconds.wav");
    }

    private static AudioClip[] PickIfRecorded(AudioClip[] current, string fileName)
    {
        if (HasClips(current)) return current;
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(NewClips + fileName);
        return clip != null ? new[] { clip } : current;
    }

    // Reads a clip from a level prefab without changing it; falls back to the known path.
    private static AudioClip LevelClip(string prefabPath, string component, string property, string fallbackPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Component c = prefab != null ? prefab.GetComponent(component) : null;
        if (c != null)
        {
            SerializedProperty p = new SerializedObject(c).FindProperty(property);
            if (p != null && p.propertyType == SerializedPropertyType.ObjectReference)
            {
                AudioClip clip = p.objectReferenceValue as AudioClip;
                if (clip != null || fallbackPath == null) return clip;
            }
        }
        return fallbackPath != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(fallbackPath) : null;
    }

    private static AudioClip[] PickSet(AudioClip[] current, params string[] paths)
    {
        if (HasClips(current)) return current;
        var clips = new List<AudioClip>();
        foreach (string path in paths)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null) clips.Add(clip);
            else Debug.LogWarning("[Tutorial] Default clip not found: " + path);
        }
        return clips.ToArray();
    }

    private static AudioClip Pick(AudioClip current, string path)
    {
        return current != null ? current : AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
#endif
}
