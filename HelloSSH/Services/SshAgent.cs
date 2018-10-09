using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows.Forms;

using HelloSSH.Services.Internal;

namespace HelloSSH.Services
{
    public class SshAgent : IDisposable
    {
        private readonly SshRequestFactory _requestFactory;
        private readonly Thread _pageantThread;

        private ApplicationContext _pageantAppContext;
        private PeageantWindow _pageantWindow;

        public SshAgent()
        {
            _requestFactory = new SshRequestFactory();

            _pageantThread = new Thread(StartPageantAppContext);
            _pageantThread.Start();
        }

        public async Task<byte[]> ProcessRequestAsync(byte[] request)
        {
            int requestCode = request[0];
            var requestHandler = _requestFactory.GetRequest(this, requestCode);
            if (requestHandler == null)
                return new byte[] { (byte)SshAgentResult.Failure };

            var result = await requestHandler.HandleAsync(request);
            if(result == null)
                return new byte[] { (byte)SshAgentResult.Failure };

            return result;
        }

        private void StartPageantAppContext()
        {
            _pageantAppContext = new ApplicationContext();
            _pageantWindow = new PeageantWindow(this);

            Application.Run(_pageantAppContext);

            _pageantWindow.Dispose();
        }

        public void Dispose()
        {
            _pageantAppContext.ExitThread();
            _pageantThread.Join();

            _pageantWindow.Dispose();
        }
    }
}
