using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AudioHeightBlendComponent : MonoBehaviour
{
    public float startFallOffHeight = 100f;
    public float endFallOffHeight = 50f;
    public bool controlDirectVolume;
    
    public AudioSource[] audioSources = System.Array.Empty<AudioSource>();
    public AnimationCurve fallOffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    Camera m_LastKnownCamera;

    float[] m_OriginalVolume;
    float m_PreviousVolume = -1f;
    AnimationCurve m_RollOffCurve;
    Keyframe[] m_RollOffKeys;

    void OnEnable()
    {
        m_RollOffCurve = AnimationCurve.Constant(0f, 20000f, 1f);
        m_RollOffKeys = m_RollOffCurve.keys;

        m_OriginalVolume = new float[audioSources.Length];
        
        for (var i = 0; i < audioSources.Length; ++i)
        {
            var audioSource = audioSources[i];
            m_OriginalVolume[i] = audioSource.volume;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.minDistance = m_RollOffKeys[0].time;
            audioSource.maxDistance = m_RollOffKeys[1].time;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, m_RollOffCurve); 
        }
        
        RenderPipelineManager.beginContextRendering += RenderPipelineManagerOnBeginContextRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= RenderPipelineManagerOnBeginContextRendering;
    }

    void RenderPipelineManagerOnBeginContextRendering(ScriptableRenderContext _, List<Camera> cameras)
    {
        if (cameras.Count > 0)
        {
            m_LastKnownCamera = cameras[^1];
        }
    }

    void LateUpdate()
    {
        var volume = 0f;

        if (m_LastKnownCamera != null)
        {
            var height = m_LastKnownCamera.transform.position.y;
            var linear = Mathf.Clamp01((height - startFallOffHeight) / (endFallOffHeight - startFallOffHeight));
            var remapped = fallOffCurve.Evaluate(linear);
            volume = remapped;
            for (int i = 0; i < audioSources.Length; i++)
            {
                //if(Vector3.Distance(m_LastKnownCamera.transform.position,audioSources[i].gameObject.transform.position) > audioSources[i].maxDistance)
                //{
                //    audioSources[i].Pause();
                //}
                //else
                //{
                //    audioSources[i].Play();
                //}
            }
        }

        if (volume == m_PreviousVolume)
        {
            return;
        }

        m_PreviousVolume = volume;
        
        if (controlDirectVolume)
        {
            for (var i = 0; i < audioSources.Length; ++i)
            {
                var audioSource = audioSources[i];
                audioSource.volume = volume * m_OriginalVolume[i];
            }
        }
        else
        {
            m_RollOffKeys[0].value = m_RollOffKeys[1].value = volume;
            m_RollOffCurve.keys = m_RollOffKeys;
            
            foreach (var audioSource in audioSources)
            {
                audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, m_RollOffCurve); 
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        var center = transform.position;
        var size = new Vector3(1000f, 0.1f, 1000f);

        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        center.y = startFallOffHeight;
        Gizmos.DrawCube(center, size);
        
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        center.y = endFallOffHeight;
        Gizmos.DrawCube(center, size);
    }
}
