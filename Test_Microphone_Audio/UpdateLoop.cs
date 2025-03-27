using System;
using System.Diagnostics;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace Test_Microphone_Audio
{
    public class UpdateLoop
    {
        private static UpdateLoop? _instance;
        private DispatcherTimer _timer;
        private event Action OnUpdate;
        private event Action<float, float> OnVolumeFrequencyUpdate;
        
        // Audio analysis values
        private float _currentVolume;
        private float _dominantFrequency;
        private float _lastValidFrequency;

        // Windows Core Audio API imports
        [DllImport("winmm.dll")]
        private static extern int waveInOpen(out IntPtr hWaveIn, int uDeviceID, ref WaveFormat lpFormat, 
            WaveInProcDelegate dwCallback, IntPtr dwCallbackInstance, int dwFlags);

        [DllImport("winmm.dll")]
        private static extern int waveInPrepareHeader(IntPtr hWaveIn, ref WaveHdr lpWaveInHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveInAddBuffer(IntPtr hWaveIn, ref WaveHdr lpWaveInHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveInStart(IntPtr hWaveIn);

        [DllImport("winmm.dll")]
        private static extern int waveInStop(IntPtr hWaveIn);

        [DllImport("winmm.dll")]
        private static extern int waveInClose(IntPtr hWaveIn);

        [DllImport("winmm.dll")]
        private static extern int waveInUnprepareHeader(IntPtr hWaveIn, ref WaveHdr lpWaveInHdr, int uSize);

        // Callback for waveIn messages
        private delegate void WaveInProcDelegate(IntPtr hWaveIn, int uMsg, IntPtr dwInstance, IntPtr wParam, IntPtr lParam);

        // Constants for waveIn flags
        private const int CALLBACK_FUNCTION = 0x00030000;
        private const int WAVE_FORMAT_PCM = 1;
        private const int WAVE_MAPPER = -1;

        // Wave format structure
        [StructLayout(LayoutKind.Sequential)]
        private struct WaveFormat
        {
            public short wFormatTag;
            public short nChannels;
            public int nSamplesPerSec;
            public int nAvgBytesPerSec;
            public short nBlockAlign;
            public short wBitsPerSample;
            public short cbSize;
        }

        // Wave header structure
        [StructLayout(LayoutKind.Sequential)]
        private struct WaveHdr
        {
            public IntPtr lpData;
            public int dwBufferLength;
            public int dwBytesRecorded;
            public IntPtr dwUser;
            public int dwFlags;
            public int dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        // Audio capture variables
        private IntPtr _waveInHandle;
        private WaveInProcDelegate _waveInProc;
        private GCHandle _bufferHandle;
        private WaveHdr _waveHeader;
        private byte[] _buffer;
        private const int BUFFER_SIZE = 8192;

        private UpdateLoop()
        {
            // Initialize audio capture
            InitializeAudioCapture();

            // Initialize timer for UI updates
            _timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / 60)
            };
            _timer.Tick += (s, e) => 
            {
                OnUpdate?.Invoke();
                OnVolumeFrequencyUpdate?.Invoke(_currentVolume, _dominantFrequency);
            };
            _timer.Start();
        }

        private void InitializeAudioCapture()
        {
            // Create wave format structure
            WaveFormat format = new WaveFormat
            {
                wFormatTag = WAVE_FORMAT_PCM,
                nChannels = 1, // Mono
                nSamplesPerSec = 44100, // 44.1kHz
                wBitsPerSample = 16, // 16-bit
                cbSize = 0
            };

            // Calculate block align and average bytes per sec
            format.nBlockAlign = (short)(format.nChannels * (format.wBitsPerSample / 8));
            format.nAvgBytesPerSec = format.nSamplesPerSec * format.nBlockAlign;

            // Create the callback function for audio data
            _waveInProc = new WaveInProcDelegate(WaveInProc);

            // Allocate buffer for audio data
            _buffer = new byte[BUFFER_SIZE];
            _bufferHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);

            // Create wave header
            _waveHeader = new WaveHdr
            {
                lpData = _bufferHandle.AddrOfPinnedObject(),
                dwBufferLength = BUFFER_SIZE,
                dwFlags = 0,
                dwLoops = 0
            };

            // Open wave input device
            int result = waveInOpen(out _waveInHandle, WAVE_MAPPER, ref format, _waveInProc, IntPtr.Zero, CALLBACK_FUNCTION);
            if (result != 0)
            {
                Debug.WriteLine($"Failed to open audio input device. Error code: {result}");
                return;
            }

            // Prepare header
            result = waveInPrepareHeader(_waveInHandle, ref _waveHeader, Marshal.SizeOf(_waveHeader));
            if (result != 0)
            {
                Debug.WriteLine($"Failed to prepare header. Error code: {result}");
                return;
            }

            // Add buffer
            result = waveInAddBuffer(_waveInHandle, ref _waveHeader, Marshal.SizeOf(_waveHeader));
            if (result != 0)
            {
                Debug.WriteLine($"Failed to add buffer. Error code: {result}");
                return;
            }

            // Start recording
            result = waveInStart(_waveInHandle);
            if (result != 0)
            {
                Debug.WriteLine($"Failed to start recording. Error code: {result}");
                return;
            }
        }

        private void WaveInProc(IntPtr hWaveIn, int uMsg, IntPtr dwInstance, IntPtr wParam, IntPtr lParam)
        {
            const int WIM_DATA = 0x3C0;

            if (uMsg == WIM_DATA)
            {
                // Calculate volume
                _currentVolume = CalculateVolume(_buffer, _waveHeader.dwBytesRecorded);
                
                // Calculate dominant frequency
                _dominantFrequency = CalculateDominantFrequency(_buffer, _waveHeader.dwBytesRecorded);

                // Return buffer to the input queue
                waveInAddBuffer(_waveInHandle, ref _waveHeader, Marshal.SizeOf(_waveHeader));
            }
        }
        
        private float CalculateDominantFrequency(byte[] buffer, int bytesRecorded)
        {
            // Simple zero-crossing method to estimate frequency
            // This is a basic approach but works for pure tones
            
            const int sampleRate = 44100;
            int sampleCount = bytesRecorded / 2; // 16-bit samples
            if (sampleCount < 100) return _lastValidFrequency; // Not enough data, return last valid frequency
            
            // Convert bytes to samples and find zero crossings
            int crossings = 0;
            bool wasPositive = false;
            bool firstSample = true;
            
            for (int i = 0; i < bytesRecorded - 1; i += 2)
            {
                // Convert two bytes to a 16-bit sample
                short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                
                bool isPositive = sample >= 0;
                
                if (firstSample)
                {
                    wasPositive = isPositive;
                    firstSample = false;
                    continue;
                }
                
                // Count when we cross from positive to negative or vice versa
                if (wasPositive != isPositive)
                {
                    crossings++;
                    wasPositive = isPositive;
                }
            }
            
            // Each complete wave has 2 zero crossings
            float frequency = (crossings * sampleRate) / (2.0f * sampleCount);
            
            // Filter out unreasonable values and noise
            if (frequency < 20 || frequency > 20000 || _currentVolume < 0.01f)
            {
                return _lastValidFrequency; // Return last valid frequency instead of 0
            }
            
            // Store this as our last valid frequency
            _lastValidFrequency = frequency;
            return frequency;
        }

        private float CalculateVolume(byte[] buffer, int bytesRecorded)
        {
            // Calculate Mean Square of the samples instead of Root Mean Square
            // For 16-bit audio (2 bytes per sample)
            int sampleCount = bytesRecorded / 2;
            if (sampleCount == 0) return 0;

            float sumOfSquares = 0;

            for (int i = 0; i < bytesRecorded; i += 2)
            {
                // Convert two bytes to a 16-bit sample
                short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                
                // Convert to float (-1.0 to 1.0 range)
                float normalizedSample = sample / 32768f;
                
                // Square the sample and add to sum
                sumOfSquares += normalizedSample * normalizedSample;
            }

            // Return Mean Square value directly (no square root)
            return sumOfSquares / sampleCount;
        }

        public static UpdateLoop Instance => _instance ??= new UpdateLoop();

        // Original methods
        public void Subscribe(Action updateAction)
        {
            OnUpdate += updateAction;
        }

        public void Unsubscribe(Action updateAction)
        {
            OnUpdate -= updateAction;
        }

        // Methods for volume and frequency updates
        public void SubscribeWithVolumeAndFrequency(Action<float, float> updateAction)
        {
            OnVolumeFrequencyUpdate += updateAction;
        }

        public void UnsubscribeWithVolumeAndFrequency(Action<float, float> updateAction)
        {
            OnVolumeFrequencyUpdate -= updateAction;
        }

        // Get current values
        public float GetCurrentVolume()
        {
            return _currentVolume;
        }
        
        public float GetCurrentFrequency()
        {
            return _dominantFrequency;
        }

        // Clean up resources
        public void Dispose()
        {
            if (_waveInHandle != IntPtr.Zero)
            {
                waveInStop(_waveInHandle);
                waveInUnprepareHeader(_waveInHandle, ref _waveHeader, Marshal.SizeOf(_waveHeader));
                waveInClose(_waveInHandle);
            }

            if (_bufferHandle.IsAllocated)
            {
                _bufferHandle.Free();
            }

            _timer.Stop();
        }
    }
}