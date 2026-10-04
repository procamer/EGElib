#version 450 core

layout (location = 0) in vec3 aPos;

uniform mat4 transformationMatrix;

void main()
{
    gl_Position = transformationMatrix * vec4(aPos, 1.0);
}
