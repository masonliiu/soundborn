Shader "UI/PixelateDissolve"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _PixelAmount("Pixel Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _PixelAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                fixed4 baseCol = tex2D(_MainTex, uv);

                float t = saturate(_PixelAmount);

                float minBlocks = 20.0;  
                float maxBlocks = 200.0; 
                float blocks = lerp(minBlocks, maxBlocks, t);

                float2 pixUV = floor(uv * blocks) / blocks;
                fixed4 pixCol = tex2D(_MainTex, pixUV);

                fixed4 col;
                col.rgb = lerp(baseCol.rgb, pixCol.rgb, t);

                col.a = baseCol.a * (1.0 - t);

                return col;
            }
            ENDCG
        }
    }
}