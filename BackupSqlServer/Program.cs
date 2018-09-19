using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace BackupSqlServer
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            var server = new Server();
#else
            const string host = "****";
            const string user = "****";
            const string pass = "****";
            var connection = new ServerConnection(host, user, pass);
            var server = new Server(connection);
#endif
            string db = "****";
            string date = DateTime.Now.ToString("yyyyMMdd-HHmm");

            var scripter = new Scripter(server);
            scripter.Options.ScriptData = true;
            scripter.Options.ScriptSchema = false;

            var builder = new StringBuilder($"USE [{db}]\r\nGO\r\n");
            var database = server.Databases[db];

            var tables = new List<string>();
            foreach (Table table in database.Tables)
            {
                var list = GetForeignKeyTables(database.Tables, table.Name);
                foreach (string name in list)
                {
                    if (!tables.Contains(name))
                    {
                        tables.Add(name);
                    }
                }
            }


            foreach (var name in tables)
            {
                Table table = database.Tables[name];
                if (table.IsSystemObject || table.Name == "__MigrationHistory")
                {
                    continue;
                }

                Console.WriteLine(name);
                foreach (string s in scripter.EnumScript(new Urn[] { table.Urn }))
                {
                    builder.AppendLine(s);
                }
            }

            string path = System.Reflection.Assembly.GetEntryAssembly().Location;
            string dir = System.IO.Path.GetDirectoryName(path);
            System.IO.File.WriteAllText($"{dir}\\{db}_{date}.sql", builder.ToString());
        }

        private static IEnumerable<string> GetForeignKeyTables(TableCollection tables, string name, List<string> list = null)
        {
            if (list == null)
            {
                list = new List<string>();
            }

            var table = tables[name];
            foreach (ForeignKey key in table.ForeignKeys)
            {
                if (!list.Contains(key.Name))
                {
                    var l = GetForeignKeyTables(tables, key.ReferencedTable, list);
                    list.AddRange(l);
                }
            }
            list.Add(name);

            return list;
        }
    }
}
