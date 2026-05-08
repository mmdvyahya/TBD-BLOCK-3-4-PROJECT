Shader "WILDLANDS/Water Shader"
{
    Properties
    {
        // Color
        _ColorShallow("Color Shallow", Color) = (0.28, 0.47, 0.68, 1.0)
        _ColorDeep("Color Deep", Color) = (0.55, 0.72, 0.87, 1.0)
        _FadeDistance("Shallow Depth", Float) = 0.5
        _WaterDepth("Gradient Size", Float) = 5.0
        _WaterClearness("Transparency", Range(0, 1)) = 0.3
        _LightContribution("Light Color Contribution", Range(0, 1)) = 0.0
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.35

        // Crest
        _CrestColor("Crest Color", Color) = (1, 1, 1, 0.9)
        _CrestSize("Crest Size", Range(0, 1)) = 0.1
        _CrestSharpness("Crest Sharpness", Range(0, 1)) = 0.1

        // Waves
        _WaveSpeed("Wave Speed", Float) = 0.5
        _WaveAmplitude("Wave Amplitude", Float) = 0.25
        _WaveFrequency("Wave Frequency", Float) = 1.0
        _WaveDirection("Wave Direction", Range(-1, 1)) = 0.0
        _WaveNoise("Wave Noise", Range(0, 2)) = 0.25

        // Foam 
        [ToggleUI] _FoamEnabled("Foam Enabled", Float) = 1
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamDepth("Shore Depth", Float) = 0.5
        _FoamAmount("Amount", Range(0, 3)) = 0.25
        _FoamScale("Scale", Range(0, 3)) = 1.0
        _FoamSharpness("Sharpness", Range(0, 1)) = 0.5
        _FoamSpeed("Foam Speed", Float) = 0.1

        // Refraction
        _RefractionFrequency("Refraction Frequency", Float) = 35
        _RefractionAmplitude("Refraction Amplitude", Range(0, 0.1)) = 0.01
        _RefractionSpeed("Refraction Speed", Float) = 0.1
        _RefractionScale("Refraction Scale", Float) = 1.0

        // Render settings 
        [IntRange] _QueueOffset("Sorting Priority", Range(-50, 50)) = 0
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

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            // Structs
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float waveHeight : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            // Cbuffer
            CBUFFER_START(UnityPerMaterial)
                half4 _ColorShallow;
                half4 _ColorDeep;
                float _FadeDistance;
                float _WaterDepth;
                half _WaterClearness;
                half _LightContribution;
                half _ShadowStrength;

                half4 _CrestColor;
                half _CrestSize;
                half _CrestSharpness;

                half _WaveSpeed;
                half _WaveAmplitude;
                half _WaveFrequency;
                half _WaveDirection;
                half _WaveNoise;

                float _FoamEnabled;
                half4 _FoamColor;
                half _FoamDepth;
                half _FoamAmount;
                half _FoamScale;
                half _FoamSharpness;
                half _FoamSpeed;

                half _RefractionFrequency;
                half _RefractionAmplitude;
                half _RefractionSpeed;
                half _RefractionScale;

                float _QueueOffset;
            CBUFFER_END

            // Gradient noise (Maybe replace with a texture noise)
            float2 GradientNoise_Dir(float2 p)
            {
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float GradientNoise(float2 uv, float scale)
            {
                float2 p  = uv * scale;
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(GradientNoise_Dir(ip), fp);
                float d01 = dot(GradientNoise_Dir(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(GradientNoise_Dir(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(GradientNoise_Dir(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
            }

            // Wave height using sine waves and noise
            float SineWave(float3 pos, float offset)
            {
                return sin(offset + _Time.z * _WaveSpeed + (pos.x * sin(offset + _WaveDirection * PI) + pos.z * cos(offset + _WaveDirection * PI)) * _WaveFrequency);
            }

            float WaveHeight(float2 uv, float3 posWS)
            {
                float2 noiseUV = posWS.xz * _WaveFrequency;
                float noise01 = GradientNoise(noiseUV, 1.0);
                float noise = (noise01 * 2.0 - 1.0) * _WaveNoise;

                float s = SineWave(posWS, noise);
                s *= SineWave(posWS, HALF_PI + noise);
                return s;
            }

            // Depth fade
            float DepthFade(float2 screenUV, float4 screenPos)
            {
                float isOrtho = unity_OrthoParams.w;
                float isPersp = 1.0 - isOrtho;

                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = lerp(_ProjectionParams.z, _ProjectionParams.y, rawDepth) * isOrtho + LinearEyeDepth(rawDepth, _ZBufferParams) * isPersp;
                float surfaceDepth = lerp(_ProjectionParams.z, _ProjectionParams.y, screenPos.z / screenPos.w) * isOrtho + screenPos.w * isPersp;

                float waterDepth = sceneDepth - surfaceDepth;
                return saturate((waterDepth - _FadeDistance) / _WaterDepth);
            }

            // Vertex shader
            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);

                // Vertex wave displacement
                float waveH = WaveHeight(v.uv, posWS);
                o.waveHeight = waveH;
                posWS.y += waveH * _WaveAmplitude;

                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.uv = v.uv;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);

                return o;
            }

            // Fragment shader
            half4 frag(Varyings i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // Refraction
                float2 refrUV = i.uv * _RefractionFrequency + _Time.zz * _RefractionSpeed;
                float  refrNoise = GradientNoise(refrUV, _RefractionScale) * 2.0 - 1.0;
                float  depthOrig = DepthFade(screenUV, i.screenPos);
                float2 displUV = screenUV + refrNoise * _RefractionAmplitude * depthOrig;
                float  depthFade = DepthFade(displUV, i.screenPos);

                // Fallback if UV is out of bounds
                if (depthFade <= 0.0)
                {
                    displUV = screenUV;
                    depthFade = DepthFade(screenUV, i.screenPos);
                }

                half3 sceneColor = SampleSceneColor(displUV);
                half3 c = sceneColor;

                // Water depth color
                half4 depthColor = lerp(_ColorShallow, _ColorDeep, depthFade);
                c = lerp(depthColor.rgb, c, _WaterClearness * depthColor.a);

                // Crest highlight
                {
                    half cInv = 1.0h - _CrestSize;
                    half crestMask = smoothstep(cInv, saturate(cInv + (1.0h - _CrestSharpness)), i.waveHeight);
                    c = lerp(c, _CrestColor.rgb, crestMask * _CrestColor.a);
                }

                // Foam
                if (_FoamEnabled > 0.5)
                {
                    float2 foamUV = i.uv * 100.0 + _Time.zz * _FoamSpeed;
                    float  foamNoise = GradientNoise(foamUV, _FoamScale);

                    float foamBlur = 1.0 - _FoamSharpness + 1e-6;
                    float shoreFade = saturate(depthFade / _FoamDepth);

                    float hardEnd = 0.1;
                    float softEnd = hardEnd + foamBlur * 0.3;
                    float foamShore = saturate(
                        smoothstep(softEnd, hardEnd, shoreFade) +
                        smoothstep(1, softEnd, shoreFade) *
                        smoothstep(0.5 - foamBlur * 0.5, 0.5 + foamBlur * 0.5, foamNoise));

                    float foamSurface = smoothstep(0.5 - foamBlur * 0.5, 0.5 + foamBlur * 0.5, smoothstep(foamNoise, foamNoise + foamBlur, _FoamAmount));

                    float foam = saturate(foamShore + foamSurface);
                    c = lerp(c, _FoamColor.rgb, foam * _FoamColor.a);
                }

                // Shadows
                #ifndef _MAIN_LIGHT_SHADOWS
                #define _MAIN_LIGHT_SHADOWS
                #endif
                #if defined(_MAIN_LIGHT_SHADOWS)
                {
                    VertexPositionInputs vertInput = (VertexPositionInputs)0;
                    vertInput.positionWS = i.positionWS;
                    float4 shadowCoord = GetShadowCoord(vertInput);
                    half shadowAtten = MainLightRealtimeShadow(shadowCoord);
                    c = lerp(c, c * _ColorShallow.rgb, _ShadowStrength * (1.0h - shadowAtten));
                }
                #endif

                // Light contribution
                c *= lerp(half3(1, 1, 1), _MainLightColor.rgb, _LightContribution);

                // Fog
                c = MixFog(c, i.fogFactor);

                return half4(c, 1.0);
            }

            ENDHLSL
        }
    }
}
