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
   [SerializeField] private bool _cutSweep = false;
   [SerializeField] private float pushRadius = 0.6f;

   private Vector3 _prev;
   private bool _hasPrev;

   private void LateUpdate()
   {
      if (!_cutSweep)
      {
         Emit();
      }
      else
      {
         CutSweepStamp();
      }
      drawer.PushGrassFromCenter(transform.position,pushRadius);
   }

   private void Emit()
   {
      if (drawer == null || stamp == null) return;

      Vector3 center = transform.position;
      
      Vector3 f = parent.forward;
      f.y = 0;

      if (f.sqrMagnitude < 1e-6f)                            
         f = Vector3.right;
      f.Normalize();
      float angle = Mathf.Atan2(f.z, f.x) * Mathf.Rad2Deg+angleOffsetDeg;
      
      drawer.CutStampWorld(stamp,center,worldSize,angle,strength);
   }

   private void CutSweepStamp()
   {
      if (drawer == null || stamp == null) return;

      Vector3 current = transform.position;

      if (!_hasPrev)
      {
         _prev = current;
         _hasPrev = true;
         return;
      }
      drawer.CutSweepStampWorld(stamp,_prev,current,worldSize,strength);
      _prev = current;
   }

}
