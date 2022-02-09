#version 450 core

#define NR_POINT_LIGHTS 3

struct PointLight
{
	vec3 position;
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	float constant;
	float linear;
	float quadratic;
};

in vec3 FragPos;
in vec2 TexCoord;
in vec3 TangentFragPos;
in vec3 TangentViewPos;
in vec3 TangentLightPos[NR_POINT_LIGHTS];

out vec4 FragColor;

uniform sampler2D texture_diffuse1;
uniform sampler2D texture_specular1;
uniform sampler2D texture_normal1;
uniform vec3 cameraPos;
uniform PointLight pointLights[NR_POINT_LIGHTS];
uniform samplerCube depthMaps[NR_POINT_LIGHTS];
uniform mat4 cubeProjection;

float materialshininess= 32.0f;

//-----------------------------------
float chebyshevNorm(in vec3 dir)
{
    vec3 tmp = abs(dir);
    return max(max(tmp.x,tmp.y),tmp.z);
}

//------------------------------------------
float getCurrentDepth(vec3 fragToLight)
{
	float lightChebyshev = -chebyshevNorm(fragToLight); // linear depth
	vec4 postProjPos =cubeProjection * vec4(fragToLight.xy, lightChebyshev,1.0);
	float NDC = postProjPos.z/postProjPos.w;
	float Window = NDC*0.5+0.5;
	return Window;
}

//---------------------------------------
vec3 CalcPointLight(int i)
{
	//i=1f;
	vec3 diffuseColor = vec3(texture(texture_diffuse1, TexCoord));
	vec3 specularColor = vec3(texture(texture_specular1, TexCoord));	
	vec3 normalColor = vec3(texture(texture_normal1, TexCoord));
	normalColor = normalize(normalColor * 2.0 - 1.0);
	
	// ambient
    vec3 ambient = pointLights[i].ambient * diffuseColor;

	// diffuse
    vec3 lightDir = normalize(TangentLightPos[i] - TangentFragPos);
    float diff = max(dot(normalColor, lightDir), 0.0);
    vec3 diffuse = pointLights[i].diffuse * diff * diffuseColor;

	// specular
	vec3 viewDir = normalize(TangentViewPos - TangentFragPos);
    vec3 reflectDir = reflect(-lightDir, normalColor);
	vec3 halfwayDir = normalize(lightDir + viewDir);      
	float spec = pow(max(dot(halfwayDir,normalColor), 0.0), materialshininess);
    vec3 specular = pointLights[i].specular * spec * specularColor;
	
	// attenuation
    float distance = length(TangentLightPos[i] - TangentFragPos) * 0.5f;
    float attenuation = 1.0 / (pointLights[i].constant + pointLights[i].linear * distance + pointLights[i].quadratic * (distance * distance));    

	// combine results
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
}

void main()
{		
	if(texture(texture_diffuse1, TexCoord).a < 0.1) discard;
	
	vec3 finalResult = vec3(0);			
	for(int i = 0; i < NR_POINT_LIGHTS; i++)
	{
		vec3 result = vec3(0);
		result += CalcPointLight(i);

		vec3 fragToLight = FragPos - pointLights[i].position;
		float closestDepth = texture(depthMaps[i], fragToLight).b;
		float shadow = getCurrentDepth(fragToLight) - 0.0001 < closestDepth ? 1 : 0;
		
		finalResult += result * max(0.0, shadow);
		//finalResult = result;
	}	
	FragColor = vec4(finalResult, 1);	
}
