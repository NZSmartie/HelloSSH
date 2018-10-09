using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HelloSSH.Services.Internal
{
    internal enum SshAgentCode : int
    {
        RequestIdentities = 11,
        SignRequest = 13,
        AddIdentity = 17,
        RemoveIdentity = 18,
        RemoveAllIdentities = 19,
        AddIdConstrained = 25,
        AddSmartcard_key = 20,
        RemoveSmartcard_key = 21,
        Lock = 22,
        Unlock = 23,
        AddSmartcardKeyConstrained = 26,
        Extension = 27,
    }

    internal enum SshAgentResult
    {
        Failure = 5,
        Success = 6,
        ExtensionFailure = 28,
        IdentitiesAnswer = 12,
        SignResponse = 14,
    }

    internal enum SshKeyConstraint
    {
        Lifetime = 1,
        Confirm = 2,
        Extension = 3,
    }

    [Flags]
    internal enum SshAgentSignitureFlags
    {
        Sha256 = 2,
        Sha512 = 4,
    }

    public abstract class SshRequest
    {
        public SshAgent Agent { get; protected set; }

        public int RequestCode { get; }

        internal SshRequest(SshAgentCode requestCode, SshAgent agent)
        {
            RequestCode = (int)requestCode;
            Agent = agent;
        }

        internal SshRequest(int requestCode, SshAgent agent)
        {
            RequestCode = requestCode;
            Agent = agent;
        }

        public abstract Task<byte[]> HandleAsync(byte[] data);
    }

    public class SshRequestFactory
    {
        private Dictionary<int, Type> _requestMap = new Dictionary<int, Type>();

        public SshRequestFactory()
        {
            RegisterRequest<SshRequestAddIdentity>(SshAgentCode.AddIdentity);
            RegisterRequest<SshRequestIdentities>(SshAgentCode.RequestIdentities);
        }

        public void RegisterRequest(int requestCode, Type requestType)
        {
            if (requestType == null)
                throw new ArgumentNullException(nameof(requestType));

            if (!requestType.IsSubclassOf(typeof(SshRequest)))
                throw new ArgumentException($"{requestType} does not inherit from the base case {typeof(SshRequest)}");

            if (_requestMap.ContainsKey(requestCode))
                throw new Exception($"Could not resgister request for {requestType}, requestCode ({requestCode}) has already been registered.");

            _requestMap.Add(requestCode, requestType);
        }

        #region RegisterRequest override aliases

        internal void RegisterRequest(SshAgentCode requestCode, Type requestType)
            => RegisterRequest((int)requestCode, requestType);

        public void RegisterRequest<TRequest>(int requestCode) where TRequest : SshRequest
            => RegisterRequest(requestCode, typeof(TRequest));

        internal void RegisterRequest<TRequest>(SshAgentCode requestCode) where TRequest : SshRequest
            => RegisterRequest((int)requestCode, typeof(TRequest));

        #endregion

        public SshRequest GetRequest(SshAgent agent, int requestCode)
        {
            if (!_requestMap.ContainsKey(requestCode))
                return null;

            var requestType = _requestMap[requestCode];
            var request = Activator.CreateInstance(requestType, agent) as SshRequest;

            System.Diagnostics.Debug.Assert(request is SshRequest);

            return request as SshRequest;
        }
    }
}
