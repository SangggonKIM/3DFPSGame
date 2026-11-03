// 파티클용 절차적 화염 쉐이더 (URP, Unlit, Additive)
// 텍스처 없이 FBM 노이즈를 위로 흘려 불꽃 모양과 색을 만든다.
// 노이즈를 월드 좌표 기준으로 샘플링해서 겹치는 파티클끼리 하나의 불처럼 이어져 보인다.
Shader "Custom/FireFlame"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (1.0, 0.92, 0.6, 1)
        [HDR] _MidColor ("Mid Color", Color) = (1.0, 0.45, 0.08, 1)
        [HDR] _EdgeColor ("Edge Color", Color) = (0.7, 0.08, 0.02, 1)
        _NoiseScale ("Noise Scale", Float) = 3.5
        _ScrollSpeed ("Scroll Speed", Float) = 1.6
        _Distortion ("Distortion", Range(0, 1)) = 0.35
        _Softness ("Edge Softness", Range(0.01, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FireUnlit"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _MidColor;
                half4 _EdgeColor;
                float _NoiseScale;
                float _ScrollSpeed;
                float _Distortion;
                float _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int octave = 0; octave < 4; octave++)
                {
                    value += amplitude * ValueNoise(p);
                    p *= 2.03;
                    amplitude *= 0.5;
                }
                return value;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 월드 좌표 기반 노이즈를 위쪽으로 흘린다.
                float2 noiseUV = float2(input.positionWS.x + input.positionWS.z, input.positionWS.y) * _NoiseScale;
                noiseUV.y -= _Time.y * _ScrollSpeed;

                float detail = Fbm(noiseUV);
                float warp = Fbm(noiseUV * 1.7 + 13.1);

                // 파티클 중심에서 멀어질수록 약해지고, 노이즈로 가장자리를 일렁이게 한다.
                float2 centered = input.uv - 0.5;
                centered.x += (warp - 0.5) * _Distortion;
                float radial = 1.0 - saturate(length(centered) * 2.0);
                float heat = radial * (0.55 + detail * 0.9);

                half alpha = smoothstep(0.05, 0.05 + _Softness, heat) * input.color.a;

                half3 color = lerp(_EdgeColor.rgb, _MidColor.rgb, smoothstep(0.25, 0.55, heat));
                color = lerp(color, _CoreColor.rgb, smoothstep(0.55, 0.85, heat));

                return half4(color * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
