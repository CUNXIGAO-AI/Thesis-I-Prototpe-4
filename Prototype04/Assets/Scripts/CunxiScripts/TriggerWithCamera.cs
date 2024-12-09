using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TriggerWithCamera : MonoBehaviour
{
    public TextMeshProUGUI gameText;
    public List<string> messages = new List<string>();
    private int currentMessageIndex = 0;
    private bool playerInRange = false;

    public Camera secondaryCamera;

    public Image backgroundImage; // 背景框的引用

    private CanvasGroup canvasGroup;

    [SerializeField]
    private float fadeDuration = 0.5f;

    private bool isFading = false;

    private void Start()
    {
        gameText.text = "";
        canvasGroup = gameText.gameObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = 0f; // 设置背景框初始透明度
            backgroundImage.color = bgColor;
        }

        if (secondaryCamera != null)
        {
            secondaryCamera.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        Debug.Log("playerInRange: " + playerInRange);
        if (playerInRange && Input.GetKeyDown(KeyCode.X) && !isFading)
        {
            if (currentMessageIndex == 0 && secondaryCamera != null)
            {
                secondaryCamera.gameObject.SetActive(true);
            }

            currentMessageIndex = (currentMessageIndex + 1) % messages.Count;
            StartCoroutine(FadeText(messages[currentMessageIndex]));

            if (currentMessageIndex == 0 && secondaryCamera != null)
            {
                secondaryCamera.gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Animal"))
        {
            playerInRange = true;
            Debug.Log("进入范围了");

            currentMessageIndex = 0;
            StartCoroutine(FadeText(messages[currentMessageIndex]));

            if (backgroundImage != null)
            {
                StartCoroutine(FadeBackground(1f)); // 背景框渐入
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Animal"))
        {
            playerInRange = false;
            Debug.Log("离开范围了");

            StartCoroutine(FadeText("")); // 离开时文字淡出

            if (backgroundImage != null)
            {
                StartCoroutine(FadeBackground(0f)); // 背景框渐出
            }

            if (secondaryCamera != null)
            {
                secondaryCamera.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator FadeText(string newText)
    {
        isFading = true;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameText.text = newText;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        isFading = false;
    }

    private IEnumerator FadeBackground(float targetAlpha)
    {
        if (backgroundImage == null)
            yield break;

        float startAlpha = backgroundImage.color.a;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            Color bgColor = backgroundImage.color;
            bgColor.a = newAlpha;
            backgroundImage.color = bgColor;
            yield return null;
        }

        Color finalColor = backgroundImage.color;
        finalColor.a = targetAlpha;
        backgroundImage.color = finalColor;
    }
}
