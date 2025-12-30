Shader "MarinsPlayLab/SolarWebGLLit_Alternative"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1

        _SpecColor ("Spec Color", Color) = (0.1,0.1,0.1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.2

        _Ambient ("Ambient (planets -> 0)", Range(0,1)) = 0

        // Rings helpers (JPG strip textures have no alpha)
        [Toggle] _AlphaFromRGB ("Alpha From RGB (for JPG rings)", Float) = 0
        [Toggle] _InvertAlpha ("Invert Alpha", Float) = 0
        _AlphaPower ("Alpha Power", Range(0.25,8)) = 1.5

        [Toggle] _TwoSidedLighting ("Two-Sided Lighting (rings)", Float) = 1
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 0

        // Emission (Sun material: set strength > 0)
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR]_EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionStrength ("Emission Strength", Range(0,200)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // Compile both, but HANDLE both.
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;

                float4 _BaseColor;
                float  _Opacity;

                float4 _SpecColor;
                float  _Smoothness;
                float  _Ambient;

                float  _AlphaFromRGB;
                float  _InvertAlpha;
                float  _AlphaPower;

                float  _TwoSidedLighting;

                float4 _EmissionColor;
                float  _EmissionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
                float4 shadowCoord  : TEXCOORD3;

                half3  vertexAddLit : TEXCOORD4; // for _ADDITIONAL_LIGHTS_VERTEX
            };

            static half NDotL_Sided(half3 N, half3 L, half twoSided)
            {
                half nd = dot(N, L);
                return (twoSided > 0.5h) ? abs(nd) : saturate(nd);
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionCS  = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(posInputs);

                OUT.vertexAddLit = half3(0,0,0);
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    // URP provides diffuse additional lighting via this when per-vertex is enabled.
                    OUT.vertexAddLit = VertexLighting(OUT.positionWS, normalize(OUT.normalWS));
                #endif

                return OUT;
            }

            half3 SimpleSpec(half3 N, half3 V, half3 L, half smoothness)
            {
                // Cheap spec (WebGL-friendly)
                half3 H = normalize(L + V);
                half ndh = saturate(dot(N, H));
                half p = lerp(8.0h, 256.0h, smoothness);
                return pow(ndh, p);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // Alpha handling (optional for JPG ring strips)
                half aMask = tex.a;
                if (_AlphaFromRGB > 0.5)
                {
                    aMask = dot(tex.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                    aMask = saturate(aMask);
                }
                if (_InvertAlpha > 0.5) aMask = 1.0h - aMask;
                aMask = pow(saturate(aMask), (half)_AlphaPower);

                half3 albedoRgb = tex.rgb * _BaseColor.rgb;
                half  alpha     = saturate(aMask * _BaseColor.a * (half)_Opacity);

                half3 N = normalize(IN.normalWS);
                half3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Start with artist ambient (planets: keep 0; rings: usually >0 or two-sided helps)
                half3 color = albedoRgb * (half)_Ambient;

                // If URP is using per-vertex additional lights, apply them here (diffuse only).
                color += albedoRgb * IN.vertexAddLit;

                // Main directional light (if present)
                {
                    Light mainL = GetMainLight(IN.shadowCoord);
                    half nd = NDotL_Sided(N, mainL.direction, (half)_TwoSidedLighting);
                    color += albedoRgb * mainL.color * nd * mainL.shadowAttenuation;

                    half3 sp = SimpleSpec(N, V, mainL.direction, (half)_Smoothness);
                    color += sp * _SpecColor.rgb * mainL.color * nd * mainL.shadowAttenuation;
                }

                // Per-pixel additional lights (when URP chooses _ADDITIONAL_LIGHTS)
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    uint count = GetAdditionalLightsCount();
                    // If you only ever want "sun" as the first light, you can change this to min(count, 1).
                    for (uint li = 0u; li < count; li++)
                    {
                        Light l = GetAdditionalLight(li, IN.positionWS, half4(1,1,1,1));
                        half nd = NDotL_Sided(N, l.direction, (half)_TwoSidedLighting);
                        half att = l.distanceAttenuation * l.shadowAttenuation;

                        color += albedoRgb * l.color * nd * att;

                        half3 sp = SimpleSpec(N, V, l.direction, (half)_Smoothness);
                        color += sp * _SpecColor.rgb * l.color * nd * att;
                    }
                }
                #endif

                // Emission
                half3 emisTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb;
                color += emisTex * _EmissionColor.rgb * (half)_EmissionStrength;

                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}