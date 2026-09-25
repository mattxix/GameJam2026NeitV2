using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Shows a check or cross in the guest's speech bubble after a serve,
// then removes the guest once the player has had time to read it.
public class ServeFeedback : MonoBehaviour
{
    public static ServeFeedback Instance { get; private set; }

    [Header("Icons")]
    [SerializeField] private Sprite correctSprite;
    [SerializeField] private Sprite wrongSprite;

    [Header("Timing")]
    [Tooltip("Seconds the result stays up before the guest leaves.")]
    [SerializeField, Min(0.1f)] private float displayTime = 1.5f;

    [Header("Layout")]
    [Tooltip("Used only if the bubble has no order icon to copy placement from.")]
    [SerializeField] private Vector2 fallbackSize = new Vector2(100f, 100f);

    [Header("Scene References")]
    [SerializeField] private MaskLogic maskLogic;

    // Renamed guests can't be found by napkins or collide with a new spawn's name.
    private const string LeavingName = "Leaving";
    private const string IceIconName = "Ice";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Play(NPCData guest, ServeResult result, int slot)
    {
        if (guest == null)
        {
            Release(slot);
            return;
        }

        // Rename first, so a second glass placed during the display can't re-serve them.
        guest.gameObject.name = LeavingName;

        Sprite icon = ServeEvaluator.IsStrike(result) ? wrongSprite : correctSprite;
        ShowIcon(guest, icon);

        StartCoroutine(RemoveAfterDelay(guest.gameObject, slot));
    }

    private void ShowIcon(NPCData guest, Sprite icon)
    {
        Canvas bubble = guest.speechBubble;
        if (bubble == null || icon == null) return;

        Transform root = bubble.transform;

        // The drink icon marks where the order sits, so the result lands in the same spot.
        RectTransform template = FindRect(root, guest.desiredFlavor);

        SetChildActive(root, guest.desiredFlavor, false);
        SetChildActive(root, guest.desiredTopping, false);
        SetChildActive(root, IceIconName, false);

        var go = new GameObject("ServeResult", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(root, false);

        if (template != null)
        {
            rect.anchorMin = template.anchorMin;
            rect.anchorMax = template.anchorMax;
            rect.pivot = template.pivot;
            rect.anchoredPosition = template.anchoredPosition;
            rect.sizeDelta = template.sizeDelta;
            rect.localRotation = template.localRotation;
            rect.localScale = template.localScale;
        }
        else
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = fallbackSize;
        }

        // Last sibling draws on top of anything else in the bubble.
        rect.SetAsLastSibling();

        var image = go.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;

        bubble.enabled = true;
    }

    private IEnumerator RemoveAfterDelay(GameObject guest, int slot)
    {
        yield return new WaitForSeconds(displayTime);

        if (guest != null) Destroy(guest);

        // Seat frees only once they're gone, so no new guest walks into an occupied chair.
        Release(slot);
    }

    private void Release(int slot)
    {
        if (slot >= 0 && maskLogic != null) maskLogic.ReleaseGuest(slot);
    }

    private static RectTransform FindRect(Transform root, string childName)
    {
        if (string.IsNullOrEmpty(childName)) return null;
        Transform child = root.Find(childName);
        return child as RectTransform;
    }

    private static void SetChildActive(Transform root, string childName, bool active)
    {
        if (string.IsNullOrEmpty(childName)) return;
        Transform child = root.Find(childName);
        if (child != null) child.gameObject.SetActive(active);
    }
}