[Begin_ResourceLayout]
	
	[Directives:Multiview 		MULTIVIEW_OFF 			MULTIVIEW]
	[Directives:ColorSpace 		GAMMA_COLORSPACE_OFF 	GAMMA_COLORSPACE]
	
	cbuffer Base : register(b0)
	{
		float4x4 	World					: packoffset(c0.x);  [World]
		float4x4	WorldViewProjection		: packoffset(c4.x);  [WorldViewProjection]
		float  		Time					: packoffset(C8.x); [Time]
	};
	
	cbuffer PerCamera : register(b1)
	{
		float3		CameraPosition			: packoffset(c0.x);  [CameraPosition]
		int			EyeCount				: packoffset(c0.w);  [MultiviewCount]
		float4x4	ViewProj[6]				: packoffset(c1.x);  [MultiviewViewProjection]
		float4		StereoCameraPosition[6]	: packoffset(c25.x); [MultiviewPosition]
	};

	cbuffer Parameters : register(b2)
	{
		float3 Color			: packoffset(c0.x); [Default(0,0.05,0.1)]
		float Alpha				: packoffset(c0.w); [Default(1)]
		
		float  FresnelPower		: packoffset(c1.x); [Default(0.1)]
		float3 FresnelColor		: packoffset(c1.y); [Default(1,1,1)]
		
		float RotationTime		: packoffset(C2.x); [Default(1)]
		float PhaseAdjust		: packoffset(c2.y); [Default(1)]
		float PhaseTime		    : packoffset(c2.z); [Default(1.5)]
		float TimeFactor 		: packoffset(c2.w); [Default(1)]
	};

[End_ResourceLayout]

[Begin_Pass:Default]
	[Profile 10_0]
	[Entrypoints VS=VS PS=PS]

	struct VS_IN
	{
		float4 Position : POSITION;
		float3 Normal	: NORMAL;
		uint   InstanceID	: SV_InstanceID;
	};

	struct PS_IN
	{
		float4 Position 	: SV_POSITION;
		float3 CameraVector : POSITION;
		float3 Normal		: NORMAL1;
		float3 NorWS		: NORMAL2;

		uint ViewId : SV_RenderTargetArrayIndex;
	};

#if !GAMMA_COLORSPACE
	float3 GammaToLinear(const float3 color)
	{
		return pow(color, 2.2);
	}
#endif

	float3 rotate(float3 i, float2 cs, float2 ss)
	{
		float3 p = i;
		
		p = float3( p.x,
					(p.y * cs.x) + (p.z * -ss.x),
					(p.y * ss.x) + (p.z * cs.x));
		
		p = float3((p.x * cs.y) + (p.z * -ss.y),
					p.y,
					(p.x * ss.y) + (p.z * cs.y));
		
		/*p = float3((p.x * cs.z) + (p.y * -ss.z),
					(p.x * ss.z) + (p.y * cs.z),
					p.z);*/
			
		return p;
	}
	
	#define PI 3.14159265359
	#define HALF_PI 1.57079632679

	PS_IN VS(VS_IN input)
	{
		PS_IN output = (PS_IN)0;
		
		float t = (Time * TimeFactor);
		
		float p1 = PhaseTime;
		float p2 = PhaseTime * 2;
		float p3 = PhaseTime * 3;
		float p4 = PhaseTime * 4;
		
		float t1 = RotationTime;
		float t2 = p1 + RotationTime;
		float t3 = p2 + RotationTime;
		float t4 = p3 + RotationTime;
		
		float tX = (t - (input.Position.x * PhaseAdjust)) % p4;
		float tY = (t - (input.Position.y * PhaseAdjust)) % p4;
		
		float fX;
		float fY;
		
		if(tX < p1)
		{
			fX = 0;
		}
		else if (tX < p2)
		{
			fX = PI * smoothstep(p1, t2, tX);
		}
		else if (tX < p3)
		{
			fX = PI;
		}
		else
		{
			fX = PI * smoothstep(t4, p3, tX);
		}

		if(tY < p1)
		{
			fY = PI * smoothstep(0, t1, tY);
		}
		else if (tY < p2)
		{
			fY = PI;
		}
		else if (tY < p3)
		{
			fY = PI * (smoothstep(t3, p2, tY));
		}
		else
		{
			fY = 0;
		}

		float2 angles = float2(fX, fY);
		
		float2 cosAngles = cos(angles);
		float2 sinAngles = sin(angles);

		float4 position = float4(rotate(input.Position.xyz, cosAngles, sinAngles), input.Position.w);
		float3 normal = rotate(input.Normal, cosAngles, sinAngles);
		
	#if MULTIVIEW
		int vid = input.InstanceID % EyeCount;

		float3 cameraPositionWS = StereoCameraPosition[vid].xyz;
		float4x4 worldViewProj = mul(World, ViewProj[vid]);
		output.ViewId = vid;
	#else
		float3 cameraPositionWS = CameraPosition;
		float4x4 worldViewProj = WorldViewProjection;
	#endif
		
		output.Position = mul(position, worldViewProj);
		output.CameraVector = cameraPositionWS - mul(position, World).xyz;
		output.Normal = normal;
		output.NorWS = mul(normal, (float3x3)World);

		return output;
	}

	float4 PS(PS_IN input) : SV_Target
	{
		float3 normal = normalize(input.NorWS);
		float3 dir = normalize(input.CameraVector);
		float fresnel1 = dot(dir, normal);
		float fresnel2 = dot(dir, -normal);
		
		float fresnel =  max(fresnel1, fresnel2);		
		fresnel = pow(fresnel, FresnelPower);		
		
		float4 output = float4(lerp(FresnelColor, Color, fresnel), Alpha);
		
		#if !GAMMA_COLORSPACE
			output.rgb = GammaToLinear(output.rgb);
		#endif
		
		output.rgb *= output.a;
		
		return output;
	}

[End_Pass]