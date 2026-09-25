using System.Collections;
using UnityEngine;

/// <summary>
/// The 13 tutorial panels, in order. The number is the panel's slot in the
/// TutorialManager's Panels list (and matches the PNG file numbers minus one).
/// </summary>
public enum TutorialStep
{
    Welcome = 0,  // 01_Part1_Welcome
    CheckOrder = 1,  // 02_Part1_Step1_CheckOrder
    GrabGlass = 2,  // 03_Part1_Step2_GrabGlass
    AddIce = 3,  // 04_Part1_Step3_AddIce
    PourDrink = 4,  // 05_Part1_Step4_PourDrink
    AddTopping = 5,  // 06_Part1_Step5_AddTopping
    ServeDrink = 6,  // 07_Part1_Step6_ServeDrink
    Part1Complete = 7,  // 08_Part1_Complete
    FindTarget = 8,  // 09_Part2_Step1_FindTarget
    MakeDrink = 9,  // 10_Part2_Step2_MakeDrink
    AddPoison = 10, // 11_Part2_Step3_AddPoison
    ServeTarget = 11, // 12_Part2_Step4_ServeTarget
    BarOpen = 12  // 13_Part2_BarIsOpen
}

/// <summary>
/// Shows one tutorial panel at a time. The scene starts with panel 0 on and
/// every other panel off. Completing the current task, or pressing Skip Task,
/// turns the current panel off and the next one on. Advancing past the last
/// panel, or pressing Exit Tutorial, ends the tutorial the same way.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Panels (in order)")]
    [Tooltip("Drag the 13 panel GameObjects here in order. Element 0 is shown first.")]
    [SerializeField] private GameObject[] panels = new GameObject[13];

    [Header("Behaviour")]
    [Tooltip("Show the first panel automatically when the scene starts.")]
    [SerializeField] private bool startOnSceneLoad = true;

    [Tooltip("Ignore extra advance calls that arrive within this many seconds, " +
             "so one trigger pull (or a task finishing on the same frame as a Skip) " +
             "can't jump two panels.")]
    [SerializeField] private float advanceCooldown = 0.3f;

    [Header("Tutorial Guests")]
    [Tooltip("Spawns the practice guest and the mafia guest. Drag in the MaskManager.")]
    [SerializeField] private MaskLogic maskLogic;

    [Tooltip("Seat the tutorial guests walk to. 2 is the middle of 5 seats.")]
    [SerializeField] private int tutorialSeat = 2;

    [Tooltip("Pause after the previous guest leaves before the next one walks up.")]
    [SerializeField] private float respawnDelay = 1.5f;

    [Header("Sounds")]
    [Tooltip("Plays when any tutorial button is pressed (Skip, Continue, Exit).")]
    [SerializeField] private AudioClip buttonPressSound;

    [Tooltip("Plays when the player completes a task and the next panel appears.")]
    [SerializeField] private AudioClip taskCompleteSound;

    [Range(0f, 1f)] [SerializeField] private float soundVolume = 1f;

    [Tooltip("Sounds come from the panel's position and fade out to silence at this distance (meters).")]
    [SerializeField] private float soundMaxDistance = 15f;

    // 3D source that moves to whichever panel is showing before it plays.
    private AudioSource audioSource;
    private Transform lastPanel;

    /// <summary>True if the tutorial starts by itself when the scene loads.</summary>
    public bool StartsOnSceneLoad => startOnSceneLoad;

    private NPCData tutorialGuest;
    private Coroutine guestRoutine;

    /// <summary>Index of the panel that's showing, or -1 if the tutorial isn't running.</summary>
    public int CurrentIndex { get; private set; } = -1;
    public bool IsRunning => CurrentIndex >= 0;
    public TutorialStep CurrentStep => (TutorialStep)CurrentIndex;

    private float lastAdvanceTime = float.NegativeInfinity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[Tutorial] More than one TutorialManager in the scene. Using the newest one.", this);
        Instance = this;

        // Turn everything off right away so no panel flashes on the first frame.
        HideAll();

        var audioObject = new GameObject("TutorialAudio");
        audioObject.transform.SetParent(transform, false);
        audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;                      // fully 3D
        audioSource.rolloffMode = AudioRolloffMode.Linear;  // predictable falloff
        audioSource.minDistance = 1f;
        audioSource.maxDistance = soundMaxDistance;
        audioSource.dopplerLevel = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (startOnSceneLoad) StartTutorial();
    }

    // ------------------------------------------------------------------
    // Public API — hook these up to buttons or call them from your scripts
    // ------------------------------------------------------------------

    /// <summary>Starts (or restarts) the tutorial from panel 0.</summary>
    public void StartTutorial()
    {
        StopGuestRoutine();
        HideAll();
        CurrentIndex = -1;
        lastAdvanceTime = float.NegativeInfinity;
        ShowPanel(0);
    }

    /// <summary>Skip Task / Continue button. Moves on from whatever panel is showing.</summary>
    public void SkipTask()
    {
        PlaySound(buttonPressSound);
        Advance();
    }

    /// <summary>
    /// Call this from gameplay code when the player finishes a task, e.g.
    /// <c>TutorialManager.Instance?.Complete(TutorialStep.PourDrink);</c>
    /// Only advances if that step's panel is the one currently showing, so
    /// actions done early or out of order are ignored.
    /// </summary>
    public void Complete(TutorialStep step)
    {
        CompleteTask((int)step);
    }

    /// <summary>
    /// Same as Complete(), but takes the panel number so it can be picked
    /// in the Inspector from any UnityEvent (e.g. an XRGrabInteractable's
    /// Select Entered event).
    /// </summary>
    public void CompleteTask(int panelIndex)
    {
        if (!IsRunning || panelIndex != CurrentIndex) return;
        if (Advance()) PlaySound(taskCompleteSound);
    }

    /// <summary>
    /// Exit Tutorial button. Ends the tutorial exactly the same way as
    /// finishing the last panel: every panel turns off.
    /// </summary>
    public void ExitTutorial()
    {
        PlaySound(buttonPressSound);
        EndTutorial();
    }

    /// <summary>
    /// Called by the napkin when a drink is served during the tutorial.
    /// Part 1: right drink -> Drink Served panel. Wrong -> back to Check the Order
    /// with a new guest. Part 2: poisoned mafia -> Bar Is Open. Anything else ->
    /// back to Find the Target with a new mafia guest.
    /// </summary>
    public void ReportServe(ServeResult result)
    {
        if (!IsRunning) return;
        int i = CurrentIndex;

        // Part 1: the practice guest.
        if (i >= (int)TutorialStep.CheckOrder && i <= (int)TutorialStep.ServeDrink)
        {
            if (result == ServeResult.Correct)
            {
                JumpTo(TutorialStep.Part1Complete);   // waits here for Continue
                PlaySound(taskCompleteSound);
            }
            else
                JumpTo(TutorialStep.CheckOrder);      // new guest, new order
            return;
        }

        // Part 2: the mafia guest.
        if (i >= (int)TutorialStep.FindTarget && i <= (int)TutorialStep.ServeTarget)
        {
            if (result == ServeResult.MafiaPoisoned)
            {
                JumpTo(TutorialStep.BarOpen);
                PlaySound(taskCompleteSound);
            }
            else
                JumpTo(TutorialStep.FindTarget);      // mafia comes back
        }
    }

    // ------------------------------------------------------------------
    // Internals
    // ------------------------------------------------------------------

    // Returns true if the tutorial actually moved on.
    private bool Advance()
    {
        if (!IsRunning) return false;
        if (Time.unscaledTime - lastAdvanceTime < advanceCooldown) return false;
        lastAdvanceTime = Time.unscaledTime;

        int next = CurrentIndex + 1;
        if (next >= panels.Length)
        {
            EndTutorial();
            return true;
        }
        ShowPanel(next);
        return true;
    }

    // Plays from the panel that's showing (or was just showing, when the tutorial ends).
    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        if (lastPanel != null) audioSource.transform.position = lastPanel.position;
        audioSource.PlayOneShot(clip, soundVolume);
    }

    private void JumpTo(TutorialStep step)
    {
        lastAdvanceTime = Time.unscaledTime;
        ShowPanel((int)step);
    }

    private void ShowPanel(int index)
    {
        // Turn off the current panel.
        if (CurrentIndex >= 0 && CurrentIndex < panels.Length && panels[CurrentIndex] != null)
            panels[CurrentIndex].SetActive(false);

        CurrentIndex = index;

        // Turn on the next one.
        if (panels[index] != null)
        {
            panels[index].SetActive(true);
            lastPanel = panels[index].transform;
        }
        else
            Debug.LogWarning($"[Tutorial] Panel slot {index} is empty in the TutorialManager.", this);

        // Panels that bring a guest to the bar.
        if (index == (int)TutorialStep.CheckOrder) SendGuest(false);
        else if (index == (int)TutorialStep.FindTarget) SendGuest(true);
    }

    private void SendGuest(bool mafia)
    {
        if (maskLogic == null)
        {
            Debug.LogWarning("[Tutorial] No MaskLogic assigned, so no tutorial guest was sent.", this);
            return;
        }
        StopGuestRoutine();
        guestRoutine = StartCoroutine(SendGuestWhenSeatFree(mafia));
    }

    private IEnumerator SendGuestWhenSeatFree(bool mafia)
    {
        // A guest from an earlier step who was never served (e.g. the player
        // skipped ahead) leaves so the next one can sit down.
        DismissWaitingGuest();

        // A served guest keeps the seat while their check or cross shows.
        bool waited = false;
        while (!maskLogic.IsSlotFree(tutorialSeat))
        {
            waited = true;
            yield return null;
        }
        if (waited) yield return new WaitForSeconds(respawnDelay);

        tutorialGuest = maskLogic.SpawnGuestAtSeat(tutorialSeat, mafia);

        // The practice guest always orders ice so the ice step is always taught.
        // Set right after spawning, before the guest's Start() picks the order.
        if (tutorialGuest != null && !mafia) tutorialGuest.forceIce = true;

        guestRoutine = null;
    }

    private void DismissWaitingGuest()
    {
        if (tutorialGuest == null) return;

        // Served guests are renamed "Leaving" and remove themselves,
        // so only one still waiting in the seat is removed here.
        if (tutorialGuest.gameObject.name == tutorialSeat.ToString())
        {
            maskLogic.ReleaseGuest(tutorialSeat);
            Destroy(tutorialGuest.gameObject);
        }
        tutorialGuest = null;
    }

    private void StopGuestRoutine()
    {
        if (guestRoutine != null) StopCoroutine(guestRoutine);
        guestRoutine = null;
    }

    /// <summary>Shared by finishing and exiting: turn everything off and stop.</summary>
    private void EndTutorial()
    {
        if (!IsRunning) return;
        StopGuestRoutine();
        HideAll();
        CurrentIndex = -1;
        tutorialGuest = null;

        // Hand the bar over to normal random spawning.
        if (maskLogic != null) maskLogic.StartSpawning();
    }

    private void HideAll()
    {
        if (panels == null) return;
        foreach (var panel in panels)
            if (panel != null) panel.SetActive(false);
    }
}