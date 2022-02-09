using OpenTK;

namespace Ege.Model
{
    public class BoneTransform
    {
        public string name;
        public Matrix4 offsetMatrix;
        private Matrix4 transformation;

        public BoneTransform(string name, Matrix4 offsetMatrix)
        {
            this.name = name;
            this.offsetMatrix = offsetMatrix;
        }

        public string GetName()
        {
            return name;
        }

        public Matrix4 GetOffsetMatrix()
        {
            return offsetMatrix;
        }

        public Matrix4 GetTransformation()
        {
            return transformation;
        }

        public void SetTransformation(Matrix4 transformation)
        {
            this.transformation = transformation;
        }

    }
}
