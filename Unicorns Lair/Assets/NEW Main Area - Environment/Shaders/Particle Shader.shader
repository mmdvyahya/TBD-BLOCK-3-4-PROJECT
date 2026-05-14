Shader "WILDLANDS/Particle Shader"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (1,1,1,1)

        _LightContribution("Light Contribution", Range(0, 1)) = 1.0

        // Emission
        [Toggle(_EMISSION)] _EmissionEnabled("Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)

        // Color modes
        [Enum(Multiply, 0, Additive, 1, Subtract, 2, Overlay, 3)] _ColorMode("Color Mode", Float) = 0

        // Surface
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Render Face", Float) = 0

        [HideInInspector] _SrcBlend("__src", Float) = 5.0
        [HideInInspector] _DstBlend("__dst", Float) = 10.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull [_Cull]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma shader_feature_local _EMISSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float  fogCoord : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EmissionColor;
                float4 _BaseMap_ST;
                float  _LightContribution;
                float  _ColorMode;
                float  _SrcBlend;
                float  _DstBlend;
                float  _Cull;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // Overlay blend
            float3 Overlay(float3 base, float3 blend)
            {
                return lerp(
                    2.0 * base * blend,
                    1.0 - 2.0 * (1.0 - base) * (1.0 - blend),
                    step(0.5, blend));
            }

            half4 frag(Varyings i) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

                // Apply color mode
                float4 blended;
                if (_ColorMode < 0.5)
                {
                    // Multiply
                    blended = _Color * i.color;
                }
                else if (_ColorMode < 1.5)
                {
                    // Additive
                    blended = float4(_Color.rgb + i.color.rgb, _Color.a * i.color.a);
                }
                else if (_ColorMode < 2.5)
                {
                    // Subtract
                    blended = float4(saturate(_Color.rgb - i.color.rgb), _Color.a * i.color.a);
                }
                else
                {
                    // Overlay
                    blended = float4(Overlay(_Color.rgb, i.color.rgb), _Color.a * i.color.a);
                }

                float4 col = tex * blended;

                // Directional light color and intensity, no NdotL!! NdotL makes light change based on camera angle :/
                Light mainLight = GetMainLight();
                col.rgb *= lerp(1.0, mainLight.color, _LightContribution);

                // Emission
                #if defined(_EMISSION)
                    col.rgb += _EmissionColor.rgb;
                #endif

                col.rgb = MixFog(col.rgb, i.fogCoord);

                return col;
            }

            ENDHLSL
        }
    }
}