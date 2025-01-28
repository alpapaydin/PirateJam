Shader "Custom/SeaSurface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WaterColor ("Water Color", Color) = (0.2, 0.5, 0.8, 0.8)
        _HighlightColor ("Highlight Color", Color) = (0.6, 0.8, 1.0, 0.8)
        _WaveSpeed ("Wave Speed", Range(0.1, 10)) = 1
        _WaveAmplitude ("Wave Amplitude", Range(0.0, 0.1)) = 0.02
        _WaveFrequency ("Wave Frequency", Range(1, 20)) = 10
        _WaveBands ("Wave Bands", Range(1, 10)) = 4
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent-100"
            "IgnoreProjector"="True"
        }
        
        Pass 
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _WaterColor;
            float4 _HighlightColor;
            float _WaveSpeed;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveBands;
            
            v2f vert (appdata v)
            {
                v2f o;
                float wave = sin(_WaveFrequency * v.vertex.x + _Time.y * _WaveSpeed);
                v.vertex.y += wave * _WaveAmplitude;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 continuousUV = i.uv * _WaveFrequency;
                float wave = sin(continuousUV.x + _Time.y * _WaveSpeed);
                wave = floor(wave * _WaveBands) / _WaveBands;
                
                float4 finalColor = lerp(_WaterColor, _HighlightColor, wave * 0.5 + 0.5);
                float gradient = 1 - i.uv.y;
                finalColor = lerp(finalColor, _WaterColor * 0.8, gradient * 0.3);
                fixed4 texColor = tex2D(_MainTex, i.uv);
                return finalColor * texColor;
            }
            ENDCG
        }
    }
}