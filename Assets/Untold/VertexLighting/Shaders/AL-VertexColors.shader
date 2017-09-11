Shader "Untold/Ambient Lit + VertexColors" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Pass {
			Tags { "RenderType" = "Opaque" }
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
            fixed4 _MainTex_ST;
			
			struct vertexInput
		    {
		        float4 vertex : POSITION;
		        half4 color : COLOR;
		        half2 texcoord : TEXCOORD;
		    };
			
			struct vertexOutput
			{
		        float4 pos : SV_POSITION;
		        half4 color : COLOR;
		        half2 uv0 : TEXCOORD;
		    };
		    
		    vertexOutput vert(vertexInput i)
		    {
		        vertexOutput o;
		        o.pos = UnityObjectToClipPos(i.vertex);
		        o.uv0 = i.texcoord;
		        o.color = i.color;
		        return o;
		    }

		    fixed4 frag(vertexOutput i) : COLOR
		    {
		    	fixed4 main_color = tex2D(_MainTex, i.uv0) * (i.color * 10) * UNITY_LIGHTMODEL_AMBIENT;
		        return main_color;
		    }
			ENDCG
		}
	} 
}
