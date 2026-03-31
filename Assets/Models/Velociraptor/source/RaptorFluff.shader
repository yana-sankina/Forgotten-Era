Shader "Custom/RaptorFeathersLit_URP"
{
    Properties
    {
        [MainTexture] _BaseMap ("Текстура кожи", 2D) = "white" {}
        _FluffScale ("Размер перышек", Range(10.0, 500.0)) = 150.0
        _FluffStrength ("Глубина рельефа", Range(0.0, 2.0)) = 0.8
        _RimPower ("Ширина пушистого края", Range(0.5, 8.0)) = 3.0
        _RimIntensity ("Яркость пушка по краям", Range(0.0, 5.0)) = 1.5
    }

    SubShader
    {
        // Указываем, что это непрозрачный объект для URP
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ПРОХОД 1: Отрисовка самого динозавра, света, теней и пушка
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Включаем поддержку теней в URP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float3 viewDirWS    : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _FluffScale;
                float _FluffStrength;
                float _RimPower;
                float _RimIntensity;
            CBUFFER_END

            // Функция случайного шума
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Генерируем "пятнистый" шум для имитации мелких перьев
            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1.0, 0.0)), f.x),
                            lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Переводим координаты для расчета освещения
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                // Вычисляем направление взгляда камеры (нужно для пушка по краям)
                output.viewDirWS = GetCameraPositionWS() - output.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // --- 1. ФЕЙКОВЫЙ РЕЛЬЕФ (Нормали) ---
                // Создаем два слоя шума для шероховатости
                float noise1 = valueNoise(input.uv * _FluffScale);
                float noise2 = valueNoise(input.uv * _FluffScale + float2(0.5, 0.5));
                
                // Искажаем нормаль. Свет будет думать, что поверхность неровная.
                float3 perturbedNormal = normalWS + float3(noise1 - 0.5, noise2 - 0.5, 0) * _FluffStrength;
                perturbedNormal = normalize(perturbedNormal);

                // --- 2. ОСВЕЩЕНИЕ И ТЕНИ ---
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                // Затенение с учетом наших фейковых неровностей
                float NdotL = saturate(dot(perturbedNormal, mainLight.direction));
                
                // --- 3. ЦВЕТ ---
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // Умножаем цвет текстуры на цвет света, силу света и тени
                half3 diffuseColor = albedo.rgb * mainLight.color * (NdotL * mainLight.shadowAttenuation);
                
                // Добавляем немного базового света, чтобы в тени раптор не был черной дырой
                diffuseColor += albedo.rgb * 0.15; 

                // --- 4. ПУШИСТЫЕ КРАЯ (Эффект Френеля) ---
                // Высчитываем края модели относительно камеры
                float rim = 1.0 - saturate(dot(viewDirWS, perturbedNormal));
                rim = pow(rim, _RimPower); // Регулируем толщину края
                
                // Окрашиваем края в цвет самой кожи, но делаем ярче
                half3 rimColor = albedo.rgb * rim * _RimIntensity;

                // Собираем всё вместе
                half3 finalColor = diffuseColor + rimColor;

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // ПРОХОД 2: Заставляем динозавра отбрасывать тень на землю
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0; // Для отбрасывания тени цвет не нужен
            }
            ENDHLSL
        }
    }
}