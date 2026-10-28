using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ===================================================================================
// TutorialManager – min ljud-tutorial för SPD (Alfred)
// ===================================================================================
// Det här är tutorialen som spelaren kör innan Level 1. Allt sker med ljud, inget ska
// behöva ses: en inspelad röst ("operatören") guidar, och spelet låter exakt som i
// Level 1/2. Jag har byggt stegen själv, men alla spelljud, regler och tider har jag
// kopierat från Enemy.cs, Player.cs, Bullet.cs och level-prefabsen/scenerna.
//
// Tutorialen lär BARA ut fem saker: gå vänster, gå höger, skjuta, lobba (mortar) och
// att skilja på civil och zombie. Ammo/reload, poäng, rundtimer och specialfiender
// lärs inte ut här, det kommer i levlarna.
// Ammo är obegränsad i tutorialen.
//
// Stegen i ordning (se Start() längre ner):
//   0 Ctrl-hjälp   1 Intro (+ eldljudet)   2 Vänster   3 Höger (+ förklaring av filer)
//   4 Skjut en docka i mitten   5 Civil vänster, zombie höger   6 En riktig zombie som går
//   7 Lobba över en civil   8 Övningsvåg 30 s   9 Avslutning   -> laddar Level1
//
// Rösten: bara inspelade repliker, ingen TTS. Är en slot tom loggar jag en varning en
// gång och steget fortsätter ändå. Ctrl spelar alltid upp den senaste instruktionen igen.
// Spelarens resultat per steg sparas i tutorialEvents.json (bredvid playerEvents.json).
public class TutorialManager : MonoBehaviour
{
    // Två sorters mål i tutorialen: zombie (ska skjutas) och civil (ska inte skjutas).
    private enum Kind { Zombie, Civilian }
    // Vad spelaren gjorde den här framen. ReadAction() returnerar en av dessa.
    private enum Act { None, Ctrl, Move, Wall, Shot, ChargeStart, LobFired, LobEarly }

    // guideSource är AudioSourcen som operatörens röst spelas på (sätts i Inspectorn).
    [Header("Audio Sources")]
    [SerializeField] private AudioSource guideSource;
    // Om den är på skalar jag rösten med Operator-reglaget i menyn (SettingsManager.operatorSoundLevel).
    [Tooltip("Multiply the narrator by the menu's Operator volume slider (SettingsManager.operatorSoundLevel).")]
    [SerializeField] private bool guideUsesOperatorVolume = true;

    // Steg 0: "Tryck Ctrl om du vill att jag upprepar". Spelas först av allt.
    [Header("Repeat Help")]
    [SerializeField] private AudioClip[] repeatHelp;

    // Steg 1: introt. Eldljudet från levlarna startar här.
    [Header("Introduction")]
    [SerializeField] private AudioClip[] introduction;

    // Steg 3: be spelaren gå höger, rätta om hen trycker vänster, beröm när det blir rätt.
    [Header("Right Calibration")]
    [SerializeField] private AudioClip[] askRight;
    [SerializeField] private AudioClip[] wrongRight;
    [SerializeField] private AudioClip[] correctRight;

    // Steg 2: samma sak fast för vänster (vänster kommer först i inspelningarna).
    [Header("Left Calibration")]
    [SerializeField] private AudioClip[] askLeft;
    [SerializeField] private AudioClip[] wrongLeft;
    [SerializeField] private AudioClip[] correctLeft;

    // Steg 4: en docka i mittenfilen. explainDummy förklarar Space, dummyHit spelas vid träff.
    [Header("Step 4: Dummy in front")]
    [SerializeField] private AudioClip[] explainDummy;
    [SerializeField] private AudioClip[] dummyHit;

    // Steg 5: civil i vänster fil, zombie i höger. Spelaren ska gå åt rätt håll och skjuta zombien.
    [Header("Step 5: Zombie right, civilian left")]
    [SerializeField] private AudioClip[] askZombieRight;
    [SerializeField] private AudioClip[] zombieRightHit;

    // Steg 6: en riktig zombie som går mot spelaren. reachedYou spelas om den hinner fram.
    [Header("Step 6: Real zombie, level timing")]
    [SerializeField] private AudioClip[] askRealZombie;
    [SerializeField] private AudioClip[] realZombieHit;
    [SerializeField] private AudioClip[] realZombieReachedYou;

    // Steg 7: civil framför, zombie bakom. Spelaren måste lobba (håll pil upp, släpp).
    [Header("Step 7: Lob")]
    [SerializeField] private AudioClip[] askLobber;
    [SerializeField] private AudioClip[] lobHowTo;              // NY (spelas in): "håll tills laddljudet tonar ut, släpp sen"
    [SerializeField] private AudioClip[] lobberHit;
    [SerializeField] private AudioClip[] tappedCivilian;
    [SerializeField] private AudioClip[] holdLonger;            // ska spelas in igen: gamla tagningen säger "vänta på klicket"
    [SerializeField] private float chargeHoldSeconds = 1.5f;    // samma som Level1/2-scenerna: Hold(duration=1.5), alltså 1,5 s laddning

    // Steg 8: övningsvågen. explainWave innan, waveMultiple när 3+ zombies lever,
    // waveCivilianShot om man skjuter en civil, waveTimeUp när tiden är slut.
    [Header("Step 8: Practice wave (zombies and civilians only, Level 1 pace)")]
    [SerializeField] private AudioClip[] explainWave;
    [SerializeField] private AudioClip[] waveMultiple;
    [SerializeField] private AudioClip[] waveCivilianShot;
    [SerializeField] private AudioClip[] waveTimeUp;
    [SerializeField] private float waveSeconds = 30f;

    // Reaktioner som kan spelas i skjutstegen 4-7: tips om fil (vänster/höger/här), fel håll,
    // vägg, nära en civil, bom i tom fil och skjuten civil.
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

    // Spelets egna ljud. Fylls i från level-prefabsen så tutorialen låter som riktiga spelet.
    [Header("Level sounds (synced from the level prefabs by 'Assign default tutorial sounds')")]
    [SerializeField] private AudioClip zombieSpawn;
    [SerializeField] private AudioClip zombieWalkLoop;
    [SerializeField] private AudioClip zombieScream;
    [SerializeField] private AudioClip zombieDeath;
    [SerializeField] private AudioClip zombieEscape;
    [SerializeField] private AudioClip civilianSpawn;
    [SerializeField] private AudioClip civilianWalkLoop;
    [SerializeField] private AudioClip civilianHit;
    [SerializeField] private AudioClip civilianEscape;          // tom i Civillian.prefab idag (tyst, precis som i levlarna)
    [SerializeField] private AudioClip gunShot;
    [SerializeField] private AudioClip lobCharge;
    [SerializeField] private AudioClip lobFire;
    [SerializeField] private AudioClip lobRelease;
    [SerializeField] private AudioClip wallThud;
    [SerializeField] private AudioClip footstep;
    [SerializeField] private AudioClip fireAmbience;

    // Steg 9: avslutningsrepliken, och vilken scen som laddas efteråt (Level1).
    [Header("Ending")]
    [SerializeField] private AudioClip[] tutorialComplete;
    [SerializeField] private string nextScene = "Level1";

