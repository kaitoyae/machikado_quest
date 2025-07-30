using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace packt.FoodyGO.UI
{
    [RequireComponent(typeof(RawImage))]
    public class CameraTextureOnRawImage : MonoBehaviour
    {        
        public RawImage rawImage;
        public WebCamTexture webcamTexture;       
        public AspectRatioFitter aspectFitter;
        private bool isInitialized = false;

        void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            StartCoroutine(InitializeCamera());
        }

        IEnumerator InitializeCamera()
        {
            // カメラ権限の確認
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.LogError("Camera permission denied");
                yield break;
            }

            // WebCamDevicesが利用可能かチェック
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("No camera devices found");
                yield break;
            }

            rawImage = GetComponent<RawImage>();
            aspectFitter = GetComponent<AspectRatioFitter>();

            // WebCamTextureを初期化
            webcamTexture = new WebCamTexture();
            rawImage.texture = webcamTexture;
            if (rawImage.material != null)
            {
                rawImage.material.mainTexture = webcamTexture;
            }
            
            webcamTexture.Play();

            // WebCamTextureが完全に初期化されるまで待機
            while (!webcamTexture.didUpdateThisFrame)
            {
                yield return null;
            }

            isInitialized = true;
        }

        void Update()
        {
            if (!isInitialized || webcamTexture == null || !webcamTexture.isPlaying)
                return;

            // WebCamTextureが完全に初期化されているかチェック
            if (webcamTexture.width <= 16 || webcamTexture.height <= 16)
                return;

            var camRotation = -webcamTexture.videoRotationAngle;
            if (webcamTexture.videoVerticallyMirrored)
            {
                camRotation += 180;
            }

            rawImage.transform.localEulerAngles = new Vector3(0f, 0f, camRotation);

            var videoRatio = (float)webcamTexture.width / (float)webcamTexture.height;
            aspectFitter.aspectRatio = videoRatio;

            if (webcamTexture.videoVerticallyMirrored)
            {
                rawImage.uvRect = new Rect(1, 0, -1, 1);
            }
            else
            {
                rawImage.uvRect = new Rect(0, 0, 1, 1);
            }
        }

        void OnDestroy()
        {
            if (webcamTexture != null)
            {
                webcamTexture.Stop();
                webcamTexture = null;
            }
        }
    }
}
