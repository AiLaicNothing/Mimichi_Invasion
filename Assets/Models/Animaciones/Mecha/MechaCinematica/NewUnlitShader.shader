Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PortalPos ("Portal Position", Vector) = (0,0,0,0)
        _PortalNormal ("Portal Normal", Vector) = (0,0,1,0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="TransparentCutout" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _PortalPos;
            float4 _PortalNormal;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float side = dot(i.worldPos - _PortalPos.xyz, _PortalNormal.xyz);

                clip(side);

                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
    }
}
