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

    [Header("Timing")]
    [SerializeField] private float idleRepeatSeconds = 8f;

    // Placeholder lines, spoken with text-to-speech until a clip is recorded
    private const string T_RepeatHelp = "If you want me to repeat what I just said, press Control, and I'll repeat the last thing I said.";
    private const string T_ExplainDummy = "I'm placing a dummy zombie in the middle lane, in front of you. Press Space to shoot it.";
    private const string T_DummyHit = "Nice shot! The dummy is down.";
    private const string T_AskZombieRight = "Now there's a zombie on your right and a civilian on your left. Move and shoot the zombie.";
    private const string T_ZombieRightHit = "Good job, you got the zombie and kept the civilian safe.";
    private const string T_AskRealZombie = "Here comes a real zombie in the middle lane. Shoot it before it reaches you.";
    private const string T_RealZombieHit = "Great, you stopped it in time.";
    private const string T_ReachedYou = "The zombie reached you. Let's try that again.";
    private const string T_AskLobber = "A civilian is in the middle lane, with a zombie behind them. Hold the Up arrow until you hear the charge sound, then let go, to lob a shot over the civilian and hit the zombie.";
    private const string T_LobberHit = "Perfect, your charged shot arched over the civilian and hit the zombie.";
    private const string T_TappedCivilian = "Space shoots straight and hits the civilian in front. Hold the Up arrow instead, then let go, to lob over them.";
    private const string T_HoldLonger = "Hold the Up arrow longer, until you hear the charge sound, then let go.";
    private const string T_GoLeft = "The target is to your left. Press left.";
    private const string T_GoRight = "The target is to your right. Press right.";
    private const string T_ShootHere = "The target is in this lane. Press Space to shoot.";
    private const string T_LobHere = "The target is in this lane. Hold the Up arrow until the charge sound, then let go.";
    private const string T_WrongWay = "That's the wrong way.";
    private const string T_Wall = "That's the edge, you can't go further.";
    private const string T_NearCivilian = "Careful, that's the civilian. Don't shoot them.";
    private const string T_Missed = "Nothing in this lane. You missed.";
    private const string T_ShotCivilian = "You shot a civilian! Never shoot civilians. Let's try again.";
    private const string T_Complete = "Tutorial complete. Get ready, the real game starts now.";

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
    private string lastText;

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
        yield return Say(repeatHelp, T_RepeatHelp);

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

        // STEP 4: Dummy zombie in front (middle lane)
        SetTargets(Target.None, Target.Zombie, Target.None, -1, dummySound != null ? new[] { dummySound } : zombieSounds);
        yield return Say(explainDummy, T_ExplainDummy);
        yield return ShootStep(1, explainDummy, T_ExplainDummy, false, false);
        StopTargets();
        yield return Say(dummyHit, T_DummyHit);

        // STEP 5: Zombie right, civilian left
        SetTargets(Target.Civilian, Target.None, Target.Zombie, -1, zombieSounds);
        yield return Say(askZombieRight, T_AskZombieRight);
        yield return ShootStep(2, askZombieRight, T_AskZombieRight, false, false);
        StopTargets();
        yield return Say(zombieRightHit, T_ZombieRightHit);

        // STEP 6: Real zombie approaching in the middle lane
        SetTargets(Target.None, Target.Zombie, Target.None, -1, zombieSounds);
        yield return Say(askRealZombie, T_AskRealZombie);
        yield return ShootStep(1, askRealZombie, T_AskRealZombie, false, true);
        StopTargets();
        yield return Say(realZombieHit, T_RealZombieHit);

        // STEP 7: Civilian in the middle, zombie behind: lob over
        SetTargets(Target.None, Target.Civilian, Target.None, 1, zombieSounds);
        yield return Say(askLobber, T_AskLobber);
        yield return ShootStep(1, askLobber, T_AskLobber, true, false);
        StopTargets();
        yield return Say(lobberHit, T_LobberHit);

        // STEP 8: Tutorial completed
        yield return Say(tutorialComplete, T_Complete);

        // STEP 9: Load actual game
        SceneManager.LoadScene(nextScene);
    }

    // One shooting exercise. Loops until the right target is hit.
    private IEnumerator ShootStep(int targetLane, AudioClip[] ask, string askText, bool lobStep, bool approach)
    {
        stepDone = false;
        approachTime = 0f;
        float holdStart = -1f;
        bool charged = false;
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
                    yield return Say(realZombieReachedYou, T_ReachedYou);
                    approachTime = 0f;
                    RestartTargets();
                    yield return Say(ask, askText);
                    yield return WaitForKeysReleased();
                    lastInputTime = Time.time;
                    continue;
                }
            }

            if (keyboard != null)
            {
                if (holdStart < 0f && CtrlPressed())
                {
                    yield return RepeatLast();
                    lastInputTime = Time.time;
                    continue;
                }

                int dir = 0;
                if (keyboard.leftArrowKey.wasPressedThisFrame) dir = -1;
                else if (keyboard.rightArrowKey.wasPressedThisFrame) dir = 1;

                if (dir != 0 && holdStart < 0f)
                {
                    yield return OnMove(dir, targetLane, lobStep);
                    continue;
                }

                if (keyboard.spaceKey.wasPressedThisFrame && holdStart < 0f)
                {
                    yield return OnShot(false, targetLane, ask, askText, lobStep);
                    continue;
                }

                if (lobStep)
                {
                    if (keyboard.upArrowKey.wasPressedThisFrame)
                    {
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
                            yield return OnShot(true, targetLane, ask, askText, lobStep);
                        }
                        else
                        {
                            yield return Say(holdLonger, T_HoldLonger);
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
                yield return Say(ask, askText);
                yield return LaneHint(targetLane, lobStep);
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
            yield return Say(hitWall, T_Wall);
            yield return LaneHint(targetLane, lobStep);
        }
        else
        {
            int before = Mathf.Abs(lane - targetLane);
            MoveLane(dir);
            int after = Mathf.Abs(lane - targetLane);

            if (lane == targetLane)
                yield return LaneHint(targetLane, lobStep);
            else
            {
                if (lanes[lane] == Target.Civilian)
                    yield return Say(nearCivilian, T_NearCivilian);
                else if (after > before)
                    yield return Say(wrongWay, T_WrongWay);
                yield return LaneHint(targetLane, lobStep);
            }
        }

        yield return WaitForKeysReleased();
        lastInputTime = Time.time;
    }

    private IEnumerator OnShot(bool lob, int targetLane, AudioClip[] ask, string askText, bool lobStep)
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
                yield return Say(tappedCivilian, T_TappedCivilian);
            else
                yield return Say(shotCivilian, T_ShotCivilian);
            approachTime = 0f;
            RestartTargets();
            yield return Say(ask, askText);
        }
        else
        {
            yield return Say(missedEmptyLane, T_Missed);
            yield return LaneHint(targetLane, lobStep);
        }

        yield return WaitForKeysReleased();
        lastInputTime = Time.time;
    }

    private IEnumerator LaneHint(int targetLane, bool lobStep)
    {
        if (lane > targetLane) yield return Say(hintGoLeft, T_GoLeft);
        else if (lane < targetLane) yield return Say(hintGoRight, T_GoRight);
        else if (lobStep) yield return Say(hintLobHere, T_LobHere);
        else yield return Say(hintShootHere, T_ShootHere);
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

    // Plays recorded clips if any are assigned, otherwise a TTS placeholder.
    private IEnumerator Say(AudioClip[] clips, string placeholder)
    {
        if (clips != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip != null)
                {
                    yield return PlayAudioList(clips);
                    yield break;
                }
            }
        }

        if (string.IsNullOrEmpty(placeholder))
            yield break;

        Debug.Log("[Tutorial] " + placeholder, this);

        lastClips = null;
        lastText = placeholder;

        if (UAP_AccessibilityManager.IsEnabled())
        {
            UAP_AccessibilityManager.Say(placeholder);
            float start = Time.time;
            float maxWait = 2f + placeholder.Length * 0.09f;
            yield return null;
            while (Time.time - start < maxWait &&
                   (Time.time - start < 0.3f || UAP_AccessibilityManager.IsSpeaking()))
            {
                if (CtrlPressed())
                {
                    // Restart the current line
                    UAP_AccessibilityManager.Say(placeholder, true, true, UAP_AudioQueue.EInterrupt.All);
                    start = Time.time;
                }
                yield return null;
            }
        }
        else
        {
            PlaySfx(beepHigh);
            yield return new WaitForSeconds(0.5f);
        }
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
        lastText = null;

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

    // Ctrl: replay the last guide line (clip list or TTS)
    private IEnumerator RepeatLast()
    {
        if (lastClips != null)
            yield return PlayAudioList(lastClips);
        else if (!string.IsNullOrEmpty(lastText))
            yield return Say(null, lastText);
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
    // Right-click the component header > "Assign default tutorial sounds", then save the scene.
    [ContextMenu("Assign default tutorial sounds")]
    private void AssignDefaultSounds()
    {
        Undo.RecordObject(this, "Assign default tutorial sounds");
        FillMissingSounds();
        if (string.Equals(nextScene, "prototype", System.StringComparison.OrdinalIgnoreCase))
            nextScene = "Prototype";
        EditorUtility.SetDirty(this);
    }

    private void FillMissingSounds()
    {
        gunShot = Pick(gunShot, "Assets/Sounds/Player/gunshot.mp3");
        moveSound = Pick(moveSound, "Assets/Sounds/Player/step_sound.mp3");
        hitZombie = Pick(hitZombie, "Assets/Sounds/Monster/Standard/ZombieDeath_2.wav");
        hitCivilian = Pick(hitCivilian, "Assets/Sounds/Civilllian/CivilianOld_hit1.wav");
        zombieSounds = PickSet(zombieSounds, "Assets/Sounds/Monster/Standard/ZombieSpawn_2.wav");
        civilianSounds = PickSet(civilianSounds,
            "Assets/Sounds/Civilllian/CivilianOld_idle2.wav",
            "Assets/Sounds/Civilllian/CivilianOld_idle3.wav");
    }

    private static AudioClip[] PickSet(AudioClip[] current, params string[] paths)
    {
        if (current != null && current.Length > 0) return current;
        AudioClip[] clips = new AudioClip[paths.Length];
        for (int i = 0; i < paths.Length; i++)
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
        return clips;
    }

    private static AudioClip Pick(AudioClip current, string path)
    {
        return current != null ? current : AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
#endif
}
