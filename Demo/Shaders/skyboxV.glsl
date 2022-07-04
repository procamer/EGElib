#version 450 core

layout (location = 0) in vec3 position;

out vec3 TexCoord;

uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
	TexCoord = position;
	gl_Position = (projectionMatrix * viewMatrix * vec4(position, 0)).xyww;	
} 