using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    [Header("Resource Settings")]
    public float currentResource = 100f;  // current resource value
    public float maxResource = 100f;      // max resource value
    public float depletionRate = 5f;      // resource depletion rate
    public float depletionMultiplier = 1f; // resource depletion multiplier

    [Header("UI Settings")]      // reference to the resource UI canvas
    public Image WaterdropImage;
    public TextMeshProUGUI WaterdropText;
    [SerializeField]
    private float fadeDuration = 0.5f;

    private bool isDepleting = false;     // flag to check if the resource is depleting
    private bool isInitialized = false;   // flag to check if the resource is initialized
    public bool isPickedUp { get; private set; } = false;

    public event System.Action OnResourceDepleted;

    private void Awake()
    {
        // Ensure the water drop image starts fully transparent

        if (WaterdropImage != null)
        {
            var tempColor = WaterdropImage.color;
            tempColor.a = 0;
            WaterdropImage.color = tempColor;
        }

        // Ensure WaterdropText starts fully transparent
        if (WaterdropText != null)
        {
            var tempColor = WaterdropText.color;
            tempColor.a = 0;
            WaterdropText.color = tempColor;
        }

    }

    private void Update()
    {
        if (isPickedUp && isDepleting && currentResource > 0) 
        {
            currentResource -= depletionRate * depletionMultiplier * Time.deltaTime;

            if (currentResource <= 0)
            {
                currentResource = 0;
                isDepleting = false;
                OnResourceDepleted?.Invoke();
            }
        }

        if (WaterdropText.color.a > 0)
        {
            WaterdropText.text = currentResource.ToString("F0");
        }

    }

    public void StartUITransition()
    {
        if (WaterdropImage != null)
        {
            StartCoroutine(PlayUITransition());
        }
    }

    private IEnumerator PlayUITransition()
    {
        float elapsedTime = 0f;

        // Fade in the WaterdropImage
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            SetImageAlpha(WaterdropImage, alpha);
            yield return null;
        }

        // Wait for 1 second with the image fully visible
        yield return new WaitForSeconds(1f);

        // Fade out the WaterdropImage
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(1 - (elapsedTime / fadeDuration));
            SetImageAlpha(WaterdropImage, alpha);
            yield return null;
        }

        // Start resource depletion after the transition
        StartResourceDepletion(currentResource);

        // Show WaterdropText after resource depletion starts with fade-in
        if (WaterdropText != null)
        {
            StartCoroutine(FadeText(WaterdropText, 0f, 1f, fadeDuration));
        }
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color tempColor = text.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            tempColor.a = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            text.color = tempColor;
            yield return null;
        }

        tempColor.a = endAlpha;
        text.color = tempColor;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image != null)
        {
            var tempColor = image.color;
            tempColor.a = alpha;
            image.color = tempColor;
        }
    }

    public void StartResourceDepletion(float initialMaxResource)
    {
        if (!isInitialized)
        {
            //maxResource = initialMaxResource;
            //currentResource = maxResource;
            isInitialized = true;
        }

        if (isPickedUp)  
        {
            isDepleting = true;
            if (WaterdropText != null)
            {
                StartCoroutine(FadeText(WaterdropText, 0f, 1f, fadeDuration));
            }
        }
    }

    public void StopResourceDepletion()
    {
        isDepleting = false;

        // Fade out WaterdropText when resource depletion stops
        if (WaterdropText != null)
        {
            //StartCoroutine(FadeText(WaterdropText, 1f, 0f, fadeDuration));
        }
    }

    public void SetDepletionMultiplier(float multiplier)
    {
        depletionMultiplier = multiplier;
    }

    public void SetDepletionRate(float rate)
    {
        depletionRate = rate;
    }

    public void TriggerResourceDepleted() // Triggered when the resource is depleted
    {
        OnResourceDepleted?.Invoke();
    }

    public void IsPickedUp()
    {
        isPickedUp = true;
           // Debug.Log("Item Picked Up!");  // 可选：在 Console 输出状态

    }

    public void IsDroppedBy()
    {
        isPickedUp = false;
            //Debug.Log("Item Dropped!");  // 可选：在 Console 输出状态
    }

    public void AddResource(float amount)
    {
        StartCoroutine(SmoothAddResource(amount));
    }

    private IEnumerator SmoothAddResource(float amount)
    {
        float targetResource = Mathf.Min(currentResource + amount, maxResource);
        float startResource = currentResource;
        float elapsedTime = 0f;

        while (elapsedTime < 1f) //在这里调整数字的速度
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / 1f;
            currentResource = Mathf.Lerp(startResource, targetResource, t);
            
            yield return null;
        }

        // 确保最终值精确
        currentResource = targetResource;
    }
}
