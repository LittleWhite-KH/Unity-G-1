Shader "DSH/TvPortalCurved"
{
    Properties
    {
        _MainTex ("Portal RenderTexture", 2D) = "black" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Glow ("Glow", Float) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off
        ZWrite On

        Pass
        {
            Name "TvPortalCurved"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float _Glow;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // view * projection of the portal camera (set per frame by TvPortal).
            float4x4 _PortalVP;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 portalPos  : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 positionWS = mul(unity_ObjectToWorld, float4(v.positionOS.xyz, 1.0));
                float4 clip = mul(_PortalVP, positionWS);
                o.portalPos = ComputeScreenPos(clip);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.portalPos.xy / max(i.portalPos.w, 1e-5);
                half3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                // Light CRT scanline plus the usual brightness lift against the main-camera post FX.
                half scan = 0.96 + 0.04 * sin(uv.y * _ScreenParams.y * 1.35);
                c *= scan * _Tint.rgb * 2.0;
                c += _Glow;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
