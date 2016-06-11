using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnFieldStorageManagmentApp
{
    public partial class DropBoxFileList : Form
    {
        public DropBoxFileList()
        {
            InitializeComponent();

            try
            {
                string[] lines = File.ReadAllLines("DropBoxListFiles.txt");
                foreach (string s in lines)
                {
                    listBox1.Items.Add(s);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
