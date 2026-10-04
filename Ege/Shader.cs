using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Ege
{
    public class Shader : IDisposable
    {
        public readonly int Handle;

        // compile-time constants injected as #define lines right after #version
        public IReadOnlyDictionary<string, object> Defines { get; }

        private bool disposedValue = false;
        private readonly Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

        // a stage's GLSL code plus a name used in error messages
        private struct Source
        {
            public string Name;
            public string Code;
        }

        // loads the stages from files; geometryPath is optional
        public Shader(string vertexPath, string fragmentPath, string geometryPath = "",
            IReadOnlyDictionary<string, object> defines = null)
            : this(FromFile(vertexPath), FromFile(fragmentPath),
                  string.IsNullOrEmpty(geometryPath) ? (Source?)null : FromFile(geometryPath), defines)
        {
        }

        // builds a program from GLSL code held in memory
        public static Shader FromSource(string vertexSource, string fragmentSource, string geometrySource = null,
            IReadOnlyDictionary<string, object> defines = null)
        {
            return new Shader(
                new Source { Name = "vertex", Code = vertexSource },
                new Source { Name = "fragment", Code = fragmentSource },
                geometrySource == null ? (Source?)null : new Source { Name = "geometry", Code = geometrySource },
                defines);
        }

        // builds a program from the GLSL files embedded in Ege.dll (Ege/Shaders)
        internal static Shader FromResources(string vertexName, string fragmentName, string geometryName = null,
            IReadOnlyDictionary<string, object> defines = null)
        {
            return new Shader(
                FromResource(vertexName),
                FromResource(fragmentName),
                geometryName == null ? (Source?)null : FromResource(geometryName),
                defines);
        }

        private Shader(Source vertex, Source fragment, Source? geometry, IReadOnlyDictionary<string, object> defines)
        {
            Defines = defines ?? new Dictionary<string, object>();

            int vertexShader = CreateShader(vertex, ShaderType.VertexShader);
            int fragmentShader = CreateShader(fragment, ShaderType.FragmentShader);
            int geometryShader = geometry.HasValue ? CreateShader(geometry.Value, ShaderType.GeometryShader) : 0;

            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            if (geometryShader != 0)
                GL.AttachShader(Handle, geometryShader);

            GL.LinkProgram(Handle);
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                GL.GetProgramInfoLog(Handle, out string infoLog);
                throw new Exception($"gölgelendirici programı bağlantılı değil ({vertex.Name}, {fragment.Name}): {infoLog}");
            }

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            if (geometryShader != 0)
            {
                GL.DetachShader(Handle, geometryShader);
                GL.DeleteShader(geometryShader);
            }
        }

        private static Source FromFile(string path)
        {
            using (StreamReader reader = new StreamReader(path, Encoding.UTF8))
                return new Source { Name = path, Code = reader.ReadToEnd() };
        }

        private static Source FromResource(string fileName)
        {
            string resource = "Ege.Shaders." + fileName;
            using (Stream stream = typeof(Shader).Assembly.GetManifestResourceStream(resource))
            {
                if (stream == null)
                    throw new FileNotFoundException($"Embedded shader '{resource}' not found.");
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    return new Source { Name = fileName, Code = reader.ReadToEnd() };
            }
        }

        private int CreateShader(Source source, ShaderType shaderType)
        {
            int id = GL.CreateShader(shaderType);
            GL.ShaderSource(id, InjectDefines(source.Code));
            GL.CompileShader(id);

            GL.GetShader(id, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                GL.GetShaderInfoLog(id, out string infoLog);
                throw new InvalidDataException($"{source.Name}: {infoLog}");
            }
            return id;
        }

        private string InjectDefines(string source)
        {
            if (Defines.Count == 0) return source;

            StringBuilder defines = new StringBuilder();
            foreach (KeyValuePair<string, object> define in Defines)
                defines.Append("#define ").Append(define.Key).Append(' ').Append(define.Value).Append('\n');

            // #version must stay the first statement
            int versionLine = source.IndexOf("#version", StringComparison.Ordinal);
            if (versionLine < 0) return defines + source;

            int lineEnd = source.IndexOf('\n', versionLine);
            if (lineEnd < 0) return source + "\n" + defines;
            return source.Insert(lineEnd + 1, defines.ToString());
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }
        
        // glProgramUniform* writes straight into this program, so the setters
        // neither need nor change the currently bound program.
        public void SetInt(string name, int value)
        {
            GL.ProgramUniform1(Handle, GetUniformLocation(name), value);
        }

        public void SetFloat(string name, float value)
        {
            GL.ProgramUniform1(Handle, GetUniformLocation(name), value);
        }

        public void SetVec3(string name, Vector3 data)
        {
            GL.ProgramUniform3(Handle, GetUniformLocation(name), data);
        }

        public void SetMat4(string name, Matrix4 data)
        {
            GL.ProgramUniformMatrix4(Handle, GetUniformLocation(name), false, ref data);
        }

        private int GetUniformLocation(string name)
        {
            if (!uniformLocations.TryGetValue(name, out int location))
            {
                location = GL.GetUniformLocation(Handle, name);
                uniformLocations.Add(name, location);
            }
            return location;
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
