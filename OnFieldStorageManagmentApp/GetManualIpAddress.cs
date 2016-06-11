using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnFieldStorageManagmentApp
{
    public partial class GetManualIpAddress : Form
    {
        string m_ip;
        public GetManualIpAddress()
        {
            InitializeComponent();
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
        public string IpAddress
        {
            get
            {
                return m_ip;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {                      
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            m_ip = textBox1.Text.ToString();
            Close();
        }

        private void GetManualIpAddress_Load(object sender, EventArgs e)
        {

        }
    }
}
