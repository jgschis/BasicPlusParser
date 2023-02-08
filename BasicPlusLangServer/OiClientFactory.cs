using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace BasicPlusLangServer{
	public class OiClientFactory{

		readonly ILanguageServerConfiguration _config;
		readonly AsyncLazy<OiClient.Client> _client;
		
		public OiClientFactory(ILanguageServerConfiguration config){
			_config = config;
			_client = new AsyncLazy<OiClient.Client>(CreateClientFromConfig);
		}

		async Task<OiClient.Client> CreateClientFromConfig(){

			var config = await _config.GetConfiguration( new ConfigurationItem() {Section = "openInsight"});

			var username = config.GetValue<string>("openInsight:oiUsername");
			var appName = config.GetValue<string>("openInsight:oiApplicationName");
			var oiPath = config.GetValue<string>("openInsight:oiPath");
			var password = config.GetValue<string>("openInsight:oiPassword");

			var settings = new OiClient.ClientSettings() {OiPath = oiPath, AppName = appName, Username =  username, Password =password };

			return await OiClient.Client.CreateClient(settings);
		}

		public async Task<OiClient.Client> GetClient(){
			return await _client;
		}	
	}
}
