Shader "MyShaders/CellTransparentURP"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 0.5)
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        
        _WireframeToggle ("Wireframe Amount", Range(0.0, 1.0)) = 0.0
        [HDR] _WireframeColor ("Wireframe Color", Color) = (0, 0, 0, 1)
        _WireframeThickness ("Wireframe Thickness", Range(0, 10)) = 1.0
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off // Important for seeing both sides

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

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

            // Properties
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

            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                // Flip normal for backfaces
                float3 normalWS = normalize(input.normalWS) * (isFrontFace ? 1.0 : -1.0);

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
                // Add wireframe to emission
                surfaceData.emission = _EmissionColor.rgb + (_WireframeColor.rgb * wireframe_amount);
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = lerp(_BaseColor.a, _WireframeColor.a, wireframe_amount);
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                // --- Input Data ---
                InputData inputData;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = 0; 
                inputData.vertexLighting = float3(0,0,0);
                inputData.bakedGI = SampleSH(normalWS);

                // --- Lighting Calculation ---
                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                return finalColor;
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
