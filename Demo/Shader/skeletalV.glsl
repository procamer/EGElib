#version 450 core

#define NR_POINT_LIGHTS 11
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
//out vec3 SurfaceNormal;
out vec3 TangentFragPos;
out vec3 TangentViewPos;
out vec3 TangentLightPos[NR_POINT_LIGHTS];

uniform mat4 transformationMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;
uniform mat4 boneTransform[MAX_BONE];

uniform vec3 cameraPos;
uniform PointLight pointLights[NR_POINT_LIGHTS];

void main()
{        
    mat4 boneTransformation = mat4(0.0) ;
    vec4 normalizedWeight = normalize(weight);    
    for(int i =0; i<MAX_WEIGHTS;i++)
       boneTransformation += boneTransform[uint(bone_id[i])] * normalizedWeight[i];	    
    mat4 transformedBoneMatrix = transformationMatrix * boneTransformation;
    
    vec4 worldPosition = transformedBoneMatrix * vec4(position, 1.0);
    gl_Position =projectionMatrix * viewMatrix * worldPosition;   	
    FragPos = worldPosition.xyz;	
    //SurfaceNormal = (transformedBoneMatrix * vec4(normal, 0.0f)).xyz;
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
