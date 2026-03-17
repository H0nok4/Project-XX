Shader "PostEffect/Fog"
{
    Properties
    {
        _FogDensity("Fog Density", Float) = 4
        _FogDistance("Fog Distance", Float) = 18
        _FogColor("Fog Color", Color) = (0.22, 0.24, 0.28, 1)
        _FogNear("Fog Near", Float) = 0
        _FogFar("Fog Far", Float) = 90
        _FogAltScale("Fog Alt Scale", Float) = 10
        _FogThinning("Fog Thinning", Float) = 260
        _NoiseScale("Noise Scale", Float) = 260
        _NoiseStrength("Noise Strength", Float) = 0.01
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Fog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _FogDensity;
            float _FogDistance;
            float4 _FogColor;
            float _FogNear;
            float _FogFar;
            float _FogAltScale;
            float _FogThinning;
            float _NoiseScale;
            float _NoiseStrength;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float rawDepth = SampleSceneDepth(uv);
#if UNITY_REVERSED_Z
                bool isSky = rawDepth <= 0.0001;
#else
                bool isSky = rawDepth >= 0.9999;
#endif

                float eyeDepth = isSky ? _ProjectionParams.z : LinearEyeDepth(rawDepth, _ZBufferParams);
                float depthStart = max(_FogDistance, 0.0);
                float fogDepth = max(eyeDepth - depthStart, 0.0);

                float rangeMask = saturate((eyeDepth - _FogNear) / max(_FogFar - _FogNear, 0.001));
                float densityScale = max(_FogDensity, 0.01) * 0.035;
                densityScale *= lerp(1.0, 0.2, saturate(_FogThinning / 500.0));
                densityScale *= lerp(0.8, 1.0, saturate(_FogAltScale / 10.0));

                float fogFactor = 1.0 - exp2(-densityScale * fogDepth);
                fogFactor *= rangeMask;

                float noiseCellSize = max(_NoiseScale * 0.05, 1.0);
                float noise = Hash21(floor((uv * _ScreenParams.xy) / noiseCellSize)) * 2.0 - 1.0;
                fogFactor = saturate(fogFactor + noise * _NoiseStrength);

                color.rgb = lerp(color.rgb, _FogColor.rgb, fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
}
