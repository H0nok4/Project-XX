Shader "PostEffect/Pixelation"
{
    Properties
    {
        _WidthPixelation("Width Pixelation", Float) = 1024
        _HeightPixelation("Height Pixelation", Float) = 576
        _ColorPrecision("Color Precision", Float) = 64
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Pixelation"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            float _WidthPixelation;
            float _HeightPixelation;
            float _ColorPrecision;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 pixelGrid = max(float2(1.0, 1.0), float2(_WidthPixelation, _HeightPixelation));
                float2 cell = floor(input.texcoord * pixelGrid);
                float2 uv = (cell + 0.5) / pixelGrid;

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float precision = max(_ColorPrecision, 1.0);
                if (precision > 1.0)
                {
                    float steps = max(precision - 1.0, 1.0);
                    float3 encoded = LinearToSRGB(saturate(color.rgb));
                    encoded = round(encoded * steps) / steps;
                    color.rgb = SRGBToLinear(encoded);
                }

                return color;
            }
            ENDHLSL
        }
    }
}
