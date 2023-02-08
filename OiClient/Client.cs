using RevelationNetCore_10.NetOI;

namespace OiClient {
    public class Client {
        Server _server;
        Dictionary<string, string> _inheritanceChain = new Dictionary<string, string>();


        Client(Server server) {
            _server = server;
        }

        public string? GetStoredProc(string storedProcName, string appName = "") {

            OIFile file = _server.OpenFile("SYSPROCS");

            if (appName == "" ) {
                appName = _server.ApplicationName;
            }

            foreach (var app in GetInheritanceChain(appName)) {
                
                string key = $"{storedProcName}*{app}".ToUpper();
                if (app == "SYSPROG") {
                    key = $"{storedProcName}".ToUpper();
                }

                OIRecord record = _server.ReadRecord(file, key);

                if (record != null && record.Record != null && record.Record.Length > 1 && record.Record[0] != "") {
                    return string.Join(Environment.NewLine, record.Record);
                }
            }

            return null;
        }

       public List<string> GetAllStoredProcs() {
            List<string> storedProcs = new();

            OIFile file = _server.OpenFile("SYSPROCS");

            var keyList = _server.SelectFile(file);
            if (keyList == null) {
                return storedProcs;
            }

            foreach (var storedProc in keyList) {
                if (storedProc is string storedProcName) {
                    storedProcs.Add(storedProcName);
                }
            }
            return storedProcs;
        }


        IEnumerable<string> GetInheritanceChain(string appName) {
            if (string.IsNullOrWhiteSpace(appName)) {
                throw new ArgumentException("parameter must not be blank or null.", nameof(appName));
            }

            yield return appName;

            while (appName != "SYSPROG") {

                if (!_inheritanceChain.TryGetValue(appName, out string parent)) {
                    var args = new string[] { appName };
                    var response = _server.CallFunction("GET_APP_INFO", ref args);

                    if (response.Length < 3) {
                        throw new InvalidOperationException($"GET_APP_INFO should return at least 3 return values, but it returned {response.Length} value/s.");
                    }

                    parent = response[2];
                    _inheritanceChain.Add(appName, parent);
                }

                appName = parent;
                yield return parent;
            }
        }

        public string? GetInsert(string insertName, string appName = "") {

            return GetStoredProc(insertName, appName);
        }

        public async static Task<Client> CreateClient(ClientSettings settings) {
            var server = await CreateServer(settings);
            var client = new Client(server);
            return client;
        }


        public static string GetAppNameFromFileName(string fileName) {
            string appName;
            int appNameStartPos = fileName.IndexOf("[");
            int appNameEndPos = fileName.IndexOf("]");
            if (appNameStartPos != -1 && appNameEndPos != -1 && appNameStartPos < appNameEndPos && appNameStartPos < fileName.Length - 1) {
                appName = fileName.Substring(appNameStartPos + 1, (appNameEndPos - appNameStartPos) - 1).ToUpper();
            } else {
                int starPos = fileName.IndexOf("*");
                if (starPos != -1 && starPos < fileName.Length - 1) {
                    appName = fileName.Substring(starPos + 1).ToUpper();
                } else {
                    appName = "";
                }
            }
            return appName;
        }


        static async Task<Server> CreateServer(ClientSettings settings){
            return await Task.Run( () => {
                Server server = new();
                server.OIConnect(settings.OiPath, "", settings.AppName, settings.Username, settings.Password );
                return server;
            });
        }
    }
    

    public class ClientSettings {
        public string OiPath {get;set;}
        public string AppName {get;set;}
        public string Username {get;set;}
        public string Password {get;set;}
    }
}
