using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Graphics.OpenGL;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace Ege.Model
{
    public class Texture
    {
        public readonly uint Handle;
        
        // shadow
        public int shadowWidth => width;
        public int shadowHeight => height;

        public int[] shadowCubemaps;
        public int[] FBO;

        private int width = 512;
        private int height = 512;

        public Texture(string filename)
        {
            GL.GenTextures(1, out Handle);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Handle);

            string ext = Path.GetExtension(filename);
            if (ext == ".tga")
            {
                LoadTGA(filename);
            }
            else
            {
                LoadBitmap(filename);
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public Texture(string[] faces)
        {
            GL.GenTextures(1, out Handle);
            GL.BindTexture(TextureTarget.TextureCubeMap, Handle);

            for (int i = 0; i < faces.Length; i++)
            {
                LoadBitmap(faces[i].ToString(), i);
            }
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (float)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        }

        public Texture(PointLight[] lights)
        {
            shadowCubemaps = new int[lights.Length];
            FBO = new int[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                GL.GenTextures(1, out shadowCubemaps[i]);
                GL.BindTexture(TextureTarget.TextureCubeMap, shadowCubemaps[i]);
                for (int index = 0; index < 6; index++)
                {
                    GL.TexImage2D(
                        TextureTarget.TextureCubeMapPositiveX + index,
                        0,
                        PixelInternalFormat.DepthComponent32f,
                        shadowWidth,
                        shadowHeight, 0,
                        PixelFormat.DepthComponent,
                        PixelType.Float,
                        IntPtr.Zero);
                }

                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

                FBO[i] = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO[i]);
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, shadowCubemaps[i], 0);
                GL.DrawBuffer(DrawBufferMode.None);
                GL.ReadBuffer(ReadBufferMode.None);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }

        internal void LoadBitmap(string filename)
        {
            using (Bitmap image = new Bitmap(filename))
            {
                BitmapData data = image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                if (data.Scan0 != null)
                {
                    GL.TexImage2D(TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Width,
                    image.Height,
                    0,
                    PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    data.Scan0);
                }
                else
                {
                    throw new Exception("Doku yükleme başarısız oldu!");
                }
            }
        }

        internal void LoadBitmap(string filename, int index)
        {
            using (var image = new Bitmap(filename))
            {
                var data = image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                if (data.Scan0 != null)
                {
                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + index,
                        0,
                        PixelInternalFormat.Rgb,
                        image.Width,
                        image.Height,
                        0,
                        PixelFormat.Bgr,
                        PixelType.UnsignedByte,
                        data.Scan0);
                }
                else
                {
                    throw new Exception("Doku yükleme başarısız oldu!");
                }
            }
        }

        internal void LoadTGA(string filename)
        {
            using (Paloma.TargaImage image = new Paloma.TargaImage(filename))
            {
                BitmapData data = image.Image.LockBits(
                new Rectangle(0, 0, image.Image.Width, image.Image.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                if (data.Scan0 != null)
                {
                    GL.TexImage2D(TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Image.Width,
                    image.Image.Height,
                    0,
                    PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    data.Scan0);
                }
                else
                {
                    throw new Exception("Doku yükleme başarısız oldu!");
                }
            }
        }




    }
}
