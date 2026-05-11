Shader "WILDLANDS/Base Shader"
{
    Properties
    {
        _Base_Color("Base Color", Color) = (1,1,1,1)

        [KeywordEnum(NONE, SINGLE)] _CEL_MODE("Cel Mode", Float) = 0
        
        // Single cel layer
        _Color_Shaded_1("Color Shaded 1", Color) = (0,0,0,1)
        _Self_Shading_Size_1("Self Shading Size 1", Range(0,1)) = 0.5
        _Edge_Size_1("Edge Size 1", Range(0,0.5)) = 0.05
        _Localized_Shading_1("Localized Shading 1", Range(0,1)) = 1
        
        // Extra cel layer
        [ToggleUI] _USE_EXTRA_CEL_SHADING("Use Extra Cel", Float) = 0
        _Color_Shaded_2("Color Shaded 2", Color) = (0,0,0,1)
        _Self_Shading_Size_2("Self Shading Size 2", Range(0,1)) = 0.6
        _Edge_Size_2("Edge Size 2", Range(0,0.5)) = 0.05
        _Localized_Shading_2("Localized Shading 2", Range(0,1)) = 1

        // Light contribution
        _Main_Light_Color_Contribution("Light Contribution", Range(0,1)) = 1

        // Texture map
        _Albedo("Albedo", 2D) = "white" {}
        [HideInInspector] _Albedo_ST("Tiling Offset", Vector) = (1,1,0,0)

        // Render settings
        [Enum(Opaque, 0, Transparent, 1)] _Surface("Surface Type", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Render Faces", Float) = 2
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Threshold("Threshold", Range(0, 1)) = 0.5
        [IntRange] _QueueOffset("Sorting Priority", Range(-50, 50)) = 0
    }
    
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma shader_feature_local _CEL_MODE_NONE _CEL_MODE_SINGLE
            #pragma shader_feature _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogCoord : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Base_Color;
                float4 _Color_Shaded_1;
                float4 _Color_Shaded_2;

                float _Self_Shading_Size_1;
                float _Self_Shading_Size_2;

                float _Edge_Size_1;
                float _Edge_Size_2;

                float _Localized_Shading_1;
                float _Localized_Shading_2;

                float _USE_EXTRA_CEL_SHADING;
                float _Main_Light_Color_Contribution;

                float4 _Albedo_ST;

                float _Surface;
                float _Cull;
                float _AlphaClip;
                float _Threshold;
                float _QueueOffset;
            CBUFFER_END

            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);

            Varyings vert (Attributes v)
            {
                Varyings o;

                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

                o.uv = TRANSFORM_TEX(v.uv, _Albedo);

                o.fogCoord = ComputeFogFactor(o.positionCS.z);

                return o;
            }

            float CelLayer(float ndotl, float size, float edge, float localized)
            {
                float v = saturate(ndotl - size);
                float smooth = smoothstep(0, edge, v);
                return lerp(v, smooth, localized);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 normal = normalize(i.normalWS);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);

                float ndotl = saturate(dot(normal, lightDir));
                ndotl = ndotl * 0.5 + 0.5;

                float3 baseCol;

                // Cel mode switching
                #if defined(_CEL_MODE_NONE)
                    baseCol = _Base_Color.rgb;
                #else
                    float layer1 = CelLayer(ndotl, _Self_Shading_Size_1, _Edge_Size_1, _Localized_Shading_1);
                    baseCol = lerp(_Color_Shaded_1.rgb, _Base_Color.rgb, layer1);
                #endif

                // Extra layer
                if (_USE_EXTRA_CEL_SHADING > 0.5)
                {
                    float layer2 = CelLayer(ndotl, _Self_Shading_Size_2, _Edge_Size_2, _Localized_Shading_2);
                    baseCol = lerp(_Color_Shaded_2.rgb, baseCol, layer2);
                }

                // Light contribution
                float3 litCol = baseCol * mainLight.color;
                baseCol = lerp(baseCol, litCol, _Main_Light_Color_Contribution);

                // Texture 
                float4 tex = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, i.uv);
                baseCol *= tex.rgb;

                #ifdef _ALPHATEST_ON
                    clip(tex.a - _Threshold);
                #endif

                // Mix with fog
                baseCol = MixFog(baseCol, i.fogCoord);

                return float4(baseCol, 1);
            }

            ENDHLSL
        }

        // Used for generating depth texture
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

        // Used for water foam edges and other effects that require normals
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo_ST;
                float  _Threshold;
            CBUFFER_END

            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            // Allows alpha clipping to work with water
            half4 frag(Varyings i) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float alpha = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, i.uv).a;
                    clip(alpha - _Threshold);
                #endif
                return float4(normalize(i.normalWS) * 0.5 + 0.5, 0);
            }

            ENDHLSL
        }
    }
}