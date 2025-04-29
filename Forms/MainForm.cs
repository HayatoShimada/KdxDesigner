
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using KdxDesigner.Data;
using KdxDesigner.Models;

namespace KdxDesigner.Forms
{
    public partial class MainForm : Form
    {

        private readonly AccessRepository _repository;
        public MainForm()
        {
            InitializeComponent();
            string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=KDX_Designer.accdb;";
            _repository = new AccessRepository(connectionString);
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            var list = _repository.GetIoInfoList();

            var savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mnemonic_output.csv");
            using var writer = new StreamWriter(savePath, false, Encoding.GetEncoding("shift_jis"));

            writer.WriteLine("No,Address,Command,Comment");

            int no = 1;
            foreach (var row in list)
            {
                string command = $"LD {row.Address}";  // ★←ここをニモニック変換ルールに変更可
                writer.WriteLine($"{no++},{row.Address},{command},{row.Comment}");
            }

            MessageBox.Show("CSV出力が完了しました！");
        }
    }
}
