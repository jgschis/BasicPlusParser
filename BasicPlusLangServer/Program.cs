using System.Diagnostics;
using Serilog;

namespace BasicPlusLangServer{
	public class Program{
		public static async Task Main(string[]args){

			Log.Logger = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.CreateLogger();


			if (args.Length > 0 && args[0].ToLower() == "debug")
			{
				Debugger.Launch();
				while (!Debugger.IsAttached)
				{
					await Task.Delay(100);
				}
			}

			var langServerHost = new LanguageServerHost();
			await langServerHost.Start();
		}
	}
}
