Shader "Hidden/SonarXRay"
{
    Properties
    {
        [HDR] _XRayColor ("Color", Color) = (0, 1, 1, 1)
        _XRayPulse ("Pulse (speed, min alpha, max alpha)", Vector) = (4, 0.25, 0.9, 0)
        _XRayRim ("Rim Power", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SonarXRay"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater   // so desenha onde esta ATRAS de algo

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _SonarBlend; // global, vem do SonarFx.cs

            CBUFFER_START(UnityPerMaterial)
                float4 _XRayColor;
                float4 _XRayPulse;
                float  _XRayRim;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS  = GetWorldSpaceViewDir(p.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pulse = 0.5 + 0.5 * sin(_Time.y * _XRayPulse.x);
                float a = lerp(_XRayPulse.y, _XRayPulse.z, pulse);

                // Borda mais forte (rim), da aspecto de silhueta
                float rim = pow(1.0 - saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS))), _XRayRim);

                return half4(_XRayColor.rgb, saturate(a * (0.4 + rim)) * _SonarBlend);
            }
            ENDHLSL
        }
    }
}