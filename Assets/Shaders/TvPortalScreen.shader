Shader "DSH/TvPortalScreen"
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
            Name "TvPortalScreen"

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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                half scan = 0.97 + 0.03 * sin(i.uv.y * _ScreenParams.y * 1.2);
                c *= scan * _Tint.rgb * 2.6;
                c += _Glow;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
