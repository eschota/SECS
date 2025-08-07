Shader "MyShaders/CellLitURP"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        
        _WireframeToggle ("Wireframe Amount", Range(0.0, 1.0)) = 0.0
        [HDR] _WireframeColor ("Wireframe Color", Color) = (0, 0, 0, 1)
        _WireframeThickness ("Wireframe Thickness", Range(0, 10)) = 1.0
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma require geometry

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float3 barycentric  : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _WireframeToggle;
                float4 _WireframeColor;
                float _WireframeThickness;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.barycentric = float3(0.0, 0.0, 0.0);
                return output;
            }

            [maxvertexcount(3)]
            void geom(triangle Varyings IN[3], inout TriangleStream<Varyings> triStream)
            {
                Varyings o;
                o = IN[0]; o.barycentric = float3(1, 0, 0); triStream.Append(o);
                o = IN[1]; o.barycentric = float3(0, 1, 0); triStream.Append(o);
                o = IN[2]; o.barycentric = float3(0, 0, 1); triStream.Append(o);
            }

            half4 frag(Varyings input) : SV_Target
            {
                // --- Wireframe Calculation ---
                float min_dist = min(input.barycentric.x, min(input.barycentric.y, input.barycentric.z));
                float edge_fwidth = fwidth(min_dist) * _WireframeThickness;
                float line_factor = 1.0 - smoothstep(0.0, edge_fwidth, min_dist);
                float wireframe_amount = line_factor * _WireframeToggle;
                
                // --- Surface Data ---
                SurfaceData surfaceData;
                surfaceData.albedo = _BaseColor.rgb;
                surfaceData.metallic = 0.0;
                surfaceData.specular = 0.5;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = float3(0,0,1);
                surfaceData.emission = _EmissionColor.rgb + (_WireframeColor.rgb * wireframe_amount);
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = _BaseColor.a;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                // --- Input Data ---
                InputData inputData;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = 0; 
                inputData.vertexLighting = float3(0,0,0);
                inputData.bakedGI = SampleSH(inputData.normalWS);

                // --- Lighting Calculation ---
                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                return finalColor;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                uint instanceID     : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                float3 lightDirection = GetMainLight().direction;
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
