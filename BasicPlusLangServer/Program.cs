using System.Diagnostics;

namespace BasicPlusLangServer{
	public class Program{
		public static async Task Main(string[]args){

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
