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
    public class StoredProc
    {
        public string Name { get; set; }

        public StoredProc(string name){
            Name = name;
        }
    }

    [Parallel, Method("openInsight/GetStoredProcList")]
    public class GetCodeHandleRequest : IRequest<Container<StoredProc>?>
    {
        //public string Code { get; set; } = null!;
    }


    [Parallel, Method("openInsight/GetStoredProcList", Direction.ClientToServer)]
    public interface IGetCodeHandler : IJsonRpcRequestHandler<GetCodeHandleRequest, Container<StoredProc>?> { }


    public class GetOiStoredProcsHandler : IGetCodeHandler
    {
        OiClientFactory _oiClientFactory;

        public GetOiStoredProcsHandler (OiClientFactory oiClientFactory){
            _oiClientFactory = oiClientFactory;
        }


        public async Task<Container<StoredProc>?> Handle(GetCodeHandleRequest request, CancellationToken cancellationToken)
        {
            var client = await _oiClientFactory.GetClient();
            var procs = client.GetAllStoredProcs();

            List<StoredProc> sp = procs.Select(p=>new StoredProc(p)).ToList();
            return new Container<StoredProc>(sp.ToArray());
        }
    }
}
