using System;
using UnityEngine;

// Central run state: strikes, kills, and the lose conditions.
// Napkins report serves here rather than scoring independently.
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    [Header("Rules")]
    [SerializeField, Min(1)] private int maxStrikes = 3;

    [Header("State")]
    [SerializeField] private int strikes;
    [SerializeField] private int mafiaKilled;

    [Header("Scene References")]
    [SerializeField] private PopupService popupService;
    [SerializeField] private MaskLogic maskLogic;

    public int Strikes => strikes;
    public int MafiaKilled => mafiaKilled;
    public int MaxStrikes => maxStrikes;
    public bool IsGameOver { get; private set; }

    // Hook UI to these rather than polling every frame.
    public event Action<int, int> OnStrikesChanged;   // (strikes, max)
    public event Action<int> OnScoreChanged;
    public event Action<string> OnGameOver;           // popup condition key

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Single entry point for every serve in either build.
    public void ReportServe(ServeResult result)
    {
        if (IsGameOver) return;

        if (ServeEvaluator.IsKill(result))
        {
            mafiaKilled++;
            OnScoreChanged?.Invoke(mafiaKilled);
            // Target is dead, so a fresh mafia profile can enter the ball.
            if (maskLogic != null) maskLogic.RetireCurrentTarget();
        }

        if (ServeEvaluator.IsInstantLoss(result))
        {
            EndGame("PoisonedInnocent");
            return;
        }

        if (ServeEvaluator.IsStrike(result))
        {
            strikes++;
            OnStrikesChanged?.Invoke(strikes, maxStrikes);
            if (strikes >= maxStrikes) EndGame("LostJob");
        }
    }

    private void EndGame(string condition)
    {
        IsGameOver = true;
        OnGameOver?.Invoke(condition);
        if (popupService != null) StartCoroutine(popupService.PopupMenu(condition));
    }
}