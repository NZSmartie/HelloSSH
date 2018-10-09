using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloSSH.Services.Internal
{
    public class SshRequestAddIdentity : SshRequest
    {
        public SshRequestAddIdentity(SshAgent agent) 
            : base(SshAgentCode.AddIdentity, agent)
        { }

        public override Task<byte[]> HandleAsync(byte[] data)
        {
            return Task.FromResult(new byte[] { (byte)SshAgentResult.Failure });
        }
    }
}
