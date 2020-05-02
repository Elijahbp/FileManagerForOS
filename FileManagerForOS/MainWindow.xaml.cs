using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static FileManagerForOS.BaseStatics;

namespace FileManagerForOS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /*TODO
         * ПО необходимости из имеющегося:
         *
         *
         * По заданию из оставшегося:
         * 4) Доделать систему подгрузки новых сборок
         * 5) Прикрутить систему проверки овления, и её непосредственной реализации (Консоль и т.д.). - Доделать
         */
        public MainWindow()
        {
            InitializeComponent();
            //WindowUpdate windowUpdate = new WindowUpdate();
            //windowUpdate.Show();
            //if (windowUpdate.IsInitialize)
            //{
            baseInit();
            //}
            //else
            //{
            //    this.Close();
            //}
        }

        private const int MODE_STANDART_HIDE = 0;
        private const int MODE_SHOW_ALL = 1;


        private const int SORT_NAME = 100;
        private const int SORT_DATE = 101;
        private const int SORT_WEIGHT = 102;

        private static Brush unselectedIconStyleFile = new LinearGradientBrush(Colors.White, Colors.CadetBlue, 45);
        private static Brush unselectedIconStyleFolder = new LinearGradientBrush(Colors.White, Colors.DarkGray, 45);
        private static Brush selectedIconStyle = new LinearGradientBrush(Colors.White, Colors.LightGray, 45);

        private int totalRAM;

        private List<object[]> listActions;

        private int selectedModeView = MODE_STANDART_HIDE;
        private Label selectedIcon;
        private string main_path;
        private DirectoryInfo selectedDirectory = null;
        private DirectoryInfo mainDirectory;
        TreeViewItem headTreeView;
        private BaseStatics.FileActions selectedAction; //Выбранное действие копирование/вырезание
        string bufPath; //Используется для временного хранения пути файла (для дальнейшего удаления/копирования/перемещения) 

        private string textAbout ;

        private string splitter = "\n"+new string('-',50) + "\n";
        private const string forHelp = "for help use -h or --help";
        private const string helpToConsole = "Команды исполнения:\n" +
            "\tCreate File: -ctf (--createFile) name (path)\n" +
            "\tCreate Directory: -ctd (--createDirectory) name (path)\n" +
            "\tDelete file: -deF (--deleteFile) path_to_file\n" +
            "\tDelete directory: -deD (--deleteDirectory) path_to_directory\n" +
            "\tLaunch: -ln (--launch) name \n" +
            "\tLogs: --logs name_output_file\n" +
            "\tAbout: --about\n" +
            "\tDocumentation: --doc\n" +
            "\tClear: --clear\n" +
            "\tHelp: -h (--help)\n" +
            "\tExit: --exit";


        private void baseInit()
        {
            //Получение корня, где лежит файловый менеджер
            main_path = Environment.CurrentDirectory;
            mainDirectory = new DirectoryInfo(main_path);
            listActions = new List<object[]>();
            totalRAM = BaseStatics.getRAM();
            txtBoxPath.Text = convertPathForView(main_path);
            Directory.CreateDirectory(main_path + "\\System");
            File.SetAttributes(main_path + "\\System", FileAttributes.Hidden);
            Directory.CreateDirectory(main_path + "\\MyDocuments");

            headTreeView = generateTreeViewItem(mainDirectory);
            headTreeView.Header = "Root";
            headTreeView.IsExpanded = true;
            mainTreeView.Items.Add(headTreeView);

            textAbout = FileManagerForOS.Properties.Resources.About + "\n" +
                "Версия программы: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            getViewByPath(main_path);
            getMainTreeView();
        }

        private void KillingBase()
        {
            Directory.Delete(main_path + "\\System", true);
            if(Directory.Exists(main_path + "\\MyDocuments"))
            {
                Directory.Delete(main_path + "\\MyDocuments", true);
            }
        }

        private string convertPathForView(string path)
        {
            return path.Replace(main_path, "");
        }

        private string reverseConvertPath(string path)
        {

            return new StringBuilder().Append(main_path).Append(path).ToString();
        }


        private void OnCreateOrDelete(FileActions fileActions, string name, string destPath)
        {
            name += " ";
            string message = DateTime.Now + ": " + fileActions + " " + name + "by path: " + convertPathForView(destPath);
            OnChange(fileActions, message);
        }

        private void OnInsert(FileActions fileActions, string pathFrom, string pathDestination)
        {
            string message = DateTime.Now + ": " + fileActions + " from " + convertPathForView(pathFrom) + " in folder " + convertPathForView(pathDestination);
            OnChange(fileActions, message);
        }

        private void OnRenamed(string path, string oldName, string newName)
        {
            string message = DateTime.Now + ": " + FileActions.Rename + " " + oldName + " on new " + newName + " by path " + convertPathForView(path);
            OnChange(FileActions.Rename, message);
        }

        private void OnOpened(string path, string name)
        {
            string message = DateTime.Now + ": " + FileActions.Open + " " + name;
            if (path.Length != 0 || path != null)
            {
                message += " by path " + convertPathForView(path);
            }
            OnChange(FileActions.Open, message);
        }

        private void OnChange(FileActions fileActions, string message)
        {
            listActions.Add(new object[] { fileActions, message });
        }


        #region логика работы с файлами/папками

        //Обновить отображение
        private void refreshView()
        {
            getViewByPath(selectedDirectory.FullName);
            getMainTreeView();
        }

        //Получение отображения древа
        private void getMainTreeView()
        {
            headTreeView.Items.Clear();
            foreach (TreeViewItem item in generateTreeView(mainDirectory))
            {
                headTreeView.Items.Add(item);
            }
        }


        private void getViewByPath(string path, int typeSort = 0)
        {
            MainSpace.Children.Clear();
            selectedDirectory = new DirectoryInfo(path);
            txtBoxPath.Text = convertPathForView(path);
            List<FileInfo> filesInfo = new List<FileInfo>(selectedDirectory.GetFiles());
            List<DirectoryInfo> directoryInfos = new List<DirectoryInfo>(selectedDirectory.GetDirectories());
            if (typeSort == 0)
            {

            }
            else if (typeSort == SORT_NAME)
            {
                filesInfo.Sort((a, b) => (a.Name.CompareTo(b.Name)));
                directoryInfos.Sort((a, b) => (a.Name.CompareTo(b.Name)));
            }
            else if (typeSort == SORT_DATE)
            {
                filesInfo.Sort((a, b) => (a.LastWriteTime.CompareTo(b.LastWriteTime)));
                directoryInfos.Sort((a, b) => (a.LastWriteTime.CompareTo(b.LastWriteTime)));
            }
            else if (typeSort == SORT_WEIGHT)
            {
                filesInfo.Sort((a, b) => (a.Length.CompareTo(b.Length)));
                //directoryInfos.Sort((a, b) => (a.Length.CompareTo(b.Length)));
            }

            foreach (DirectoryInfo directoryInfo in directoryInfos)
            {
                if (selectedModeView == MODE_SHOW_ALL)
                {
                    this.MainSpace.Children.Add(generateShortcut(directoryInfo));
                }
                else if (selectedModeView == MODE_STANDART_HIDE)
                {
                    if (!directoryInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        this.MainSpace.Children.Add(generateShortcut(directoryInfo));
                    }
                }


            }
            foreach (FileInfo fileInfo in filesInfo)
            {

                if (selectedModeView == MODE_SHOW_ALL)
                {
                    this.MainSpace.Children.Add(generateShortcut(fileInfo));
                }
                else if (selectedModeView == MODE_STANDART_HIDE)
                {
                    if (!fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        this.MainSpace.Children.Add(generateShortcut(fileInfo));
                    }
                }
            }



        }

        private Label generateShortcut(FileSystemInfo info)
        {
            Label shortcut = new Label()
            {
                Width = 100,
                Height = 100,
                Margin = new Thickness(10),
                VerticalContentAlignment = VerticalAlignment.Bottom,

            };
            if (info.Extension.Length != 0)
            {
                shortcut.Content = info.Name.Replace(info.Extension, "");

            }
            else
            {
                shortcut.Content = info.Name;
            }
            shortcut.ToolTip = info.Name;

            if (info is DirectoryInfo)
            {
                shortcut.Background = unselectedIconStyleFolder;

            }
            else if (info is FileInfo)
            {
                shortcut.Background = unselectedIconStyleFile;
            }
            shortcut.MouseDoubleClick += Label_DoubleClick_OpenFile;
            shortcut.MouseDown += Select_Icon;

            string[] infoForTag = new string[] { info.GetType().Name, info.FullName };
            shortcut.Tag = infoForTag;

            #region элементы контекстного меню
            List<MenuItem> menuItems = new List<MenuItem>();

            MenuItem deleteElement = new MenuItem()
            {
                Header = "Удалить",
                Tag = infoForTag,
            };
            deleteElement.Click += MenuItem_Click_DeleteFile;

            MenuItem copyElement = new MenuItem()
            {
                Header = "Копировать",
                Tag = infoForTag
            };
            copyElement.Click += MenuItem_Click_CopyFile;

            MenuItem cutElement = new MenuItem()
            {
                Header = "Вырезать",
                Tag = infoForTag
            };
            cutElement.Click += MenuItem_Click_CutFile;

            MenuItem renameElement = new MenuItem()
            {
                Header = "Переименовать",
                Tag = infoForTag
            };
            renameElement.Click += MenuItem_Click_RenameFile;



            menuItems.Add(deleteElement);
            menuItems.Add(copyElement);
            menuItems.Add(cutElement);
            menuItems.Add(renameElement);
            shortcut.ContextMenu = new ContextMenu()
            {
                Name = "contextMenu",
                ItemsSource = menuItems,
            };
            #endregion
            return shortcut;
        }


        private List<TreeViewItem> generateTreeView(DirectoryInfo df)
        {
            if (df.GetDirectories().Length == 0)
            {
                return null;
            }
            else
            {
                List<TreeViewItem> treeViewItems = new List<TreeViewItem>();
                foreach (DirectoryInfo directoryInfo in df.GetDirectories())
                {
                    if (selectedModeView == MODE_STANDART_HIDE)
                    {
                        if (!directoryInfo.Attributes.HasFlag(FileAttributes.Hidden))
                        {
                            List<TreeViewItem> gtv = generateTreeView(directoryInfo);
                            if (gtv == null)
                            {
                                treeViewItems.Add(generateTreeViewItem(directoryInfo));
                            }
                            else
                            {
                                TreeViewItem viewItem = generateTreeViewItem(directoryInfo);
                                foreach (TreeViewItem item in gtv)
                                {
                                    viewItem.Items.Add(item);
                                }
                                treeViewItems.Add(viewItem);
                            }
                        }
                    }
                    else if (selectedModeView == MODE_SHOW_ALL)
                    {
                        List<TreeViewItem> gtv = generateTreeView(directoryInfo);
                        if (gtv == null)
                        {
                            treeViewItems.Add(generateTreeViewItem(directoryInfo));
                        }
                        else
                        {
                            TreeViewItem viewItem = generateTreeViewItem(directoryInfo);
                            foreach (TreeViewItem item in gtv)
                            {
                                viewItem.Items.Add(item);
                            }
                            treeViewItems.Add(viewItem);
                        }
                    }


                }
                return treeViewItems;
            }
        }

        //Отображает исключительно иерархию и отображение папок
        private TreeViewItem generateTreeViewItem(DirectoryInfo directoryInfo)
        {
            TreeViewItem treeViewItem = new TreeViewItem();
            treeViewItem.Header = directoryInfo.Name;
            treeViewItem.MouseUp += TreeViewItem_MouseUp_OpenDirectory;

            List<MenuItem> menuItems = new List<MenuItem>();
            string[] infoForTag = new string[] { Properties.Resources.TYPE_DIRECTORY, directoryInfo.FullName };

            treeViewItem.Tag = infoForTag;

            MenuItem deleteElement = new MenuItem()
            {
                Header = "Удалить",
                Tag = infoForTag,
            };
            deleteElement.Click += MenuItem_Click_DeleteFile;

            MenuItem copyElement = new MenuItem()
            {
                Header = "Копировать",
                Tag = infoForTag
            };
            copyElement.Click += MenuItem_Click_CopyFile;

            MenuItem cutElement = new MenuItem()
            {
                Header = "Вырезать",
                Tag = infoForTag
            };
            cutElement.Click += MenuItem_Click_CutFile;

            MenuItem renameElement = new MenuItem()
            {
                Header = "Переименовать",
                Tag = infoForTag
            };
            renameElement.Click += MenuItem_Click_RenameFile;

            MenuItem createNewDirectory = new MenuItem()
            {
                Header = "Создать новую папку",
                Tag = infoForTag
            };
            createNewDirectory.Click += SelectNewFolder;
            createNewDirectory.Click += MenuItem_Click_CreateNewFolder;

            menuItems.Add(deleteElement);
            menuItems.Add(copyElement);
            menuItems.Add(cutElement);
            menuItems.Add(renameElement);
            menuItems.Add(createNewDirectory);
            treeViewItem.ContextMenu = new ContextMenu()
            {
                Name = "contextMenu",
                ItemsSource = menuItems,
            };

            return treeViewItem;
        }

        #region Действия menu_item

        //Сокрытие/открытие всех файлов с флагом
        private void MenuItem_Click_ShowHidenFiles(object sender, RoutedEventArgs e)
        {
            if (selectedModeView == MODE_STANDART_HIDE)
            {

                ((MenuItem)sender).Header = "Скрыть файлы";
                selectedModeView = MODE_SHOW_ALL;
            }
            else if (selectedModeView == MODE_SHOW_ALL)
            {

                ((MenuItem)sender).Header = "Показать скрытые файлы";
                selectedModeView = MODE_STANDART_HIDE;
            }
            refreshView();
            getMainTreeView();

        }

        //Удалить
        private void MenuItem_Click_DeleteFile(object sender, RoutedEventArgs e)
        {
            string[] tagData = ((string[])((MenuItem)sender).Tag);
            string typeElement = tagData[0];
            string path = tagData[1];
            DeleteFile(typeElement, path);
            refreshView();

        }

        //Копировать
        private void MenuItem_Click_CopyFile(object sender, RoutedEventArgs e)
        {
            bufPath = ((string[])((MenuItem)sender).Tag)[1];
            selectedAction = BaseStatics.FileActions.Copy;
            insertMenuItem.IsEnabled = true;
        }

        //Вырезать
        private void MenuItem_Click_CutFile(object sender, RoutedEventArgs e)
        {
            bufPath = ((string[])((MenuItem)sender).Tag)[1];
            selectedAction = BaseStatics.FileActions.Excision;
            insertMenuItem.IsEnabled = true;
        }

        //Вставить
        private void MenuItem_Click_InsertFile(object sender, RoutedEventArgs e)
        {
            //Пока вставляем в папку, в которой сейчс и находимся
            string destFileName = selectedDirectory.FullName;
            FileInfo fileInfo = new FileInfo(bufPath);
            destFileName += "\\" + fileInfo.Name;
            if (selectedAction == BaseStatics.FileActions.Copy)
            {
                if (File.Exists(bufPath))
                {
                    File.Copy(bufPath, destFileName, true);
                    OnInsert(FileActions.Copy, bufPath, destFileName);
                }

            }
            else if (selectedAction == BaseStatics.FileActions.Excision)
            {
                try
                {
                    if (File.Exists(bufPath))
                    {
                        File.Move(bufPath, destFileName);
                        OnInsert(FileActions.Excision, bufPath, destFileName);
                    }
                    else if (Directory.Exists(bufPath))
                    {
                        Directory.Move(bufPath, destFileName);
                        OnInsert(FileActions.Excision, bufPath, destFileName);
                    }
                    insertMenuItem.IsEnabled = false;
                }
                catch (IOException exception)
                {
                    MessageBox.Show(exception.Message);
                }

            }
            refreshView();
        }


        //Переименовать
        private void MenuItem_Click_RenameFile(object sender, RoutedEventArgs e)
        {
            string[] tagData = ((string[])((MenuItem)sender).Tag);
            string typeElement = tagData[0];
            string path = tagData[1];
            EditionWindow editionWindow = new EditionWindow(typeElement, true);
            editionWindow.Owner = this;
            editionWindow.ShowDialog();

            string newName = editionWindow.NameFile;
            if (newName.Length != 0)
            {
                if (isStringEquals(typeElement, Properties.Resources.TYPE_FILE))
                {
                    FileInfo k = new FileInfo(path);
                    string oldName = k.Name;
                    newName = newName + k.Extension;
                    string newPath = k.DirectoryName + "\\" + newName;
                    File.Move(path, newPath);
                    OnRenamed(path, oldName, newName);

                }
                else if (isStringEquals(typeElement, Properties.Resources.TYPE_DIRECTORY))
                {
                    FileInfo fileInfo = new FileInfo(path);
                    string oldName = fileInfo.Name;
                    string newPath = fileInfo.DirectoryName + "\\" + newName;
                    Directory.Move(path, newPath);
                    OnRenamed(path, oldName, newName);
                }

            }
            refreshView();
        }

        //Создать новый файл
        private void MenuItem_Click_CreateNewFile(object sender, RoutedEventArgs e)
        {
            EditionWindow creationWindow = new EditionWindow(Properties.Resources.TYPE_FILE, false);
            creationWindow.Owner = this;
            creationWindow.ShowDialog();
            string nameFile = creationWindow.txtBoxNameFile.Text;
            if (nameFile.Length != 0)
            {
                string destPath = selectedDirectory.FullName;
                try
                {
                    CreateFile(nameFile, destPath);
                }
                catch (IOException w)
                {
                    MessageBox.Show(w.Message);
                }


                refreshView();
            }

        }

        //Создание файла
        private void MenuItem_Click_CreateNewFolder(object sender, RoutedEventArgs e)
        {
            EditionWindow creationWindow = new EditionWindow(Properties.Resources.TYPE_DIRECTORY, false);
            creationWindow.Owner = this;
            creationWindow.ShowDialog();
            string nameFolder = creationWindow.txtBoxNameFile.Text;
            if (nameFolder.Length != 0)
            {
                string dest = selectedDirectory.FullName;
                CreateDirectory(dest, nameFolder);
                refreshView();
            }
        }


        //Обновление отображения
        private void MenuItem_Click_RefreshView(object sender, RoutedEventArgs e)
        {
            refreshView();
            getMainTreeView();
        }

        //Отображение окна Справки
        private void MenuItem_Click_ShowReference(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(FileManagerForOS.Properties.Resources.Reference);
        }

        //Отображение окна "О программе"
        private void MenuItem_Click_ShowAboutProgramm(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(textAbout);
        }

        private void MenuItem_Click_SortByName(object sender, RoutedEventArgs e)
        {
            getViewByPath(selectedDirectory.FullName, typeSort: SORT_NAME);
        }

        private void MenuItem_Click_SortByDate(object sender, RoutedEventArgs e)
        {
            getViewByPath(selectedDirectory.FullName, typeSort: SORT_DATE);
        }

        private void MenuItem_Click_SortByWeigth(object sender, RoutedEventArgs e)
        {
            getViewByPath(selectedDirectory.FullName, typeSort: SORT_WEIGHT);
        }

        private void MenuItem_Click_OpenWindowLogs(object sender, RoutedEventArgs e)
        {
            WindowLogs windowLogs = new WindowLogs(listActions, totalRAM);
            windowLogs.Owner = this;
            windowLogs.ShowDialog();
            refreshView();
        }

        //Предварительный выбор файла (используется только для элементов TreeViewItem)
        private void SelectNewFolder(object sender, RoutedEventArgs e)
        {
            string[] tagData = ((string[])((MenuItem)sender).Tag);
            string path = tagData[1];
            selectedDirectory = new DirectoryInfo(path);
        }
        #endregion

        //Событие "открытия" папки/файла
        private void Label_DoubleClick_OpenFile(object sender, RoutedEventArgs e)
        {
            string[] tagData = ((string[])((Label)sender).Tag);
            string typeElement = tagData[0];
            string path = tagData[1];
            //Обработка файлов и папок:
            OpenFile(typeElement, path);
        }

        //Событие "открытия" папки через элемент дерева
        private void TreeViewItem_MouseUp_OpenDirectory(object sender, MouseButtonEventArgs e)
        {
            if (((TreeViewItem)sender).IsSelected)
            {
                string[] tagData = ((string[])((TreeViewItem)sender).Tag);
                string typeElement = tagData[0];
                string path = tagData[1];
                //Обработка файлов и папок:
                OpenFile(typeElement, path);
            }
            /*
            else if (typeElement.Equals(Properties.Resources.TYPE_FILE))
            {
                ProcessStartInfo processStart = new ProcessStartInfo(path);
                try
                {
                    Process.Start(processStart);
                }
                catch (Exception)
                {
                    MessageBox.Show("Невозможно открыть программу");
                }
            }*/
        }

        //Событие выбора иконки
        private void Select_Icon(object sender, RoutedEventArgs e)
        {
            string typeSender = sender.GetType().Name;
            if (BaseStatics.isStringEquals(e.Source.GetType().Name, "Label") && BaseStatics.isStringEquals(typeSender, "Label"))
            {
                if (selectedIcon == null)
                {
                    selectedIcon = (Label)sender;
                    selectedIcon.Background = selectedIconStyle;
                }
                else
                {
                    string type = ((string[])selectedIcon.Tag)[0];
                    if (BaseStatics.isStringEquals(type, Properties.Resources.TYPE_FILE))
                    {
                        selectedIcon.Background = unselectedIconStyleFile;
                    }
                    else if (BaseStatics.isStringEquals(type, Properties.Resources.TYPE_DIRECTORY))
                    {
                        selectedIcon.Background = unselectedIconStyleFolder;
                    }
                    selectedIcon = (Label)sender;
                    selectedIcon.Background = selectedIconStyle;
                }
            }
            else if (BaseStatics.isStringEquals(e.Source.GetType().Name, "WrapPanel"))
            {
                if (selectedIcon != null)
                {
                    string type = ((string[])selectedIcon.Tag)[0];
                    if (BaseStatics.isStringEquals(type, Properties.Resources.TYPE_FILE))
                    {
                        selectedIcon.Background = unselectedIconStyleFile;
                    }
                    else if (BaseStatics.isStringEquals(type, Properties.Resources.TYPE_DIRECTORY))
                    {
                        selectedIcon.Background = unselectedIconStyleFolder;
                    }
                    selectedIcon = null;

                }
            }

        }

        //Событие нажатия кнопки "Вверх"
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            back_view(selectedDirectory.Parent.FullName);
        }

        //Обработчик нажатия клавиши клавиатуры "Backspace"
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                back_view(selectedDirectory.Parent.FullName);
            }

        }

        //Обработчик нажатия клавиши клавиатуры "Enter" при вводе пути в строку
        private void txtBoxPath_KeyDownEnterPath(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                getViewByPath(reverseConvertPath(txtBoxPath.Text));
                txtBoxPath.IsReadOnly = true;
            }
        }

        //Метод отображения по пути
        private void back_view(string path)
        {
            if (selectedDirectory.FullName != main_path)
            {
                getViewByPath(path);
            }
        }

        //Событие закрытия приложения
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            KillingBase();
            //new WindowUpdate().ShowDialog();
            //TODO: Проверка на обновления
            Console.WriteLine("Ok");
        }

        private void MenuItem_Click_Launch_CMD(object sender, RoutedEventArgs e)
        {
            LaunchProcess("", "CMD.exe");
        }

        private void MenuItem_Click_Launch_PowerShell(object sender, RoutedEventArgs e)
        {
            LaunchProcess("", "PowerShell.exe");
        }

        private void MenuItem_Click_Launch_MonitorResources(object sender, RoutedEventArgs e)
        {
            LaunchProcess("", "perfmon.exe");
        }

        private void MenuItem_Click_Launch_Services(object sender, RoutedEventArgs e)
        {
            LaunchProcess("", "services.msc");
        }

        private void MenuItem_Click_Launch_Note(object sender, RoutedEventArgs e)
        {
            LaunchProcess("", "notepad.exe");
        }

        #endregion


        //Основные методы по работе с файлами (за исключением copy,cut)
        private void CreateDirectory(string destPath, string nameFolder)
        {
            
            Directory.CreateDirectory(destPath + "\\" + nameFolder);
            OnCreateOrDelete(FileActions.Create, nameFolder, destPath);
        }

        private void CreateFile(string nameFile, string destPath)
        {
            destPath += "\\" + nameFile + ".txt";
            File.Create(destPath).Close();
            OnCreateOrDelete(FileActions.Create, nameFile, destPath);
        }

        private void OpenFile(string typeElement, string path)
        {
            if (isStringEquals(typeElement, Properties.Resources.TYPE_DIRECTORY))
            {
                getViewByPath(path);
            }
            else if (isStringEquals(typeElement, Properties.Resources.TYPE_FILE))
            {
                ProcessStartInfo processStart = new ProcessStartInfo(path);
                try
                {
                    Process.Start(processStart);
                    OnOpened(path, new FileInfo(path).Name);
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("ФАйл не найден!");
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("Невозможно открыть файл!");
                }
            }
        }

        private bool DeleteFile(string typeElement, string path)
        {
            if (isStringEquals(typeElement, Properties.Resources.TYPE_FILE))
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    OnCreateOrDelete(FileActions.Delete, "", path);
                    return true;
                }
                else
                {
                    return false;
                }
                
            }
            else if (isStringEquals(typeElement, Properties.Resources.TYPE_DIRECTORY))
            {
                if (Directory.Exists(path)){
                    if (isStringEquals(path, selectedDirectory.FullName))
                    {
                        selectedDirectory = selectedDirectory.Parent;
                    }
                    Directory.Delete(path, true);
                    OnCreateOrDelete(FileActions.Delete, "", path);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private void LaunchProcess(string path, string nameProcess)
        {
            Process.Start(nameProcess);
            OnOpened(path, nameProcess);
        }



        #region логика работы Консоли
        private void txtBoxConsole_KeyDown(object sender, KeyEventArgs e)
        {
            //Пока реализуем исполнение по одной команде
            if (e.Key == Key.Enter)
            {
                List<string> executedComand = parse(txtBoxConsoleLine.Text);
                txtBlockConsole.Text += txtBoxConsoleLine.Text +"\n";
                executeComand(executedComand);
                txtBoxConsoleLine.Text = "";
            }
        }



        private List<string> parse(string command)
        {
            string[] bufArray = command.Split(' ');
            List<string> commandData = new List<string>();
            for (int i =0;i< bufArray.Length; i++)
            {
                if (bufArray[i].Length != 0)
                {
                    commandData.Add(bufArray[i]);
                }
            }
            return commandData;
        }

        private void executeComand (List<string> executedComand)
        {
            int iterator = 0;
            string part;
            for (int i = 0; i < executedComand.Count; i++)
            {
                part = executedComand[i];
                if  (executedComand.Count > 1 && executedComand.Count <=3)
                {
                    if (isStringEquals(part, "-ctf") || isStringEquals(part, "--createFile"))
                    {
                        //Проверка на обязательный атрибут: имя файла (он всегда в txt)
                        string name = executedComand[1];
                        if (executedComand.Count == 3)
                        {
                            string path = main_path + executedComand[2];
                            if (isPath(executedComand[2]))
                            {
                                CreateFile(name, path);
                                txtBlockConsole.Text += "File " + name + " on path "+path+ " - created";
                            }
                            else
                            {
                                txtBlockConsole.Text += "Error path: "+path;
                            }
                        }else if (executedComand.Count == 2)
                        {
                            CreateFile(name, selectedDirectory.FullName);
                            txtBlockConsole.Text += "File " + name + " on path " + selectedDirectory.FullName + " - created";
                            
                        }
                        txtBlockConsole.Text += splitter;
                        break;

                    }
                    else if (isStringEquals(part, "-ctd") || isStringEquals(part, "--createDirectory"))
                    {
                        string name = executedComand[1];
                        if (executedComand.Count == 3)
                        {
                            string path = main_path + executedComand[2];
                            if (isPath(executedComand[2]))
                            {
                                CreateDirectory(path, name);
                                txtBlockConsole.Text += "Directory " + name + " on path "+path +" - created";
                            }
                            else
                            {
                                txtBlockConsole.Text += "Error path: " + path;
                            }
                        }
                        else if (executedComand.Count == 2)
                        {
                            CreateDirectory(selectedDirectory.FullName, name);
                            txtBlockConsole.Text += "Directory " + name + " - created";
                        }
                        txtBlockConsole.Text += splitter;
                        break;

                    }
                    else if (isStringEquals(part, "-deD") || isStringEquals(part, "--deleteDirectory"))
                    {
                        string path = main_path + executedComand[1];
                        if (isPath(executedComand[1]))
                        {
                            if (DeleteFile(Properties.Resources.TYPE_DIRECTORY, path))
                            {

                                txtBlockConsole.Text += "Directory " + path + " - deleted";
                            }
                        }
                        else
                        {
                            txtBlockConsole.Text += "Unexpected path: "+path+". "+forHelp;
                        }
                        txtBlockConsole.Text += splitter;
                        break;
                    }
                    else if (isStringEquals(part, "-deF") || isStringEquals(part, "--deleteFile"))
                    {
                        string path = main_path + executedComand[1];
                        //if (isPath(executedComand[1]))
                        //{
                        if (DeleteFile(Properties.Resources.TYPE_FILE, path))
                        {

                            txtBlockConsole.Text += "File " + path + " - deleted";
                        }
                        else
                        {
                            txtBlockConsole.Text += "Unexpected file on path: " + path + ". " + forHelp;
                        }
                        //}
                        //else
                        //{
                        //    txtBlockConsole.Text += "Unexpected path: " + path;
                        //}
                        txtBlockConsole.Text += splitter;
                        break;
                    }
                    else if (isStringEquals(part, "-ln") || isStringEquals(part, "--launch"))
                    {
                        string nameProcess = executedComand[1];
                        LaunchProcess("", nameProcess);
                        txtBlockConsole.Text += splitter;
                        break;
                    }
                    else if (isStringEquals(part, "--logs"))
                    {
                        //SaveLogs();
                        string nameLogs = executedComand[1];
                        SaveLogs(nameLogs, listActions,FileActions.All);
                        txtBlockConsole.Text += "Logs "+nameLogs +" -created";
                        txtBlockConsole.Text += splitter;
                        break;
                    }
                    else
                    {
                        txtBlockConsole.Text += "Unexpected command: ";
                        executedComand.ForEach((string k) => {
                            txtBlockConsole.Text += k + " ";
                        });
                        txtBlockConsole.Text += ". " + forHelp;
                        txtBlockConsole.Text += splitter;
                        break;
                    }
                }
                else if (executedComand.Count == 1)
                {
                    if (isStringEquals(part, "--about"))
                    {
                        txtBlockConsole.Text += textAbout;
                        txtBlockConsole.Text += splitter;
                    }
                    else if (isStringEquals(part, "--doc"))
                    {
                        txtBlockConsole.Text += FileManagerForOS.Properties.Resources.Reference;
                        txtBlockConsole.Text += splitter;
                    }
                    else if (isStringEquals(part, "-h") || isStringEquals(part, "--help"))
                    {
                        txtBlockConsole.Text += helpToConsole;
                        txtBlockConsole.Text += splitter;
                    }
                    else if (isStringEquals(part, "--exit"))
                    {
                        this.Close();
                    }
                    else if (isStringEquals(part, "--clear"))
                    {
                        txtBlockConsole.Text = "";
                    }
                    else
                    {
                        txtBlockConsole.Text += "Unexpected command: "+ executedComand[i] + ". "+ forHelp;
                        txtBlockConsole.Text += splitter;
                    }
                }
            }
            refreshView();

        }
        private bool isPath(string path)
        {
            return Directory.Exists(main_path+path);
        }


        #endregion
    }


}