    [Header("Extras (optional)")]
    // Extra repliker som är frivilliga. Är en slot tom hoppar jag bara över den (ingen paus, ingen varning).
    [SerializeField] private AudioClip[] calibrationDone;       // efter correctRight: Fm7 "Looks like your headset is calibrated."
    [SerializeField] private AudioClip[] explainLanes;          // efter correctRight, innan steg 4: hur man byter fil
    [SerializeField] private AudioClip[] explainSoundDirection; // början av steg 5, innan askZombieRight: lyssna var ljudet kommer ifrån
    [SerializeField] private AudioClip[] explainLob;            // steg 7, innan askLobber: vad en lob är
    [SerializeField] private AudioClip[] inRightLane;           // första gången spelaren kommer till rätt fil i ett steg (inte i lob-steget)
    [SerializeField] private AudioClip[] tryAgain;              // efter missedEmptyLane: "Almost. Try that again."
    [SerializeField] private AudioClip[] praiseShort;           // efter zombieRightHit och realZombieHit: kort beröm
    [SerializeField] private AudioClip[] lobKeepInMind;         // efter lobberHit: "that's the lob"
    [SerializeField] private AudioClip[] patience;              // andra gången i rad jag upprepar instruktionen i ett skjutsteg
    [SerializeField] private AudioClip[] closing;               // efter tutorialComplete, precis innan nästa scen laddas

    [Header("Timing")]
    // Hur många sekunder spelaren får vara stilla innan jag upprepar instruktionen.
    [SerializeField] private float idleRepeatSeconds = 8f;

    // ---- Värden från levlarna (Level1/2-scenerna, prefabsen, Enemy.cs, Player.cs, Bullet.cs) ----
    // Jag har kopierat dem hit så att tutorialen beter sig och låter som i spelet.
    private const float LaneX = 5f;              // filerna ligger på x = -5 / 0 / +5 (EnemyLane/PlayerLane)
    private const float SpawnY = 4f;             // där fienderna spawnar (EnemyLane y)
    private const float PlayerY = -4f;           // där spelaren står (PlayerLane y)
    private const float WalkSpeed = 1f;          // speed 50 (force) * 0.02 fixed step = 1 enhet/s
    private const float IdleStartAge = 1.6f;     // gå-loopen startar efter 1,6 s (Enemy.StartIdleSounds(1.6f))
    private const float MobSoundDuration = 1.5f; // ett fiendeljud stängs av efter 1,5 s (soundDuration i prefabsen)
    private const float MobPlayCooldown = 0.5f;  // minst 0,5 s mellan två fiendeljud (Enemy playCooldown)
    private const float ScreamAge = 5.3f;        // zombien går in i SoundTrigger (y = -2.3) och skriker
    private const float KillBoxAge = 9.7f;       // zombien når KillBox (y = -6.69) och tar sig förbi
    // Y-läget där KillBox ligger, räknat från spawn och gånghastighet.
    private const float KillBoxY = SpawnY - WalkSpeed * KillBoxAge;
    private const float DestroyDelay = 1.5f;     // fienden tas bort 1,5 s efter att den dött (Destroy(gameObject, 1.5f))
    private const float MuffleRay = 3.5f;        // längden på raycasten i Enemy.Update som gör ljudet dämpat
    private const float LowPassCurveValue = 0.09049568f; // lowpass-värdet från prefabsens lowpassLevelCustomCurve
    private const float BulletSpeed = 10f;       // kulans fart: bulletSpeed 500 * 0.02 = 10 enheter/s
    private const float WeaponCooldown = 0.3f;   // minst 0,3 s mellan två skott (Player.prefab)
    // Lite spärr så att en knapptryckning inte flyttar spelaren två filer.
    private const float MoveCooldown = 0.1f;
    private const float StepSoundDuration = 2f;  // fotstegsljudet stängs av efter 2 s (soundDuration i Player.prefab)
    // Minsta tid mellan två fotstegsljud.
    // 2026-10-28 (Alfred): 0,2 -> 0,5 s. Player.prefab har playCooldown 0.5 (Player.MakeSound för fotsteg),
    // så nu låter snabba filbyten likadant som i Level 1/2 (fotsteg högst varannan halvsekund).
    private const float StepPlayCooldown = 0.5f;
    // Samma poäng som i levlarna. Jag räknar dem tyst bara för loggen, rösten säger aldrig poängen.
    private const int ZombieReward = 100, ZombieEscapePenalty = -100, CivilianShotPenalty = -100, CivilianEscapeReward = 50;
    // 2026-10-28 (Alfred): DefaultOperatorLevel (0,5) är borttagen. Standardvärdet för Operator sätts nu av
    // SettingsManager.EnsureDefaultSoundLevels() i InitVolumes, precis som GameManager gör i levlarna.

    // Gamla tagningar som säger fel saker om spelet (det finns inget "klick" när loben är laddad,
    // och zombies blir knappt starkare när de kommer närmare). De spelas aldrig, även om de ligger kvar i en slot.
    private static readonly string[] RetiredClips =
    {
        "Testing mortar - Press dowm MK1",
        "Released to early",
        "Zombie gets loader the closer they get",
    };

    // Ett mål i en fil (zombie eller civil). Det är min egen enkla version av en Enemy:
    // age/idleAt/stopAt räknas i fiendens egen tid (står still när den pausas), destroyAt och
    // nextPlayTime i vanlig Time.time.
    private class Mob
    {
        public Kind kind;
        public int lane;
        public float y;
        public bool walking;
        public float age;
        public bool alive = true;
        public bool screamed;
        public float idleAt = -1f;
        public float stopAt = -1f;
        public float destroyAt = -1f;
        public float nextPlayTime;
        public AudioSource source;
        public AudioLowPassFilter filter;
    }

    // Ett skott på väg. victim är målet det kommer träffa, hitTime när kulan är framme.
    private class Shot
    {
        public Mob victim;
        public float hitTime;
        public bool done;
        public Mob killed;
    }

