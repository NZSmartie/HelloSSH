using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloSSH.Services.Internal
{
    public class SshRequestIdentities : SshRequest
    {
        public SshRequestIdentities(SshAgent agent) 
            : base(SshAgentCode.RequestIdentities, agent)
        { }

        public override Task<byte[]> HandleAsync(byte[] data)
        {
            return Task.FromResult(new byte[] { (byte)SshAgentResult.IdentitiesAnswer, 0, 0, 0, 0 });
        }
    }
}
