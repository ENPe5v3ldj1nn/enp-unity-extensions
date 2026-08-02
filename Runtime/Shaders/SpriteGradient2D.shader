Shader "Sprite2D/SpriteGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _GradientTex ("Gradient Ramp (256x1)", 2D) = "white" {}
        _GradientAngle ("Gradient Angle", Range(-180, 180)) = 0
        _GradientOffset ("Gradient Offset", Vector) = (0, 0, 0, 0)
        _GradientIgnoreRatio ("Gradient Ignore Ratio", Float) = 1
        [PerRendererData] _SpriteBoundsSize ("Sprite Bounds Size", Vector) = (1, 1, 0, 0)
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _GradientTex;
            float _GradientAngle;
            float4 _GradientOffset;
            float _GradientIgnoreRatio;
            float4 _SpriteBoundsSize;

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
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 local : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.texcoord;
                o.local = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);

                float2 halfSize = max(_SpriteBoundsSize.xy, float2(1e-5, 1e-5)) * 0.5;

                float rad = radians(_GradientAngle);
                float2 dir = float2(cos(rad), sin(rad));
                if (_GradientIgnoreRatio < 0.5)
                {
                    float ratio = halfSize.y / halfSize.x;
                    dir = normalize(float2(dir.x * ratio, dir.y));
                }

                float2 p = i.local.xy - _GradientOffset.xy;

                float denom = halfSize.x * abs(dir.x) + halfSize.y * abs(dir.y);
                denom = max(denom, 1e-5);

                float t = saturate((dot(p, dir) / denom) * 0.5 + 0.5);
                fixed4 gradColor = tex2D(_GradientTex, float2(t, 0.5));

                fixed4 res = texColor * i.color * gradColor;
                return res;
            }
            ENDCG
        }
    }
}
