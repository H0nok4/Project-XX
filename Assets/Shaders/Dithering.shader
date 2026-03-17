Shader "PostEffect/Dithering"
{
    Properties
    {
        _PatternIndex("Pattern Index", Int) = 2
        _DitherThreshold("Dither Threshold", Float) = 8
        _DitherStrength("Dither Strength", Float) = 0.1
        _DitherScale("Dither Scale", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Dithering"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            int _PatternIndex;
            float _DitherThreshold;
            float _DitherStrength;
            float _DitherScale;

            float GetPatternValue(int patternIndex, int x, int y)
            {
                int index = y * 4 + x;

                if (patternIndex == 0)
                {
                    const float pattern[16] =
                    {
                        0.0, 1.0, 0.0, 1.0,
                        1.0, 0.0, 1.0, 0.0,
                        0.0, 1.0, 0.0, 1.0,
                        1.0, 0.0, 1.0, 0.0
                    };

                    return pattern[index];
                }

                if (patternIndex == 1)
                {
                    const float pattern[16] =
                    {
                        0.0 / 16.0, 8.0 / 16.0, 2.0 / 16.0, 10.0 / 16.0,
                        12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0, 6.0 / 16.0,
                        3.0 / 16.0, 11.0 / 16.0, 1.0 / 16.0, 9.0 / 16.0,
                        15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0, 5.0 / 16.0
                    };

                    return pattern[index];
                }

                if (patternIndex == 2)
                {
                    const float pattern[16] =
                    {
                        0.0 / 16.0, 12.0 / 16.0, 3.0 / 16.0, 15.0 / 16.0,
                        8.0 / 16.0, 4.0 / 16.0, 11.0 / 16.0, 7.0 / 16.0,
                        2.0 / 16.0, 14.0 / 16.0, 1.0 / 16.0, 13.0 / 16.0,
                        10.0 / 16.0, 6.0 / 16.0, 9.0 / 16.0, 5.0 / 16.0
                    };

                    return pattern[index];
                }

                const float pattern[16] =
                {
                    1.0, 0.0, 0.0, 1.0,
                    0.0, 1.0, 1.0, 0.0,
                    0.0, 1.0, 1.0, 0.0,
                    1.0, 0.0, 0.0, 1.0
                };

                return pattern[index];
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                float scale = max(_DitherScale, 0.25);
                float2 pixelPos = floor((uv * _ScreenParams.xy) / scale);
                int x = ((int)pixelPos.x) & 3;
                int y = ((int)pixelPos.y) & 3;

                float pattern = GetPatternValue(_PatternIndex, x, y);
                float strength = saturate(_DitherStrength);
                float spread = max(_DitherThreshold, 1.0);
                float offset = (pattern - 0.5) * (strength / spread);

                float3 dithered = saturate(color.rgb + offset.xxx);
                color.rgb = lerp(color.rgb, dithered, strength);
                return color;
            }
            ENDHLSL
        }
    }
}
