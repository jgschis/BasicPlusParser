using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Generation;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Generation;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusLangServer
{

    [Parallel, Method("openInsight/GetStoredProc")]
    public class GetStoredProcRequest : IRequest<string>
    {
        public string StoredProcName { get; set; }
    }


    [Parallel, Method("openInsight/GetStoredProc", Direction.ClientToServer)]
    public interface IGetStoredProcHandler : IJsonRpcRequestHandler<GetStoredProcRequest, string> { }


    public class GetOiStoredProcHandler : IGetStoredProcHandler
    {
        OiClientFactory _oiClientFactory;

        public GetOiStoredProcHandler (OiClientFactory oiClientFactory){
            _oiClientFactory = oiClientFactory;
        }

        public async Task<string> Handle(GetStoredProcRequest request, CancellationToken cancellationToken)
        {
            var client = await _oiClientFactory.GetClient();
            var sourceCode = client.GetStoredProc(request.StoredProcName);
            return sourceCode;
        }
    }
}
