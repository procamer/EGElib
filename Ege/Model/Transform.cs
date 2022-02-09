using OpenTK;

namespace Ege.Model
{
    public class Transform
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation= Vector3.Zero;
        public Vector3 Scale= Vector3.One;

        public Matrix4 TransformationMatrix()
        {
            return Matrix4.CreateScale(Scale) *
                Matrix4.CreateRotationX(Rotation.X) *
                Matrix4.CreateRotationY(Rotation.Y) *
                Matrix4.CreateRotationZ(Rotation.Z) *
                Matrix4.CreateTranslation(Position);
        }

    }
}
