using UnityEngine;

/// <summary>
/// Минимальный цикл дня и ночи.
/// Управляет только вращением Directional Light.
/// Вся визуальная реакция неба и освещения настраивается руками в Unity.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    private static readonly int TintProperty = Shader.PropertyToID("_Tint");
    private static readonly int ExposureProperty = Shader.PropertyToID("_Exposure");
    private static readonly int RotationProperty = Shader.PropertyToID("_Rotation");

    [Header("References")]
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;
    [SerializeField] private Material daySkyboxMaterial;
    [SerializeField] private Material nightSkyboxMaterial;

    [Header("Timing")]
    [SerializeField] private float dayLengthSeconds = 300f;
    [SerializeField, Range(0f, 1f)] private float startNormalizedTime = 0.25f;

    [Header("Rotation")]
    [SerializeField] private float minSunPitch = -90f;
    [SerializeField] private float maxSunPitch = 270f;

    [Header("Sun Light")]
    [SerializeField] private float daySunIntensity = 1f;
    [SerializeField] private float nightSunIntensity = 0f;
    [SerializeField] private float dayMoonIntensity = 0f;
    [SerializeField] private float nightMoonIntensity = 0.2f;
    [SerializeField] private Gradient sunColorOverDay;
    [SerializeField] private float horizonFadeRange = 0.08f;
    [SerializeField] private bool disableShadowsAtNight = true;
    [SerializeField] private LightShadows dayShadows = LightShadows.Soft;
    [SerializeField] private LightShadows nightShadows = LightShadows.None;

    [Header("Environment")]
    [SerializeField] private float dayAmbientIntensity = 1f;
    [SerializeField] private float nightAmbientIntensity = 0.2f;
    [SerializeField] private float dayReflectionIntensity = 1f;
    [SerializeField] private float nightReflectionIntensity = 0.2f;
    [SerializeField, Range(0f, 1f)] private float daySkyboxThreshold = 0.2f;
    [SerializeField, Range(0f, 1f)] private float nightSkyboxThreshold = 0.1f;
    [SerializeField, Range(0f, 1f)] private float skyboxSwitchTwilightThreshold = 0.6f;
    [SerializeField, Range(0.1f, 1f)] private float twilightLightMultiplier = 0.45f;

    private float normalizedTime;
    private LightShadows originalShadowMode;
    private LightShadows originalMoonShadowMode;
    private bool shadowsEnabled;
    private bool moonShadowsEnabled;
    private float initialSunYaw;
    private float initialSunRoll;
    private float initialMoonYaw;
    private float initialMoonRoll;
    private bool useDaySkybox = true;
    private float previousSunAltitude;
    private Material runtimeSkyboxMaterial;
    private Color daySkyboxTint = Color.white;
    private Color nightSkyboxTint = Color.white;
    private float daySkyboxExposure = 1f;
    private float nightSkyboxExposure = 1f;
    private float daySkyboxRotation;
    private float nightSkyboxRotation;
    private bool skyboxHasTint;
    private bool skyboxHasExposure;
    private bool skyboxHasRotation;

    private void Start()
    {
        if (sun == null)
        {
            Debug.LogError("DayNightCycle: не назначен Sun (Directional Light).", this);
            enabled = false;
            return;
        }

        if (sun != null)
        {
            originalShadowMode = sun.shadows;
            shadowsEnabled = sun.shadows != LightShadows.None;
            Vector3 initialEuler = sun.transform.rotation.eulerAngles;
            initialSunYaw = initialEuler.y;
            initialSunRoll = initialEuler.z;
        }

        if (moon != null)
        {
            originalMoonShadowMode = moon.shadows;
            moonShadowsEnabled = moon.shadows != LightShadows.None;
            Vector3 initialEuler = moon.transform.rotation.eulerAngles;
            initialMoonYaw = initialEuler.y;
            initialMoonRoll = initialEuler.z;
        }

        InitializeSkybox();
        normalizedTime = Mathf.Clamp01(startNormalizedTime);
        UpdateSun();
        previousSunAltitude = Vector3.Dot(sun.transform.forward, Vector3.down);
    }

    private void Update()
    {
        if (sun == null || dayLengthSeconds <= 0f)
            return;

        normalizedTime += Time.deltaTime / dayLengthSeconds;
        if (normalizedTime > 1f)
            normalizedTime -= 1f;

        UpdateSun();
    }

    private void UpdateSun()
    {
        if (sun == null)
            return;

        float pitch = Mathf.Lerp(minSunPitch, maxSunPitch, normalizedTime);
        sun.transform.rotation = Quaternion.Euler(pitch, initialSunYaw, initialSunRoll);

        float horizonRange = Mathf.Max(0.001f, horizonFadeRange);
        float sunAltitude = Vector3.Dot(sun.transform.forward, Vector3.down);
        float sunHeight01 = Mathf.InverseLerp(-horizonRange, horizonRange, sunAltitude);
        bool sunIsDescending = sunAltitude < previousSunAltitude;
        float moonHeight01 = 0f;
        sun.color = EvaluateSunColor(normalizedTime);

        if (moon != null)
        {
            float moonPitch = Mathf.Repeat(pitch + 180f, 360f);
            moon.transform.rotation = Quaternion.Euler(moonPitch, initialMoonYaw, initialMoonRoll);

            float moonAltitude = Vector3.Dot(moon.transform.forward, Vector3.down);
            moonHeight01 = Mathf.InverseLerp(-horizonRange, horizonRange, moonAltitude);

            if (disableShadowsAtNight)
            {
                float disableThreshold = -horizonRange * 0.5f;
                float enableThreshold = horizonRange * 0.5f;

                if (moonShadowsEnabled && moonAltitude <= disableThreshold)
                    moonShadowsEnabled = false;
                else if (!moonShadowsEnabled && moonAltitude >= enableThreshold)
                    moonShadowsEnabled = true;

                moon.shadows = moonShadowsEnabled ? nightShadows : LightShadows.None;
            }
            else
            {
                moon.shadows = nightShadows != LightShadows.None ? nightShadows : originalMoonShadowMode;
            }
        }

        float twilightBlend = 1f - Mathf.Max(sunHeight01, moonHeight01);
        float twilightMask = Mathf.InverseLerp(nightSkyboxThreshold, skyboxSwitchTwilightThreshold, twilightBlend);
        float lightMultiplier = Mathf.Lerp(1f, twilightLightMultiplier, twilightMask);

        sun.intensity = Mathf.Lerp(nightSunIntensity, daySunIntensity, sunHeight01) * lightMultiplier;
        if (moon != null)
            moon.intensity = Mathf.Lerp(dayMoonIntensity, nightMoonIntensity, moonHeight01) * lightMultiplier;

        float skyBlend = Mathf.Max(sunHeight01, moonHeight01);
        RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbientIntensity, dayAmbientIntensity, sunHeight01) * lightMultiplier;
        RenderSettings.reflectionIntensity = Mathf.Lerp(nightReflectionIntensity, dayReflectionIntensity, skyBlend) * lightMultiplier;
        UpdateSkyboxProperties(sunHeight01);
        UpdateSkybox(sunHeight01, moonHeight01, sunIsDescending);
        previousSunAltitude = sunAltitude;

        if (disableShadowsAtNight)
        {
            float disableThreshold = -horizonRange * 0.5f;
            float enableThreshold = horizonRange * 0.5f;

            if (shadowsEnabled && sunAltitude <= disableThreshold)
                shadowsEnabled = false;
            else if (!shadowsEnabled && sunAltitude >= enableThreshold)
                shadowsEnabled = true;

            sun.shadows = shadowsEnabled ? dayShadows : LightShadows.None;
        }
        else
            sun.shadows = dayShadows != LightShadows.None ? dayShadows : originalShadowMode;
    }

    private void UpdateSkybox(float sunHeight01, float moonHeight01, bool sunIsDescending)
    {
        if (runtimeSkyboxMaterial == null)
            return;

        bool inTwilightWindow = sunHeight01 <= daySkyboxThreshold && moonHeight01 <= daySkyboxThreshold;

        if (useDaySkybox)
        {
            if (sunIsDescending && inTwilightWindow && sunHeight01 <= nightSkyboxThreshold && nightSkyboxMaterial != null)
                useDaySkybox = false;
        }
        else
        {
            if (!sunIsDescending && inTwilightWindow && sunHeight01 >= nightSkyboxThreshold && daySkyboxMaterial != null)
                useDaySkybox = true;
        }

        Material sourceSkybox = useDaySkybox ? daySkyboxMaterial : nightSkyboxMaterial;
        if (sourceSkybox == null)
            return;

        if (sourceSkybox.HasProperty("_Tex") && runtimeSkyboxMaterial.HasProperty("_Tex"))
            runtimeSkyboxMaterial.SetTexture("_Tex", sourceSkybox.GetTexture("_Tex"));
    }

    private void InitializeSkybox()
    {
        Material sourceSkybox = daySkyboxMaterial != null ? daySkyboxMaterial : nightSkyboxMaterial;
        if (sourceSkybox == null)
            return;

        runtimeSkyboxMaterial = new Material(sourceSkybox);
        RenderSettings.skybox = runtimeSkyboxMaterial;

        CacheSkyboxValues(daySkyboxMaterial, ref daySkyboxTint, ref daySkyboxExposure, ref daySkyboxRotation);
        CacheSkyboxValues(nightSkyboxMaterial, ref nightSkyboxTint, ref nightSkyboxExposure, ref nightSkyboxRotation);

        ApplySkyboxSnapshot(daySkyboxTint, daySkyboxExposure, daySkyboxRotation);
    }

    private void CacheSkyboxValues(Material source, ref Color tint, ref float exposure, ref float rotation)
    {
        if (source == null)
            return;

        if (source.HasProperty(TintProperty))
        {
            tint = source.GetColor(TintProperty);
            skyboxHasTint = true;
        }

        if (source.HasProperty(ExposureProperty))
        {
            exposure = source.GetFloat(ExposureProperty);
            skyboxHasExposure = true;
        }

        if (source.HasProperty(RotationProperty))
        {
            rotation = source.GetFloat(RotationProperty);
            skyboxHasRotation = true;
        }
    }

    private void UpdateSkyboxProperties(float sunHeight01)
    {
        if (runtimeSkyboxMaterial == null)
            return;

        Color targetTint = Color.Lerp(nightSkyboxTint, daySkyboxTint, sunHeight01);
        float targetExposure = Mathf.Lerp(nightSkyboxExposure, daySkyboxExposure, sunHeight01);
        float targetRotation = Mathf.LerpAngle(nightSkyboxRotation, daySkyboxRotation, sunHeight01);

        ApplySkyboxSnapshot(targetTint, targetExposure, targetRotation);
    }

    private void ApplySkyboxSnapshot(Color tint, float exposure, float rotation)
    {
        if (runtimeSkyboxMaterial == null)
            return;

        if (skyboxHasTint)
            runtimeSkyboxMaterial.SetColor(TintProperty, tint);

        if (skyboxHasExposure)
            runtimeSkyboxMaterial.SetFloat(ExposureProperty, exposure);

        if (skyboxHasRotation)
            runtimeSkyboxMaterial.SetFloat(RotationProperty, rotation);
    }

    private Color EvaluateSunColor(float time01)
    {
        if (sunColorOverDay == null || sunColorOverDay.colorKeys.Length == 0)
            return Color.white;

        return sunColorOverDay.Evaluate(time01);
    }

    private void OnDestroy()
    {
        if (runtimeSkyboxMaterial != null)
        {
            if (RenderSettings.skybox == runtimeSkyboxMaterial)
                RenderSettings.skybox = null;

            Destroy(runtimeSkyboxMaterial);
            runtimeSkyboxMaterial = null;
        }
    }
}
