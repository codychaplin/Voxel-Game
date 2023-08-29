Shader "Universal Render Pipeline/TransparentTextureMap"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalRenderPipeline" }
        ZWrite ON
        AlphaToMask On
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            Name "MainPass"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                if (v.normal.y > 0.1)
                    o.color = float4(0, 0, 0, 0);
                else
                    o.color = float4(0, 0, 0, 0.6);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col = lerp(col, float4(0, 0, 0, 1), i.color.a);
                return col;
            }
            ENDCG
        }
    }
}