    // Loggen för ett steg i tutorialEvents.json: tid, försök, fel, Ctrl, skott, träffar osv.
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
        public int lobsFired;
        public int lobsEarly;
        public int score;
    }

    // En genomkörning av tutorialen (en session) med alla steg i ordning.
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

    // Hela filen tutorialEvents.json: en lista med alla sessioner som körts.
    [System.Serializable]
    private class TutorialLogFile
    {
        public List<SessionLog> sessions = new List<SessionLog>();
    }

    private int lane = 1; // 0 = vänster, 1 = mitten, 2 = höger
    // Filen spelaren stod i innan förra förflyttningen (för att veta om hen gick åt fel håll).
    private int previousLane = 1;
    // Mittpunkten i världen som filerna räknas från (10 enheter framför lyssnaren).
    private Vector3 origin;
    private readonly List<Mob> mobs = new List<Mob>();
    private readonly List<Shot> shots = new List<Shot>();
    private AudioSource playerSource;  // som Player-källa 0: 3D vid spelaren (skott, lob-laddning, väggduns)
    private AudioSource stepSource;    // som Player-källa 1: 2D, fotsteg
    private readonly List<AudioSource> ambienceSources = new List<AudioSource>();
    private float stepStopAt = -1f;
    private float nextStepTime;
    private float guideBaseVolume = 1f;

    // Ammo är obegränsad i tutorialen (reload presenteras i levlarna, inte här).
    // Här håller jag koll på cooldowns och lob-laddningen, som i Player.cs.
    private float lastShotTime = -10f;
    private float lastMoveTime = -10f;
    private bool charging;
    private bool chargeChecked;
    private bool lobReady;
    private float chargeStart;
    private Shot lastShot;

    // I de guidade stegen fryser målen medan rösten pratar, så man inte förlorar på att lyssna.
    private bool pauseMobsWhileTalking = true;
    private int score;
    private int zombieEscapes;
    private int civilianPasses;
    private int civiliansShotTotal;

    // Senaste gången spelaren gjorde något (för att veta när jag ska upprepa instruktionen).
    private float lastInputTime;
    private bool stepDone;
    private AudioClip[] lastClips; // senaste instruktionen, den som Ctrl spelar upp igen
    // Slots som jag redan varnat för, så att varje tom slot bara loggas en gång.
    private readonly HashSet<string> warnedSlots = new HashSet<string>();
    private bool inRightLaneSaid; // inRightLane spelas bara en gång per skjutsteg

    // Loggen: hela filen, sessionen just nu, steget just nu och tider för dem.
    private TutorialLogFile logFile;
    private SessionLog session;
    private StepLog current = new StepLog { step = "setup" };
    private float stepStartTime;
    private float sessionStartTime;
    private string logPath;

    // Här kör jag hela tutorialen från början till slut, ett steg i taget.
    // Varje steg börjar med BeginStep och slutar med EndStep, som skriver loggen.
    private IEnumerator Start()
    {
        if (guideSource == null)
        {
            Debug.LogError("TutorialManager: assign Guide Source in the Inspector.", this);
            yield break;
        }
        guideSource.loop = false;

// Innan stegen: fyll tomma slots (bara i editorn), ta bort gamla tagningar, sätt volymer,
// skapa ljudkällorna, starta loggen och ställ spelaren i mittenfilen.
#if UNITY_EDITOR
        FillMissingSounds();
#endif
        RetireContradictingClips();
        InitVolumes();
        CreateSources();
        StartLog();
        lane = 1;
        PlacePlayer();

        // STEG 0: berätta att Ctrl upprepar det jag sa senast.
        BeginStep("0_repeatHelp");
        yield return Say(repeatHelp, nameof(repeatHelp));
        EndStep();

        // STEG 1: introt. Här startar också eldljudet från levlarna.
        BeginStep("1_intro");
        StartAmbience();
        yield return PlayAudioList(introduction);
        EndStep();

        // STEG 2: vänster först, eftersom inspelningarna är i den ordningen.
        // Fel pil ger wrongLeft + frågan igen, är man stilla 8 s upprepar jag frågan.
        BeginStep("2_left");
        yield return PlayAudioList(askLeft);
        yield return WaitForDirection(Key.LeftArrow, Key.RightArrow, wrongLeft, askLeft);
        MoveLane(-1); // mitten -> vänster (med fotstegsljud)
        yield return PlayAudioList(correctLeft);
        EndStep();

        // STEG 3: höger. Om correctLeft redan säger "nu höger" kan askRight lämnas tom,
        // så slipper man höra det två gånger.
        BeginStep("3_right");
        yield return PlayAudioList(askRight);
        yield return WaitForDirection(Key.RightArrow, Key.LeftArrow, wrongRight, askRight);
        MoveLane(1); // vänster -> mitten
        yield return PlayAudioList(correctRight);
        if (!ContainsAny(correctRight, calibrationDone)) yield return SayExtra(calibrationDone);
        yield return SayExtra(explainLanes);
        EndStep();

        // STEG 4: en docka i mittenfilen som står still, med zombiens riktiga ljud.
        // Klart när spelaren skjuter den. dummy() ställer upp steget igen om det måste börja om.
        BeginStep("4_dummy");
        System.Action dummy = () => { ClearMobs(); SpawnMob(Kind.Zombie, 1, SpawnY, false); };
        dummy();
        yield return Say(explainDummy, nameof(explainDummy));
        yield return ShootStep(1, explainDummy, nameof(explainDummy), false, dummy);
        ClearMobs();
        yield return Say(dummyHit, nameof(dummyHit));
        EndStep();

        // STEG 5: civil i vänster fil, zombie i höger. Panoreringen är fast per fil, som i levlarna.
        // Spelaren ska höra var zombien är, gå dit och skjuta den, inte den civila.
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

        // STEG 6: en riktig zombie som går ner i mittenfilen med levelns tider:
        // stön -> gå-loop -> skrik vid 5,3 s -> tyst -> skratt vid KillBox vid 9,7 s.
        BeginStep("6_realZombie");
        yield return Say(askRealZombie, nameof(askRealZombie));
        System.Action walker = () => { ClearMobs(); SpawnMob(Kind.Zombie, 1, SpawnY, true); };
        walker();
        yield return ShootStep(1, askRealZombie, nameof(askRealZombie), false, walker, true);
        ClearMobs();
        yield return Say(realZombieHit, nameof(realZombieHit));
        yield return SayExtra(praiseShort);
        EndStep();

        // STEG 7: en civil framför och en zombie bakom (dämpat ljud). Spelaren måste lobba över.
        BeginStep("7_lob");
        System.Action lobSetup = () => { ClearMobs(); SpawnMob(Kind.Civilian, 1, 1f, false); SpawnMob(Kind.Zombie, 1, 3f, false); };
        lobSetup();
        yield return SayExtra(explainLob);
        // 2026-10-28 (Alfred): Jag spelar inte längre askLobber ("Testing mortar function" / "Once released")
        // här, för att det var för många lob-repliker i rad innan spelaren ens fått försöka.
        // Nu blir det bara explainLob (vad en lob är) och lobHowTo (hur man gör).
        // Klippen och sloten ligger kvar i inspektorn, de anropas bara inte.
        // 2026-10-28 (Alfred): lobHowTo är nu huvudinstruktionen i lob-steget, så jag spelar den med Say
        // (inte SayExtra) så att Ctrl upprepar just den. Tills lobHowTo är inspelad säger hintLobHere
        // samma sak ("...hold the up arrow...") som reserv, så att steget aldrig blir tyst.
        AudioClip[] lobAsk = HasClips(lobHowTo) ? lobHowTo : hintLobHere;
        string lobAskSlot = HasClips(lobHowTo) ? nameof(lobHowTo) : nameof(hintLobHere);
        yield return Say(lobAsk, lobAskSlot);
        // 2026-10-28 (Alfred): Jag skickar lobHowTo (eller reserven) till ShootStep istället för askLobber,
        // så när spelaren står still eller lobbar på en civil upprepas hur man gör, inte "Testing mortar function".
        yield return ShootStep(1, lobAsk, lobAskSlot, true, lobSetup);
        ClearMobs();
        yield return Say(lobberHit, nameof(lobberHit));
        // 2026-10-28 (Alfred): Jag spelar inte längre lobKeepInMind ("That's the lob") efter träffen,
        // för att lobberHit redan bekräftar att det lyckades och det blev en replik för mycket.
        EndStep();

        // STEG 8: ungefär 30 s övningsvåg i Level 1-tempo med bara det vi lärt oss:
        // gå, skjuta, lobba, civil eller zombie. Bara zombies och civila, obegränsad ammo,
        // inget snack om poäng eller tid, inga specialfiender.
        BeginStep("8_practiceWave");
        yield return MiniWave();
        EndStep();

        // STEG 9: tutorialen är klar. Avslutningsrepliker, sen markerar jag sessionen som klar i loggen.
        BeginStep("9_end");
        yield return Say(tutorialComplete, nameof(tutorialComplete));
        yield return SayExtra(closing);
        EndStep();
        FinishLog();

        // STEG 10: ladda första leveln, på samma sätt som Level 1-knappen i menyn gör.
        LoadNextScene();
    }

    // ---------------------------------------------------------------- guidade skjutsteg

    // Ett skjutsteg (steg 4-7). Loopar tills spelaren dödar en zombie i targetLane.
    // Varje frame läser jag vad spelaren gör och svarar: Ctrl upprepar, vägg/fel håll ger tips,
    // skott väntar in kulan. Gör spelaren inget på 8 s upprepar jag frågan (inte medan zombien går).
    private IEnumerator ShootStep(int targetLane, AudioClip[] ask, string askSlot, bool lobStep, System.Action respawn, bool walking = false)
    {
        stepDone = false;
        int idleRepeats = 0; // hur många upprepningar i rad, för patience-repliken
        int escapesBefore = zombieEscapes;
        inRightLaneSaid = false;
        pauseMobsWhileTalking = true;

        yield return WaitForKeysReleased();
        lastInputTime = Time.time;

        while (!stepDone)
        {
            // Den gående zombien (steg 6) har tagit sig förbi: nytt försök med en ny zombie.
            if (walking)
            {
                if (zombieEscapes != escapesBefore)
                {
                    // Skrattet har redan spelats från zombien (som i levlarna).
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

            // Svara på det spelaren gjorde.
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
                    // 2026-10-28 (Alfred): Vid för tidigt släpp spelar jag bara holdLonger ("Too early, the lob
                    // wasn't charged yet"). Förut kom lobHowTo och hintLobHere direkt efter, alltså tre
                    // lob-repliker i rad, och det var för mycket. lobHowTo spelas bara om holdLonger saknas.
                    if (HasClips(holdLonger)) yield return Say(holdLonger, nameof(holdLonger));
                    else
                    {
                        // Tills "Too early..." är inspelad: "Almost. Try that again." (LaneHint nedanför säger vilken knapp)
                        WarnEmpty(nameof(holdLonger));
                        yield return SayExtra(tryAgain);
                        yield return SayExtra(lobHowTo);
                    }
                    // 2026-10-28 (Alfred): LaneHint ger nu bara gå vänster/höger om man står i fel fil
                    // (i rätt fil är den tyst i lob-steget när lobHowTo finns, se LaneHint).
                    yield return LaneHint(targetLane, lobStep);
                    yield return WaitForKeysReleased();
                    lastInputTime = Time.time;
                    continue;
                case Act.None:
                    break;
                default:
                    lastInputTime = Time.time;
                    break;
            }

            // Stilla för länge: upprepa instruktionen + tips (inte medan en riktig zombie går
            // eller medan man laddar en lob). Andra gången i rad lägger jag till en patience-replik.
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

    // Spelaren bytte fil. Kom hen till rätt fil säger jag det (en gång), annars kollar jag om
    // hen står framför en civil eller gick åt fel håll, och ger ett tips om vart hen ska.
    private IEnumerator OnMoved(int targetLane, bool lobStep)
    {
        int before = Mathf.Abs(previousLane - targetLane);
        int after = Mathf.Abs(lane - targetLane);

        if (lane == targetLane)
        {
            // Första gången man kommer rätt i ett steg: inRightLane istället för "skjut här"-tipset.
            // Tom slot, lob-steget eller senare gånger: vanliga tipset.
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

    // Spelaren sköt (eller lobbade). Jag väntar tills kulan är framme och kollar vad den träffade:
    // rätt zombie = steget klart, civil = tillsägelse och omstart, tom fil = bom + tips.
    private IEnumerator OnShot(Shot shot, int targetLane, AudioClip[] ask, string askSlot, bool lobStep, System.Action respawn)
    {
        // Vänta på att kulan flyger fram (10 enheter/s, som Bullet.cs).
        while (shot != null && !shot.done) yield return null;
        Mob hit = shot != null ? shot.killed : null;

        if (hit != null && hit.kind == Kind.Zombie && hit.lane == targetLane)
        {
            stepDone = true;
            yield return WaitWhileGuideOrSeconds(0.8f); // låt dödsljudet höras klart
            yield break;
        }

        if (hit != null && hit.kind == Kind.Civilian)
        {
            current.wrongPresses++;
            yield return WaitWhileGuideOrSeconds(0.8f);
            // I lob-steget betyder det att man tryckte Space istället för att lobba.
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
            // Dödade en zombie i en annan fil (finns inte i något steg idag): fortsätt bara.
            yield return LaneHint(targetLane, lobStep);
        }

        yield return WaitForKeysReleased();
        lastInputTime = Time.time;
    }

    // Tipset om vart spelaren ska: gå vänster, gå höger, eller (i rätt fil) skjut/lobba här.
    private IEnumerator LaneHint(int targetLane, bool lobStep)
    {
        if (lane > targetLane) yield return Say(hintGoLeft, nameof(hintGoLeft));
        else if (lane < targetLane) yield return Say(hintGoRight, nameof(hintGoRight));
        // 2026-10-28 (Alfred): I lob-steget spelar jag inte längre hintLobHere som stående tips när spelaren
        // redan står i rätt fil, för att det blev för många lob-repliker (lobHowTo säger redan hur man gör).
        // Bara om lobHowTo inte är inspelad än får hintLobHere vara kvar som reserv, annars är det tyst här.
        // Fel fil ger fortfarande hintGoLeft/hintGoRight ovanför.
        else if (lobStep)
        {
            if (!HasClips(lobHowTo)) yield return Say(hintLobHere, nameof(hintLobHere));
        }
        else yield return Say(hintShootHere, nameof(hintShootHere));
    }

    // ---------------------------------------------------------------- övningsvågen

    // Steg 8: övningsvågen (bara zombies och civila). En ny var 3-4:e sekund (Level 1: 3-5 s), 2-3 lever
    // samtidigt, två zombies i samma fil och en zombie bakom en civil (lob). Obegränsad ammo.
    // Slutar som en runda: allt försvinner på en gång när tiden är slut.
    private IEnumerator MiniWave()
    {
        yield return Say(explainWave, nameof(explainWave));

        // Schemat: när (sekunder), vad och i vilken fil (0 = vänster, 1 = mitten, 2 = höger).
        float[] times = { 0f, 4f, 8f, 11f, 15f, 18f, 22f, 25f };
        Kind[] kinds = { Kind.Zombie, Kind.Zombie, Kind.Civilian, Kind.Zombie, Kind.Zombie, Kind.Zombie, Kind.Civilian, Kind.Zombie };
        int[] spawnLanes = { 0, 2, 1, 1, 0, 0, 2, 1 };

        ClearMobs();
        pauseMobsWhileTalking = false; // som en riktig runda: ingenting väntar på rösten
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
            // Här blockerar jag aldrig spelet: Ctrl och kommentarerna spelas bara om rösten är tyst.
            if (act == Act.Ctrl && !guideSource.isPlaying) StartCoroutine(RepeatLast());

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

        // Tiden är slut: som i GameManager tas alla zombies och civila bort på en gång.
        ClearMobs();
        charging = false;
        lobReady = false;
        pauseMobsWhileTalking = true;
        current.score = score - scoreBefore;
        while (guideSource.isPlaying) yield return null;
        yield return Say(waveTimeUp, nameof(waveTimeUp));
    }

    // ---------------------------------------------------------------- spelaren (som Player.cs)

    // Läser tangentbordet en gång per frame och gör samma sak som Player.cs:
    // pilar = byt fil (vägg = duns), Space = skott, håll pil upp = ladda lob, Ctrl = upprepa.
    private Act ReadAction()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null) return Act.None;

        // Medan man laddar loben går det inte att gå eller skjuta (som i Player.cs).
        // Efter 1,5 s är loben redo. Släpper man innan dess blir det inget skott.
        if (charging)
        {
            if (!chargeChecked && Time.time - chargeStart >= chargeHoldSeconds)
            {
                chargeChecked = true;
                lobReady = true; // inget "redo"-ljud i levlarna heller
            }
            if (!keyboard.upArrowKey.isPressed)
            {
                charging = false;
                if (lobReady)
                {
                    lobReady = false;
                    PlayPlayer(lobFire); // loben drar ingen ammo, som i levlarna
                    lastShot = Fire(true);
                    current.lobsFired++;
                    return Act.LobFired;
                }
                // Släppte för tidigt: laddljudet stoppas och släppljudet spelas.
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

        // Vänster/höger pil: byt fil. Utanför kanten blir det väggduns istället.
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

        // R (reload) gör ingenting här: ammo är obegränsad i tutorialen.

        // Space: vanligt skott med skottljudet från spelet.
        if (keyboard.spaceKey.wasPressedThisFrame && Time.time >= lastShotTime + WeaponCooldown)
        {
            lastShotTime = Time.time;
            PlayPlayer(gunShot);
            lastShot = Fire(false);
            return Act.Shot;
        }

        // Pil upp: börja ladda loben (laddljudet spelas).
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

    // Flyttar spelaren en fil och spelar fotsteg.
    private void MoveLane(int dir)
    {
        previousLane = lane;
        lane = Mathf.Clamp(lane + dir, 0, 2);
        PlacePlayer();
        PlayStep();
    }

    // Flyttar spelarens ljudkälla till rätt fil, så skotten hörs därifrån.
    // 2026-10-28 (Alfred): lyssnaren sitter på samma objekt, så den flyttar med (som Player i Level 1/2).
    private void PlacePlayer()
    {
        if (playerSource != null)
            playerSource.transform.position = origin + new Vector3((lane - 1) * LaneX, PlayerY, 0f);
    }

    // Spelarljud (skott, lob, vägg) på Player-volymen från menyn.
    private void PlayPlayer(AudioClip clip)
    {
        if (clip != null && playerSource != null)
            playerSource.PlayOneShot(clip, PlayerLevel);
    }

    // Fotsteg som i Player.cs: lite cooldown, och ljudet stängs av efter 2 s i Update().
    private void PlayStep()
    {
        if (footstep == null || stepSource == null || Time.time < nextStepTime) return;
        nextStepTime = Time.time + StepPlayCooldown;
        stepSource.Stop();
        stepSource.PlayOneShot(footstep, PlayerLevel);
        stepStopAt = Time.time + StepSoundDuration;
    }

    // Vanligt skott: träffar första zombie/civil i filen. Lob (bågskott): hoppar över den
    // första och träffar nästa i filen, finns ingen andra blir det ingen träff.
    private Shot Fire(bool lob)
    {
        current.shotsFired++;
        Mob victim = FirstInLane(lane, lob ? 1 : 0);
        Shot shot = new Shot { victim = victim };
        // Kulan tar tid: avståndet till målet delat med kulans fart.
        shot.hitTime = Time.time + (victim != null ? Mathf.Max(0f, victim.y - PlayerY) / BulletSpeed : 0f);
        shots.Add(shot);
        return shot;
    }

    // Kollar vilka kulor som kommit fram den här framen: lever målet dör det, annars är det en bom.
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

    // ---------------------------------------------------------------- fiender (som Enemy.cs)

    // Skapar ett mål med en egen 3D-ljudkälla i rätt fil, med samma inställningar som prefabsen:
    // fast panorering per fil, linjär rolloff 2-15 och en lowpass som slås på när något står framför.
    private Mob SpawnMob(Kind kind, int mobLane, float y, bool walking)
    {
        Mob m = new Mob { kind = kind, lane = mobLane, y = y, walking = walking };
        GameObject go = new GameObject(kind + "_Lane" + mobLane);
        go.transform.SetParent(transform, false);
        m.source = go.AddComponent<AudioSource>();
        m.source.playOnAwake = false;
        m.source.volume = 1f;
        m.source.priority = 128;
        m.source.panStereo = mobLane == 1 ? 0f : (mobLane > 1 ? 1f : -1f); // som Enemy.SetSoundPosition: vänster -1, mitten 0, höger 1
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

        // Spawnljudet direkt, och gå-loopen startar efter 1,6 s (sköts i Update).
        MobSound(m, kind == Kind.Zombie ? zombieSpawn : civilianSpawn);
        m.idleAt = IdleStartAge;
        return m;
    }

    // Flyttar målets ljudkälla till dess fil och y-läge.
    private void PlaceMob(Mob m)
    {
        if (m.source != null)
            m.source.transform.position = origin + new Vector3((m.lane - 1) * LaneX, m.y, 0f);
    }

    // Som Enemy.MakeSound: cooldown, stoppa, spela en gång på Monster-volymen och stäng av ljudet efter 1,5 s.
    private void MobSound(Mob m, AudioClip clip)
    {
        if (m.source == null || clip == null) return;
        if (Time.time < m.nextPlayTime) return;
        m.nextPlayTime = Time.time + MobPlayCooldown;
        m.idleAt = -1f; // motsvarar StopAllCoroutines: gå-loopen startar inte igen
        m.source.Stop();
        // 2026-10-28 (Alfred): civila spelades på Monster-volymen + 0,5 här, men Enemy.MakeSound i levlarna spelar
        // både zombies och civila på bara SettingsManager.monsterSoundLevel. Nu är det samma som i Level 1/2.
        m.source.PlayOneShot(clip, MonsterLevel);
        m.stopAt = m.age + MobSoundDuration;
    }

    // Målet blev träffat: dödsljud (zombie) eller skrik (civil), poäng till loggen, tas bort efter 1,5 s.
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

    // Målet kom förbi spelaren (KillBox): zombien skrattar (-100), en civil ger +50 tyst.
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
            MobSound(m, civilianEscape); // tom i levlarna: gå-loopen fortsätter bara
        }
    }

    // Varje frame: stäng av fotsteg, följ Ambience-reglaget, flytta/åldra målen, starta
    // gå-loopar, skrik och KillBox, dämpa ljud bakom andra mål och kolla kulorna.
    private void Update()
    {
        if (stepStopAt >= 0f && Time.time >= stepStopAt)
        {
            stepStopAt = -1f;
            if (stepSource != null) stepSource.Stop();
        }
        foreach (AudioSource a in ambienceSources)
            if (a != null) a.volume = AmbienceLevel; // som GameManager.UpdateSoundVolume

        // Pratar rösten i ett guidat steg står målen still (deras tid går inte).
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
                // Dags för gå-loopen.
                AudioClip loop = m.kind == Kind.Zombie ? zombieWalkLoop : civilianWalkLoop;
                if (loop != null)
                {
                    // Som Enemy.StartIdleSounds: Play() på källans volym (1), inte skalad med reglaget.
                    m.source.clip = loop;
                    m.source.loop = true;
                    m.source.Play();
                }
            }
            if (!m.alive) continue;
            // Gående mål rör sig mot spelaren med 1 enhet/s.
            if (m.walking)
            {
                m.y = SpawnY - WalkSpeed * m.age;
                if (m.kind == Kind.Zombie && !m.screamed && m.age >= ScreamAge)
                {
                    m.screamed = true;
                    MobSound(m, zombieScream); // stoppar loopen, sen är det tyst ca 1,5 s senare tills KillBox
                }
                if (m.age >= KillBoxAge) Escape(m);
            }
            PlaceMob(m);
        }

        // Dämpning: lowpass när en annan zombie/civil står inom 3,5 enheter framför (som raycasten i Enemy.Update).
        // Jag mäter från framkanten på den bakre (y - 0,6) till bakkanten på den främre (y + 0,5).
        foreach (Mob m in mobs)
        {
            if (m.filter == null) continue;
            bool blocked = false;
            if (m.alive)
            {
                foreach (Mob o in mobs)
                {
                    if (o == m || !o.alive || o.lane != m.lane || o.y >= m.y) continue;
                    if ((m.y - 0.6f) - (o.y + 0.5f) <= MuffleRay) { blocked = true; break; }
                }
            }
            m.filter.enabled = blocked;
        }

        ResolveShots();
    }

    // Målen i en fil sorterade från närmast spelaren. skip = 0 ger första, 1 ger den bakom (lob).
    private Mob FirstInLane(int l, int skip)
    {
        List<Mob> inLane = new List<Mob>();
        foreach (Mob m in mobs)
            if (m.alive && m.lane == l && m.y > PlayerY) inLane.Add(m);
        inLane.Sort((a, b) => a.y.CompareTo(b.y));
        return skip < inLane.Count ? inLane[skip] : null;
    }


    // Hur många levande mål av en sort som finns just nu.
    private int AliveCount(Kind kind)
    {
        int n = 0;
        foreach (Mob m in mobs) if (m.alive && m.kind == kind) n++;
        return n;
    }

    // Tar bort ett mål och dess ljudkälla helt.
    private void RemoveMob(Mob m)
    {
        mobs.Remove(m);
        if (m.source != null) Destroy(m.source.gameObject);
    }

    // Tömmer alla mål och kulor, används när ett steg börjar om eller är klart.
    private void ClearMobs()
    {
        for (int i = mobs.Count - 1; i >= 0; i--) RemoveMob(mobs[i]);
        foreach (Shot s in shots) s.done = true; // ett steg som väntar på en kula får inte fastna
        shots.Clear();
    }

    // ---------------------------------------------------------------- ljudkällor och volymer

    // Volymerna kommer direkt från reglagen i menyn (SettingsManager).
    private static float MonsterLevel => SettingsManager.monsterSoundLevel;
    private static float PlayerLevel => SettingsManager.playerSoundLevel;
    private static float AmbienceLevel => SettingsManager.ambienceSoundLevel;

    private void InitVolumes()
    {
        // Reglagen är 0 tills menyn har satt dem (t.ex. om man startar tutorialen direkt i editorn).
        // 2026-10-28 (Alfred): jag anropar nu samma SettingsManager.EnsureDefaultSoundLevels() som GameManager.Start
        // i levlarna, istället för min egen koll. Har menyn inte körts får alla reglage menyns standardvärden
        // (player 0.25, monster 0.4, ambience 0.05, operator 0.5, menu 0.5). Har menyn satt dem rörs ingenting.
        SettingsManager.EnsureDefaultSoundLevels();

        // Rösten: volymen från Inspectorn (1 i Tutorial.unity) gånger Operator-reglaget.
        guideBaseVolume = guideSource.volume;
        ApplyGuideVolume();
    }

    // 2026-10-28 (Alfred): rösten följer Operator-reglaget på samma sätt som GameManager.PlayVoiceQueue i levlarna
    // (voiceSource.volume = operatorSoundLevel före varje replik). Förut använde jag 0,5 om reglaget var 0, men
    // det gör inte levlarna, så nu är rösten lika stark mot skotten och fienderna som i Level 1/2.
    private void ApplyGuideVolume()
    {
        if (guideSource == null) return;
        guideSource.volume = guideUsesOperatorVolume ? guideBaseVolume * SettingsManager.operatorSoundLevel : guideBaseVolume;
    }

    private void CreateSources()
    {
        // Filerna räknas från en punkt 10 enheter framför kameran (Main Camera på (0, 0, -10)).
        // 2026-10-28 (Alfred): förut satt lyssnaren kvar på Main Camera, 10 enheter bakom spelaren. I Level 1/2
        // sitter AudioListener på Player-objektet (tillagd komponent i scenerna) och följer med när man byter fil.
        // Med lyssnaren på kameran var skottet (3D, logaritmisk 1-500) ca 11 enheter bort och spelades på
        // ungefär 1/11 av volymen, alltså nästan inte hörbart. Samma sak gjorde fienderna och elden tystare.
        // Nu stänger jag av lyssnarna i scenen och sätter en lyssnare på spelarens ljudkälla, precis som i levlarna.
        AudioListener[] sceneListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        Camera cam = Camera.main;
        Vector3 viewPoint = sceneListeners.Length > 0 ? sceneListeners[0].transform.position
                          : (cam != null ? cam.transform.position : transform.position);
        origin = viewPoint + new Vector3(0f, 0f, 10f);
        foreach (AudioListener l in sceneListeners) l.enabled = false;

        playerSource = NewSource("PlayerSounds");
        playerSource.spatialBlend = 1f; // som Player.prefab källa 0: 3D, logaritmisk 1-500
        playerSource.rolloffMode = AudioRolloffMode.Logarithmic;
        playerSource.minDistance = 1f;
        playerSource.maxDistance = 500f;
        playerSource.gameObject.AddComponent<AudioListener>(); // 2026-10-28 (Alfred): lyssnaren följer spelaren, som i Level 1/2
        stepSource = NewSource("PlayerSteps"); // som Player.prefab källa 1: 2D
        PlacePlayer();
    }

    // Startar eldljudet till vänster och höger, som i levlarna (en gång).
    private void StartAmbience()
    {
        if (fireAmbience == null || ambienceSources.Count > 0) return;
        // Level1/2 LightLeft / LightRight: FireSound i loop, 3D logaritmisk, volym = Ambience-reglaget.
        // 2026-10-28 (Alfred): exakta lägen och minDistance från LightLeft/LightRight i Level1/2.unity
        // (förut avrundat till y -4, z 0 och 3,15 för båda).
        Vector3[] positions = { new Vector3(-9.1f, -3.99f, -0.06737615f), new Vector3(9.2f, -3.95f, -0.06737615f) };
        float[] minDistances = { 3.1782417f, 3.1322117f };
        for (int i = 0; i < 2; i++)
        {
            AudioSource a = NewSource(i == 0 ? "FireLeft" : "FireRight");
            a.transform.position = origin + positions[i];
            a.spatialBlend = 1f;
            a.rolloffMode = AudioRolloffMode.Logarithmic;
            a.minDistance = minDistances[i];
            a.maxDistance = 500f;
            a.panStereo = i == 0 ? -1f : 1f;
            a.clip = fireAmbience;
            a.loop = true;
            a.volume = AmbienceLevel;
            a.Play();
            ambienceSources.Add(a);
        }
    }

    // Skapar en tom 2D-ljudkälla som barn till tutorial-objektet.
    private AudioSource NewSource(string sourceName)
    {
        GameObject go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    // Tar bort de gamla tagningarna (RetiredClips) från de slots där de kan ligga kvar.
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

    // Ger tillbaka listan utan tomma platser och utan gamla tagningar (jämför på klippets namn).
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

    // Laddar nästa scen. Är den tom, "prototype" eller inte med i Build Settings blir det Level1.
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
            // Samma som LevelLoader.StartGame(): nytt playthrough-nummer till playerEvents.json.
            SettingsManager.playthrough++;
            LogSystem.ResetCount();
        }
        SceneManager.LoadScene(target);
    }

    // ---------------------------------------------------------------- rösten

    // Spelar upp de inspelade klippen i en slot. Ingen TTS: är sloten tom blir det tyst och jag
    // loggar en varning med slotens namn, så man ser vilka repliker som saknas.
    private IEnumerator Say(AudioClip[] clips, string slot)
    {
        if (HasClips(clips))
        {
            yield return PlayAudioList(clips);
            yield break;
        }
        WarnEmpty(slot);
    }

    // Kollar om något av klippen redan finns i listan (så Fm7 inte spelas två gånger).
    private static bool ContainsAny(AudioClip[] list, AudioClip[] clips)
    {
        if (list == null || clips == null) return false;
        foreach (AudioClip c in clips)
            if (c != null && System.Array.IndexOf(list, c) >= 0) return true;
        return false;
    }

    // Loggar en tom slot, men bara första gången.
    private void WarnEmpty(string slot)
    {
        if (!string.IsNullOrEmpty(slot) && warnedSlots.Add(slot))
            Debug.LogWarning("[Tutorial] No recorded clip in slot '" + slot + "', skipped.", this);
    }

    // Spelar alla klipp i listan i ordning och väntar tills de är klara. Listan blir "senaste repliken" för Ctrl.
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
            ApplyGuideVolume(); // 2026-10-28 (Alfred): som GameManager, volymen sätts före varje replik
            guideSource.Play();

            // Vänta tills klippet har spelat klart
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
                // Ctrl medan jag pratar: börja om just den här repliken från början
                current.ctrlRepeats++;
                guideSource.Stop();
                i--;
                yield return null;
            }
        }
    }

    // Frivillig extra-replik: tyst om sloten är tom (ingen paus, ingen logg).
    // Den blir inte "senaste repliken", så Ctrl upprepar fortfarande huvudinstruktionen.
    private IEnumerator SayExtra(AudioClip[] clips)
    {
        if (!HasClips(clips))
            yield break;

        AudioClip[] keepClips = lastClips;
        yield return PlayAudioList(clips);
        lastClips = keepClips;
    }

    // Sant om listan har minst ett riktigt klipp.
    private static bool HasClips(AudioClip[] clips)
    {
        if (clips == null) return false;
        foreach (AudioClip c in clips) if (c != null) return true;
        return false;
    }

    // Ctrl: spela senaste instruktionen igen (finns ingen än händer inget)
    private IEnumerator RepeatLast()
    {
        if (lastClips != null)
            yield return PlayAudioList(lastClips);
    }

    // Väntar minst så många sekunder, och längre om rösten fortfarande pratar.
    private IEnumerator WaitWhileGuideOrSeconds(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end || (guideSource != null && guideSource.isPlaying))
            yield return null;
    }

    // Sant den frame som vänster eller höger Ctrl trycks ner.
    private bool CtrlPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.rightCtrlKey.wasPressedThisFrame);
    }

    // Steg 2 och 3: vänta tills spelaren trycker rätt pil. Fel pil ger rättelsen + frågan igen,
    // Ctrl upprepar, och är spelaren stilla 8 s upprepar jag frågan.
    private IEnumerator WaitForDirection(
        Key correctKey,
        Key wrongKey,
        AudioClip[] wrongAudio,
        AudioClip[] repeatInstruction)
    {
        // Ta inte emot en knapp som hölls nere medan jag pratade.
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
                    // Spela rättelsen
                    yield return PlayAudioList(wrongAudio);

                    // Upprepa instruktionen
                    yield return PlayAudioList(repeatInstruction);

                    // Vänta på ett nytt tryck
                    yield return WaitForKeysReleased();
                    lastInputTime = Time.time;
                }
                else if (keyboard[correctKey].wasPressedThisFrame)
                {
                    // Rätt knapp
                    yield break;
                }
            }

            // Stilla för länge: upprepa instruktionen
            if (Time.time - lastInputTime > idleRepeatSeconds)
            {
                current.idleRepeats++;
                yield return PlayAudioList(repeatInstruction);
                lastInputTime = Time.time;
            }

            yield return null;
        }
    }

    // Väntar tills pilarna, Space och R är släppta, så ett gammalt tryck inte räknas
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

        // Börja lyssna först nästa frame
        yield return null;
    }

    // ---------------------------------------------------------------- testloggen (tutorialEvents.json)

    // Sparas bredvid playerEvents.json (projektmappen i editorn, bredvid _Data-mappen i ett bygge),
    // med JsonUtility som LogSystem. En session per körning, en rad per steg, skrivs om efter varje steg.
    private void StartLog()
    {
        // Läser in den gamla filen om den finns, så tidigare körningar sparas.
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

    // Startar loggen för ett nytt steg.
    private void BeginStep(string stepName)
    {
        current = new StepLog { step = stepName };
        stepStartTime = Time.time;
    }

    // Avslutar steget: sparar tiden, lägger till steget i sessionen och skriver filen.
    private void EndStep()
    {
        current.seconds = Time.time - stepStartTime;
        session.steps.Add(current);
        session.totalSeconds = Time.time - sessionStartTime;
        session.totalScore = score;
        WriteLog();
    }

    // Hela tutorialen är klar: markera sessionen som completed och skriv filen.
    private void FinishLog()
    {
        session.completed = true;
        session.totalSeconds = Time.time - sessionStartTime;
        session.totalScore = score;
        WriteLog();
    }

    // Skriver hela filen. Går det fel loggar jag bara en varning, tutorialen ska aldrig krascha av loggen.
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
    // Inspectorn: TutorialManager-komponentens meny (tre prickar) > "Assign default tutorial sounds",
    // spara sen scenen. Den fyller tomma slots, tar bort gamla tagningar och sätter Next Scene till Level1.
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

    // Mapparna och prefabsen som standardljuden hämtas från.
    private const string NewClips = "Assets/Sounds/Newtutorialmaterial/";
    private const string IntroClips = "Assets/Sounds/Tutorial.intro/";
    private const string Ucs = "Assets/Sounds/Ljudklipp_UCS_Remastered/";
    private const string ZombiePrefab = "Assets/Prefabs/Enemies/StandardEnemy.prefab";
    private const string CivilianPrefab = "Assets/Prefabs/Enemies/Civillian.prefab";
    private const string PlayerPrefab = "Assets/Prefabs/Player/Player.prefab";

    // Spelljuden läses alltid om från level-prefabsen (bara läsning), så tutorialen följer med om
    // teamet ändrar något där. Röst-slots: bara TOMMA fylls (PickSet behåller det som valts i Inspectorn).
    // Körs också i början av Start() i editorn, så det funkar i Play mode även om man inte sparat.
    private void FillMissingSounds()
    {
        // Spelljud, samma klipp som Level 1/2 (fallback-sökvägen om prefaben inte har något)
        zombieSpawn = LevelClip(ZombiePrefab, "Enemy", "spawnSound", "Assets/Sounds/Monster/Standard/ZombieSpawn_2.wav");
        zombieWalkLoop = LevelClip(ZombiePrefab, "Enemy", "idleSounds.Array.data[0]", "Assets/Sounds/Monster/Standard/ZombieAttack_2.wav");
        zombieScream = LevelClip(ZombiePrefab, "Enemy", "attackSound", "Assets/Sounds/Monster/Standard/ZombieAttack_3.wav");
        zombieDeath = LevelClip(ZombiePrefab, "Enemy", "deathSound", "Assets/Sounds/Monster/Standard/ZombieDeath_2.wav");
        zombieEscape = LevelClip(ZombiePrefab, "Enemy", "escapeSound", "Assets/Sounds/Monster/Standard/ZombieEscape_1.wav");
        civilianSpawn = LevelClip(CivilianPrefab, "Enemy", "spawnSound", "Assets/Sounds/Civilllian/CivilianOld_idle2.wav");
        civilianWalkLoop = LevelClip(CivilianPrefab, "Enemy", "idleSounds.Array.data[0]", "Assets/Sounds/Civilllian/CivilianOld_idle3.wav");
        civilianHit = LevelClip(CivilianPrefab, "Enemy", "deathSound", "Assets/Sounds/Civilllian/CivilianOld_hit1.wav");
        civilianEscape = LevelClip(CivilianPrefab, "Enemy", "escapeSound", null);
        gunShot = LevelClip(PlayerPrefab, "Player", "playerShoot", "Assets/Sounds/Player/gunShotSound.mp3");
        lobCharge = LevelClip(PlayerPrefab, "Player", "chargeWeapon", "Assets/Sounds/Player/ChargeWeapon.wav");
        lobFire = LevelClip(PlayerPrefab, "Player", "firedChargedWeapon", "Assets/Sounds/Player/ChargeShot.wav");
        lobRelease = LevelClip(PlayerPrefab, "Player", "releaseChargingWeapon", "Assets/Sounds/Player/ReleaseChargingWeapon.wav");
        wallThud = LevelClip(PlayerPrefab, "Player", "wallHit", "Assets/Sounds/Thud.wav");
        footstep = LevelClip(PlayerPrefab, "Player", "playerMove.Array.data[0]", "Assets/Sounds/Player/step_sound.mp3");
        fireAmbience = Pick(fireAmbience, "Assets/Sounds/Ambience/FireSound.mp3");

        // Nya inspelningar (Newtutorialmaterial, filnamnet = det som sägs)
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

        // Nya inspelningar: extra ("Zombie gets loader the closer they get" används inte: levlarna blir knappt starkare)
        calibrationDone = PickSet(calibrationDone, "Assets/Sounds/FirstMovement/Fm7-DoneLR.wav");            // säger: "Looks like your headset is calibrated."
        explainLanes = PickSet(explainLanes, NewClips + "PRess the left or right arrow to move a lane at a time.wav");
        explainSoundDirection = PickSet(explainSoundDirection, NewClips + "Zombies makes sound from the lane they come from, move there.wav");
        explainLob = PickSet(explainLob, NewClips + "Lob shots is the only way to shot awhats behind a civilian.wav");
        inRightLane = PickSet(inRightLane, NewClips + "Great you are in the right lane now, take the shot.wav");
        tryAgain = PickSet(tryAgain, NewClips + "Almost, try again.wav");
        praiseShort = PickSet(praiseShort, NewClips + "Nice oneee.wav");
        lobKeepInMind = PickSet(lobKeepInMind, NewClips + "thats the lob.wav");
        patience = PickSet(patience, NewClips + "Take your time.wav", NewClips + "Im not goping anywhere.wav", NewClips + "I still get payed on the clock.wav");
        closing = PickSet(closing, NewClips + "I taught you all i know, lets go.wav");

        // Tidigare inspelningar (Tutorial.intro)
        explainDummy = PickSet(explainDummy, IntroClips + "Testing you weapomn.wav", IntroClips + "Dont move to the sides.wav");
        dummyHit = PickSet(dummyHit, IntroClips + "Well Done.wav", IntroClips + "Shoot the dead Dead.wav");
        askZombieRight = PickSet(askZombieRight, IntroClips + "Civilian and zombie dummy.wav", IntroClips + "Dont shoot civilians.wav");
        zombieRightHit = PickSet(zombieRightHit, IntroClips + "you shot the correct target.wav");
        // "Press dowm MK1" säger "until you hear a click": levlarna har inget klick, så den är bortplockad.
        askLobber = PickSet(askLobber, IntroClips + "Testing mortar function.wav", IntroClips + "Once released - MK 1.wav");
        lobberHit = PickSet(lobberHit, IntroClips + "Released button success.wav");
        // holdLonger: "Released to early" säger "wait for the click", så sloten står tom tills den spelas in igen.
        shotCivilian = PickSet(shotCivilian, IntroClips + "Dont shoot the civilian.wav");
        nearCivilian = PickSet(nearCivilian, IntroClips + "Dont shoot civilians.wav");

        // Operatörsklipp från Ljudklipp_UCS_Remastered (maskintranskriberade, lyssna och kolla)
        realZombieHit = PickSet(realZombieHit, Ucs + "Hit/Hit_Remaster_3.wav");                       // säger: "Target neutralized."
        explainWave = PickSet(explainWave, Ucs + "Guidance/Guidance_Remaster_15.wav");                // säger: "Targets approaching from multiple directions."
        waveMultiple = PickSet(waveMultiple, Ucs + "Intense/Intense_Remaster_3.wav");                 // säger: "...multiple contacts closing in."
        waveCivilianShot = PickSet(waveCivilianShot, Ucs + "CivilianKill/CivilianKill_Remaster_5.wav"); // säger: "I will pretend that I didn't see that."
        waveTimeUp = PickSet(waveTimeUp, Ucs + "Ljud1/Ljud1_Remaster_1.wav");                         // säger: "Alright, that's time."
        tutorialComplete = PickSet(tutorialComplete, Ucs + "Ljud1/Ljud1_Remaster_4.wav");             // säger: "Alright, it looks like we're finished here."

        // Repliker som ska spelas in (Voice_lines_to_record_20261027). Spara dem i Newtutorialmaterial med
        // exakt de här namnen så plockas de upp här. Tills dess står sloten tom (ingen varning).
        lobHowTo = PickIfRecorded(lobHowTo, "Hold the up arrow until the charge sound fades, then let go.wav");
        holdLonger = PickIfRecorded(holdLonger, "Too early, the lob wasnt charged yet.wav");
        // "Last drill..." är inspelad nu, så den tar över efter operatörsklippet (Guidance_Remaster_15) innan övningsvågen.
        // Har jag lagt något annat i explainWave i Inspectorn rör jag det inte.
        explainWave = ReplaceDefault(explainWave, Ucs + "Guidance/Guidance_Remaster_15.wav", "Last drill, shoot the zombies, spare the civilians.wav");
    }

    // Fyller en tom slot med en ny inspelning om filen finns, annars händer inget.
    private static AudioClip[] PickIfRecorded(AudioClip[] current, string fileName)
    {
        if (HasClips(current)) return current;
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(NewClips + fileName);
        return clip != null ? new[] { clip } : current;
    }

    // Byter ut ett gammalt standardklipp mot min nya inspelning om filen finns. Är sloten tom fyller jag den,
    // ligger något annat än standardklippet där (valt i Inspectorn) behåller jag det.
    private static AudioClip[] ReplaceDefault(AudioClip[] current, string oldPath, string fileName)
    {
        AudioClip recorded = AssetDatabase.LoadAssetAtPath<AudioClip>(NewClips + fileName);
        if (recorded == null) return current;
        if (!HasClips(current)) return new[] { recorded };
        AudioClip old = AssetDatabase.LoadAssetAtPath<AudioClip>(oldPath);
        if (current.Length == 1 && current[0] == old) return new[] { recorded };
        return current;
    }

    // Läser ett klipp från en level-prefab utan att ändra den. Finns inget där tar jag den kända sökvägen.
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

    // Fyller en tom slot med klippen på sökvägarna. Saknas en fil loggar jag en varning.
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

    // Samma sak för ett enskilt klipp (eldljudet).
    private static AudioClip Pick(AudioClip current, string path)
    {
        return current != null ? current : AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
#endif
}
