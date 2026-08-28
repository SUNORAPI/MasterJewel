using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 弾へチーム色の加算合成パーティクル・残光・ライトを付ける。
[DisallowMultipleComponent]
public sealed class ProjectileAura : MonoBehaviour
{
    static readonly Color BlueAura = new Color(0.05f, 0.4f, 1f, 1f);
    static readonly Color RedAura = new Color(1f, 0.05f, 0.08f, 1f);

    Material auraMaterial;
    bool initialized;

    public void Initialize(int team)
    {
        if (initialized) return;
        initialized = true;

        Color color = team == 0 ? BlueAura : team == 1 ? RedAura : Color.white;
        auraMaterial = CreateAuraMaterial(color);
        BuildTrail(color);
        BuildParticles(color);
        BuildLight(color);
        RemoveLegacyPostProcessing();
    }

    void BuildTrail(Color color)
    {
        var trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.22f;
        trail.minVertexDistance = 0.04f;
        trail.startWidth = 0.9f;
        trail.endWidth = 0f;
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;
        trail.startColor = WithAlpha(color * 4f, 0.85f);
        trail.endColor = WithAlpha(color, 0f);
        trail.material = auraMaterial;
    }

    void BuildParticles(Color color)
    {
        var particleObject = new GameObject("Team Aura Particles");
        particleObject.transform.SetParent(transform, false);
        var particles = particleObject.AddComponent<ParticleSystem>();

        // AddComponent直後は自動再生中なので、duration等を変更する前に完全停止する。
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = WithAlpha(color * 4f, 0.8f);
        main.maxParticles = 64;

        var emission = particles.emission;
        emission.rateOverTime = 55f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.65f;
        shape.radiusThickness = 1f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color * 4f, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = auraMaterial;
        particles.Play();
    }

    void BuildLight(Color color)
    {
        var light = gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 2.5f;
        light.range = 7f;
        light.shadows = LightShadows.None;
    }

    static Material CreateAuraMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var material = new Material(shader) { name = "Projectile Team Aura (Runtime)" };
        Color hdrColor = color * 6f;
        hdrColor.a = 0.8f;
        material.color = hdrColor;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", hdrColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", hdrColor);
        }
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 2f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.One);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    static void RemoveLegacyPostProcessing()
    {
        // 以前の実装がPlay中のスクリプト更新後にも残っていた場合は破棄する。
        var legacyVolume = GameObject.Find("Projectile Aura Post Processing");
        if (legacyVolume != null) Destroy(legacyVolume);

        var camera = Camera.main;
        if (camera != null)
        {
            var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null) cameraData.renderPostProcessing = false;
        }
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    void OnDestroy()
    {
        if (auraMaterial != null) Destroy(auraMaterial);
    }
}
