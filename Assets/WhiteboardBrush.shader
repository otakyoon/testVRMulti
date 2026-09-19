Shader "Unlit/WhiteboardBrush"
{
    Properties
    {
        _MainTex ("Brush", 2D) = "white" {}
        _Color   ("Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 b = tex2D(_MainTex, i.uv);
                return fixed4(_Color.rgb, b.a * _Color.a);
            }
            ENDCG
        }
    }
}
