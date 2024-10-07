using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebTest
{
    public partial class SetTime : Form
    {
        public SetTime()
        {
            InitializeComponent();
        }

        public DateTime Time { get; set; }

        private void SetTime_Load(object sender, EventArgs e)
        {
            if (Time.Ticks != 0)
            {
                dateTimePicker1.Value = Time;
            }
            else
            {
                dateTimePicker1.Value = DateTime.Now;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Time = dateTimePicker1.Value;

            DialogResult = DialogResult.OK;
            this.Hide();
        }
        
    }
}
