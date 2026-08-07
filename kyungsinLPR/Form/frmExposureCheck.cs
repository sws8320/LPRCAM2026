using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace KyungsinLPR
{
    public partial class frmExposureCheck : Form
    {
        private string ImagePath = string.Empty;

        struct chkec
        {
            public int cnt;
            public int sum;
            public int max;
            public int min;
            public int avr;
        }

        public frmExposureCheck(string _imagePath)
        {
            ImagePath = _imagePath;
            InitializeComponent();

            InitListView();
        }

        private void InitListView()
        {
            listView1.View = View.Details;
            listView1.BeginUpdate();

            listView1.Columns.Add("시간대");
            listView1.Columns.Add("평균값");
            listView1.Columns.Add("최소값");
            listView1.Columns.Add("최대값");
            listView1.Columns.Add("건수");
            listView1.Columns[0].TextAlign = HorizontalAlignment.Center;
            listView1.Columns[1].TextAlign = HorizontalAlignment.Right;
            listView1.Columns[2].TextAlign = HorizontalAlignment.Right;
            listView1.Columns[3].TextAlign = HorizontalAlignment.Right;
            listView1.Columns[4].TextAlign = HorizontalAlignment.Right;
            
            for (int i = 0; i < 24; i++)
            {
                ListViewItem Litem = new ListViewItem(string.Format("{0} ~ {1}", i, i + 1));
                Litem.SubItems.Add("0");
                Litem.SubItems.Add("0");
                Litem.SubItems.Add("0");
                Litem.SubItems.Add("0");
                listView1.Items.Add(Litem);
            }
            listView1.EndUpdate();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            btnSearch.Enabled = false;
            dateTimePicker1.Enabled = false;
            dateTimePicker2.Enabled = false;
            Thread t = new Thread(new ThreadStart(search));
            t.IsBackground = true;
            t.Start();
        }

        private void search()
        {
            int Start = Util.Function.IntTryParse(dateTimePicker1.Value.ToString("yyyyMMdd"));
            int End = Util.Function.IntTryParse(dateTimePicker2.Value.ToString("yyyyMMdd"));
            chkec[] chk = new chkec[24];

            for (int i = 0; i < 24; i++)
            {
                chk[i].min = 999999999;
                chk[i].max = 0;
                chk[i].cnt = 0;
                chk[i].sum = 0;
                chk[i].avr = 0;
            }
            DirectoryInfo di = new DirectoryInfo(ImagePath);

            foreach (DirectoryInfo item in di.GetDirectories())
            {
                if (Start <= Util.Function.IntTryParse(item.Name) && End >= Util.Function.IntTryParse(item.Name))
                {
                    foreach (FileInfo file in item.GetFiles())
                    {
                        string[] sp = file.FullName.Split('_');
                        string tmp = sp[sp.Length - 1].ToUpper().Replace(".JPG", "");
                        int value = 0;
                        if (tmp.Length == 14)
                        {
                            int idx = Util.Function.IntTryParse(tmp.Substring(8, 2));
                            //21 530,394,1174,734 4000 89누9570
                            tmp = clsFunction.GetMetaData(file.FullName);
                            if (tmp != string.Empty)
                            {
                                sp = tmp.Split(' ');
                                value = Util.Function.IntTryParse(sp[2]);
                                chk[idx].cnt++;
                                chk[idx].sum += value;
                                if (chk[idx].max < value)
                                    chk[idx].max = value;
                                if (chk[idx].min > value)
                                    chk[idx].min = value;
                                chk[idx].avr = chk[idx].sum / chk[idx].cnt;
                            }
                        }
                    }
                }
            }

            //Listview display
            listView1.BeginUpdate();
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                ListViewItem item = listView1.Items[i];
                if (chk[i].max == 0) chk[i].min = 0;
                item.SubItems[1].Text = string.Format("{0:#,##0}", chk[i].avr);
                item.SubItems[2].Text = string.Format("{0:#,##0}", chk[i].min);
                item.SubItems[3].Text = string.Format("{0:#,##0}", chk[i].max);
                item.SubItems[4].Text = string.Format("{0:#,##0}", chk[i].cnt);
            }
            listView1.EndUpdate();
            btnSearch.Enabled = true;
            dateTimePicker1.Enabled = true;
            dateTimePicker2.Enabled = true;
        }
    }
}
