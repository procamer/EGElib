#version 450 core

// NR_POINT_LIGHTS is injected by the application (Shader defines)
#ifndef NR_POINT_LIGHTS
#error NR_POINT_LIGHTS must be defined by the application
#endif
#define MAX_BONE 50
#define MAX_WEIGHTS 4

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec2 texCoord;
layout (location = 3) in vec3 tangent;
layout (location = 4) in vec3 bitangent;
layout (location = 5) in vec4 bone_id;
layout (location = 6) in vec4 weight;

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
uniform mat4 boneTransform[MAX_BONE];
uniform float skeletal;
uniform vec3 cameraPos;
uniform PointLight pointLights[NR_POINT_LIGHTS];

void main()
{            
	mat4 boneTransformation = mat4(0.0) ;
	// weights must sum to 1 (not unit length), otherwise vertices get scaled
	vec4 normalizedWeight = weight / max(dot(weight, vec4(1.0)), 0.0001);
	for(int i =0; i<MAX_WEIGHTS;i++)
		boneTransformation += boneTransform[uint(bone_id[i])] * normalizedWeight[i];

	mat4 model = transformationMatrix * boneTransformation;
	vec4 worldPosition = model * vec4(position, 1.0);
	gl_Position =projectionMatrix * viewMatrix * worldPosition;
    FragPos = worldPosition.xyz;
    TexCoord = texCoord;

    vec3 T = normalize(vec3(model * vec4(tangent, 0.0)));
	vec3 B = normalize(vec3(model * vec4(bitangent, 0.0)));
	vec3 N = normalize(vec3(model * vec4(normal, 0.0)));
	mat3 TBN = mat3(T, B, N);

	// world -> tangent space (same as staticV.glsl)
	TangentFragPos = FragPos * TBN;
	TangentViewPos = cameraPos * TBN;
	for(int i = 0; i < NR_POINT_LIGHTS; i++)
		TangentLightPos[i] = pointLights[i].position * TBN;
}
