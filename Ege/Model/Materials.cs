using Assimp;
using System;
using System.Collections.Generic;

namespace Ege.Model
{
    public class Materials
    {
        public static string directory;
        public List<TextureInfo> textureInfos;
        private static readonly List<TextureInfo> texturesLoaded = new List<TextureInfo>();

        public List<TextureInfo> LoadMaterialTextures(Material mat, TextureType type)
        {
            List<TextureInfo> textures = new List<TextureInfo>();
            for (int i = 0; i < mat.GetMaterialTextureCount((Assimp.TextureType)type); i++)
            {
                mat.GetMaterialTexture((Assimp.TextureType)type, i, out TextureSlot str);
                bool skip = false;
                for (int j = 0; j < texturesLoaded.Count; j++)
                {
                    if (texturesLoaded[j].Path == str.FilePath)
                    {
                        textures.Add(texturesLoaded[j]);
                        skip = true;
                        break;
                    }
                }
                if (!skip)
                {
                    Console.WriteLine(str.TextureType + " -- " + str.FilePath);
                    TextureInfo texture = new TextureInfo
                    {
                        Id = TextureFromFile(str.FilePath, directory),
                        Type = type,
                        Path = str.FilePath
                    };
                    textures.Add(texture);
                    texturesLoaded.Add(texture);
                }
            }
            return textures;
        }

        private uint TextureFromFile(string path, string directory)
        {
            string tPath = System.IO.Path.Combine(directory, path);
            Texture t = new Texture(tPath);

            return t.Handle;
        }

    }
}

//public enum TextureType
//{
//    None = 0,
//    Diffuse = 1,
//    Specular = 2,
//    Ambient = 3,
//    Emissive = 4,
//    Height = 5,
//    Normals = 6,
//    Shininess = 7,
//    Opacity = 8,
//    Displacement = 9,
//    Lightmap = 10,
//    Reflection = 11,
//    Unknown = 12
//}

public struct TextureInfo
{
    public uint Id { get; set; }
    public TextureType Type { get; set; }
    public string Path { get; set; }
}