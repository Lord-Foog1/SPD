using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TutorialManager : MonoBehaviour
{
    private enum Target { None, Zombie, Civilian }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource guideSource;
    [SerializeField] private AudioSource dummySource;

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
    [SerializeField] private AudioClip dummySound;
    [SerializeField] private AudioClip[] dummyHit;

    [Header("Step 5: Zombie right, civilian left")]
    [SerializeField] private AudioClip[] askZombieRight;
    [SerializeField] private AudioClip[] zombieRightHit;

    [Header("Step 6: Real zombie approaching")]
    [SerializeField] private AudioClip[] askRealZombie;
    [SerializeField] private AudioClip[] realZombieHit;
    [SerializeField] private AudioClip[] realZombieReachedYou;
    [SerializeField] private float approachSeconds = 12f;

    [Header("Step 7: Lobber")]
    [SerializeField] private AudioClip[] askLobber;
    [SerializeField] private AudioClip[] lobberHit;
    [SerializeField] private AudioClip[] tappedCivilian;
    [SerializeField] private AudioClip[] holdLonger;
    [SerializeField] private float chargeHoldSeconds = 1.5f; // matches Player "Charge Special" Hold(1.5)

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

    [Header("Sound Effects")]
    [SerializeField] private AudioClip gunShot;
    [SerializeField] private AudioClip lobShot;
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip wallSound;
    [SerializeField] private AudioClip[] zombieSounds;
    [SerializeField] private AudioClip[] civilianSounds;
    [SerializeField] private float targetSoundGap = 0.6f;
    [SerializeField] private AudioClip hitZombie;
    [SerializeField] private AudioClip hitCivilian;

    [Header("Ending")]
    [SerializeField] private AudioClip[] tutorialComplete;
    [SerializeField] private string nextScene = "Prototype";

    [Header("Extras (optional)")]
    // Optional narrator lines. An empty slot is skipped silently (no delay, no warning).
    [SerializeField] private AudioClip[] explainLanes;          // after correctRight, before Step 4
    [SerializeField] private AudioClip[] explainSoundDirection; // start of Step 5, before askZombieRight
    [SerializeField] private AudioClip[] explainApproach;       // Step 6, after askRealZombie
    [SerializeField] private AudioClip[] explainLob;            // Step 7, before askLobber
    [SerializeField] private AudioClip[] inRightLane;           // first arrival in the target lane per step (non-lob steps)
    [SerializeField] private AudioClip[] tryAgain;              // after missedEmptyLane
    [SerializeField] private AudioClip[] praiseShort;           // after zombieRightHit and realZombieHit
    [SerializeField] private AudioClip[] lobKeepInMind;         // after lobberHit
    [SerializeField] private AudioClip[] patience;              // second idle repeat in a row during a shooting step
    [SerializeField] private AudioClip[] closing;               // after tutorialComplete, before the next scene

    [Header("Timing")]
    [SerializeField] private float idleRepeatSeconds = 8f;

    private int lane = 1; // 0 = left, 1 = middle, 2 = right
    private readonly Target[] lanes = new Target[3];
    private int behindLane = -1;
    private readonly AudioSource[] laneSources = new AudioSource[3];
    private AudioSource behindSource;
    private AudioSource sfxSource;
    private AudioSource lobSource;
    private AudioClip beepLow;
    private AudioClip beepHigh;
    private float lastInputTime;
    private float approachTime;
    private bool stepDone;
    private readonly AudioClip[][] laneSets = new AudioClip[3][];
    private AudioClip[] behindSet;
    private readonly float[] nextPlay = new float[4];
    private readonly int[] nextIndex = new int[4];
    private bool targetsActive;
    private AudioClip[] lastClips; // last guide line, for Ctrl repeat
    private readonly System.Collections.Generic.HashSet<string> warnedSlots = new System.Collections.Generic.HashSet<string>();
    private bool inRightLaneSaid; // inRightLane plays once per shooting step

    private IEnumerator Start()
    {
        if (guideSource == null)
        {
            Debug.LogError("TutorialManager: assign Guide Source in the Inspector.", this);
            yield break;
        }
        guideSource.loop = false;

        if (string.Equals(nextScene, "prototype", System.StringComparison.OrdinalIgnoreCase))
            nextScene = "Prototype";

#if UNITY_EDITOR
        FillMissingSounds();
#endif
        CreateSources();
        lane = 1;

        // STEP 0: How to repeat (Ctrl)
        yield return Say(repeatHelp, nameof(repeatHelp));

        // STEP 1: Introduction
        yield return PlayAudioList(introduction);

        // STEP 2: Face left first, matching the recorded instructions.
        yield return PlayAudioList(askLeft);
        yield return WaitForDirection(
            Key.LeftArrow,
            Key.RightArrow,
            wrongLeft,
            askLeft
        );
        MoveLane(-1); // middle -> left
        yield return PlayAudioList(correctLeft);

        // STEP 3: correctLeft may already say "now right";
        // leave askRight empty in that case to avoid repeating it.
        yield return PlayAudioList(askRight);
        yield return WaitForDirection(
            Key.RightArrow,
            Key.LeftArrow,
            wrongRight,
            askRight
        );
        MoveLane(1); // left -> middle
        yield return PlayAudioList(correctRight);
        yield return SayExtra(explainLanes);

        // STEP 4: Dummy zombie in front (middle lane)
        SetTargets(Target.None, Target.Zombie, Target.None, -1, dummySound != null ? new[] { dummySound } : zombieSounds);
        yield return Say(explainDummy, nameof(explainDummy));
        yield return ShootStep(1, explainDummy, nameof(explainDummy), false, false);
        StopTargets();
        yield return Say(dummyHit, nameof(dummyHit));

        // STEP 5: Zombie right, civilian left
        SetTargets(Target.Civilian, Target.None, Target.Zombie, -1, zombieSounds);
        yield return SayExtra(explainSoundDirection);
        yield return Say(askZombieRight, nameof(askZombieRight));
        yield return ShootStep(2, askZombieRight, nameof(askZombieRight), false, false);
        StopTargets();
        yield return Say(zombieRightHit, nameof(zombieRightHit));
        yield return SayExtra(praiseShort);

        // STEP 6: Real zombie approaching in the middle lane
        SetTargets(Target.None, Target.Zombie, Target.None, -1, zombieSounds);
        yield return Say(askRealZombie, nameof(askRealZombie));
        yield return SayExtra(explainApproach);
        yield return ShootStep(1, askRealZombie, nameof(askRealZombie), false, true);
        StopTargets();
        yield return Say(realZombieHit, nameof(realZombieHit));
        yield return SayExtra(praiseShort);

        // STEP 7: Civilian in the middle, zombie behind: lob over
        SetTargets(Target.None, Target.Civilian, Target.None, 1, zombieSounds);
        yield return SayExtra(explainLob);
        yield return Say(askLobber, nameof(askLobber));
        yield return ShootStep(1, askLobber, nameof(askLobber), true, false);
        StopTargets();
        yield return Say(lobberHit, nameof(lobberHit));
        yield return SayExtra(lobKeepInMind);

        // STEP 8: Tutorial completed
        yield return Say(tutorialComplete, nameof(tutorialComplete));
        yield return SayExtra(closing);

        // STEP 9: Load actual game
        SceneManager.LoadScene(nextScene);
    }

    // One shooting exercise. Loops until the right target is hit.
    private IEnumerator ShootStep(int targetLane, AudioClip[] ask, string askSlot, bool lobStep, bool approach)
    {
        stepDone = false;
        approachTime = 0f;
        float holdStart = -1f;
        bool charged = false;
        int idleRepeats = 0; // idle repeats in a row, for the patience line
        inRightLaneSaid = false;
        UpdatePans();

        yield return WaitForKeysReleased();
        lastInputTime = Time.time;

        while (!stepDone)
        {
            Keyboard keyboard = Keyboard.current;

            if (approach)
            {
                approachTime += Time.deltaTime;
                UpdatePans();
                if (approachTime >= approachSeconds)
                {
                    StopTargets();
                    PlaySfx(hitZombie != null ? hitZombie : beepLow);
                    yield return Say(realZombieReachedYou, nameof(realZombieReachedYou));
                    approachTime = 0f;
                    RestartTargets();
                    yield return Say(ask, askSlot);
                    yield return WaitForKeysReleased();
                    lastInputTime = Time.time;
                    continue;
                }
            }

            if (keyboard != null)
            {
                if (holdStart < 0f && CtrlPressed())
                {
                    idleRepeats = 0;
                    yield return RepeatLast();
                    lastInputTime = Time.time;
                    continue;
                }

                int dir = 0;
                if (keyboard.leftArrowKey.wasPressedThisFrame) dir = -1;
                else if (keyboard.rightArrowKey.wasPressedThisFrame) dir = 1;

                if (dir != 0 && holdStart < 0f)
                {
                    idleRepeats = 0;
                    yield return OnMove(dir, targetLane, lobStep);
                    continue;
                }

                if (keyboard.spaceKey.wasPressedThisFrame && holdStart < 0f)
                {
                    idleRepeats = 0;
                    yield return OnShot(false, targetLane, ask, askSlot, lobStep);
                    continue;
                }

                if (lobStep)
                {
                    if (keyboard.upArrowKey.wasPressedThisFrame)
                    {
                        idleRepeats = 0;
                        holdStart = Time.time;
                        charged = false;
                        lastInputTime = Time.time;
                    }
                    else if (holdStart >= 0f && keyboard.upArrowKey.isPressed)
                    {
                        if (!charged && Time.time - holdStart >= chargeHoldSeconds)
                        {
                            charged = true;
                            PlaySfx(chargeSound != null ? chargeSound : beepHigh);
                        }
                    }
                    else if (holdStart >= 0f)
                    {
                        holdStart = -1f;
                        if (charged)
                        {
                            yield return OnShot(true, targetLane, ask, askSlot, lobStep);
                        }
                        else
                        {
                            yield return Say(holdLonger, nameof(holdLonger));
                            yield return WaitForKeysReleased();
                            lastInputTime = Time.time;
                        }
                        continue;
                    }
                }
            }

            // Idle: repeat the instruction
            if (!approach && holdStart < 0f && Time.time - lastInputTime > idleRepeatSeconds)
            {
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

    private IEnumerator OnMove(int dir, int targetLane, bool lobStep)
    {
        int newLane = lane + dir;
        if (newLane < 0 || newLane > 2)
        {
            PlaySfx(wallSound != null ? wallSound : beepLow);
            yield return Say(hitWall, nameof(hitWall));
            yield return LaneHint(targetLane, lobStep);
        }
        else
        {
            int before = Mathf.Abs(lane - targetLane);
            MoveLane(dir);
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
                if (lanes[lane] == Target.Civilian)
                    yield return Say(nearCivilian, nameof(nearCivilian));
                else if (after > before)
                    yield return Say(wrongWay, nameof(wrongWay));
                yield return LaneHint(targetLane, lobStep);
            }
        }

        yield return WaitForKeysReleased();
        lastInputTime = Time.time;
    }

    private IEnumerator OnShot(bool lob, int targetLane, AudioClip[] ask, string askSlot, bool lobStep)
    {
        if (lob)
        {
            if (lobShot != null) PlaySfx(lobShot);
            else if (gunShot != null) lobSource.PlayOneShot(gunShot);
        }
        else
        {
            PlaySfx(gunShot);
        }

        // A shot only hits what is in the player's current lane.
        // A lob flies over the first target and hits what is behind it.
        Target hit = lob ? (lane == behindLane ? Target.Zombie : Target.None) : lanes[lane];

        if (hit == Target.Zombie && lane == targetLane)
        {
            PlaySfx(hitZombie);
            stepDone = true;
            yield break;
        }

        if (hit == Target.Civilian)
        {
            PlaySfx(hitCivilian);
            StopTargets();
            if (lobStep && lane == targetLane)
                yield return Say(tappedCivilian, nameof(tappedCivilian));
            else
                yield return Say(shotCivilian, nameof(shotCivilian));
            approachTime = 0f;
            RestartTargets();
            yield return Say(ask, askSlot);
        }
        else
        {
            yield return Say(missedEmptyLane, nameof(missedEmptyLane));
            yield return SayExtra(tryAgain);
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

    private void MoveLane(int dir)
    {
        lane = Mathf.Clamp(lane + dir, 0, 2);
        PlaySfx(moveSound);
        UpdatePans();
    }

    private void CreateSources()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i == 1 && dummySource != null)
                laneSources[i] = dummySource;
            else
                laneSources[i] = NewSource("Lane" + i);
            laneSources[i].loop = false;
        }
        behindSource = NewSource("BehindLane");
        behindSource.loop = false;
        AudioLowPassFilter muffle = behindSource.gameObject.AddComponent<AudioLowPassFilter>();
        muffle.cutoffFrequency = 1200f;
        sfxSource = NewSource("Sfx");
        lobSource = NewSource("LobFallback");
        lobSource.pitch = 0.7f;
        beepLow = Tone("BeepLow", 220f, 0.15f);
        beepHigh = Tone("BeepHigh", 880f, 0.2f);
    }

    private AudioSource NewSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private void SetTargets(Target left, Target middle, Target right, int zombieBehind, AudioClip[] zombieClips)
    {
        lanes[0] = left;
        lanes[1] = middle;
        lanes[2] = right;
        behindLane = zombieBehind;
        for (int i = 0; i < 3; i++)
        {
            laneSets[i] = lanes[i] == Target.Zombie ? zombieClips
                        : lanes[i] == Target.Civilian ? civilianSounds : null;
        }
        behindSet = behindLane >= 0 ? zombieClips : null;
        approachTime = 0f;
        RestartTargets();
    }

    private void RestartTargets()
    {
        UpdatePans();
        StopTargets();
        for (int i = 0; i < 4; i++) nextPlay[i] = 0f;
        targetsActive = true;
    }

    private void StopTargets()
    {
        targetsActive = false;
        for (int i = 0; i < 3; i++) laneSources[i].Stop();
        behindSource.Stop();
    }

    // Target sounds are one-shots: replay them with short gaps.
    private void Update()
    {
        if (!targetsActive) return;
        for (int i = 0; i < 4; i++)
        {
            AudioSource source = i < 3 ? laneSources[i] : behindSource;
            AudioClip[] set = i < 3 ? laneSets[i] : behindSet;
            if (source == null || set == null || set.Length == 0) continue;
            if (source.isPlaying || Time.time < nextPlay[i]) continue;

            AudioClip clip = set[nextIndex[i] % set.Length];
            nextIndex[i]++;
            if (clip == null) continue;
            source.clip = clip;
            source.Play();
            nextPlay[i] = Time.time + clip.length + targetSoundGap;
        }
    }

    // Targets are heard relative to the player's lane.
    private void UpdatePans()
    {
        for (int i = 0; i < 3; i++)
        {
            if (laneSources[i] == null) continue;
            int offset = i - lane;
            laneSources[i].panStereo = Mathf.Clamp(offset, -1, 1);
            float volume = Mathf.Abs(offset) == 2 ? 0.5f : 1f;
            if (approachSeconds > 0f && lanes[i] == Target.Zombie && approachTime > 0f)
                volume *= Mathf.Lerp(0.15f, 1f, approachTime / approachSeconds);
            laneSources[i].volume = volume;
        }
        if (behindSource != null && behindLane >= 0)
        {
            int offset = behindLane - lane;
            behindSource.panStereo = Mathf.Clamp(offset, -1, 1);
            behindSource.volume = (Mathf.Abs(offset) == 2 ? 0.5f : 1f) * 0.7f;
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    private AudioClip Tone(string name, float frequency, float seconds)
    {
        int rate = 44100;
        int count = (int)(rate * seconds);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / rate) * 0.3f * (1f - (float)i / count);
        AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Plays the recorded clips of a slot. No text-to-speech: an empty slot stays
    // silent and logs one warning with the slot name, so missing lines are visible.
    private IEnumerator Say(AudioClip[] clips, string slot)
    {
        if (HasClips(clips))
        {
            yield return PlayAudioList(clips);
            yield break;
        }

        if (!string.IsNullOrEmpty(slot) && warnedSlots.Add(slot))
            Debug.LogWarning("[Tutorial] No recorded clip in slot '" + slot + "', skipped.", this);
    }

    // Play every recording in a list, in order
    private IEnumerator PlayAudioList(AudioClip[] clips)
    {
        if (clips == null || guideSource == null)
            yield break;

        bool any = false;
        foreach (AudioClip c in clips) if (c != null) any = true;
        if (!any)
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
                    yield return RepeatLast();
                    lastInputTime = Time.time;
                }
                else if (keyboard[wrongKey].wasPressedThisFrame)
                {
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
                yield return PlayAudioList(repeatInstruction);
                lastInputTime = Time.time;
            }

            yield return null;
        }
    }

    // Wait until movement and shoot keys are released
    private IEnumerator WaitForKeysReleased()
    {
        while (Keyboard.current != null &&
              (Keyboard.current.leftArrowKey.isPressed ||
               Keyboard.current.rightArrowKey.isPressed ||
               Keyboard.current.upArrowKey.isPressed ||
               Keyboard.current.spaceKey.isPressed))
        {
            yield return null;
        }

        // Start listening on the following frame
        yield return null;
    }

#if UNITY_EDITOR
    // Inspector: TutorialManager component menu (three dots) > "Assign default tutorial sounds", then save the scene.
    [ContextMenu("Assign default tutorial sounds")]
    private void AssignDefaultSounds()
    {
        Undo.RecordObject(this, "Assign default tutorial sounds");
        FillMissingSounds();
        if (string.Equals(nextScene, "prototype", System.StringComparison.OrdinalIgnoreCase))
            nextScene = "Prototype";
        EditorUtility.SetDirty(this);
    }

    private const string NewClips = "Assets/Sounds/Newtutorialmaterial/";
    private const string IntroClips = "Assets/Sounds/Tutorial.intro/";

    // Fills only EMPTY slots (PickSet/Pick keep anything already set in the Inspector).
    private void FillMissingSounds()
    {
        // Sound effects
        gunShot = Pick(gunShot, "Assets/Sounds/Player/gunshot.mp3");
        // step_sound.mp3 is a 15.5 s loop that stacks on every lane move: use the short footstep.
        if (moveSound != null && AssetDatabase.GetAssetPath(moveSound).EndsWith("/step_sound.mp3")) moveSound = null;
        moveSound = Pick(moveSound, "Assets/Sounds/Player/footstep_1.mp3");
        hitZombie = Pick(hitZombie, "Assets/Sounds/Monster/Standard/ZombieDeath_2.wav");
        hitCivilian = Pick(hitCivilian, "Assets/Sounds/Civilllian/CivilianOld_hit1.wav");
        zombieSounds = PickSet(zombieSounds, "Assets/Sounds/Monster/Standard/ZombieSpawn_2.wav");
        civilianSounds = PickSet(civilianSounds,
            "Assets/Sounds/Civilllian/CivilianOld_idle2.wav",
            "Assets/Sounds/Civilllian/CivilianOld_idle3.wav");

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

        // New recordings: extras
        explainLanes = PickSet(explainLanes, NewClips + "PRess the left or right arrow to move a lane at a time.wav");
        explainSoundDirection = PickSet(explainSoundDirection, NewClips + "Zombies makes sound from the lane they come from, move there.wav");
        explainApproach = PickSet(explainApproach, NewClips + "Zombie gets loader the closer they get.wav");
        explainLob = PickSet(explainLob, NewClips + "Lob shots is the only way to shot awhats behind a civilian.wav");
        inRightLane = PickSet(inRightLane, NewClips + "Great you are in the right lane now, take the shot.wav");
        tryAgain = PickSet(tryAgain, NewClips + "Almost, try again.wav");
        praiseShort = PickSet(praiseShort, NewClips + "Nice oneee.wav");
        lobKeepInMind = PickSet(lobKeepInMind, NewClips + "thats the lob.wav");
        patience = PickSet(patience, NewClips + "Take your time.wav", NewClips + "Im not goping anywhere.wav", NewClips + "I still get payed on the clock.wav");
        closing = PickSet(closing, NewClips + "I taught you all i know, lets go.wav");

        // Earlier recordings (Tutorial.intro), so no step is left silent now that there is no text-to-speech
        explainDummy = PickSet(explainDummy, IntroClips + "Testing you weapomn.wav", IntroClips + "Dont move to the sides.wav");
        dummyHit = PickSet(dummyHit, IntroClips + "Well Done.wav", IntroClips + "Shoot the dead Dead.wav");
        askZombieRight = PickSet(askZombieRight, IntroClips + "Civilian and zombie dummy.wav", IntroClips + "Dont shoot civilians.wav");
        zombieRightHit = PickSet(zombieRightHit, IntroClips + "you shot the correct target.wav");
        askLobber = PickSet(askLobber, IntroClips + "Testing mortar function.wav", IntroClips + "Testing mortar - Press dowm MK1.wav", IntroClips + "Once released - MK 1.wav");
        lobberHit = PickSet(lobberHit, IntroClips + "Released button success.wav");
        holdLonger = PickSet(holdLonger, IntroClips + "Released to early.wav");
        shotCivilian = PickSet(shotCivilian, IntroClips + "Dont shoot the civilian.wav");
        nearCivilian = PickSet(nearCivilian, IntroClips + "Dont shoot civilians.wav");
    }

    private static AudioClip[] PickSet(AudioClip[] current, params string[] paths)
    {
        if (HasClips(current)) return current;
        var clips = new System.Collections.Generic.List<AudioClip>();
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
