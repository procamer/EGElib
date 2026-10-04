using Assimp;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Ege.Model
{
    public static class Materials
    {
        // shared by every model, keyed by full file path
        private static readonly Dictionary<string, TextureInfo> texturesLoaded = new Dictionary<string, TextureInfo>();

        public static List<TextureInfo> LoadMaterialTextures(Material mat, TextureType type, string directory)
        {
            List<TextureInfo> textures = new List<TextureInfo>();
            for (int i = 0; i < mat.GetMaterialTextureCount(type); i++)
            {
                mat.GetMaterialTexture(type, i, out TextureSlot str);
                string fullPath = System.IO.Path.Combine(directory, str.FilePath);

                if (!texturesLoaded.TryGetValue(fullPath, out TextureInfo texture))
                {
                    Console.WriteLine(str.TextureType + " -- " + str.FilePath);
                    texture = new TextureInfo
                    {
                        Id = new Texture(fullPath).Handle,
                        Type = type == TextureType.Height ? TextureType.Normals : type,
                        Path = str.FilePath
                    };
                    texturesLoaded.Add(fullPath, texture);
                }
                textures.Add(texture);
            }
            return textures;
        }

        public static void DeleteLoadedTextures()
        {
            foreach (TextureInfo texture in texturesLoaded.Values)
                GL.DeleteTexture(texture.Id);
            texturesLoaded.Clear();
        }
    }

    public struct TextureInfo
    {
        public uint Id { get; set; }
        public TextureType Type { get; set; }
        public string Path { get; set; }
    }
}
