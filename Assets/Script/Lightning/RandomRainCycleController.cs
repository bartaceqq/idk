using System.Collections;
using DigitalRuby.RainMaker;
using UnityEngine;

[DisallowMultipleComponent]
public class RandomRainCycleController : MonoBehaviour
{
    [Header("Rain Reference")]
    [SerializeField] private BaseRainScript rainScript;

    [Header("State Durations (seconds)")]
    [SerializeField] private Vector2 dryDurationRange = new Vector2(180f, 900f);
    [SerializeField] private Vector2 rainDurationRange = new Vector2(90f, 420f);

    [Header("Rain Intensity")]
    [Range(0f, 1f)]
    [SerializeField] private float minRainIntensity = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float maxRainIntensity = 0.85f;

    [Header("Intensity Variation During Rain (seconds)")]
    [SerializeField] private Vector2 intensityTargetHoldRange = new Vector2(20f, 90f);
    [SerializeField] private Vector2 intensityBlendDurationRange = new Vector2(8f, 30f);

    [Header("Start/Stop Transition (seconds)")]
    [SerializeField] private Vector2 rainStartDurationRange = new Vector2(8f, 35f);
    [SerializeField] private Vector2 rainStopDurationRange = new Vector2(8f, 30f);

    [Header("Startup")]
    [SerializeField] private bool startRaining;

    [Header("Wind")]
    [SerializeField] private bool controlWind = true;
    [SerializeField] private bool windWhenRaining = true;
    [SerializeField] private bool windWhenDry;

    private Coroutine weatherRoutine;

    private void Reset()
    {
        rainScript = GetComponent<BaseRainScript>();
    }

    private void OnEnable()
    {
        if (rainScript == null)
        {
            rainScript = GetComponent<BaseRainScript>();
        }

        if (rainScript == null)
        {
            Debug.LogError("RandomRainCycleController requires a BaseRainScript reference.");
            enabled = false;
            return;
        }

        weatherRoutine = StartCoroutine(WeatherLoop());
    }

    private void OnDisable()
    {
        if (weatherRoutine != null)
        {
            StopCoroutine(weatherRoutine);
            weatherRoutine = null;
        }
    }

    private void OnValidate()
    {
        dryDurationRange = SanitizeRange(dryDurationRange, 60f);
        rainDurationRange = SanitizeRange(rainDurationRange, 45f);
        intensityTargetHoldRange = SanitizeRange(intensityTargetHoldRange, 10f);
        intensityBlendDurationRange = SanitizeRange(intensityBlendDurationRange, 2f);
        rainStartDurationRange = SanitizeRange(rainStartDurationRange, 3f);
        rainStopDurationRange = SanitizeRange(rainStopDurationRange, 3f);

        minRainIntensity = Mathf.Clamp01(minRainIntensity);
        maxRainIntensity = Mathf.Clamp01(maxRainIntensity);
        if (maxRainIntensity < minRainIntensity)
        {
            maxRainIntensity = minRainIntensity;
        }
    }

    private IEnumerator WeatherLoop()
    {
        bool currentlyRaining = startRaining;

        if (!currentlyRaining)
        {
            rainScript.RainIntensity = 0f;
            ApplyWind(false);
        }

        while (true)
        {
            if (currentlyRaining)
            {
                yield return RainPhase();
            }
            else
            {
                yield return DryPhase();
            }

            currentlyRaining = !currentlyRaining;
        }
    }

    private IEnumerator DryPhase()
    {
        ApplyWind(false);
        yield return TransitionToIntensity(0f, RandomInRange(rainStopDurationRange));
        yield return new WaitForSeconds(RandomInRange(dryDurationRange));
    }

    private IEnumerator RainPhase()
    {
        ApplyWind(true);

        float phaseDuration = RandomInRange(rainDurationRange);
        float phaseEndTime = Time.time + phaseDuration;

        float intensityTarget = RandomRainTarget();
        float intensitySpeed = SpeedToTarget(intensityTarget, RandomInRange(rainStartDurationRange));
        float nextRetargetTime = Time.time + RandomInRange(intensityTargetHoldRange);

        while (Time.time < phaseEndTime)
        {
            if (Time.time >= nextRetargetTime)
            {
                intensityTarget = RandomRainTarget();
                intensitySpeed = SpeedToTarget(intensityTarget, RandomInRange(intensityBlendDurationRange));
                nextRetargetTime = Time.time + RandomInRange(intensityTargetHoldRange);
            }

            rainScript.RainIntensity = Mathf.MoveTowards(rainScript.RainIntensity, intensityTarget, intensitySpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator TransitionToIntensity(float target, float duration)
    {
        if (duration <= 0f)
        {
            rainScript.RainIntensity = target;
            yield break;
        }

        float speed = SpeedToTarget(target, duration);
        while (Mathf.Abs(rainScript.RainIntensity - target) > 0.001f)
        {
            rainScript.RainIntensity = Mathf.MoveTowards(rainScript.RainIntensity, target, speed * Time.deltaTime);
            yield return null;
        }

        rainScript.RainIntensity = target;
    }

    private void ApplyWind(bool raining)
    {
        if (!controlWind || rainScript == null)
        {
            return;
        }

        rainScript.EnableWind = raining ? windWhenRaining : windWhenDry;
    }

    private float RandomRainTarget()
    {
        return Random.Range(minRainIntensity, maxRainIntensity);
    }

    private float SpeedToTarget(float target, float duration)
    {
        duration = Mathf.Max(duration, 0.01f);
        return Mathf.Abs(target - rainScript.RainIntensity) / duration;
    }

    private static float RandomInRange(Vector2 range)
    {
        return Random.Range(range.x, range.y);
    }

    private static Vector2 SanitizeRange(Vector2 value, float minimum)
    {
        float min = Mathf.Max(minimum, value.x);
        float max = Mathf.Max(min, value.y);
        return new Vector2(min, max);
    }
}
