using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Audio;

public class EndPointUI : MonoBehaviour
{
    [Header("UI Settings")]      // reference to the resource UI canvas
    public Image waterdropImage;
    public Image backgroundImage;
    public Image blackScreenImage; // 新增的全屏黑色遮罩
    private float waterdropFadeDuration = 2f;
    private float backgroundFadeDuration = 0.5f;

    private void Start()
    {
        if (waterdropImage != null)
        {
            var tempColor = waterdropImage.color;
            tempColor.a = 0;
            waterdropImage.color = tempColor;
        }
        if (backgroundImage != null)
        {
            var tempColor = backgroundImage.color;
            tempColor.a = 0;
            backgroundImage.color = tempColor;
        }
        if (blackScreenImage != null)
        {
            var tempColor = blackScreenImage.color;
            tempColor.a = 0; // 初始化为完全透明
            blackScreenImage.color = tempColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            StartCoroutine(FadeInAndOutUI());
            //StartCoroutine(AudioManager.instance.EnableGlassBrokenSFXWithDelay(6.5f));
        }
    }

    private IEnumerator FadeInAndOutUI()
    {   
        // Fade in backgroundImage first
        yield return StartCoroutine(FadeUI(backgroundImage, 0f, 1f, backgroundFadeDuration));

        // Immediately start fading in waterdropImage after backgroundImage completes
        yield return StartCoroutine(FadeUI(waterdropImage, 0f, 1f, waterdropFadeDuration));

        // Keep waterdropImage fully visible for 0.5 second
        yield return new WaitForSeconds(1f);

        // Fade out waterdropImage
        yield return StartCoroutine(FadeUI(waterdropImage, 1f, 0f, waterdropFadeDuration));

        // Fade out backgroundImage
        yield return StartCoroutine(FadeUI(backgroundImage, 1f, 0f, backgroundFadeDuration));

        // Wait 1 second before black screen
        yield return new WaitForSeconds(1f);

        // Instant black screen
        if (blackScreenImage != null)
        {
            var tempColor = blackScreenImage.color;
            tempColor.a = 1; // 完全不透明
            blackScreenImage.color = tempColor;
            StartCoroutine(AudioManager.instance.EnableGlassBrokenSFXWithDelay(1f));
        }
    }

    private IEnumerator FadeUI(Image image, float startAlpha, float endAlpha, float duration)
    {
        if (image == null)
            yield break;

        float elapsedTime = 0f;
        Color tempColor = image.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            tempColor.a = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            image.color = tempColor;
            yield return null;
        }

        tempColor.a = endAlpha;
        image.color = tempColor;
    }
}
