Shader "Hidden/SonarFX_URP"
{
    Properties
    {
        // rgb = cor do alvo, a = intensidade do destaque (0 = objeto normal)
        [HDR] _SonarTargetColor ("Target Color / Strength", Color) = (0, 0, 0, 0)
        // x = velocidade do pulso, y = brilho do pulso, z = quanto a onda assume a cor do alvo
        _SonarPulse ("Pulse (speed, glow, wave blend)", Vector) = (4, 1, 1, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SonarForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ SONAR_SPHERICAL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Globais enviados pelo SonarFx.cs (fora do CBUFFER)
            float4 _SonarBaseColor;
            float4 _SonarWaveColor;
            float4 _SonarWaveParams; // Amp, Exp, Interval, Speed
            float4 _SonarWaveVector;
            float4 _SonarAddColor;
            float _SonarBlend;

            // Por material / por objeto
            CBUFFER_START(UnityPerMaterial)
                float4 _SonarTargetColor;
                float4 _SonarPulse;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
            #ifdef SONAR_SPHERICAL
                float w = length(IN.positionWS - _SonarWaveVector.xyz);
            #else
                float w = dot(IN.positionWS, _SonarWaveVector.xyz);
            #endif

                w -= _Time.y * _SonarWaveParams.w;
                w /= _SonarWaveParams.z;
                w = w - floor(w);

                float p = _SonarWaveParams.y;
                w = (pow(w, p) + pow(1 - w, p * 4)) * 0.5;
                w *= _SonarWaveParams.x;

                // Destaque do alvo
                float isTarget = _SonarTargetColor.a;
                float3 waveCol = lerp(_SonarWaveColor.rgb, _SonarTargetColor.rgb, isTarget * _SonarPulse.z);
                float pulse    = 0.5 + 0.5 * sin(_Time.y * _SonarPulse.x);
                float3 glow    = _SonarTargetColor.rgb * isTarget * _SonarPulse.y * pulse;

                half3 n = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                half3 lighting = mainLight.color * saturate(dot(n, mainLight.direction)) + SampleSH(n);

                half3 color = _SonarBaseColor.rgb * lighting
                            + waveCol * w
                            + glow
                            + _SonarAddColor.rgb;

                return half4(color, _SonarBlend);
            }
            ENDHLSL
        }
    }
}