// The gameplay backdrop: a plain two-colour vertical gradient laid over the screen, used as the scene's skybox.
// The gradient follows the screen, not the view direction, so it does not move with the camera's tilt or the phone's
// aspect. A hair of dither hides the banding a soft gradient gets on an 8-bit display.
Shader "HoldIt/Backdrop"
{
    Properties
    {
        _Top ("Top", Color) = (0.93, 0.89, 0.80, 1)
        _Bottom ("Bottom", Color) = (0.84, 0.78, 0.68, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Backdrop"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Top;
                half4 _Bottom;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionNDC : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionNDC = positions.positionNDC;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.positionNDC.xy / input.positionNDC.w;
                half t = smoothstep(0.0, 1.0, saturate(uv.y));
                half3 color = lerp(_Bottom.rgb, _Top.rgb, t);

                // Interleaved gradient noise, one 8-bit step wide, centred on zero.
                float noise = frac(52.9829189 * frac(dot(input.positionCS.xy, float2(0.06711056, 0.00583715))));
                color += (noise - 0.5) / 255.0;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
