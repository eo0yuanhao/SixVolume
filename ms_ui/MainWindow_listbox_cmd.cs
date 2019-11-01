using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace ms_ui
{
    public partial class MainWindow 
    {
        private async void cmd_work_login()
        {
            await pan.login(username,password);
        }

        async void cmd_work_mkdir(string name)
        {
            var t = await pan.mkdir(name);
        }

        private async void cmd_work_upload(string v)
        {
            if (v == null)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                if (!dlg.ShowDialog().GetValueOrDefault())
                    return;
                v = dlg.FileName;
            }
            else
            {
                if (!File.Exists(v))
                {
                    MessageBox.Show("文件不存在");
                    return;
                }
            }
            await pan.upload("/", v);

        }

        private void cmd_work_logout(string v)
        {
            if (v == null)
                pan.logout();
            else
                pan.logoutAll();
        }

        private void cmd_show_Help()
        {
            const string msg =
                "\t ? -- 显示帮助\n" +
                "\t login -- 强制登录\n" +
                "\t mkdir <name> -- 新建目录\n";

            MessageBox.Show(msg);
        }

    }
}
