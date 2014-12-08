//float4x4 World;
//float4x4 View;
//float4x4 Projection;
//
//// TODO: add effect parameters here.
//
//struct VertexShaderInput
//{
//    float4 Position : POSITION0;
//
//    // TODO: add input channels such as texture
//    // coordinates and vertex colors here.
//};
//
//struct VertexShaderOutput
//{
//    float4 Position : POSITION0;
//
//    // TODO: add vertex shader outputs such as colors and texture
//    // coordinates here. These values will automatically be interpolated
//    // over the triangle, and provided as input to your pixel shader.
//};
//
//VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
//{
//    VertexShaderOutput output;
//
//    float4 worldPosition = mul(input.Position, World);
//    float4 viewPosition = mul(worldPosition, View);
//    output.Position = mul(viewPosition, Projection);
//
//    // TODO: add your vertex shader code here.
//
//    return output;
//}
//
//float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
//{
//    // TODO: add your pixel shader code here.
//
//    return float4(1, 0, 0, 1);
//}
//
//technique Technique1
//{
//    pass Pass1
//    {
//        // TODO: set renderstates here.
//
//        VertexShader = compile vs_2_0 VertexShaderFunction();
//        PixelShader = compile ps_2_0 PixelShaderFunction();
//    }
//}
float4x4 world;
float4x4 view;
float4x4 projection;
float3 lightPosition;
float4 ambientLightColor = { 0, 0, 0.2, 1 };
float4 diffuseLightColor = { 1, 1, 1, 1 };
float4 specularLightColor = { 1, 1, 1, 1 };
float specularIntensity = 1;
float specularPower = 10;
float3 cameraPosition;

struct GouraudVertex
{
	float4 Position : POSITION;
	float4 Color    : COLOR0;
};

GouraudVertex GouraudVertexShader(float3 position : POSITION,
	float3 normal : NORMAL,
	float4 color : COLOR)
{
	GouraudVertex output;

	float4x4 wvp = mul(mul(world, view), projection);
		output.Position.xyzw = mul(float4(position, 1.0), wvp);
	

	float3 worldNormal = mul(float4(normal, 0.0), world).xyz;
		float4 worldPosition = mul(float4(position, 1.0), world);
		worldPosition = worldPosition / worldPosition.w;

	float3 directionToLight = normalize(lightPosition - worldPosition.xyz);
		float diffuseIntensity = saturate(dot(directionToLight, worldNormal));
	float4 diffuse = color * diffuseLightColor * diffuseIntensity;

		float3 reflectionVector = normalize(reflect(-directionToLight, worldNormal));
		float3 directionToCamera = normalize(cameraPosition - worldPosition.xyz);
		float4 specular = specularLightColor * specularIntensity *
		pow(saturate(dot(reflectionVector, directionToCamera)),
		specularPower);

	output.Color = ambientLightColor + diffuse + specular;
	output.Color.a = 1.0;

	return output;
}

float4 GouraudPixelShader(GouraudVertex vertex) : COLOR0
{
	return vertex.Color;
}

technique Gouraud
{
	pass Pass0
	{
		//VertexShader = compile vs_2_0 GouraudVertexShader();
		//PixelShader = compile ps_2_0 GouraudPixelShader();

		VertexShader = compile vs_4_0_level_9_1 GouraudVertexShader();
		PixelShader = compile ps_4_0_level_9_1 GouraudPixelShader();

//		VertexShader = compile ps_4_0_level_9_1 GouraudVertexShader();
//		PixelShader = compile ps_4_0_level_9_1 GouraudPixelShader();
		//VertexShader = compile ps_3_0 GouraudVertexShader();
		//PixelShader = compile ps_3_0 GouraudPixelShader();
	}
}



struct VSOutPhong
{
	float4 Position : POSITION;
	float3 WorldNormal : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float4 Color : COLOR0;
};

//struct PixelShaderInputPerPixelDiffuse
//{
//	float3 WorldNormal : TEXCOORD0;
//	float3 WorldPosition : TEXCOORD1;
//	float4 Color : COLOR0;
//};

VSOutPhong PhongVS(
	float3 position : POSITION,
	float3 normal : NORMAL,
	float4 color : COLOR)
{
	VSOutPhong output;

	float4x4 wvp = mul(mul(world, view), projection);

	output.Position.xyzw = mul(float4(position, 1.0), wvp);

	output.WorldNormal = mul(float4(normal, 0.0), world).xyz;
	float4 worldPosition = mul(float4(position, 1.0), world);
	output.WorldPosition = worldPosition.xyz / worldPosition.w;
	output.Color = color;

	return output;
}

float4 PhongPS(VSOutPhong input) : COLOR0
{
	float3 directionToLight = normalize(lightPosition - input.WorldPosition);
	float diffuseIntensity = saturate(dot(directionToLight, input.WorldNormal));
	float4 diffuse = input.Color * diffuseLightColor * diffuseIntensity;

		float3 reflectionVector = normalize(reflect(-directionToLight, input.WorldNormal));
		float3 directionToCamera = normalize(cameraPosition - input.WorldPosition);

		float4 specular = specularLightColor * specularIntensity *
		pow(saturate(dot(reflectionVector, directionToCamera)),
		specularPower);

	float4 color = specular + diffuse + ambientLightColor;
		color.a = 1.0;
	return color;
}

technique Phong
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 PhongVS();
		PixelShader = compile ps_2_0 PhongPS();

		VertexShader = compile vs_4_0_level_9_1 PhongVS();
		PixelShader = compile ps_4_0_level_9_1 PhongPS();
//		VertexShader = compile ps_4_0_level_9_1 PerPixelDiffuseVS();
//		PixelShader = compile ps_4_0_level_9_1 DiffuseAndPhongPS();
		//PixelShader = compile ps_3_0 DiffuseAndPhongPS();
		//PixelShader = compile ps_3_0 DiffuseAndPhongPS();
	}
}




//struct VSOutPhong
//{
//	float4 Position : POSITION;
//	float4 Color : COLOR0;
//};
//
//VSOutPhong PhongVS(
//	float3 position : POSITION,
//	float3 normal : NORMAL,
//	float4 color : COLOR)
//{
//	VSOutPhong output;
//
//	float4x4 wvp = mul(mul(world, view), projection);
//	float4 poss  = float4(position.x, position.y, position.z, 1.0);
//	output.Position = mul(poss , wvp);
////	output.Position.xyzw = mul(float4(position, 1.0), wvp);
//
//	output.Color = color;
//
//	return output;
//}
//
//float4 PhongPS(VSOutPhong input) : COLOR0
//{
//	//float4 color = specular + diffuse + ambientLightColor;
//	//color.a = 1.0;
//	float4 color = input.Color;
//	return color;
//}
//
//technique Phong
//{
//	pass Pass0
//	{
//		VertexShader = compile vs_2_0 PhongVS();
//		PixelShader = compile ps_2_0 PhongPS();
////		VertexShader = compile ps_4_0_level_9_1 PerPixelDiffuseVS();
////		PixelShader = compile ps_4_0_level_9_1 DiffuseAndPhongPS();
//		//PixelShader = compile ps_3_0 DiffuseAndPhongPS();
//		//PixelShader = compile ps_3_0 DiffuseAndPhongPS();
//	}
//}
//
