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
//using System.Data.SQLite;
using System.Collections.ObjectModel;
using SQLite;
namespace ms_ui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    using Pr = Sixpan.Pr;
    public class Stock
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Symbol { get; set; }
    }

    public partial class MainWindow : Window
    {
        Sixpan pan = new Sixpan();
        VDirectory workdir;
        string old_token;
        string username;
        string password;
        string _tempPath="./";
        string _tempDB = "sixpan_sqlite.db";
        Guid _rootGuid;
        //System.Data.SQLite.SQLiteConnection _sqlcon;
        SQLiteConnection _sqlcon;// ("a:/foofoo_sqlite.db");
        readonly List<Guid> _markedFiles = new List<Guid>();
        public static Dictionary<int, HashSet<int>> markedFaceFileDict = new Dictionary<int, HashSet<int>>() { { -1, new HashSet<int>() } };
        public MainWindow()
        {
            InitializeComponent();

            config_common();            
            //tim();
            Common.configLogger();
        }
        public void tim()
        {
            _sqlcon = new SQLiteConnection("a:/vvv.db");
            _sqlcon.CreateTable<Stock>();
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
                _rootGuid = Guid.Parse(toml["root_dir_guid"].ToString());
                //dl_filePath = toml["downloadPath"].ToString();
                //sys_dir_id = toml["_DIR_ID"].Get<int>();
            }
            var last = _tempPath.Last();
            if (last != '/' && last != '\\')
                _tempPath = _tempPath + '/';
            Common.makeSureDirectoryExist(_tempPath);
            string dbfile = _tempPath + "sixpan_sqlite.db";
            _sqlcon = new SQLiteConnection("a:/vvv.db");
            _sqlcon.CreateTable<Stock>();
            //if (!File.Exists(dbfile))
            //{

            //    _sqlcon = new SQLiteConnection("a:/vvv.db");
            //    _sqlcon.CreateTable<Stock>();
            //}else
            //{
            //    _sqlcon = new SQLiteConnection(dbfile);
            //}

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


        private async  void ListViewItem_DoubleClick(object sender, RoutedEventArgs e)
        {
            var dir = listView.SelectedItem as VDirectory;
            if(dir != null)
            {
                //Task.Run(async () => await setCurrentDir(Pr.from(dir.id)) ).Wait();
                //Task.Run(async ()=> workdir = await pan.listdir(Pr.from(dir.id))).Wait();
                //workdir = await pan.listdir(Pr.from(dir.id));
                //listView.ItemsSource = workdir.Children;
                await setCurrentDir(Pr.from(dir.id));
            }
        }
        //public async Task ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        //{

        //}

        private void context_MarkFile(object sender, RoutedEventArgs e)
        {
            foreach (VFileBase x in listView.SelectedItems)
            {
                bool b = x.Marked = !x.Marked;
                if (b)
                    _markedFiles.Add(x.id);
                else _markedFiles.Remove(x.id);

            }
            listView.ItemsSource = null;
            listView.ItemsSource = workdir.Children;

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

        private async void context_DownloadFaceFile(object sender, RoutedEventArgs e)
        {
            var p = from VFile x in listView.SelectedItems where x != null select Pr.@from(x.id) ;
            foreach (var v in p)
                await pan.download(v, _tempPath);
        }

        private async void context_UploadFaceFile(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Multiselect = true;
            if (!dlg.ShowDialog().GetValueOrDefault())
                return;
            var uploadfiles = dlg.FileNames;
            var path = Pr.from(workdir.id);
            foreach( var v in uploadfiles )
                await pan.upload(path, v);
        }

        private async void context_DeleteFile(object sender, RoutedEventArgs e)
        {
            var p =   from VFileBase x in listView.SelectedItems select Pr.@from(x.id) ;
            await pan.rm(p);
        }

        private async void context_MoveMarkedFile(object sender, RoutedEventArgs e)
        {
            var p = from x in _markedFiles select Pr.@from(x) ;
            await pan.move(p, Pr.from(workdir.id));
            _markedFiles.Clear();
        }

        private async void listBtn_Click(object sender, RoutedEventArgs e)
        {
            //var db = new DataModels.SixpanDB("a:/vfs/sixpan_work.sqlite");
            //if (_rootGuid == Guid.Empty)
            //    _rootGuid = db.Filesystems.Where(x => x.Path == "/").Select(x => x.Id).First();// from x in db.Filesystems where x.Path == "/" select (x.Id) first

            await setCurrentDir(Pr.from("/"));
        }
        public async Task setCurrentDir(Pr pr)
        {

            //var files = from x in db.FileSystem where x.parent == _rootGuid select x;
            if (!pr.isId && pr.Value == "/")
                pr = Pr.from(_rootGuid);
            VDirectory curdir = null;
            bool db_needupdate_parent = false;
            bool db_needupdate_child = true;
            Guid id=Guid.Empty;
            try
            {            
                //var db = new DataContext();
                if (pr.isId)
                {
                    id = Guid.Parse(pr.Value);
                }else {                  
                    //var ids = from x in db.FileSystem where x.path == pr.Value select x.id;
                    var ids = from x in _sqlcon.Table<Filesystem>() where x.path == pr.Value select x.id;
                    id = ids.First();
                }
            }
            catch( Exception)
            {
                db_needupdate_parent = true;
            }
            if (!db_needupdate_parent)
            {
                //var db = new DataContext();
                //var filebases = db.FileSystem.Where(x => x.parent == id);
                var filebases = _sqlcon.Table<Filesystem>().Where(x => x.parent == id);

                //var filebases = from x in db.FileSystem where x.parent == id select x;
                if (filebases.Count() == 0)
                {
                    db_needupdate_child = true;
                }else
                {
                    db_needupdate_child = false;
                    curdir = new VDirectory();
                    curdir.id = id;
                    var files  = from x in filebases where (x.isdir == false) select (new VFile() { id = x.id, ModifiedTime = x.time, Name = x.name, Size = x.size}); ;
                    var dirs = from x in filebases where (x.isdir == true) select (new VDirectory() { id = x.id, ModifiedTime = x.time, Name = x.name }); ;
                    ObservableCollection<VDirectory> td = new ObservableCollection<VDirectory>();
                    ObservableCollection<VFile> tf = new ObservableCollection<VFile>();
                    foreach (var x in files)
                        tf.Add(x);
                    foreach (var x in dirs)
                        td.Add(x);
                    curdir.Files = tf;
                    curdir.SubDirs = td;

                }
            }

            if(db_needupdate_parent || db_needupdate_child)
            {
                curdir = await pan.listdir(pr);
                if(curdir.Files.Count()!=0 || curdir.SubDirs.Count() != 0)
                {
                    updateDB(curdir,db_needupdate_parent,db_needupdate_child);
                }
            }
            var mfs = from x in _markedFiles join y in curdir.Files on x equals y.id select y;
            foreach (var x in mfs)
                x.Marked = true;
            listView.ItemsSource = curdir.Children;
            workdir = curdir;   
            
        }

        private void updateDB(VDirectory dir,bool updateParent,bool updateChild)
        {            
            var ls = new List<Filesystem>();
            //var db = new DataContext();
            //var t = from x in db.FileSystem where x.id == dir.id select x;
            var t = from x in _sqlcon.Table<Filesystem>() where x.id == dir.id select x;
            if (t.Count() == 0 && updateParent)
            {
                Filesystem fs = new Filesystem()
                {
                    id = dir.id,
                    isdir = true,
                    name = dir.Name,
                    size = 0,
                    path = dir.path,
                    parent = dir.parentId,
                    time = dir.ModifiedTime
                };
                ls.Add(fs);
                //db.Add(fs);
                //db.SaveChanges();
            }
            //void insertFiles(List<Filesystem> ls)
            //{
            //    if (ls.Count == 0)
            //        return;
            //    _sqlcon.Open();
            //    var cmd = _sqlcon.CreateCommand();
            //    cmd.CommandText = "insert into Filesystem(id,isdir,size,time,parent,name,path) values(@id,@isdir,@size,@time,@parent,@name,@path)";
            //    System.Data.SQLite.SQLiteParameter addParam(string name)
            //    {
            //        var r = cmd.CreateParameter();
            //        r.ParameterName = name;
            //        cmd.Parameters.Add(r);
            //        return r;
            //    }

            //    var id = addParam("@id");
            //    var isdir = addParam("@isdir");
            //    var size = addParam("@size");
            //    var time = addParam("@time");
            //    var parent = addParam("@parent");
            //    var name = addParam("@name");
            //    var path = addParam("@path");

            //    foreach ( var x in ls)
            //    {
            //        id.Value = x.id;
            //        isdir.Value = x.isdir;
            //        name.Value = x.name;
            //        size.Value = x.size;
            //        path.Value = x.path;
            //        parent.Value = x.parent;
            //        time.Value = x.time;
            //        cmd.ExecuteNonQuery();
            //    }
            //    _sqlcon.Close();
            //}
            void insertFiles2(List<Filesystem> ls)
            {
                if (ls.Count == 0)
                    return;
                _sqlcon.InsertAll(ls);
            }





            if (!updateChild)
            {
                insertFiles2(ls);
                return;
            }

            foreach (var x in dir.SubDirs)
            {
                Filesystem f = new Filesystem()
                {
                    id = x.id,
                    isdir = true,
                    name = x.Name,
                    size = 0,
                    path = x.path,
                    parent = x.parentId,
                    time = x.ModifiedTime,                   
                };
                ls.Add(f);
            }
            foreach (var x in dir.Files)
            {
                Filesystem f = new Filesystem()
                {
                    id = x.id,
                    isdir = false,
                    name = x.Name,
                    size = x.Size,
                    path = x.path,
                    parent = x.parentId,
                    time = x.ModifiedTime
                };
                ls.Add(f);
            }
            // AddRange 在SaveChanges时，会出现异常，不知道原因，暂时只能一条一条添加了
            //db.FileSystem.AddRange(ls);
            //db.Add(ls[0]);
            //foreach (var x in ls)
            //    db.FileSystem.Add(x);
            //db.SaveChanges();
            insertFiles2(ls);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!pan.IsLogged())
                return;

            var token = pan.getHeaderToken();
            if(old_token != token)
                File.WriteAllText("a:/sixpan/token.inf",token);
        }


        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void refreshBtn_Click(object sender, RoutedEventArgs e)
        {


        }
    }
}
