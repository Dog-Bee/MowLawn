using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class LevelCompletion : MonoBehaviour
{
    [SerializeField] private GrassInstanceDrawer drawer;
    [field: Range(0, 1), SerializeField] private float cutThreshold = 0.5f;
    [field: Range(0, 1), SerializeField] private float target = 0.95f;
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private int sampleSize = 256;

    private float _timer;
    private bool _isFinished;

    public float lastPercent { get; private set; }

    private void Update()
    {
        if (_isFinished || drawer == null || drawer.CutMask == null) return;

        _timer -= Time.deltaTime;
        if (_timer > 0) return;
        _timer = checkInterval;
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        RenderTexture src = drawer.CutMask;
        RenderTexture tmp = RenderTexture.GetTemporary(sampleSize, sampleSize, 0, src.format);

        Graphics.Blit(src, tmp);

        AsyncGPUReadback.Request(tmp, 0, req =>
        {
            RenderTexture.ReleaseTemporary(tmp);
            if (req.hasError) return;

            var data = req.GetData<Color32>();
            int total = data.Length;
            int cut = 0;
            byte thr = (byte)Mathf.RoundToInt(cutThreshold * 250f);
            for (int i = 0; i < total; i++)
            {
                if (data[i].r < thr) 
                    cut++;
            }

            lastPercent = (float)cut / total;
            if (lastPercent >= target)
            {
                _isFinished = true;
                drawer.CutAllGrass();
                EndGameState.GameEndedEvent?.Invoke();
            }
        });
    }
}