using System;
using System.Collections.Generic;
using RevelationNetCore_10.NetOI;

namespace OiClient {
    public class Client {
        Server _server;
        List<string> _inheritaneChain;
        

        Client(Server server) {
            _server = server;
        }

        public string GetStoredProc(string name) {

            OIFile file = _server.OpenFile("SYSPROCS");

            foreach (var app in GetInheritanceChain()) {
                string key = $"{name}*{app}".ToUpper();

                OIRecord record = _server.ReadRecord(file, key);

                if (record.Record.Length > 1 && record.Record[0] != "") {
                    return string.Join(Environment.NewLine, record.Record);
                }
            }

            return "";
        }

       public List<string> GetAllStoredProcs() {

            OIFile file = _server.OpenFile("SYSPROCS");

            var keylist = _server.SelectFile(file);
            List<string> procs = new();
            foreach (var proc in keylist) {
                procs.Add(proc.ToString());
            }
            return procs;
        }

        List<string> GetInheritanceChain() {
            if (_inheritaneChain == null) {
                string app = _server.ApplicationName;

                _inheritaneChain = new();
                _inheritaneChain.Add(app);

                while (app != "SYSPROG") {
                    var args = new string[] { app };
                    var response = _server.CallFunction("GET_APP_INFO", ref args);
                    
                    if (response.Length < 3) {
                        throw new InvalidOperationException("TODO");
                    }

                    app = response[2];
                    _inheritaneChain.Add(app);
                }
            }

            return _inheritaneChain;

        }

        public string GetInsert(string name) {
            return GetStoredProc(name);
        }

        public static Client CreateClient(string OiPath, string appName, string username, string password) {

            Server server = new Server();
            server.OIConnect(OiPath, "", appName, username, password);
            
            var client = new Client(server);

            return client;
        }
    }
}
