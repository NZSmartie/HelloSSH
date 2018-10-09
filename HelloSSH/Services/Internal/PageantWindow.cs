using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace HelloSSH.Services.Internal
{
    internal partial class PeageantWindow : IDisposable
    {
        private WndProc _wndProcDelegate;
        private IntPtr _hwnd;

        private static readonly SecurityIdentifier _user = WindowsIdentity.GetCurrent().User;
        private readonly SshAgent _agent;

        public PeageantWindow(SshAgent agent)
        {
            _wndProcDelegate = PegeantWndProc;

            // Create WNDCLASS
            WndClass windClass = new WndClass();
            windClass.lpszClassName = CLASS_NAME_PAGEANT;
            windClass.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);

            var classAtom = RegisterClassW(ref windClass);

            int lastError = Marshal.GetLastWin32Error();

            if (classAtom == 0 && lastError != ERROR_CLASS_ALREADY_EXISTS)
            {
                throw new Exception("Could not register window class");
            }

            // Create window
            _hwnd = CreateWindowExW(
                0,
                CLASS_NAME_PAGEANT,
                CLASS_NAME_PAGEANT,
                0,
                0,
                0,
                0,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero
            );
            _agent = agent;
        }

        private IntPtr PegeantWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg != WM_COPYDATA)
                return DefWindowProcW(hWnd, msg, wParam, lParam);

            // convert lParam to something usable
            CopyData copyData = Marshal.PtrToStructure<CopyData>(lParam);

            var isCopyDataRequest = IntPtr.Size == 4
                ? (copyData.dwData.ToInt32() == AGENT_COPYDATA_ID)
                : (copyData.dwData.ToInt64() == AGENT_COPYDATA_ID);

            if (!isCopyDataRequest)
                return IntPtr.Zero; 
            

            string mapName = Marshal.PtrToStringAnsi(copyData.lpData);

            if (mapName.Length != copyData.cbData - 1)
                return IntPtr.Zero;

            using (var fileMap = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.FullControl))
            {
                if (fileMap.SafeMemoryMappedFileHandle.IsInvalid)
                    return IntPtr.Zero;

                var mapOwner = fileMap.GetAccessControl().GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;

                // Maintain backards combatability with PuTTY 0.6.0 (and WinSCP) 
                // see http://www.chiark.greenend.org.uk/~sgtatham/putty/wishlist/pageant-backwards-compatibility.html
                var processOwner = GetProcessOwner(Process.GetCurrentProcess());

                //Process otherProcess = null;
                //try
                //{
                //    if (RestartManager.StartSession(out var rmSessionHandle, 0, Guid.NewGuid().ToString()) != RmResult.ERROR_SUCCESS)
                //        throw new Exception("Could not start session to determin file locks.");

                //    if(RestartManager.RegisterResources(rmSessionHandle, 1, new string[] { mapName }, 0, null, 0, null) != RmResult.ERROR_SUCCESS)
                //        throw new Exception("Could not register resource");

                //    var processCount = 1u; // There should only be one process locking the file
                //    var processes = new RM_PROCESS_INFO[processCount];
                //    if (RestartManager.GetList(rmSessionHandle, out var foundProcesses, ref processCount, processes, out var rebootReason) != RmResult.ERROR_SUCCESS)
                //        throw new Exception("at least you tried");

                //    otherProcess = Process.GetProcessById(processes[0].Process.dwProcessId);

                //    //herProcess = WinInternals.FindProcessWithMatchingHandle(fileMap);
                //}
                //catch (Exception ex)
                //{
                //    Debug.Fail(ex.ToString());
                //}

                if (_user == mapOwner || processOwner == mapOwner)
                {
                    using (var stream = fileMap.CreateViewStream())
                    {
                        var requestLength = new byte[4];
                        stream.Read(requestLength, 0, 4);
                        if(BitConverter.IsLittleEndian)
                            Array.Reverse(requestLength);

                        var message = new byte[BitConverter.ToInt32(requestLength, 0)];
                        stream.Read(message, 0, message.Length);

                        byte[] response = new byte[] { (byte)SshAgentResult.Failure };

                        try
                        {
                            response = _agent.ProcessRequestAsync(message).ConfigureAwait(true).GetAwaiter().GetResult();
                        }
                        catch(Exception)
                        {
                            response = new byte[] { (byte)SshAgentResult.Failure };
                            
                            // TODO: Retrow this exception without interrupting the SSH client
                            //throw;
                        }

                        var responseLength = BitConverter.GetBytes(response.Length);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(responseLength);

                        stream.Seek(0, SeekOrigin.Begin);
                        stream.Write(responseLength, 0, responseLength.Length);
                        stream.Write(response, 0, response.Length);
                        stream.Flush();
                    }

                    return new IntPtr(1);
                }
            }
            
            return IntPtr.Zero;

        }

        private static SecurityIdentifier GetProcessOwner(Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                OpenProcessToken(process.Handle, 8, out processHandle);
                WindowsIdentity wi = new WindowsIdentity(processHandle);
                return wi.Owner;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                    CloseHandle(processHandle);
                
            }
        }

        public void Dispose()
        {
            if (_hwnd != IntPtr.Zero)
            {
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }
    }
}
