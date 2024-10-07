using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveData.DataController
{
    public class DBInit
    {
        private string SQLData = DBHelper.DataDB;

        public void CreateDB()
        {
            if (!File.Exists(SQLData))
            {
                if (!Directory.Exists(SQLData))
                {
                    string directoryPath = Path.GetDirectoryName(SQLData);
                    Directory.CreateDirectory(directoryPath);
                }

                SQLiteConnection.CreateFile(SQLData);
                CreateTable();
            }
        }

        public void CreateTable()
        {
            DBHelper db = new DBHelper();

            StringBuilder str = new StringBuilder();
            str.Append("CREATE TABLE \"main\".\"webTable\"( \"Guid\" TEXT NOT NULL, \"url\" TEXT NOT NULL, \"createTime\" text, \"state\" integer, \"stateTime\" text, PRIMARY KEY (\"url\"));");
            db.Execute(str.ToString());
        }
    
        public DataTable SelectTable()
        {
            DBHelper db = new DBHelper();

            string sql = "select * from \"main\".\"webTable\";";
            return db.QueryTable(sql);
        }
    }
}
