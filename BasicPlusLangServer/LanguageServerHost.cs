using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Microsoft.Extensions.Logging;

namespace BasicPlusLangServer {

	public class LanguageServerHost: IDisposable{

		IServiceCollection? _services;
		LanguageServer? _server;
		LanguageServerOptions _options;

		public LanguageServerHost(){
			_options = new LanguageServerOptions()
				.WithInput(Console.OpenStandardInput())
				.WithOutput(Console.OpenStandardOutput())
				.ConfigureLogging(ConfigureLogging)
				.WithServices(ConfigureServices)
				.WithHandler<TextDocumentSyncHandler>()
				.WithHandler<SemanticTokenHandler>()
				.WithHandler<FoldingRangeHandler>()
				.WithHandler<HoverHandler>()
				.WithHandler<DefinitionHandler>()
				.WithHandler<ReferencesHandler>()
				.WithHandler<GetOiStoredProcsHandler>()
				.WithHandler<DocumentSymbolHandler>()
				//.WithHandler<CompletionHandler>()				
				.OnInitialize(Initialize);
		}

        void ConfigureServices(IServiceCollection services)
		{
			_services = services;
			_services.AddSingleton<TextDocumentManager>();

			var client = OiClient.Client.CreateClient(@"Q:\REVSOFT\OpenInsight10","TVCORYX","TVCORYX","4J94@9fkdjdk4###dd");
			_services.AddSingleton<OiClient.Client>(client);
		}

		void ConfigureLogging(ILoggingBuilder logBuilder){
			logBuilder.ClearProviders();
			logBuilder.AddLanguageProtocolLogging();
			logBuilder.SetMinimumLevel(LogLevel.Debug);
		}

		Task Initialize(ILanguageServer server, InitializeParams initializeParams,
			CancellationToken cancellationToken) {
				_services.AddSingleton<ILanguageServerFacade>(server);

				return Task.CompletedTask;
		}

		public async Task Start(){
			_server = await LanguageServer.From(_options);
			await _server.WaitForExit;
		}

		public void Dispose()
		{
			_server?.Dispose();
		}
	}
}