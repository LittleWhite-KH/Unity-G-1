Shader "DSH/TvPortal"
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
            Name "TvPortal"

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

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / max(i.screenPos.w, 1e-5);
                half3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                // 很轻的 CRT 扫描线 + 电子管偏色，保留画面细节；整体压暗避免被主相机 Bloom 溢光
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
