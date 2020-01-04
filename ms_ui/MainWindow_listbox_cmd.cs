using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ms_ui
{
    using Pr = Sixpan.Pr;
    public partial class MainWindow 
    {
        private async void cmd_work_login()
        {
            await pan.login(username,password);
        }

        async void cmd_work_mkdir(string name)
        { //"25ef26a1a3fe368434524a7f154978e7"
            var t = await pan.mkdir(name,null);
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
            var path = Pr.from("/");
            if (workdir != null)
                path = Pr.from(workdir.id);
            await pan.upload(path, v);

        }

        private void cmd_work_logout(string v)
        {
            if (v == null)
                pan.logout();
            else
                pan.logoutAll();
        }
        private async Task cmd_work_chdir(string param)
        {
            await setCurrentDir(Pr.from(param));
        }


        private void cmd_work_mark_proc(string param)
        {
            if (workdir == null)
                return;
            switch (param)
            {
                case "clear":
                    foreach (var x in workdir.Children)
                        x.Marked = false;
                    _markedFiles.Clear();
                    break;
                case "show":
                    StringBuilder sb = new StringBuilder();
                    foreach (var x in _markedFiles)
                        sb.Append($"\t guid:{x} \n");
                    MessageBox.Show(sb.ToString());
                    break;
                default:break;
            }
        }
        private void cmd_show_Help()
        {
            const string msg =
                "\t ? -- 显示帮助\n" +
                "\t login -- 强制登录\n" +
                "\t logout [all] -- 登出(全部)\n" +
                "\t mkdir <name> -- 新建目录\n" +
                "\t upload <filepath> -- 上传文件\n" +
                "\t cd <dir> -- 显示<dir>目录内容\n";

            MessageBox.Show(msg);
        }

        private async void CmdBox_KeyUp(object sender, KeyEventArgs e)
        {
            Tuple<string, string> parse(string text)
            {
                string cmd; string param = null;
                text = text.Trim();
                var ind = text.IndexOf(' ');
                if (ind == -1)
                    cmd = text;
                else
                {
                    cmd = text.Substring(0, ind);
                    param = text.Substring(ind + 1).TrimStart();
                    if (param[0] == '"' && param[param.Length - 1] == '"')
                    {
                        param = param.Substring(1, param.Length - 2);
                    }
                }

                return Tuple.Create<string, string>(text, param);
            }
            if (e.Key == Key.Enter)
            {
                string[] cmd = cmdBox.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (cmd.Count() == 0)
                    return;
                string param = cmd.Count() > 1 ? cmd[1] : null;
                switch (cmd[0])
                {
                    case "?":
                        cmd_show_Help();
                        break;
                    case "login":
                        cmd_work_login();
                        break;
                    case "mkdir":
                        cmd_work_mkdir(cmd[1]);
                        break;
                    case "logout":
                        cmd_work_logout(param);
                        break;
                    case "upload":
                        cmd_work_upload(param);
                        break;
                    case "cd":
                        await cmd_work_chdir(parse(cmdBox.Text).Item2);
                        break;
                    case "mark":
                        cmd_work_mark_proc(param);
                        break;
                    default:
                        MessageBox.Show("无效的命令！使用 < ? >获取帮助。");
                        break;

                }
            }
        }

    }
}
