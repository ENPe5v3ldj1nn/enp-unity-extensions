Shader "UI/ENP/RoundedRectInnerFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _TopColor ("Top Color", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0,1)) = 0
        _Coverage ("Coverage", Range(0,1)) = 0.45
        _Contrast ("Contrast", Range(0,1)) = 0.45
        _Softness ("Softness", Range(0,1)) = 0.65
        _NoiseScale ("Noise Scale", Float) = 4
        _WarpAmount ("Warp Amount", Range(0,1)) = 0.2
        _WarpSpeed ("Warp Speed", Float) = 0.35
        _CenterProtection ("Center Protection", Range(0,1)) = 0.35
        _EdgeBoost ("Edge Boost", Range(0,1)) = 0.25
        _Roundness ("Roundness", Float) = 24
        _AnimationTime ("Animation Time", Float) = 0
        _RectSize ("Rect Size", Vector) = (100,100,0,0)
        _RectCenter ("Rect Center", Vector) = (0,0,0,0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "RoundedRectInnerFog"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TopColor;
            fixed4 _BottomColor;
            float _Intensity;
            float _Coverage;
            float _Contrast;
            float _Softness;
            float _NoiseScale;
            float _WarpAmount;
            float _WarpSpeed;
            float _CenterProtection;
            float _EdgeBoost;
            float _Roundness;
            float _AnimationTime;
            float4 _RectSize;
            float4 _RectCenter;
            float4 _ClipRect;

            float SdRoundedBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i + float2(0.0, 0.0));
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                float ab = lerp(a, b, f.x);
                float cd = lerp(c, d, f.x);
                return lerp(ab, cd, f.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                value += ValueNoise(p) * amplitude;
                p = p * 2.03 + 17.13;
                amplitude *= 0.5;
                value += ValueNoise(p) * amplitude;
                p = p * 2.01 + 9.71;
                amplitude *= 0.5;
                value += ValueNoise(p) * amplitude;
                return value;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.localPos = v.vertex.xy;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 rectSize = max(_RectSize.xy, float2(1.0, 1.0));
                float2 halfSize = rectSize * 0.5;
                float2 p = i.localPos - _RectCenter.xy;
                float2 uv = saturate((p + halfSize) / rectSize);

                float halfMin = min(halfSize.x, halfSize.y);
                float radius = min(max(_Roundness, 0.0), max(halfMin - 0.0001, 0.0));
                float sd = SdRoundedBox(p, halfSize, radius);
                float aa = max(fwidth(sd), 0.75);
                float insideMask = saturate((-sd + aa) / aa);

                float2 drift = float2(_AnimationTime * _WarpSpeed * 0.09, -_AnimationTime * _WarpSpeed * 0.07);
                float2 noiseUv = uv * max(_NoiseScale, 0.001) + drift;
                float2 warp;
                warp.x = Fbm(noiseUv * 1.31 + 11.7) * 2.0 - 1.0;
                warp.y = Fbm(noiseUv * 1.17 + 37.1) * 2.0 - 1.0;

                float2 q = noiseUv + warp * (_WarpAmount * 1.35);
                float primary = Fbm(q);
                float secondary = Fbm(q * 1.93 + 23.4);
                float noiseValue = lerp(primary, secondary, 0.35);

                float threshold = lerp(0.82, 0.18, saturate(_Coverage));
                float width = lerp(0.32, 0.05, saturate(_Contrast));
                float cloud = smoothstep(threshold - width, threshold + width, noiseValue);
                float density = pow(cloud, lerp(2.1, 0.75, saturate(_Softness)));

                float2 normalizedPos = abs(p) / max(halfSize, float2(1.0, 1.0));
                float centerMetric = saturate(max(normalizedPos.x, normalizedPos.y));
                float protectStart = lerp(0.0, 0.78, saturate(_CenterProtection));
                float protectedMask = saturate((centerMetric - protectStart) / max(1.0 - protectStart, 0.0001));
                float centerMask = lerp(1.0, protectedMask, saturate(_CenterProtection));

                float distanceInside = max(-sd, 0.0);
                float edgeMask = 1.0 - saturate(distanceInside / max(halfMin * 0.72, 0.0001));
                float edgeBoost = lerp(1.0, lerp(0.55, 1.0, edgeMask), saturate(_EdgeBoost));

                float gradientT = saturate(uv.y);
                fixed4 gradient = lerp(_BottomColor, _TopColor, gradientT);

                float alpha = saturate(_Intensity) * insideMask * density * centerMask * edgeBoost;
