using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ms_ui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Sixpan pan = new Sixpan();
        VDirectory workdir;
        string old_token;
        string username;
        string password;
        string _tempPath="./";
        public static Dictionary<int, HashSet<int>> markedFaceFileDict = new Dictionary<int, HashSet<int>>() { { -1, new HashSet<int>() } };
        public MainWindow()
        {
            InitializeComponent();
            Common.configLogger();
            config_common();

        }
        public void config_common()
        {
            if (File.Exists("a:/sixpan/token.inf"))
            {
                old_token = File.ReadAllText("a:/sixpan/token.inf");
                pan.restoreHeaderToken(old_token);
            }
            if (File.Exists("../../config.tml"))
            {
                var toml = Nett.Toml.ReadFile("../../config.tml");
                username = toml["username"].ToString();
                password = toml["password"].ToString();
                _tempPath = toml["tempPath"].ToString();
                //dl_filePath = toml["downloadPath"].ToString();
                //sys_dir_id = toml["_DIR_ID"].Get<int>();
            }
        }
        public async Task<bool> login()
        {

            var c = await pan.login(username,password);
            return c;
        }

        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("a:/sixpan/token.inf"))
                pan.restoreHeaderToken(File.ReadAllText("a:/sixpan/token.inf"));
            else
            {
                var b = await login();
                if (b)
                    MessageBox.Show("login ok!");
                else MessageBox.Show("login fail!");
            }

        }


        private void ListViewItem_DoubleClick(object sender ,RoutedEventArgs e)
        {
           
        }

        public void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.Title = "double click";
        }

        private void context_MarkFile(object sender, RoutedEventArgs e)
        {

        }


        private void DirTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }

        private void ListView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //HitTest 用法错误，以后慢慢研究
            //HitTestResult hitTestResult =  VisualTreeHelper.HitTest(listView, e.GetPosition(listView));
            //if (hitTestResult.VisualHit == listView)
            //    listView.SelectedIndex = -1;
            listView.SelectedIndex = -1;
        }

        private void context_DownloadFaceFile(object sender, RoutedEventArgs e)
        {

        }

        private void context_UploadFaceFile(object sender, RoutedEventArgs e)
        {

        }

        private void context_DeleteFile(object sender, RoutedEventArgs e)
        {

        }

        private void context_MoveMarkedFile(object sender, RoutedEventArgs e)
        {

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //Newtonsoft.Json.Linq.JObject obj = await pan.acquireFileInfo_byPath("/online.ts.ppt");
            //Title = $"{obj["size"].ToString()}";

            var curdir = await pan.listdir("/backup");
            listView.ItemsSource = curdir.Children;
            workdir = curdir;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!pan.IsLogged())
                return;

            var token = pan.getHeaderToken();
            if(old_token != token)
                File.WriteAllText("a:/sixpan/token.inf",token);
        }

        private void CmdBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string[] cmd = cmdBox.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
                        cmd_work_logout(cmd.Count() > 1 ? cmd[1] : null);
                        break;
                    case "upload":
                        cmd_work_upload(cmd.Count() > 1 ? cmd[1] : null);
                        break;
                    default:
                        MessageBox.Show("无效的命令！使用 < ? >获取帮助。");
                        break;

                }
            }
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
