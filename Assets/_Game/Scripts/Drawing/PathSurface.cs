using UnityEngine;

namespace PawPath.Drawing
{
    public enum PathSurfaceType
    {
        Normal,
        Bounce,
        Hazard,
        Ice
    }

    /// <summary>Çizilen yolun kediye uygulayacağı davranışı taşır.</summary>
    public class PathSurface : MonoBehaviour
    {
        public PathSurfaceType Type { get; private set; }

        public void Configure(PathSurfaceType type) => Type = type;
    }
}
