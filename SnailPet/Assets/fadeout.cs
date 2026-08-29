using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFadeInOutAnimation : MonoBehaviour
{
    public GraphicRaycaster graphicRaycaster;
    public Image animationImage;
    public float animationSpeed = 1.0f;
    Color baseColor;

    WaitForSeconds waitForSeconds = new WaitForSeconds(0.5f);
    WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
    // Awake is called
    void Awake()
    {
        Init();
    }

    void Init()
    {
        graphicRaycaster.enabled = false;
        animationImage.enabled = false;
        baseColor = animationImage.color;
    }

    public IEnumerator FadeOut()
    {
        float alphaValue = 0.0f;
        graphicRaycaster.enabled = true;
        animationImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alphaValue);
        animationImage.enabled = true;

        yield return waitForSeconds;

        while (1.0f > alphaValue)
        {
            yield return waitForEndOfFrame;
            alphaValue += Time.deltaTime * animationSpeed;
            animationImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alphaValue);
        }
    }
}