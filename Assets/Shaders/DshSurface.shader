Shader "DSH/Surface"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0,2)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.35

        [Header(Tiling)]
        [ToggleUI] _Triplanar ("Triplanar (world space)", Float) = 0
        _WorldTiling ("Triplanar Tiling", Float) = 1

        [Header(Detail)]
        _DetailScale ("Detail Scale", Float) = 10
        _DetailStrength ("Detail Strength", Range(0,1)) = 0.3

        [Header(Grunge)]
        _GrungeScale ("Grunge Scale", Float) = 0.5
        _GrungeStrength ("Grunge Amount", Range(0,1)) = 0.3
        _GrungeColor ("Grunge Color", Color) = (0.22,0.2,0.17,1)

        [Header(Panels)]
        _PanelScale ("Panel Lines Tiling", Float) = 0
        _PanelStrength ("Panel Lines Strength", Range(0,1)) = 0.5

        [Header(Wear)]
        _WearStrength ("Edge Wear", Range(0,1)) = 0.15

        [Header(Pattern)]
        _Pattern ("Pattern (0 none, 1 wood, 2 fabric, 3 marble)", Float) = 0
        _PatternScale ("Pattern Scale", Float) = 4
        _PatternStrength ("Pattern Strength", Range(0,1)) = 0.5
        _PatternColor ("Pattern Color", Color) = (0.45,0.3,0.16,1)

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _NormalMap_ST;
                half _NormalScale;
                half _Metallic;
                half _Smoothness;
                half _Triplanar;
                half _WorldTiling;
                half _DetailScale;
                half _DetailStrength;
                half _GrungeScale;
                half _GrungeStrength;
                half4 _GrungeColor;
                half _PanelScale;
                half _PanelStrength;
                half _WearStrength;
                half4 _EmissionColor;
                half _Pattern;
                half _PatternScale;
                half _PatternStrength;
                half4 _PatternColor;
            CBUFFER_END

            // _BaseMap / sampler_BaseMap come from SurfaceInput.hlsl
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half3 vertexSH : TEXCOORD4;
                float fogFactor : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ---- procedural noise -------------------------------------------------
            float DshHash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float DshNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = DshHash(i);
                float b = DshHash(i + float2(1.0, 0.0));
                float c = DshHash(i + float2(0.0, 1.0));
                float d = DshHash(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // ---- procedural surface patterns --------------------------------------
            half3 DshPattern(half3 albedo, float3 p, half pattern, half scale, half strength, half3 color)
            {
                if (pattern < 0.5 || strength < 0.001) return albedo;

                float2 wp = p.xz * scale;
                half3 result = albedo;

                if (pattern < 1.5)
                {
                    // wood grain: warped bands + fine fibre
                    float warp = DshNoise(wp * 0.35) * 2.0 + DshNoise(wp * 1.7) * 0.4;
                    float bands = sin((wp.x + warp * 3.0) * 12.0) * 0.5 + 0.5;
                    float grain = smoothstep(0.25, 0.85, bands);
                    float fibre = DshNoise(float2(wp.x * 14.0, wp.y * 0.6)) * 0.25 + 0.75;
                    result *= lerp(1.0, (0.62 + grain * 0.55) * fibre, strength);
                    result = lerp(result, result * color * 1.6, grain * strength * 0.5);
                }
                else if (pattern < 2.5)
                {
                    // fabric / carpet weave
                    float weave = (sin(wp.x * 180.0) * sin(wp.y * 180.0)) * 0.5 + 0.5;
                    float fuzz = DshNoise(wp * 40.0);
                    result *= lerp(1.0, 0.78 + weave * 0.12 + fuzz * 0.22, strength);
                }
                else
                {
                    // marble / concrete veining
                    float m = DshNoise(wp * 1.3) * 0.6 + DshNoise(wp * 3.1) * 0.3 + DshNoise(wp * 7.3) * 0.1;
                    float veins = 1.0 - abs(m * 2.0 - 1.0);
                    veins = pow(veins, 3.0);
                    result = lerp(result, color * 1.4, veins * strength * 0.55);
                    result *= lerp(1.0, 0.85 + m * 0.3, strength * 0.4);
                }

                return result;
            }

            // ---- vertex -----------------------------------------------------------
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.vertexSH = SampleSH(normalInputs.normalWS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(positionInputs);

                return output;
            }

            // ---- fragment ---------------------------------------------------------
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float sgn = input.tangentWS.w;
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = sgn * cross(normalWS, tangentWS);
                float3x3 tbn = float3x3(tangentWS, bitangentWS, normalWS);

                float2 uv = input.uv;
                float3 p = input.positionWS;

                // ---- albedo ------------------------------------------------------
                half3 albedo;
                if (_Triplanar > 0.5)
                {
                    float3 blend = pow(abs(normalWS), 4.0);
                    blend /= max(blend.x + blend.y + blend.z, 1e-4);
                    float3 wp = p * _WorldTiling;
                    half3 cx = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp.zy).rgb;
                    half3 cy = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp.xz).rgb;
                    half3 cz = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp.xy).rgb;
                    albedo = (cx * blend.x + cy * blend.y + cz * blend.z) * _BaseColor.rgb;
                }
                else
                {
                    albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
                }

                // ---- procedural pattern ------------------------------------------
                albedo = DshPattern(albedo, p, _Pattern, _PatternScale, _PatternStrength, _PatternColor.rgb);

                // ---- detail + grunge ---------------------------------------------
                float2 detailUV = p.xz * _DetailScale + p.y * 3.7;
                float detail = DshNoise(detailUV) * 0.62 + DshNoise(detailUV * 2.73 + 11.3) * 0.38;

                float grunge = DshNoise(p.xz * _GrungeScale + p.y * 0.21 + 7.7);
                grunge = smoothstep(0.42, 0.95, grunge) * _GrungeStrength;

                albedo = lerp(albedo, albedo * _GrungeColor.rgb * 1.4, grunge);
                albedo *= lerp(1.0, 0.80 + detail * 0.40, _DetailStrength);

                // ---- panel lines --------------------------------------------------
                if (_PanelScale > 0.001)
                {
                    float3 g = abs(frac(p * _PanelScale) - 0.5);
                    float edge = min(g.x, min(g.y, g.z));
                    float panelLine = 1.0 - smoothstep(0.008, 0.045, edge);
                    albedo *= lerp(1.0, 0.55, panelLine * _PanelStrength);
                }

                // ---- normal -------------------------------------------------------
                half3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_NormalMap, sampler_NormalMap), _NormalScale);
                float2 dnd = float2(
                    DshNoise(detailUV + float2(0.013, 0.0)) - DshNoise(detailUV - float2(0.013, 0.0)),
                    DshNoise(detailUV + float2(0.0, 0.013)) - DshNoise(detailUV - float2(0.0, 0.013)));
                normalTS.xy += dnd * _DetailStrength * 1.6;
                normalTS = normalize(normalTS);

                float3 finalNormalWS = TransformTangentToWorld(normalTS, tbn);
                finalNormalWS = NormalizeNormalPerPixel(finalNormalWS);

                // ---- smoothness / wear -------------------------------------------
                half smoothness = _Smoothness;
                smoothness *= lerp(1.0, 0.62 + detail * 0.62, _DetailStrength);
                smoothness *= lerp(1.0, 0.55, grunge);

                float fresnel = pow(1.0 - saturate(dot(finalNormalWS, viewDirWS)), 4.0);
                smoothness = saturate(smoothness + fresnel * _WearStrength * 0.35);
                albedo = lerp(albedo, albedo * 0.78, fresnel * _WearStrength);

                // ---- shading ------------------------------------------------------
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = finalNormalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0.0, 0.0, 0.0);
                inputData.bakedGI = input.vertexSH;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1.0, 1.0, 1.0, 1.0);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0.0, 0.0, 0.0);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = _EmissionColor.rgb;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _NormalMap_ST;
                half _NormalScale;
                half _Metallic;
                half _Smoothness;
                half _Triplanar;
                half _WorldTiling;
                half _DetailScale;
                half _DetailStrength;
                half _GrungeScale;
                half _GrungeStrength;
                half4 _GrungeColor;
                half _PanelScale;
                half _PanelStrength;
                half _WearStrength;
                half4 _EmissionColor;
                half _Pattern;
                half _PatternScale;
                half _PatternStrength;
                half4 _PatternColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings shadowVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float3 lightDirectionWS = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightPositionWS = _LightPosition;
                    lightDirectionWS = normalize(lightPositionWS - positionWS);
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 shadowFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _NormalMap_ST;
                half _NormalScale;
                half _Metallic;
                half _Smoothness;
                half _Triplanar;
                half _WorldTiling;
                half _DetailScale;
                half _DetailStrength;
                half _GrungeScale;
                half _GrungeStrength;
                half4 _GrungeColor;
                half _PanelScale;
                half _PanelStrength;
                half _WearStrength;
                half4 _EmissionColor;
                half _Pattern;
                half _PatternScale;
                half _PatternStrength;
                half4 _PatternColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings depthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 depthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex dnVert
            #pragma fragment dnFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _NormalMap_ST;
                half _NormalScale;
                half _Metallic;
                half _Smoothness;
                half _Triplanar;
                half _WorldTiling;
                half _DetailScale;
                half _DetailStrength;
                half _GrungeScale;
                half _GrungeStrength;
                half4 _GrungeColor;
                half _PanelScale;
                half _PanelStrength;
                half _WearStrength;
                half4 _EmissionColor;
                half _Pattern;
                half _PatternScale;
                half _PatternStrength;
                half4 _PatternColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings dnVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 dnFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                return half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
