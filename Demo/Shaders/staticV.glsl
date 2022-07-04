#version 450 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec2 texCoord;
layout (location = 3) in vec3 tangent;
layout (location = 4) in vec3 bitangent;

#define NR_POINT_LIGHTS 11

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

out vec3 FragPos;
out vec2 TexCoord;
out vec3 TangentFragPos;
out vec3 TangentViewPos;
out vec3 TangentLightPos[NR_POINT_LIGHTS];

uniform mat4 transformationMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform vec3 cameraPos;
uniform PointLight pointLights[NR_POINT_LIGHTS];

void main()
{
	vec4 worldPosition = transformationMatrix * vec4(position, 1.0);
    gl_Position =  projectionMatrix * viewMatrix *  worldPosition;		
	FragPos = worldPosition.xyz;	
	TexCoord = texCoord;
	
	vec3 T = normalize(vec3(transformationMatrix * vec4(tangent, 0.0)));
	vec3 B = normalize(vec3(transformationMatrix * vec4(bitangent, 0.0)));
	vec3 N = normalize(vec3(transformationMatrix * vec4(normal, 0.0)));	
	mat3 TBN = mat3(T, B, N);	
		
	TangentFragPos = FragPos * TBN;
	TangentViewPos = cameraPos * TBN;
	for(int i = 0; i < NR_POINT_LIGHTS; i++)
		TangentLightPos[i] = pointLights[i].position * TBN;	
}
