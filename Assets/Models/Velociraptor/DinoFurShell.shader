Shader "Custom/DinoFurShell"
{
    Properties
    {
        _BaseColor ("Base Tint", Color) = (1, 1, 1, 1)
        _TipColor ("Tip Tint", Color) = (1, 1, 1, 1)
        baseColorTexture ("Base Color Tex", 2D) = "white" {}
        normalTexture ("Normal Tex", 2D) = "bump" {}
        occlusionTexture ("Occlusion Tex", 2D) = "white" {}
        metallicRoughnessTexture ("Metallic-Roughness Tex", 2D) = "black" {}
        _ShellLength ("Shell Length", Float) = 0.3
        _ShellCount ("Shell Count", Float) = 32
        _ShellIndex ("Shell Index", Float) = 0
        _Density ("Feather Density", Float) = 30
        _Thickness ("Feather Width", Range(0, 1)) = 0.7
        _Elongation ("Feather Elongation", Range(1, 6)) = 3.0
        _OcclusionAttenuation ("Feather AO", Range(0, 5)) = 1.5
        _CombDir ("Comb Direction", Vector) = (0, 0, -1, 0)
        _CombStrength ("Comb Strength", Float) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "FurShellPass"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float4 baseColorTexture_ST;
                float _ShellLength;
                float _ShellCount;
                float _ShellIndex;
                float _Density;
                float _Thickness;
                float _Elongation;
                float _OcclusionAttenuation;
                float4 _CombDir;
                float _CombStrength;
            CBUFFER_END

            TEXTURE2D(baseColorTexture);          SAMPLER(sampler_baseColorTexture);
            TEXTURE2D(normalTexture);              SAMPLER(sampler_normalTexture);
            TEXTURE2D(occlusionTexture);           SAMPLER(sampler_occlusionTexture);
            TEXTURE2D(metallicRoughnessTexture);   SAMPLER(sampler_metallicRoughnessTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float  shellNorm   : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
                float3 tangentWS   : TEXCOORD5;
                float3 bitangentWS : TEXCOORD6;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float shellNorm = _ShellIndex / max(_ShellCount, 1.0);
                float shellHeight = shellNorm * _ShellLength;

                float3 posOS = IN.positionOS.xyz;
                float3 nrmOS = normalize(IN.normalOS);

                // Minimal normal lift + comb displacement
                posOS += nrmOS * (shellHeight * 0.05);
                float hf = shellNorm * shellNorm;
                posOS += _CombDir.xyz * (shellHeight * _CombStrength * hf);

                OUT.positionWS = TransformObjectToWorld(posOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(nrmOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, baseColorTexture);
                OUT.shellNorm = shellNorm;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                OUT.tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.bitangentWS = cross(OUT.normalWS, OUT.tangentWS) * IN.tangentOS.w;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float shellNorm = IN.shellNorm;

                float4 albedo = SAMPLE_TEXTURE2D(baseColorTexture, sampler_baseColorTexture, IN.uv);
                float texAO = SAMPLE_TEXTURE2D(occlusionTexture, sampler_occlusionTexture, IN.uv).r;

                // Normal map
                float3 nTS = UnpackNormal(SAMPLE_TEXTURE2D(normalTexture, sampler_normalTexture, IN.uv));
                float3 N = normalize(IN.normalWS);
                float3 T = normalize(IN.tangentWS);
                float3 B = normalize(IN.bitangentWS);
                float3 normalWS = normalize(T * nTS.x + B * nTS.y + N * nTS.z);

                // --- Feather overlap pattern ---
                float2 cellUV = IN.uv * _Density;
                float row = floor(cellUV.y);
                cellUV.x += row * 0.5;

                float2 cellID = floor(cellUV);
                float2 cellLocal = frac(cellUV); // 0 to 1 within cell

                // Per-feather random values
                float randOffset = hash21(cellID) * 0.4 - 0.2;           // position jitter
                float randSize = 0.8 + hash21(cellID + 1.1) * 0.4;       // size variation
                float randAngle = (hash21(cellID + 2.2) - 0.5) * 0.5;    // rotation jitter

                // Jitter center position
                float centerX = 0.5 + randOffset;
                float centerY = 0.5 + (hash21(cellID + 3.3) * 0.3 - 0.15);

                // Rotate local coords around feather center
                float2 p = cellLocal - float2(centerX, centerY);
                float cs = cos(randAngle);
                float sn = sin(randAngle);
                float2 pr = float2(p.x * cs - p.y * sn, p.x * sn + p.y * cs);

                // featherT: 0 at base, 1 at tip (along rotated Y)
                float featherT = saturate(pr.y / (_Elongation * 0.5 * randSize) + 0.5);

                // Feather shape: wide at base, pointed at tip
                float halfWidth = _Thickness * 0.5 * randSize * (1.0 - featherT * featherT);
                bool inFeather = abs(pr.x) < halfWidth;

                // Flat overlap: pixel's featherT determines which shell it shows on
                float exposedAt = featherT;
                float shellBand = 1.0 / max(_ShellCount, 1.0);
                bool inLayer = abs(exposedAt - shellNorm) < shellBand * 1.5;

                // Barb lines
                float barbFreq = 20.0;
                float barbAngle2 = abs(pr.x) * barbFreq + featherT * barbFreq * 0.5;
                float barbs = smoothstep(0.35, 0.55, frac(barbAngle2));

                // Rachis (central stem) — darker line down the middle
                float rachis = 1.0 - smoothstep(0.0, 0.03 * randSize, abs(pr.x));


                if (_ShellIndex < 0.5)
                {
                    // base shell: solid
                }
                else if (!inFeather || !inLayer)
                {
                    discard;
                }

                // Lighting
                Light mainLight = GetMainLight();
                float wrap = saturate(dot(normalWS, mainLight.direction) * 0.5 + 0.5);
                float3 diffuse = mainLight.color * wrap;
                float3 ambient = SampleSH(normalWS);

                float3 finalColor;

                if (_ShellIndex < 0.5)
                {
                    finalColor = albedo.rgb * (diffuse + ambient) * texAO;
                }
                else
                {
                    // Feather shells
                    float featherAO = max(pow(shellNorm, _OcclusionAttenuation), 0.25);

                    float3 tint = lerp(_BaseColor.rgb, _TipColor.rgb, featherT);
                    float3 featherColor = albedo.rgb * tint;

                    // Per-feather variation
                    featherColor *= (1.0 - hash21(cellID + 7.77) * 0.08);

                    // Barb detail
                    float barbDetail = lerp(0.85, 1.0, barbs);
                    featherColor *= barbDetail;

                    // Rachis — dark central stem
                    featherColor = lerp(featherColor, featherColor * 0.4, rachis);

                    finalColor = featherColor * (diffuse + ambient) * featherAO * texAO;
                }

                finalColor = MixFog(finalColor, IN.fogFactor);
                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings shadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                posWS = ApplyShadowBias(posWS, normalWS, _LightDirection);
                OUT.positionCS = TransformWorldToHClip(posWS);
                #if UNITY_REVERSED_Z
                    OUT.positionCS.z = min(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    OUT.positionCS.z = max(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return OUT;
            }

            half4 shadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
