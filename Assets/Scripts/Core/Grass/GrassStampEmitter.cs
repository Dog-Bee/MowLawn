using UnityEngine;

public class GrassStampEmitter : MonoBehaviour
{
   [SerializeField] private Transform parent;
   [Header("Target")] 
   [SerializeField] private GrassInstanceDrawer drawer;
   [SerializeField] private Texture stamp;

   [Header("Stamp in World")]
   [SerializeField] private Vector2 worldSize = new Vector2(1f,0.3f);
   [SerializeField] private float angleOffsetDeg = 0;

   [Range(0, 1), SerializeField] private float strength = 1;


   private void Update()
   {
      Emit();
   }

   private void Emit()
   {
      if (drawer == null || stamp == null) return;

      Vector3 center = transform.position;
      
      Vector3 f = parent.forward;
      f.y = 0;

      if (f.sqrMagnitude < 0)
         f = Vector3.right;
      f.Normalize();
      float angle = Mathf.Atan2(f.z, f.x) * Mathf.Rad2Deg+angleOffsetDeg;
      
      drawer.CutStampWorld(stamp,center,worldSize,angle,strength);
   }

}
