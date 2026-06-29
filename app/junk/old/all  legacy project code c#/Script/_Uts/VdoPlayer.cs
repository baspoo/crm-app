
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.Collections;
using System.Runtime.InteropServices;
using GameStore.Core;
using System;

public class VdoPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public VideoClip editorPreviewClip; // ลากใส่ใน Inspector เพื่อ Test ใน Editor


    [VInspector.Button]
    public void Play()
    {
        videoPlayer.Stop();
        videoPlayer.Play();
    }
    [VInspector.Button]
    public void Prepare(byte[] rawData)
    {
        videoPlayer.targetCamera = Camera.main;
        if (!Application.isEditor)
        {
            StartCoroutine(LoadFromBundle(rawData));
        }
        else
        {
            PlayInEditor();
        }
    }



    void PlayInEditor()
    {
        if (editorPreviewClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = editorPreviewClip;
            PrepareAndPlay();
        }
    }

    IEnumerator LoadFromBundle(byte[] rawData)
    {
        string blobUrl = "";
        GameStore.WebGLService.GetBlobVdo(rawData, (data) =>
        {
            blobUrl = data;
        });
        yield return new WaitUntil(() => !string.IsNullOrEmpty(blobUrl));
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = blobUrl;
        PrepareAndPlay();
    }

    bool prepared = false;
    public System.Action onVideoPrepared;
    public System.Action onVideoStarted;
    void PrepareAndPlay()
    {
        videoPlayer.prepareCompleted += (vp) =>
        {
            PrepareDone();
        };
        videoPlayer.started += (vp) =>
        {
            Debug.Log("Video Started.");
            onVideoStarted?.Invoke();
            onVideoStarted = null;
        };
        videoPlayer.Prepare();
    }
    void PrepareDone()
    {
        prepared = true;
        Debug.Log("Video Prepared, now playing.");
        onVideoPrepared?.Invoke();
        onVideoPrepared = null;
    }
}