Shader "UI/ENP/RoundedRectProceduralVignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TopColor ("Top Color", Color) = (1,1,1,0)
        _BottomColor ("Bottom Color", Color) = (1,1,1,0)
        _Intensity ("Intensity", Range(0,1)) = 0
        _Thickness ("Thickness", Float) = 52
        _Softness ("Softness", Range(0,1)) = 0.58
        _Roundness ("Roundness", Float) = 24
        _CenterClear ("Center Clear", Range(0,1)) = 0.74
        _WarpAmount ("Warp Amount", Float) = 5
        _WarpScale ("Warp Scale", Float) = 0.024
        _WarpSpeed ("Warp Speed", Float) = 0.45
        _NoiseAmount ("Noise Amount", Float) = 3.5
        _NoiseScale ("Noise Scale", Float) = 0.055
        _NoiseSpeed ("Noise Speed", Float) = 0.7
        _TopEdgeStrength ("Top Edge Strength", Float) = 1
        _BottomEdgeStrength ("Bottom Edge Strength", Float) = 1
        _LeftEdgeStrength ("Left Edge Strength", Float) = 1
        _RightEdgeStrength ("Right Edge Strength", Float) = 1
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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            Name "RoundedRectProceduralVignette"

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
            float _Thickness;
            float _Softness;
            float _Roundness;
            float _CenterClear;
            float _WarpAmount;
            float _WarpScale;
            float _WarpSpeed;
            float _NoiseAmount;
            float _NoiseScale;
            float _NoiseSpeed;
            float _TopEdgeStrength;
            float _BottomEdgeStrength;
            float _LeftEdgeStrength;
            float _RightEdgeStrength;
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
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float WaveWarp(float2 p, float scale, float timeValue)
            {
                float a = sin((p.x + p.y * 0.27) * scale + timeValue);
                float b = sin((p.y - p.x * 0.19) * (scale * 1.73) - timeValue * 1.31);
                float c = sin((p.x * 0.11 - p.y) * (scale * 0.67) + timeValue * 0.73);
                return (a + b * 0.6 + c * 0.35) / 1.95;
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

                float halfMin = min(halfSize.x, halfSize.y);
                float radius = min(max(_Roundness, 0.0), max(halfMin - 0.0001, 0.0));
                float baseSd = SdRoundedBox(p, halfSize, radius);

                float thicknessLimit = halfMin * saturate(1.0 - _CenterClear);
                float thickness = min(max(_Thickness, 0.0), thicknessLimit);

                float baseBorderDistance = max(-baseSd, 0.0);
                float edgeRange = max(thickness * 1.5, 1.0);
                float nearEdgeMask = thickness > 0.0001 ? saturate(1.0 - (baseBorderDistance / edgeRange)) : 0.0;

                float distLeft = p.x + halfSize.x;
                float distRight = halfSize.x - p.x;
                float distBottom = p.y + halfSize.y;
                float distTop = halfSize.y - p.y;

                float proxLeft = saturate(1.0 - (distLeft / edgeRange));
                float proxRight = saturate(1.0 - (distRight / edgeRange));
                float proxBottom = saturate(1.0 - (distBottom / edgeRange));
                float proxTop = saturate(1.0 - (distTop / edgeRange));

                float edgeStrength = max(
                    max(proxTop * _TopEdgeStrength, proxBottom * _BottomEdgeStrength),
                    max(proxLeft * _LeftEdgeStrength, proxRight * _RightEdgeStrength));

                float timeValue = _Time.y;
                float wave = WaveWarp(p, max(_WarpScale, 0.0001), timeValue * _WarpSpeed);
                float noise = ValueNoise(p * max(_NoiseScale, 0.0001) + float2(timeValue * _NoiseSpeed, -timeValue * _NoiseSpeed * 0.73)) * 2.0 - 1.0;
                float warp = (wave * _WarpAmount + noise * _NoiseAmount) * nearEdgeMask * edgeStrength;

                float sd = baseSd - warp;
                float aa = max(fwidth(sd), 0.75);
                float insideMask = saturate((-sd + aa) / aa);

                float borderDistance = max(-sd, 0.0);
                float edge01 = thickness > 0.0001 ? saturate(1.0 - (borderDistance / thickness)) : 0.0;
                float edgePower = lerp(2.6, 0.9, saturate(_Softness));
                float bandMask = pow(edge01, edgePower);

                float gradientT = saturate((p.y + halfSize.y) / rectSize.y);
                fixed4 gradient = lerp(_BottomColor, _TopColor, gradientT);

                float mask = saturate(_Intensity) * insideMask * bandMask;

                fixed4 col = gradient * i.color * _Color;
                col.rgb *= mask;
                col.a *= mask;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
