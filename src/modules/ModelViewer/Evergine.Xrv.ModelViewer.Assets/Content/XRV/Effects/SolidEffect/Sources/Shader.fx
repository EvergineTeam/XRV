[Begin_ResourceLayout]
	
	[Directives:Color BASECOLOR_OFF VERTEXCOLOR TEXTURECOLOR]
	[Directives:Multiview MULTIVIEW_OFF MULTIVIEW_RTI MULTIVIEW_VI]
	[Directives:ColorSpace 		GAMMA_COLORSPACE_OFF 	GAMMA_COLORSPACE]
	
	cbuffer PerDrawCall : register(b0)
	{
		float4x4 	World					: packoffset(c0.x);  [World]	
	};
	
	cbuffer PerCamera : register(b1)
	{
		float3 SunDirection  			: packoffset(c0); 	[SunDirection]
		float  SunIntensity 			: packoffset(c0.w); [SunIntensity]
		int	   EyeCount				    : packoffset(c1.x);  [MultiviewCount]
		float4x4 ViewProj				: packoffset(c2.x); [ViewProjection]
		float4x4 MultiviewViewProj[6]	: packoffset(c6.x);  [MultiviewViewProjection]		
	};

	cbuffer Parameters : register(b2)
	{
		float3 Color			: packoffset(c0.x); [Default(1.0,1.0,1.0)]
		float Alpha				: packoffset(c0.w); [Default(1)]
		float  Ambient			: packoffset(c1.x); [Default(0.15)]
		float AlphaCutOff		: packoffset(c1.y); [Default(0.0)]
	};
	
	Texture2D BaseColorTexture	: register(t0);
	SamplerState BaseColorSampler : register(s0);

[End_ResourceLayout]

[Begin_Pass:Default]
	[Profile 10_0]
	[Entrypoints VS=VS PS=PS]

	struct VS_IN
	{
		float4 Position : POSITION;
		float3 Normal	: NORMAL;
	#if TEXTURECOLOR
		float2 TexCoord : TEXCOORD;
	#endif
	#if VERTEXCOLOR
		float4 VColor	: COLOR;
	#endif
	#if MULTIVIEW_VI	
		uint ViewID : SV_ViewID;
	#elif MULTIVIEW_RTI
		uint InstId : SV_InstanceID;
	#endif
	};

	struct PS_IN
	{
		float4 Position 	: SV_POSITION;
		float3 Normal		: NORMAL;
	#if TEXTURECOLOR
		float2 TexCoord		: TEXCOORD;
	#endif
	#if VERTEXCOLOR
		float4 VColor		: COLOR;
	#endif

	#if MULTIVIEW_RTI
		uint viewId : SV_RenderTargetArrayIndex;
	#endif
	};

#if !GAMMA_COLORSPACE
	float3 GammaToLinear(const float3 color)
	{
		return pow(color, 2.2);
	}
#endif

	PS_IN VS(VS_IN input)
	{
		PS_IN output = (PS_IN)0;		
		
	#if MULTIVIEW_RTI
		const int vid = input.InstId % EyeCount;
		const float4x4 viewProj = MultiviewViewProj[vid];
	
		// Note which view this vertex has been sent to. Used for matrix lookup.
		// Taking the modulo of the instance ID allows geometry instancing to be used
		// along with stereo instanced drawing; in that case, two copies of each 
		// instance would be drawn, one for left and one for right.
	
		output.viewId = vid;
	#elif MULTIVIEW_VI
		const float4x4 viewProj = MultiviewViewProj[input.ViewID];
	#else
		float4x4 viewProj = ViewProj;
	#endif
		
		float4x4 worldViewProj = mul(World, viewProj);

		
		output.Position = mul(input.Position, worldViewProj);		
		output.Normal = normalize(mul(float4(input.Normal, 0), World).xyz);
		#if TEXTURECOLOR
		output.TexCoord = input.TexCoord;
		#endif
		#if VERTEXCOLOR
		output.VColor = input.VColor;
		#endif

		return output;
	}

	float4 PS(PS_IN input) : SV_Target
	{
		float l = saturate(dot(input.Normal, SunDirection));
		l = (1 - Ambient) * l  + Ambient;
		
		float3 color = Color * l * SunIntensity;	
		float alpha = Alpha;		
		
		#if TEXTURECOLOR
			float4 textureColor = BaseColorTexture.Sample(BaseColorSampler, input.TexCoord);
			color *= textureColor.rgb;
			alpha*= textureColor.a;
			
		#elif VERTEXCOLOR
			color *= input.VColor;
		#endif
		
		#if !GAMMA_COLORSPACE
			color = GammaToLinear(color);
		#endif		
				
		clip(alpha - AlphaCutOff);
		
		return float4(color, alpha);
	}

[End_Pass]