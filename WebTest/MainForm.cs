using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;

namespace WebTest
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        #region
        private static string ConfigData = Environment.CurrentDirectory + @"\data\config.ini";
        private string tempPath = Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine);
        private List<Thread> ThreadPool = new List<Thread>();

        private enum StateEnum
        {
            normal,     //正常
            notfound,   //404
            other,      //503等异常
            edit        //修改值
        }
        #endregion


        private void MainForm_Load(object sender, EventArgs e)
        {
            versionToolStripMenuItem.Text += Application.ProductVersion;

            new Thread(() =>
            {
                SaveData.DataController.DBInit Init = new SaveData.DataController.DBInit();
                Init.CreateDB();

                Init_UI();
            })
            { IsBackground = true }.Start();

        }

        private void Init_UI()
        {
            new Thread(() =>
            {
                SaveData.DataController.DBInit Init = new SaveData.DataController.DBInit();
                var webTable = Init.SelectTable();

                foreach (DataRow item in webTable.Rows)
                {
                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(dataGridView1);

                    row.Tag = item["Guid"].ToString();
                    row.Cells[(int)DataInfoCell_1.Column1].Value = item["url"].ToString();
                    row.Cells[(int)DataInfoCell_1.Column2].Value = item["createTime"] == DBNull.Value ? "" : item["createTime"].ToString();
                    row.Cells[(int)DataInfoCell_1.Column3].Value = item["state"] == DBNull.Value ? "" : WebState((StateEnum)Convert.ToInt32(item["state"].ToString()));
                    row.Cells[(int)DataInfoCell_1.Column4].Value = item["stateTime"] == DBNull.Value ? "" : item["stateTime"].ToString();

                    BeginInvoke(new Action(delegate
                    {
                        dataGridView1.Rows.Add(row);
                    }));
                }
            })
            { IsBackground = true }.Start();
        }

        private string WebState(StateEnum state)
        {
            string str = string.Empty;

            switch (state)
            {
                case StateEnum.normal: str = "正常"; break;
                case StateEnum.notfound: str = "404"; break;
                case StateEnum.other: str = "无法访问"; break;
                case StateEnum.edit: str = "站点关闭"; break;
                default: str = "异常情况"; break;
            }
            return str;
        }
    
        private void ShowLog(string txt)
        {
            txtlog.Text += txt + "\r\n";
        }

        private StateEnum GetHttpConnect(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url.Trim().Replace("\r\n", ""));
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd().ToLower();
                if (responseString.Contains("<title>系统维护</title>"))
                    return StateEnum.edit;
                else
                    return StateEnum.normal;
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("404"))
                    return StateEnum.notfound;
                else
                    return StateEnum.other;
            }
        }

        private enum DataInfoCell_1
        {
            Column1,    //url
            Column2,    //create
            Column3,    //state
            Column4,    //time
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ShowLog("测试开始...");
            button4_Click(null, null);
            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                var th = new Thread(() =>
                {
                    var value = GetHttpConnect(item.Cells[((int)DataInfoCell_1.Column1)].Value.ToString());
                    string time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                    BeginInvoke(new Action(delegate
                    {
                        item.Cells[(int)DataInfoCell_1.Column3].Value = WebState(value);
                        item.Cells[(int)DataInfoCell_1.Column4].Value = time;
                    }));

                    SaveData.DataController.DBHelper db = new SaveData.DataController.DBHelper();
                    db.Execute($"UPDATE main.webTable SET state={(int)value},stateTime='{time}' WHERE Guid = '{item.Tag}'");

                })
                { IsBackground = true };
                th.Start();
                ThreadPool.Add(th);
            }

            new Thread(() =>
            {
                foreach (var item in ThreadPool)
                {
                    item.Join();
                }

                BeginInvoke(new Action(delegate
                {
                    button3_Click(null, null);
                    ShowLog("任务结束。");
                }));
                ThreadPool.Clear();

            })
            { IsBackground = true }.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtlog.Text = string.Empty;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                item.Cells[(int)DataInfoCell_1.Column3].Value = null;
                item.Cells[(int)DataInfoCell_1.Column4].Value = null;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveData.DataController.DBHelper db = new SaveData.DataController.DBHelper();
            ShowLog($"正常站点:{db.Command("SELECT COUNT(*) FROM main.webTable WHERE state = '0'")}个");
            ShowLog($"异常站点:{db.Command("SELECT COUNT(*) FROM main.webTable WHERE state <> '0'")}个");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                foreach (var item in ThreadPool)
                {
                    try
                    {
                        item.Abort();
                    }
                    catch (Exception)
                    {

                    }
                }
            })
            { IsBackground = true }.Start();
        }
        
        private void button7_Click(object sender, EventArgs e)
        {
            ShowLog($"[{Guid.NewGuid().ToString()}]");
        }

        private void 站点导入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IWorkbook workbook = new HSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("web");
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("在下方写入url地址");

            string path = tempPath + $"\\{DateTime.Now.ToString("yyyyMMddHHmmss")}.xls";
            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fileStream);
            }
            Process.Start(path);

            new Thread(() =>
            {
                var th = IsOpenAndClose(path);
                th.Join();

                AddUrlINFO(path);
                File.Delete(path);
            })
            { IsBackground = true }.Start();
        }

        private void RemoveUrlInfo(string path)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                StringBuilder NotDel = new StringBuilder();

                IWorkbook workbook = new HSSFWorkbook(fileStream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);
                    if (row != null)
                    {
                        ICell cell = row.GetCell(0);
                        if (cell != null)
                        {
                            if (NotDel.ToString() == string.Empty)
                            {
                                NotDel.Append($"'{cell.StringCellValue}'");
                            }
                            else
                                NotDel.Append($",'{cell.StringCellValue}'");
                        }
                    }
                }

                if (NotDel.ToString() != string.Empty)
                {
                    SaveData.DataController.DBHelper db = new SaveData.DataController.DBHelper();
                    int i = db.Execute($"DELETE FROM main.webTable WHERE url NOT IN ({NotDel})");
                }
            }

        }

        private void AddUrlINFO(string path)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new HSSFWorkbook(fileStream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);
                    if (row != null)
                    {
                        ICell cell = row.GetCell(0);
                        if (cell != null)
                        {
                            string cellValue = cell.StringCellValue;
                            SaveData.DataController.DBHelper db = new SaveData.DataController.DBHelper();
                            int i = db.Execute($"INSERT INTO main.webTable VALUES('{Guid.NewGuid().ToString()}','{cellValue}','{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}',NULL,NULL)");
                        }
                    }
                }
            }
            button1_Click(null,null);
        }

        private void 站点移除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IWorkbook workbook = new HSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("web");
           
            SaveData.DataController.DBHelper db = new SaveData.DataController.DBHelper();
            var table = db.QueryTable("SELECT * FROM \"main\".\"webTable\"");
            int rows = 0;
            foreach (DataRow item in table.Rows)
            {
                IRow row = sheet.CreateRow(rows);
                row.CreateCell(0).SetCellValue(item["url"].ToString());
            }
            string path = tempPath + $"\\{DateTime.Now.ToString("yyyyMMddHHmmss")}.xls";
            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fileStream);
            }
            Process.Start(path);

            new Thread(() =>
            {
                var th = IsOpenAndClose(path);
                th.Join();

                RemoveUrlInfo(path);
                File.Delete(path);
            })
            { IsBackground = true }.Start();

        }

        /// <summary>
        /// 已经打开文件并且已经关闭了该文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private Thread IsOpenAndClose(string path)
        {
            Thread th1 = new Thread(() =>
            {
                while (true)
                {
                lable1:
                    Thread.Sleep(1000);
                    try
                    {
                        using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            goto lable1;
                        }
                    }
                    catch (Exception)
                    {
                        //MessageBox.Show("ok");
                        break;
                    }
                }

            })
            { IsBackground = true, Name = "open" };

            th1.Start();
            Thread th2 = new Thread(() =>
            {
                th1.Join();
                while (true)
                {
                    try
                    {
                        using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            break;
                        }
                    }
                    catch (IOException)
                    {
                    }
                    Thread.Sleep(1000);
                }
            })
            { IsBackground = true, Name = "close" };
            th2.Start();

            return th2;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ThreadPool.Clear();
            this.Dispose();
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            Init_UI();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            SaveData.SystemDLL configINI = new SaveData.SystemDLL();
            configINI.PATH = ConfigData;

            SetTime time = new SetTime();
            if (File.Exists(ConfigData))
            {
                try
                {
                    time.Time = Convert.ToDateTime(configINI.INIRead("TIME"));
                }
                catch (Exception)
                {
                    time.Time = DateTime.Now;
                }
            }
            
            if (time.ShowDialog() == DialogResult.OK)
            {
                DateTime timeData = time.Time;
                configINI.INIWrite("TIME", $"{timeData.Hour}:{timeData.Minute}:{timeData.Second}");
            }
            time.Dispose();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string err = "还未设置计划时间!";

            if (File.Exists(ConfigData))
            {
                SaveData.SystemDLL configINI = new SaveData.SystemDLL();
                configINI.PATH = ConfigData;

                try
                {
                    DateTime SetTime = Convert.ToDateTime(configINI.INIRead("TIME"));
                    DateTime current = DateTime.Now;
                    TimeSpan span = SetTime - current;

                    if(span < TimeSpan.Zero)
                    {
                        SetTime = SetTime.AddDays(1);
                        span = SetTime - current;
                    }

                    System.Threading.Timer time = new System.Threading.Timer((object obj) =>
                    {
                        this.Activate();

                        BeginInvoke(new Action(delegate
                        {
                            button5_Click(null, null);
                            button9.Enabled = true;
                        }));
                    }, null, (int)span.TotalMilliseconds, Timeout.Infinite);
                    
                    button9.Enabled = false;
                    ShowLog("计划开始");
                    ShowLog($"将于{SetTime.ToString("yyyy/MM/dd HH:mm:ss")}开始任务");
                    ShowLog($"等待时间为{span.Hours}:{span.Minutes}:{span.Seconds}");

                }
                catch (Exception)
                {
                    ShowLog(err);
                }
            }
            else
            {
                ShowLog(err);
            }

        }
    }
}
