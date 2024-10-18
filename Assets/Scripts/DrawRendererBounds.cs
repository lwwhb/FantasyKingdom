using UnityEngine;

namespace Unity.FantasyKingdom
{
    public class DrawRendererBounds : MonoBehaviour
    {
        // Draws a wireframe box around the selected object,
        // indicating world space bounding volume.
        public void OnDrawGizmosSelected()
        {
            var r = GetComponent<Renderer>();
            if (r == null)
                return;
            var bounds = r.bounds;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bounds.center, (bounds.max - bounds.min).magnitude / 2.0f);
            Gizmos.color = Color.green;

            Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
        }
    }
}
