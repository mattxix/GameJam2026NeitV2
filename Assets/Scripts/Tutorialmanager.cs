using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The 12 tutorial panels, in order. The number is the panel's slot in the
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
    BarOpen = 11  // 12_Part2_BarIsOpen
}

/// <summary>
/// Shows one tutorial panel at a time. The scene starts with panel 0 on and
/// every other panel off. Completing the current task, or pressing Skip Task,
/// turns the current panel off and the next one on. Advancing past the last
/// panel ends the tutorial.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Panels (in order)")]
    [Tooltip("Drag the 12 panel GameObjects here in order. Element 0 is shown first.")]
    [SerializeField] private GameObject[] panels = new GameObject[12];

    [Header("Behaviour")]
    [Tooltip("Show the first panel automatically when the scene starts.")]
    [SerializeField] private bool startOnSceneLoad = true;

    [Tooltip("Ignore extra advance calls that arrive within this many seconds, " +
             "so one trigger pull (or a task finishing on the same frame as a Skip) " +
             "can't jump two panels.")]
    [SerializeField] private float advanceCooldown = 0.3f;

    [Header("Events")]
    [Tooltip("Fires every time a new panel turns on. Passes the panel index (0-11).")]
    public UnityEvent<int> onPanelChanged;

    [Tooltip("Fires after the last panel, when the tutorial is done.")]
    public UnityEvent onTutorialFinished;

    [Tooltip("Fires when the player presses Exit Tutorial.")]
    public UnityEvent onTutorialExited;

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
        HideAll();
        CurrentIndex = -1;
        lastAdvanceTime = float.NegativeInfinity;
        ShowPanel(0);
    }

    /// <summary>Skip Task / Continue button. Moves on from whatever panel is showing.</summary>
    public void SkipTask()
    {
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
        Advance();
    }

    /// <summary>Exit Tutorial button. Turns every panel off and ends the tutorial.</summary>
    public void ExitTutorial()
    {
        if (!IsRunning) return;
        HideAll();
        CurrentIndex = -1;
        onTutorialExited?.Invoke();
    }

    // ------------------------------------------------------------------
    // Internals
    // ------------------------------------------------------------------

    private void Advance()
    {
        if (!IsRunning) return;
        if (Time.unscaledTime - lastAdvanceTime < advanceCooldown) return;
        lastAdvanceTime = Time.unscaledTime;

        int next = CurrentIndex + 1;
        if (next >= panels.Length)
        {
            FinishTutorial();
            return;
        }
        ShowPanel(next);
    }

    private void ShowPanel(int index)
    {
        // Turn off the current panel.
        if (CurrentIndex >= 0 && CurrentIndex < panels.Length && panels[CurrentIndex] != null)
            panels[CurrentIndex].SetActive(false);

        CurrentIndex = index;

        // Turn on the next one.
        if (panels[index] != null)
            panels[index].SetActive(true);
        else
            Debug.LogWarning($"[Tutorial] Panel slot {index} is empty in the TutorialManager.", this);

        onPanelChanged?.Invoke(index);
    }

    private void FinishTutorial()
    {
        HideAll();
        CurrentIndex = -1;
        onTutorialFinished?.Invoke();
    }

    private void HideAll()
    {
        if (panels == null) return;
        foreach (var panel in panels)
            if (panel != null) panel.SetActive(false);
    }
}