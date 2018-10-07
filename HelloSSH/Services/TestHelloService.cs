using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace HelloSSH.WPF.Services
{
    public class TestHelloService
    {
        private const string MS_NGC_KEY_STORAGE_PROVIDER = "Microsoft Passport Key Storage Provider";
        private const string NCRYPT_ALGORITHM_GROUP_PROPERTY = "Algorithm Group";
        private const string NCRYPT_WINDOW_HANDLE_PROPERTY = "HWND Handle";
        private const string NCRYPT_USE_CONTEXT_PROPERTY = "Use Context";
        private const string NCRYPT_PIN_CACHE_IS_GESTURE_REQUIRED_PROPERTY = "PinCacheIsGestureRequired";
        private const string NCRYPT_LENGTH_PROPERTY = "Length";
        private const string NCRYPT_BLOCK_LENGTH_PROPERTY = "Block Length";

        private const int NCRYPT_PAD_PKCS1_FLAG = 0x00000002;

        [DllImport("cryptngc.dll", CharSet = CharSet.Unicode)]
        private static extern int NgcGetDefaultDecryptionKeyName(string pszSid, int dwReserved1, int dwReserved2, [Out] out string ppszKeyName);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int NCryptOpenStorageProvider([Out] out SafeNCryptProviderHandle phProvider, string pszProviderName, int dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int NCryptOpenKey(SafeNCryptProviderHandle hProvider, [Out] out SafeNCryptKeyHandle phKey, string pszKeyName, int dwLegacyKeySpec, CngKeyOpenOptions dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        internal static extern int NCryptGetProperty(SafeNCryptHandle hObject,
                                                           string pszProperty,
                                                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbOutput,
                                                           int cbOutput,
                                                           [Out] out int pcbResult,
                                                           CngKeyOpenOptions dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        internal static extern int NCryptGetProperty(SafeNCryptHandle hObject,
                                                           string pszProperty,
                                                           ref int pbOutput,
                                                           int cbOutput,
                                                           [Out] out int pcbResult,
                                                           CngKeyOpenOptions dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        internal static extern int NCryptGetProperty(SafeNCryptHandle hObject,
                                                           string pszProperty,
                                                           [Out] out IntPtr pbOutput,
                                                           int cbOutput,
                                                           [Out] out int pcbResult,
                                                           CngKeyOpenOptions dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int NCryptSetProperty(SafeNCryptHandle hObject, string pszProperty, string pbInput, int cbInput, CngPropertyOptions dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int NCryptSetProperty(SafeNCryptHandle hObject, string pszProperty, [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput, int cbInput, CngPropertyOptions dwFlags);

        [DllImport("ncrypt.dll")]
        private static extern int NCryptEncrypt(SafeNCryptKeyHandle hKey,
                                               [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput,
                                               int cbInput,
                                               IntPtr pvPaddingZero,
                                               [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbOutput,
                                               int cbOutput,
                                               [Out] out int pcbResult,
                                               int dwFlags);

        [DllImport("ncrypt.dll")]
        private static extern int NCryptDecrypt(SafeNCryptKeyHandle hKey,
                                               [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput,
                                               int cbInput,
                                               IntPtr pvPaddingZero,
                                               [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbOutput,
                                               int cbOutput,
                                               [Out] out int pcbResult,
                                               int dwFlags);

        private string _currentPassportKeyName;

        protected string CurrentPassportKeyName
        {
            get
            {
                if (string.IsNullOrEmpty(_currentPassportKeyName))
                    NgcGetDefaultDecryptionKeyName(WindowsIdentity.GetCurrent().User.Value, 0, 0, out _currentPassportKeyName);
                return _currentPassportKeyName;
            }
        }

        public IntPtr ParentHWind { get; set; } = IntPtr.Zero;

        public bool IsAvailable()
        {
            return !string.IsNullOrEmpty(CurrentPassportKeyName);
        }

        public byte[] Encrypt(byte[] data)
        {
            if (!IsAvailable())
                throw new NotSupportedException("Windows Hello is not available");

            byte[] cbResult;
            if (NCryptOpenStorageProvider(out var ngcProviderHandle, MS_NGC_KEY_STORAGE_PROVIDER, 0) < 0)
                throw new Exception("Could not open secure storage provider");

            using (ngcProviderHandle)
            {
                if (NCryptOpenKey(ngcProviderHandle, out var ngcKeyHandle, CurrentPassportKeyName, 0, CngKeyOpenOptions.Silent) < 0)
                    throw new Exception($"Could not retreive key for {CurrentPassportKeyName}");

                using (ngcKeyHandle)
                {
                    int pcbResult, ngcResult;

                    // Perform encryption operation
                    ngcResult = NCryptEncrypt(ngcKeyHandle, data, data.Length, IntPtr.Zero, null, 0, out pcbResult, NCRYPT_PAD_PKCS1_FLAG);
                    if (ngcResult < 0)
                        throw new Exception($"Could not encrypted requested data. Error Code: 0x{ngcResult:X}");

                    // Allowcate the output buffer size
                    cbResult = new byte[pcbResult];

                    // Perform encryption operation
                    ngcResult = NCryptEncrypt(ngcKeyHandle, data, data.Length, IntPtr.Zero, cbResult, cbResult.Length, out pcbResult, NCRYPT_PAD_PKCS1_FLAG);
                    if (ngcResult< 0)
                        throw new Exception($"Could not encrypted requested data. Error Code: 0x{ngcResult:X}");

                    System.Diagnostics.Debug.WriteLine($"cbResult.Length: {cbResult.Length}, pcbResult: {pcbResult}");
                    System.Diagnostics.Debug.Assert(cbResult.Length == pcbResult);
                }
            }

            return cbResult;
        }

        // TODO: define error codes
        public byte[] PromptToDecrypt(byte[] data, string prompt = null)
        {
            if (!IsAvailable())
                throw new NotSupportedException("Windows Hello is not available");

            byte[] cbResult;

            if (NCryptOpenStorageProvider(out var ngcProviderHandle, MS_NGC_KEY_STORAGE_PROVIDER, 0) < 0)
                throw new Exception("Could not open storage provider for Windows Hello");

            using (ngcProviderHandle)
            {
                if (NCryptOpenKey(ngcProviderHandle, out var ngcKeyHandle, CurrentPassportKeyName, 0, CngKeyOpenOptions.None) < 0)
                    throw new Exception("Could not open Windows Hello");

                using (ngcKeyHandle)
                {
                    if (ParentHWind != IntPtr.Zero)
                    {
                        byte[] handle = BitConverter.GetBytes(IntPtr.Size == 8 ? ParentHWind.ToInt64() : ParentHWind.ToInt32());
                        if (NCryptSetProperty(ngcKeyHandle, NCRYPT_WINDOW_HANDLE_PROPERTY, handle, handle.Length, CngPropertyOptions.None) < 0)
                            throw new Exception("Could not set parent window for Windows Hello");
                    }

                    if (!string.IsNullOrEmpty(prompt))
                    {
                        if (NCryptSetProperty(ngcKeyHandle, NCRYPT_USE_CONTEXT_PROPERTY, prompt, (prompt.Length + 1) * 2, CngPropertyOptions.None) < 0)
                            throw new Exception("Failed to set prompt for Windows Hello");
                    }

                    byte[] pinRequired = BitConverter.GetBytes(1);
                    if (NCryptSetProperty(ngcKeyHandle, NCRYPT_PIN_CACHE_IS_GESTURE_REQUIRED_PROPERTY, pinRequired, pinRequired.Length, CngPropertyOptions.None) < 0)
                        throw new Exception();

                    // The pbInput and pbOutput parameters can point to the same buffer. In this case, this function will perform the decryption in place.
                    cbResult = new byte[data.Length * 2];
                    if (NCryptDecrypt(ngcKeyHandle, data, data.Length, IntPtr.Zero, cbResult, cbResult.Length, out var pcbResult, NCRYPT_PAD_PKCS1_FLAG) < 0)
                        throw new Exception("Could not decrypt data");

                    // TODO: secure resize
                    Array.Resize(ref cbResult, pcbResult);
                }
            }

            return cbResult;
        }
    }
}
