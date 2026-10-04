#version 450 core

#define MAX_BONE 50
#define MAX_WEIGHTS 4

layout (location = 0) in vec3 aPos;
layout (location = 5) in vec4 bone_id;
layout (location = 6) in vec4 weight;

uniform mat4 transformationMatrix;
uniform mat4 boneTransform[MAX_BONE];

void main()
{
	mat4 boneTransformation = mat4(0.0);
	vec4 normalizedWeight = weight / max(dot(weight, vec4(1.0)), 0.0001);
	for(int i = 0; i < MAX_WEIGHTS; i++)
		boneTransformation += boneTransform[uint(bone_id[i])] * normalizedWeight[i];

	gl_Position = transformationMatrix * boneTransformation * vec4(aPos, 1.0);
}
