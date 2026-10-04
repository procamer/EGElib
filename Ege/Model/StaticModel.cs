using Assimp;

namespace Ege.Model
{
    public class StaticModel : Model
    {
        // OBJ files store their normal maps in the "bump" (Height) slot
        public StaticModel(string file)
            : base(file,
                PostProcessSteps.Triangulate |
                PostProcessSteps.FlipUVs |
                PostProcessSteps.CalculateTangentSpace |
                PostProcessSteps.GenerateSmoothNormals,
                TextureType.Height)
        {
        }
    }
}
