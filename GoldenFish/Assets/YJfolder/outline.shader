Shader "Unlit/outline"
{
    Properties
    {
        // ������ɫ����
        [Header(Base Color)]
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _BaseSmoothness("Smoothness", Range(0,1)) = 0.5

        // ��Ӱ��ɫ����
        [Header(Shadow Settings)]
        _ShadowColor("Shadow Color", Color) = (0.3,0.3,0.3,1)
        _ShadowThreshold("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSoftness("Shadow Softness", Range(0.01,1)) = 0.1

        // ��߿���
        [Header(Outline Settings)]
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0.001, 0.3)) = 0.02
        [Toggle] _OutlineNoise("Enable Outline Noise", Float) = 0
        _OutlineNoiseScale("Noise Scale", Range(0,10)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // -------------------------------------
        // ���Pass (�ɶ�������)
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _OUTLINENOISE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Noise.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineNoiseScale;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                
                // �������� + ��ѡ�벨�Ŷ�
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                
                #if _OUTLINENOISE_ON
                    float noise = SimpleNoise(IN.uv * _OutlineNoiseScale) * 0.1;
                    posWS += normalWS * (_OutlineWidth + noise);
                #else
                    posWS += normalWS * _OutlineWidth;
                #endif
                
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // -------------------------------------
        // ����ɫPass (����Ӱ����)
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float4 shadowCoord : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _BaseSmoothness;
                float4 _ShadowColor;
                float _ShadowThreshold;
                float _ShadowSoftness;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vertexInput.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord = GetShadowCoord(vertexInput);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                // ���ռ���
                Light mainLight = GetMainLight(IN.shadowCoord);
                float shadow = smoothstep(
                    _ShadowThreshold - _ShadowSoftness,
                    _ShadowThreshold + _ShadowSoftness,
                    mainLight.shadowAttenuation
                );

                // ��ɫ���
                float3 albedo = lerp(_ShadowColor.rgb, _BaseColor.rgb, shadow);
                
                // ���
                half4 color = half4(albedo, 1);
                color.a = _BaseSmoothness; // ʹ��ƽ���ȿ���͸���ȣ����裩
                return color;
            }
            ENDHLSL
        }

        // ��ӰͶ��Pass�����룩
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    CustomEditor "CustomShaderGUI" // ��ѡ���Զ���༭������
}
