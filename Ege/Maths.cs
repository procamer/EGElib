using OpenTK;

namespace Ege
{
    class Maths
    {
        internal static Matrix4 ConvertMatrix(Assimp.Matrix4x4 matrix)
        {
            return new Matrix4(
                matrix.A1, matrix.B1, matrix.C1, matrix.D1,
                matrix.A2, matrix.B2, matrix.C2, matrix.D2,
                matrix.A3, matrix.B3, matrix.C3, matrix.D3,
                matrix.A4, matrix.B4, matrix.C4, matrix.D4
            );

            //return new Matrix4(
            //    matrix.A1, matrix.A2, matrix.A3, matrix.A4,
            //    matrix.B1, matrix.B2, matrix.B3, matrix.B4,
            //    matrix.C1, matrix.C2, matrix.C3, matrix.C4,
            //    matrix.D1, matrix.D2, matrix.D3, matrix.D4
            //);
        }

        internal static Vector3 ConvertVector3(Assimp.Vector3D vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        internal static Vector2 ConvertVector2(Assimp.Vector3D vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        internal static Quaternion ConvertQuaternion(Assimp.Quaternion quaternion)
        {
            return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        internal static Matrix4 createViewMatrix(Camera camera)
        {
            Vector3 negativeR = Vector3.Multiply(camera.GetRotation(), -1.0f);
            Vector3 negativeT = Vector3.Multiply(camera.Position, -1.0f);

            Matrix4 matrix = Matrix4.CreateRotationX(negativeR.X) *
                                         Matrix4.CreateRotationY(negativeR.Y) *
                                         Matrix4.CreateRotationX(negativeR.X) *
                                         Matrix4.CreateTranslation(negativeT);
            return matrix;
        }

    }
}
