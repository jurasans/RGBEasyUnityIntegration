namespace Com.ISI.CaptureCards
{
    using Datapath.RGBEasy;
    using System;
    using System.Runtime.InteropServices;
    using UnityEngine;
    using UnityEngine.UI;

    public class CaptureTex : MonoBehaviour
    {

        //driver details
        private static IntPtr rgb = IntPtr.Zero;

        //unity specifics
        [SerializeField]
        private Image _img;

        private AudioClip _clip;
        private Texture2D _tex;

        //card misc vars
        private RGBERROR loadError = RGBERROR.NO_ERROR;

        private GCHandle gcHandle;

        //card callbacks
        private RGBMODECHANGEDFN m_ModeChangedFN;

        private RGBVALUECHANGEDFN m_ValueChangedFN;
        private RGBFRAMECAPTUREDFN m_cap;
        private IntPtr hRGBDLL;
        private IntPtr driver = IntPtr.Zero;

        //rendering details
        private IntPtr _texturePointer;

        private uint _size;
        private Color32[] _result, fix;
        private int w;
        private int h;

        private delegate void ChangeWindowSizeDelegate();

        public static void ModeChangedFN(IntPtr window, IntPtr capture, ref RGBMODECHANGEDINFO info, IntPtr userData)
        {
            Debug.LogError(info.RefreshRate + "X" + info.Size);
        }

        public static void ValueChangedFN(IntPtr window, IntPtr capture, ref RGBVALUECHANGEDINFO info, IntPtr userData)
        {
            Debug.LogError(info.CaptureHeight + "X" + info.CaptureWidth);
        }

        public void FrameCapedFn(IntPtr hWnd, IntPtr hRGB, IntPtr bitmapInfo, IntPtr bitmapBits, IntPtr userData)
        {
            Debug.Log("framecap");
            unsafe
            {
                BITMAPINFOHEADER* pBitmapInfoHeader = (BITMAPINFOHEADER*)bitmapInfo;
                /* TODO: Directly access BITMAPINFOHEADER here if required. */
                BITMAPINFOHEADER info = *pBitmapInfoHeader;
                _size = info.biSizeImage;
                _texturePointer = bitmapBits;
            }
            RGB.ChainOutputBuffer(rgb, bitmapInfo, bitmapBits);
        }

        private void OnApplicationQuit()
        {
            //RGB.SetFrameCapturedFn(rgb, m_cap, IntPtr.Zero);
            RGB.StopCapture(rgb);
            RGB.CloseInput(rgb);
            RGB.Free(hRGBDLL);
        }

        // Use this for initialization
        private void Start()
        {
            RGBERROR error = 0;
            error = RGB.Load(out hRGBDLL);
            if (error == RGBERROR.NO_ERROR)
            {
                //set base callbacks.
                Construct(hRGBDLL);
                //define buffers
            }
            else
            {
                string msg = "Load Failed: 0x" + error.ToString("X");
                Debug.LogError(msg);
            }
        }

        private void Construct(IntPtr hRGBDLL)
        {
            driver = hRGBDLL;
            if (driver != IntPtr.Zero)
            {
                loadError = RGB.OpenInput(0, out rgb);
                if (loadError == 0)
                {
                    m_ModeChangedFN = ModeChangedFN;
                    m_ValueChangedFN = ValueChangedFN;
                    RGB.SetModeChangedFn(rgb, m_ModeChangedFN, GCHandle.ToIntPtr(gcHandle));
                    RGB.SetValueChangedFn(rgb, m_ValueChangedFN, GCHandle.ToIntPtr(gcHandle));
                    RGB.SetFrameDropping(rgb, 0);
                    RGB.SetDMADirect(rgb, 0);
                    RGB.SetPixelformat(rgb, PIXELFORMAT.RGB565);
                    m_cap = FrameCapedFn;
                    RGB.SetFrameCapturedFn(rgb, m_cap, GCHandle.ToIntPtr(gcHandle));
                    RGB.DetectInput(rgb);
                    RGB.StartCapture(rgb);
                    
                }
            }
            uint w, h;
            RGB.GetOutputSize(rgb, out w, out h);
            this.w = (int)w;
            this.h = (int)h;
            _tex = new Texture2D(this.w, this.h, TextureFormat.RGB565, false);
            RGB.UseOutputBuffers(rgb, 1);
            _img.materialForRendering.mainTexture = _tex;
        }

        private void Update()
        {
            _tex.LoadRawTextureData(_texturePointer, (int)_size);

            _tex.Apply();

        }
    }
}