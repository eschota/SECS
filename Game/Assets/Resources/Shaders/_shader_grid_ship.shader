Shader "Unlit/_shader_grid_ship"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0, 1, 0, 0.5)
        _GridAlpha ("Grid Alpha", Range(0.0, 1.0)) = 0.5
        _LineWidth ("Line Width", Range(0.01, 0.5)) = 0.1
        _GridSize ("Grid Size", Float) = 1.0
        _BlurAmount ("Blur Amount", Range(0.0, 0.1)) = 0.02
        _FadeColor ("Fade Color", Color) = (0, 0, 0, 0)
        _FadeRadius ("Fade Radius", Range(0.1, 50.0)) = 5.0
        _FadeCenter ("Fade Center", Vector) = (0, 0, 0, 0)
        _UseMouseFade ("Use Mouse Fade", Float) = 0.0
        _FadeCurve ("Fade Curve", 2D) = "white" {}
        _UseFadeCurve ("Use Fade Curve", Float) = 0.0
        _PulseColor ("Pulse Color", Range(0.0, 1.0)) = 0.0
        _PulseRadius ("Pulse Radius", Range(0.0, 1.0)) = 0.0
        _PulseColorSpeed ("Pulse Color Speed", Range(0.1, 10.0)) = 1.0
        _PulseRadiusSpeed ("Pulse Radius Speed", Range(0.1, 10.0)) = 1.0
        _PulseColorAlt ("Pulse Color Alternative", Color) = (1, 0, 0, 0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
            };

            float4 _GridColor;
            float _GridAlpha;
            float _LineWidth;
            float _GridSize;
            float _BlurAmount;
            float4 _FadeColor;
            float _FadeRadius;
            float4 _FadeCenter;
            float _UseMouseFade;
            sampler2D _FadeCurve;
            float _UseFadeCurve;
            float _PulseColor;
            float _PulseRadius;
            float _PulseColorSpeed;
            float _PulseRadiusSpeed;
            float4 _PulseColorAlt;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            // Функция для создания мягких линий сетки
            float smoothGridLine(float2 grid, float lineWidth, float blurAmount)
            {
                float lineX = smoothstep(lineWidth + blurAmount, lineWidth, grid.x) + 
                             smoothstep(lineWidth + blurAmount, lineWidth, 1.0 - grid.x);
                float lineZ = smoothstep(lineWidth + blurAmount, lineWidth, grid.y) + 
                             smoothstep(lineWidth + blurAmount, lineWidth, 1.0 - grid.y);
                return max(lineX, lineZ);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Используем UV координаты для создания сетки
                float2 uv = i.uv;
                
                // Масштабируем UV координаты для создания сетки нужного размера
                float2 scaledUV = uv * _GridSize;
                
                // Вычисляем остаток от деления на размер сетки
                float2 grid = fmod(scaledUV, 1.0);
                
                // Создаем мягкие линии сетки
                float isLine = smoothGridLine(grid, _LineWidth, _BlurAmount);
                
                // Базовый цвет сетки - полностью прозрачный фон
                fixed4 gridColor = _GridColor;
                gridColor.a *= _GridAlpha; // Применяем дополнительную прозрачность
                fixed4 col = lerp(fixed4(0, 0, 0, 0), gridColor, isLine);
                
                // Вычисляем затухание
                float2 worldPos = i.worldPos.xz;
                float2 fadeCenter = _FadeCenter.xz;
                
                // Вычисляем пульсацию радиуса
                float radiusPulseTime = _Time.y * _PulseRadiusSpeed;
                float radiusPulseFactor = (sin(radiusPulseTime) + 1.0) * 0.5; // 0-1
                
                // Применяем пульсацию к радиусу
                float currentRadius = _FadeRadius * (1.0 + _PulseRadius * (radiusPulseFactor - 0.5) * 2.0);
                
                // Вычисляем расстояние до центра затухания
                float distanceToCenter = length(worldPos - fadeCenter);
                
                // Нормализуем расстояние (0-1) с учетом пульсирующего радиуса
                float normalizedDistance = saturate(distanceToCenter / currentRadius);
                
                // Создаем затухание
                float fadeFactor;
                if (_UseFadeCurve > 0.5)
                {
                    // Используем кривую затухания
                    fadeFactor = tex2D(_FadeCurve, float2(normalizedDistance, 0.5)).r;
                }
                else
                {
                    // Используем линейное затухание
                    fadeFactor = 1.0 - smoothstep(0.0, _FadeRadius, distanceToCenter);
                }
                
                // Применяем затухание только если включено
                if (_UseMouseFade > 0.5)
                {
                    // Применяем пульсацию к цвету затухания
                    float4 currentFadeColor = _FadeColor;
                    if (_PulseColor > 0.0)
                    {
                        // Вычисляем пульсацию цвета
                        float colorPulseTime = _Time.y * _PulseColorSpeed;
                        float colorPulseFactor = (sin(colorPulseTime) + 1.0) * 0.5; // 0-1
                        
                        // Лерп между основным цветом и альтернативным цветом
                        currentFadeColor = lerp(_FadeColor, _PulseColorAlt, colorPulseFactor);
                    }
                    
                    col = lerp(currentFadeColor, col, fadeFactor);
                }
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
