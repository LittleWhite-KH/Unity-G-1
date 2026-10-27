Shader "DSH/SanGlitch"
{
    Properties
    {
        _BlitTexture ("Source", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SanGlitch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _SanGlitch;
            float _SanGlitchTime;

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float g = saturate(_SanGlitch);
                float t = _SanGlitchTime;

                // Thin horizontal slices slip sideways: the core glitch-art language.
                float bandId = floor(uv.y * 72.0);
                float bandNoise = Hash12(float2(bandId, floor(t * 9.0)));
                float tear = step(1.0 - g * 0.5, bandNoise);
                float tearShift = (Hash12(float2(bandId * 1.7, floor(t * 13.0))) - 0.5) * 0.10 * g * tear;
                uv.x += tearShift;

                // Coarse blocks tear as well.
                float2 cell = float2(floor(uv.x * 12.0), floor(uv.y * 18.0));
                float block = step(1.0 - g * 0.22, Hash12(cell + floor(t * 5.0)));
                uv.x += block * (Hash12(float2(cell.y * 3.1, floor(t * 6.0))) - 0.5) * 0.04 * g;

                // Chromatic aberration (RGB split), the reference image's strongest cue.
                float ca = (0.0006 + g * 0.022) * (0.4 + 0.6 * bandNoise);
                half3 col;
                col.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(ca, 0.0)).r;
                col.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                col.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(ca, 0.0)).b;

                // Fine CRT scanlines.
                float scan = 1.0 - (sin(uv.y * _ScreenParams.y * 0.85) * 0.5 + 0.5) * (0.012 + g * 0.05);
                col *= scan;

                // Finest grain only; the reference keeps noise minimal.
                float grain = Hash12(uv * _ScreenParams.xy + frac(t) * 137.0);
                col += (grain - 0.5) * (0.003 + g * 0.016);

                // Desaturate and push slightly cool as sanity drops.
                half luma = dot(col, half3(0.299, 0.587, 0.114));
                col = lerp(col, half3(luma * 0.95, luma * 1.02, luma * 1.10), g * 0.28);

                // Vignette + flicker.
                float2 d = uv - 0.5;
                col *= saturate(1.0 - dot(d, d) * (0.15 + g * 0.85));
                col *= 1.0 - g * 0.06 * Hash12(float2(floor(t * 30.0), 7.0));

                // Small lift so heavy distortion does not darken the scene.
                col *= 1.0 + g * 0.2;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
