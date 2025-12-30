Shader "MarinsPlayLab/SolarWebGLLit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _SpecColor ("Spec Color", Color) = (0.1,0.1,0.1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
        _Ambient ("Ambient (planets -> 0)", Range(0,1)) = 0

        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR]_EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionStrength ("Emission Strength", Range(0,200)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SpecColor;
                float  _Smoothness;
                float  _Ambient;

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
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;

                half3  vertexAddLit : TEXCOORD4; // when _ADDITIONAL_LIGHTS_VERTEX
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionCS  = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = IN.uv;
                OUT.shadowCoord = GetShadowCoord(posInputs);

                OUT.vertexAddLit = half3(0,0,0);
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    OUT.vertexAddLit = VertexLighting(OUT.positionWS, normalize(OUT.normalWS));
                #endif

                return OUT;
            }

            half3 EvalDiffuseSpec(Light l, half3 N, half3 V, half3 albedoRgb, out half3 specOut)
            {
                half3 diff = LightingLambert(l.color, l.direction, N);
                specOut = LightingSpecular(l.color, l.direction, N, V, _SpecColor, _Smoothness);
                return albedoRgb * diff;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half3 albedoRgb = (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor).rgb;

                half3 N = normalize(IN.normalWS);
                half3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                half3 color = albedoRgb * _Ambient;

                // Per-vertex additional lights (if enabled in URP)
                color += albedoRgb * IN.vertexAddLit;

                // Optional additional lights per-pixel
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    uint count = GetAdditionalLightsCount();
                    for (uint li = 0u; li < count; li++)
                    {
                        Light l = GetAdditionalLight(li, IN.positionWS, half4(1,1,1,1));
                        half3 specL;
                        half3 diffL = EvalDiffuseSpec(l, N, V, albedoRgb, specL);
                        half att = l.distanceAttenuation * l.shadowAttenuation;
                        color += (diffL + specL) * att;
                    }
                }
                #endif

                // Main light (directional)
                {
                    Light mainL = GetMainLight(IN.shadowCoord);
                    half3 specM;
                    half3 diffM = EvalDiffuseSpec(mainL, N, V, albedoRgb, specM);
                    color += (diffM + specM) * mainL.shadowAttenuation;
                }

                // Emission
                half3 emisTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb;
                color += emisTex * _EmissionColor.rgb * _EmissionStrength;

                return half4(color, 1.0h);
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