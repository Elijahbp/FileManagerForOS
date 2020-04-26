using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
         * 5) Сделать корректное получение и отображение метрик ПК
         * 6) Поправить двойной клик на TreeViewItem
         *
         * По заданию из оставшегося:
         * 4) Доделать систему подгрузки новых сборок
         * 5) Прикрутить систему проверки овления, и её непосредственной реализации (Консоль и т.д.). - Доделать
         */
        public MainWindow()
        {
            InitializeComponent();
            baseInit();
        }

        private const int MODE_STANDART_HIDE = 0;
        private const int MODE_SHOW_ALL = 1;

        private const int ACTION_COPY = 10;
        private const int ACTION_EXCISION = 11;

        private const int SORT_NAME = 100;
        private const int SORT_DATE = 101;
        private const int SORT_WEIGHT = 102;

        private static Brush unselectedIconStyleFile = new LinearGradientBrush(Colors.White, Colors.CadetBlue, 45);
        private static Brush unselectedIconStyleFolder = new LinearGradientBrush(Colors.White, Colors.DarkGray, 45);
        private static Brush selectedIconStyle = new LinearGradientBrush(Colors.White, Colors.LightGray, 45);



        private int selectedModeView = MODE_STANDART_HIDE;
        private Label selectedIcon;
        private string main_path;
        private DirectoryInfo selectedDirectory = null;
        private DirectoryInfo mainDirectory;
        private int selectedAction; //Выбранное действие копирование/вырезание
        string bufPath; //Используется для временного хранения пути файла (для дальнейшего удаления/копирования/перемещения) 


        private void baseInit()
        {
            lblNamePC.Content = System.Environment.MachineName;

            //Получение корня, где лежит файловый менеджер
            main_path = Environment.CurrentDirectory;
            mainDirectory = new DirectoryInfo(main_path);
            txtBoxPath.Text = main_path;
            Directory.CreateDirectory(main_path + "\\System");
            File.SetAttributes(main_path + "\\System", FileAttributes.Hidden);
            Directory.CreateDirectory(main_path + "\\Мои документы");
            getViewByPath(main_path);
            getMainTreeView();
        }

        private void KillingBase()
        {
            Directory.Delete(main_path + "\\System", true);
            Directory.Delete(main_path + "\\Мои документы", true);
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
            mainTreeView.Items.Clear();
            foreach (TreeViewItem item in generateTreeView(mainDirectory))
            {
                mainTreeView.Items.Add(item);
            }
        }


        private void getViewByPath(string path,int typeSort=0)
        {
            MainSpace.Children.Clear();
            selectedDirectory = new DirectoryInfo(path);
            txtBoxPath.Text = path;
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
            if (!info.Extension.Equals(""))
            {
                shortcut.Content = info.Name.Replace(info.Extension, "");
            }
            else
            {
                shortcut.Content = info.Name;
            }


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
            treeViewItem.MouseDoubleClick += TreeViewItem_DoubleClick_OpenDirectory;

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

            MenuItem createNewFolder = new MenuItem()
            {
                Header = "Создать новую папку",
                Tag = infoForTag
            };
            createNewFolder.Click += SelectNewFolder;
            createNewFolder.Click += MenuItem_Click_CreateNewFolder;

            menuItems.Add(deleteElement);
            menuItems.Add(copyElement);
            menuItems.Add(cutElement);
            menuItems.Add(renameElement);
            menuItems.Add(createNewFolder);
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
            if (typeElement.Equals(Properties.Resources.TYPE_FILE))
            {
                File.Delete(path);
            }
            else if (typeElement.Equals(Properties.Resources.TYPE_DIRECTORY))
            {
                Directory.Delete(path, true);
            }
            refreshView();

        }

        //Копировать
        private void MenuItem_Click_CopyFile(object sender, RoutedEventArgs e)
        {
            bufPath = ((string[])((MenuItem)sender).Tag)[1];
            selectedAction = ACTION_COPY;
            insertMenuItem.IsEnabled = true;
        }

        //Вырезать
        private void MenuItem_Click_CutFile(object sender, RoutedEventArgs e)
        {
            bufPath = ((string[])((MenuItem)sender).Tag)[1];
            selectedAction = ACTION_EXCISION;
            insertMenuItem.IsEnabled = true;
        }

        //Вставить
        private void MenuItem_Click_InsertFile(object sender, RoutedEventArgs e)
        {
            //Пока вставляем в папку, в которой сейчс и находимся
            string destFileName = selectedDirectory.FullName;
            FileInfo fileInfo = new FileInfo(bufPath);
            destFileName += "\\" + fileInfo.Name;
            if (selectedAction == ACTION_COPY)
            {
                if (File.Exists(bufPath))
                {
                    File.Copy(bufPath, destFileName, true);
                }
                /*else if (Directory.Exists(bufPath))
                {
                    //Now Create all of the directories
                    Directory.CreateDirectory(dirPath.Replace(bufPath, destFileName));
                    foreach (string dirPath in Directory.GetDirectories(bufPath, "*",
                        SearchOption.AllDirectories))
                        Directory.CreateDirectory(dirPath.Replace(bufPath, destFileName));

                    //Copy all the files & Replaces any files with the same name
                    foreach (string newPath in Directory.GetFiles(bufPath, "*.*",
                        SearchOption.AllDirectories))
                        File.Copy(newPath, newPath.Replace(bufPath, destFileName), true);
                }*/

            }
            else if (selectedAction == ACTION_EXCISION)
            {
                try
                {
                    if (File.Exists(bufPath))
                    {
                        File.Move(bufPath, destFileName);
                    }
                    else if (Directory.Exists(bufPath))
                    {
                        Directory.Move(bufPath, destFileName);
                    }
                    insertMenuItem.IsEnabled = false;
                }
                catch (Exception exception)
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

            string newName = editionWindow.txtBoxNameFile.Text;
            if (newName != "")
            {
                if (typeElement.Equals(Properties.Resources.TYPE_FILE))
                {
                    FileInfo k = new FileInfo(path);
                    File.Move(path, k.DirectoryName + "\\" + newName + k.Extension);

                }
                else if (typeElement.Equals(Properties.Resources.TYPE_DIRECTORY))
                {
                    Directory.Move(path, new FileInfo(path).DirectoryName + "\\" + newName);
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
            string name_file = creationWindow.txtBoxNameFile.Text;
            if (name_file != "")
            {
                File.Create(selectedDirectory.FullName + "\\" + name_file + ".txt");
                refreshView();
            }

        }

        //Создание файла
        private void MenuItem_Click_CreateNewFolder(object sender, RoutedEventArgs e)
        {
            EditionWindow creationWindow = new EditionWindow(Properties.Resources.TYPE_DIRECTORY, false);
            creationWindow.Owner = this;
            creationWindow.ShowDialog();
            string name_folder = creationWindow.txtBoxNameFile.Text;
            if (name_folder != "")
            {
                Directory.CreateDirectory(selectedDirectory.FullName + "\\" + name_folder);
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
            MessageBox.Show(FileManagerForOS.Properties.Resources.About + "\n" +
                "Версия программы: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
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
            if (typeElement.Equals(Properties.Resources.TYPE_DIRECTORY))
            {
                getViewByPath(path);
            }
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
            }
        }

        //Событие "открытия" папки через элемент дерева
        private void TreeViewItem_DoubleClick_OpenDirectory(object sender, RoutedEventArgs e)
        {
            string[] tagData = ((string[])((TreeViewItem)sender).Tag);
            string typeElement = tagData[0];
            string path = tagData[1];
            //Обработка файлов и папок:
            if (typeElement.Equals(Properties.Resources.TYPE_DIRECTORY))
            {
                getViewByPath(path);
            }/*
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
            if (e.Source.GetType().Name.Equals("Label") && typeSender.Equals("Label"))
            {
                if (selectedIcon == null)
                {
                    selectedIcon = (Label)sender;
                    selectedIcon.Background = selectedIconStyle;
                }
                else
                {
                    string type = ((string[])selectedIcon.Tag)[0];
                    if (type.Equals(Properties.Resources.TYPE_FILE))
                    {
                        selectedIcon.Background = unselectedIconStyleFile;
                    }
                    else if (type.Equals(Properties.Resources.TYPE_DIRECTORY))
                    {
                        selectedIcon.Background = unselectedIconStyleFolder;
                    }
                    selectedIcon = (Label)sender;
                    selectedIcon.Background = selectedIconStyle;
                }
            }
            else if (e.Source.GetType().Name.Equals("WrapPanel"))
            {
                if (selectedIcon != null)
                {
                    string type = ((string[])selectedIcon.Tag)[0];
                    if (type.Equals(Properties.Resources.TYPE_FILE))
                    {
                        selectedIcon.Background = unselectedIconStyleFile;
                    }
                    else if (type.Equals(Properties.Resources.TYPE_DIRECTORY))
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
                getViewByPath(txtBoxPath.Text);
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
        }

        private void MenuItem_Click_SortByName(object sender, RoutedEventArgs e)
        {
            getViewByPath(selectedDirectory.FullName,typeSort:SORT_NAME);
        }

        private void MenuItem_Click_SortByDate(object sender, RoutedEventArgs e)
        {
            getViewByPath(selectedDirectory.FullName, typeSort:SORT_DATE );
        }

        private void MenuItem_Click_SortByWeigth(object sender, RoutedEventArgs e)
        {
            getViewByPath(selectedDirectory.FullName, typeSort:SORT_WEIGHT);
        }
    }

    #endregion

}
