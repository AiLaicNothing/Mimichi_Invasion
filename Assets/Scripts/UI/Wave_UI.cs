using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// "WAVE X" banner: fades in when a wave starts, stays a few seconds, fades out.
/// Put this on the banner panel (background Image + TextMeshProUGUI child).
/// A CanvasGroup is added automatically to fade everything together.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class Wave_UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Text")]
    [Tooltip("{0} = wave number")]
    [SerializeField] private string normalFormat = "WAVE {0}";
    [SerializeField] private string eliteFormat = "WAVE {0}\n<size=60%>ELITE INCOMING</size>";
    [SerializeField] private string bossFormat = "WAVE {0}\n<size=60%>BOSS WAVE</size>";

    [Header("Timing")]
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float displayTime = 2.5f;
    [SerializeField] private float fadeOutTime = 0.8f;

    private CanvasGroup group;
    private Coroutine routine;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private IEnumerator Start()
    {
        // Wait until the Wave_Manager exists (script order safe).
        while (Wave_Manager.Instance == null) yield return null;
        Wave_Manager.Instance.onWaveStart += ShowWave;
    }

    private void OnDestroy()
    {
        if (Wave_Manager.Instance != null) Wave_Manager.Instance.onWaveStart -= ShowWave;
    }

    private void ShowWave(int wave, Wave_Manager.WaveType type)
    {
        string format = type switch
        {
            Wave_Manager.WaveType.Elite => eliteFormat,
            Wave_Manager.WaveType.Boss => bossFormat,
            _ => normalFormat
        };

        waveText.text = string.Format(format, wave);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Banner());
    }

    private IEnumerator Banner()
    {
        yield return Fade(group.alpha, 1f, fadeInTime);
        yield return new WaitForSeconds(displayTime);
        yield return Fade(1f, 0f, fadeOutTime);
        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
