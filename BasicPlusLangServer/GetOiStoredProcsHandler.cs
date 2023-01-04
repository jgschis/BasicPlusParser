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
        OiClient.Client _client;

        public GetOiStoredProcsHandler (OiClient.Client client){
            _client = client;
        }


        public  Task<Container<StoredProc>?> Handle(GetCodeHandleRequest request, CancellationToken cancellationToken)
        {
            var procs = _client.GetAllStoredProcs();

            List<StoredProc> sp = procs.Select(p=>new StoredProc(p)).ToList();
            return Task.FromResult<Container<StoredProc>?>(new Container<StoredProc>(sp.ToArray()));
        }
    }
}
