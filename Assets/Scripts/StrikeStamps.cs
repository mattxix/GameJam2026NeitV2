using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Stamps a red X into a row of printed boxes on the target sheet each time the
// player takes a strike. Sits on a RectTransform under the sheet's canvas.
[RequireComponent(typeof(RectTransform))]
public class StrikeStamps : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Sprite boxSprite;
    [SerializeField] private Sprite stampSprite;

    [Header("Layout")]
    [Tooltip("Gap between boxes, as a fraction of box size.")]
    [SerializeField, Range(0f, 1f)] private float gapFraction = 0.25f;
    [Tooltip("Stamp size relative to its box. Slightly over 1 lets the ink bleed past the lines.")]
    [SerializeField, Range(0.5f, 1.5f)] private float stampScale = 1.05f;

    [Header("Stamp Feel")]
    [SerializeField, Min(0.05f)] private float punchDuration = 0.18f;
    [Tooltip("How large the stamp starts before slamming down onto the paper.")]
    [SerializeField, Min(1f)] private float punchStartScale = 1.8f;
    [Tooltip("Random tilt range in degrees, so no two stamps land the same.")]
    [SerializeField, Range(0f, 30f)] private float maxTilt = 12f;
    [Tooltip("Random nudge off-centre, as a fraction of box size.")]
    [SerializeField, Range(0f, 0.2f)] private float maxOffset = 0.06f;

    [Header("Audio")]
    [Tooltip("Optional thud played at the sheet when a stamp lands.")]
    [SerializeField] private AudioClip stampSound;
    [SerializeField, Range(0f, 1f)] private float stampVolume = 0.8f;

    [Header("Fallback")]
    [Tooltip("Box count used only if no GameState is in the scene.")]
    [SerializeField, Min(1)] private int fallbackSlots = 3;

    private Image[] stamps;
    private float slotSize;
    private int shown;
    private GameState state;

    private void Start()
    {
        state = GameState.Instance;

        // Box count follows the real strike limit, so changing it in GameState just works.
        Build(state != null ? state.MaxStrikes : fallbackSlots);

        if (state == null)
        {
            Debug.LogWarning("StrikeStamps: no GameState found, strikes won't be shown.", this);
            return;
        }

        state.OnStrikesChanged += HandleStrikes;

        // A mid-game scene state still shows correctly, without replaying animations.
        for (int i = 0; i < Mathf.Min(state.Strikes, stamps.Length); i++) Stamp(i, false);
        shown = Mathf.Min(state.Strikes, stamps.Length);
    }

    private void OnDestroy()
    {
        if (state != null) state.OnStrikesChanged -= HandleStrikes;
    }

    private void HandleStrikes(int strikes, int max)
    {
        int target = Mathf.Clamp(strikes, 0, stamps.Length);

        while (shown < target)
        {
            Stamp(shown, true);
            shown++;
        }
    }

    // Boxes are sized to fit the row's rect, so resizing the row in the editor lays them out.
    private void Build(int count)
    {
        Vector2 size = ((RectTransform)transform).rect.size;
        slotSize = Mathf.Min(size.y, size.x / (count + gapFraction * (count - 1)));
        float step = slotSize * (1f + gapFraction);

        stamps = new Image[count];
        for (int i = 0; i < count; i++)
        {
            Image box = NewImage("StrikeBox" + i, transform, boxSprite);
            RectTransform boxRect = box.rectTransform;
            Center(boxRect);
            boxRect.sizeDelta = new Vector2(slotSize, slotSize);
            boxRect.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * step, 0f);

            Image stamp = NewImage("Stamp", boxRect, stampSprite);
            Center(stamp.rectTransform);
            stamp.rectTransform.sizeDelta = new Vector2(slotSize * stampScale, slotSize * stampScale);
            stamp.gameObject.SetActive(false);

            stamps[i] = stamp;
        }
    }

    private void Stamp(int index, bool animate)
    {
        Image stamp = stamps[index];
        RectTransform r = stamp.rectTransform;

        r.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-maxTilt, maxTilt));
        r.anchoredPosition = Random.insideUnitCircle * (maxOffset * slotSize);
        stamp.gameObject.SetActive(true);

        if (!animate)
        {
            r.localScale = Vector3.one;
            SetAlpha(stamp, 1f);
            return;
        }

        StartCoroutine(Punch(stamp));
        if (stampSound != null) AudioSource.PlayClipAtPoint(stampSound, transform.position, stampVolume);
    }

    private IEnumerator Punch(Image stamp)
    {
        RectTransform r = stamp.rectTransform;
        float t = 0f;

        while (t < punchDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / punchDuration);

            // Ease-in accelerates into the paper, reading as a slam rather than a fade.
            r.localScale = Vector3.one * Mathf.Lerp(punchStartScale, 1f, k * k);
            SetAlpha(stamp, Mathf.Clamp01(k * 2.5f));
            yield return null;
        }

        r.localScale = Vector3.one;
        SetAlpha(stamp, 1f);
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void Center(RectTransform r)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }
}