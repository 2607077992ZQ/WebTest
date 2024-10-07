using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace SaveData.DataController
{
    public class DBHelper
    {
        public const string DataDB = "data/data.db";

        private string DBStr = $"Data Source={DataDB};Version=3;";

        public DataSet QueryDataSet(string sqlStr)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(DBStr))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(sqlStr, connection))
                    {
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {
                            DataSet ds = new DataSet();
                            adapter.Fill(ds);
                            return ds;
                        }
                    }
                }
            }
            catch (SQLiteException ex)
            {
                SaveErrorLog(sqlStr, ex.Message);
                return null;
            }
        }

        public DataTable QueryTable(string sqlStr)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(DBStr))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(sqlStr, connection))
                    {
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);
                            return table;
                        }
                    }
                }
            }
            catch (SQLiteException ex)
            {
                SaveErrorLog(sqlStr, ex.Message);
                return null;
            }
        }

        public int Execute(string sqlStr)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(DBStr))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(sqlStr, connection))
                    {
                        return command.ExecuteNonQuery();
                    }
                }
            }
            catch (SQLiteException ex)
            {
                SaveErrorLog(sqlStr, ex.Message);
                return -1;
            }
        }

        public object Command(string sqlStr)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(DBStr))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(sqlStr, connection))
                    {
                        return command.ExecuteScalar();
                    }
                }
            }
            catch (SQLiteException ex)
            {
                SaveErrorLog(sqlStr, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 本地缓存错误信息
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="error"></param>
        private void SaveErrorLog(string sql, string error)
        {
            string logfile = $"{Path.GetDirectoryName(DataDB)}/error/{DateTime.Now.ToString("yyyyMMdd")}.log";

            string log = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {sql} {error}";

            if (!File.Exists(logfile))
            {
                if (!File.Exists(Path.GetDirectoryName(logfile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logfile));
                }

                using (FileStream fs = File.Create(logfile))
                {
                    byte[] txtStream = Encoding.UTF8.GetBytes(log + "\r\n");
                    fs.Write(txtStream, 0, txtStream.Length);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(logfile))
                {
                    sw.WriteLine(log);
                }
            }

        }

    }
}
