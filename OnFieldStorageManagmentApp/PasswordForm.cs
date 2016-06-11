using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnFieldStorageManagmentApp
{
    public partial class PasswordForm : Form
    {
        string m_password = string.Empty;
        public PasswordForm()
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            InitializeComponent();
        }

        public string GetPassword()
        {
            return m_password;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            m_password = textBox1.Text;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
