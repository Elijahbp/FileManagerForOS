using System;
using System.Collections.Generic;
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
using Microsoft.Win32;
using System.Management.Instrumentation;
using System.Management;
using System.IO;
using System.Diagnostics;

namespace FileManagerForOS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /*TODO
         * ПО необходимости из имеющегося:
         * 1) Исправить ошибку сокрытия/открытия файлов
         * 2) Прикрутить хот-кеи?
         * 3) Сделать отображение в левой части окна типа "дерево"
         *  3.1) К пунктам меню добавить контекстное меню 
         * 4) Сделать окно (или в рамках одной формы), в котором может происходить ихменеие наименования файлов
         * 5) Сделать корректное получение и отображение метрик
         * 6) Поправить анимацию нажатия на иконки. Если было событие нажатие на пустую часть поля, при том что было предварительно выбрана иконка - обнулить значение выбранной иконки
         * 7) Добавить ползунок на основном поле отображения 
         * 
         * По заданию из оставшегося:
         * 1) Добавить пункт меню "О Программе"
         * 2) Добавить пункт меню "Справка"
         * 3) Продумать задание 4 - о чём может идти речь
         * 4) Прикрутить систему контроля запущенных приложений за время работы ФМ, и их дату со временем
         * 5) Прикрутить систему проверки обновления, и её непосредственной реализации (Консоль и т.д.).
         */
        public MainWindow()
        {
            InitializeComponent();
        }

        private const int MODE_STANDART_HIDE = 0;
        private const int MODE_SHOW_ALL = 1;

        private const int ACTION_COPY = 10;
        private const int ACTION_EXCISION = 11;

        private const string TYPE_FILE = "FileInfo";
        private const string TYPE_DIRECTORY = "DirectoryInfo";

        private static Brush unselectedIconStyleFile = new LinearGradientBrush(Colors.White, Colors.CadetBlue, 45);
        private static Brush unselectedIconStyleFolder = new LinearGradientBrush(Colors.White, Colors.DarkGray, 45);
        private static Brush selectedIconStyle = new LinearGradientBrush(Colors.White, Colors.LightGray, 45);



        private int selectedModeView = MODE_STANDART_HIDE;
        private Label selectedIcon;
        private string main_path;
        private DirectoryInfo selectedFolder = null;
        private int selectedAction; //Выбранное действие копирование/вырезание
        string bufPath; //Используется для временного хранения пути файла (для дальнейшего удаления/копирования/перемещения) 


        private void Window_Initialized(object sender, EventArgs e)
        {
            lblNamePC.Content = System.Environment.MachineName;
            lblInfoPath.Content = System.Environment.WorkingSet;

            //Получение корня, где лежит файловый менеджер
            main_path = Environment.CurrentDirectory;
            baseInit();
            getViewByPath(main_path);


        }

        private void baseInit()
        {
            //TODO: Определить сокрытие системных файлов.
            Directory.CreateDirectory(main_path + "\\System");
            Directory.CreateDirectory(main_path + "\\Мои документы");
        }

        private void KillingBase (){
            Directory.Delete(main_path + "\\System",true);
            Directory.Delete(main_path + "\\MyDocuments", true);
        }

        #region логика работы с файлами/папками

        private void refreshView()
        {
            getViewByPath(selectedFolder.FullName);
        }

        private void getViewByPath(string path)
        {
            MainSpace.Children.Clear();
            selectedFolder = new DirectoryInfo(path);
            lblInfoPath.Content = path;
            FileInfo[] filesInfo = selectedFolder.GetFiles();
            DirectoryInfo[] directoryInfos = selectedFolder.GetDirectories();
            foreach (DirectoryInfo directoryInfo in directoryInfos)
            {
                this.MainSpace.Children.Add(generateShortcut(directoryInfo));
            }
            foreach (FileInfo fileInfo in filesInfo)
            {
                this.MainSpace.Children.Add(generateShortcut(fileInfo));
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

            

            menuItems.Add(deleteElement);
            menuItems.Add(copyElement);
            menuItems.Add(cutElement);
            shortcut.ContextMenu = new ContextMenu()
            {
                Name = "contextMenu",
                ItemsSource = menuItems,
            };
            #endregion
            return shortcut;
        }

        //Изменить название файла
        private void RenameFile(object sender, RoutedEventArgs e)
        {
        }
        //Сокрытие всех файлов с флагом
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
        }
        //удалить элемент
        private void MenuItem_Click_DeleteFile(object sender,RoutedEventArgs e)
        {
            string[] tagData= ((string[])((MenuItem)sender).Tag);
            string typeElement = tagData[0];
            string path = tagData[1];
            if (typeElement.Equals(TYPE_FILE))
            {
                File.Delete(path);
            }else if (typeElement.Equals(TYPE_DIRECTORY))
            {
                Directory.Delete(path);
            }
            refreshView();
            
        }
        //Копировать элемент
        private void MenuItem_Click_CopyFile(object sender, RoutedEventArgs e)
        {
            bufPath = ((string[])((MenuItem)sender).Tag)[1];
            selectedAction = ACTION_COPY;
            insertMenuItem.IsEnabled = true;
        }
        //Вырезать элементо
        private void MenuItem_Click_CutFile(object sender, RoutedEventArgs e)
        {
            bufPath = ((string[])((MenuItem)sender).Tag)[1];
            selectedAction = ACTION_EXCISION;
            insertMenuItem.IsEnabled = true;
        }
        //Вставить элемент
        private void MenuItem_Click_InsertFile(object sender, RoutedEventArgs e)
        {
            //Пока вставляем в папку, в которой сейчс и находимся
            string destFileName = selectedFolder.FullName ;
            FileInfo fileInfo = new FileInfo(bufPath);
            destFileName += "\\"+fileInfo.Name;
            if (selectedAction == ACTION_COPY)
            {
                File.Copy(bufPath,destFileName,true);
            }
            else if (selectedAction == ACTION_EXCISION)
            {
                try {
                    
                    File.Move(bufPath, destFileName);
                    insertMenuItem.IsEnabled = false;
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
                
            }
            refreshView();
        }

        private void Label_DoubleClick_OpenFile(object sender, RoutedEventArgs e)
        {
            string[] tagData = ((string[])((Label)sender).Tag);
            string typeElement = tagData[0];
            string path = tagData[1];
            //Обработка файлов и папок:
            if (typeElement.Equals(TYPE_DIRECTORY))
            {
                getViewByPath(path);
            }
            else if (typeElement.Equals(TYPE_FILE))
            {
                ProcessStartInfo processStart = new ProcessStartInfo(path);
                try {
                    Process.Start(processStart);
                }
                catch (Exception)
                {
                    MessageBox.Show("Невозможно открыть программу");
                }
            }

            /*
            else
            {

                selectedIcon = (Canvas)sender;
                selectedIcon.Background = selectedIconStyle;
            }
             */

        }

        private void Select_Icon(object sender, RoutedEventArgs e)
        {
            if (selectedIcon == null)
            {
                selectedIcon = (Label)sender;
                selectedIcon.Background = selectedIconStyle;
            }
            else
            {
                string type = ((string[])selectedIcon.Tag)[0];
                if (type.Equals(TYPE_FILE))
                {
                    selectedIcon.Background = unselectedIconStyleFile;
                }
                else if (type.Equals(TYPE_DIRECTORY))
                {
                    selectedIcon.Background = unselectedIconStyleFolder;
                }
                selectedIcon = (Label)sender;
                selectedIcon.Background = selectedIconStyle;
            }
        }

        //Дореализовать
        private void MenuItem_Click_CreateNewFile(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        //Дореализовать
        private void MenuItem_Click_CreateNewFolder(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItem_Click_RefreshFile(object sender, RoutedEventArgs e)
        {
            refreshView();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            back_view(selectedFolder.Parent.FullName);

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Back)
            {
                back_view(selectedFolder.Parent.FullName);
            }
            
        }

        private void back_view(string path)
        {
            if (selectedFolder.FullName != main_path)
            {
                getViewByPath(path);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            KillingBase();
        }
    }

    #endregion

}
