using System;
using System.IO;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Ege
{
    public class Shader : IDisposable
    {
        public readonly int Handle;
        private bool disposedValue = false;

        public Shader(string vertexPath, string fragmentPath, string geometryPath = "")
        {
            int vertexShader = CreateShader(vertexPath, ShaderType.VertexShader);
            int fragmentShader = CreateShader(fragmentPath, ShaderType.FragmentShader);
            int geometryShader = 0;
            
            if (!string.IsNullOrEmpty(geometryPath))
            {
                geometryShader = CreateShader(geometryPath, ShaderType.GeometryShader);
            }

            Handle = GL.CreateProgram();
            
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            if (!string.IsNullOrEmpty(geometryPath))
            {
                GL.AttachShader(Handle, geometryShader);
            }

            GL.LinkProgram(Handle);
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
            if (success == -1)
            {
                GL.GetProgramInfoLog(Handle, out string infoLog);
                throw new Exception($"gölgelendirici programı bağlantılı değil: {infoLog}");
            }

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);            
            if (!string.IsNullOrEmpty(geometryPath))
            {
                GL.DetachShader(Handle, geometryShader);
                GL.DeleteShader(geometryShader);
            }

        }

        private int CreateShader(string shaderPath, ShaderType shaderType)
        {
            int id = GL.CreateShader(shaderType);

            using (StreamReader reader = new StreamReader(shaderPath, Encoding.UTF8))
            {
                GL.ShaderSource(id, reader.ReadToEnd());
            }

            GL.CompileShader(id);

            GL.GetShader(id, ShaderParameter.CompileStatus, out int success);
            if (success == -1)
            {
                GL.GetShaderInfoLog(id, out string infoLog);
                throw new InvalidDataException(infoLog);
            }
            return id;
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }
        
        public void SetInt(string name, int value)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(GL.GetUniformLocation(Handle, name), value);
        }
        
        public void SetFloat(string name, float value)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(GL.GetUniformLocation(Handle, name), value);
        }
        
        public void SetVec3(string name, Vector3 data)
        {
            GL.UseProgram(Handle);
            GL.Uniform3(GL.GetUniformLocation(Handle, name), data);
        }
        
        public void SetMat4(string name, Matrix4 data)
        {
            GL.UseProgram(Handle);
            GL.UniformMatrix4(GL.GetUniformLocation(Handle, name), false, ref data);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);
                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
