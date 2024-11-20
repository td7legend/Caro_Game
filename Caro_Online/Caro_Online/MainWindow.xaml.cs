using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNetCore.SignalR.Client;
using System.Runtime.InteropServices;

using static System.Runtime.CompilerServices.RuntimeHelpers;
namespace Caro_Online
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        ObservableCollection<OptionDetail> _Maps;
        public ObservableCollection<OptionDetail> Maps
        {
            get
            {
                return _Maps;
            }

            set
            {
                _Maps = value;
                OnPropertyChanged();
            }
        }
        ObservableCollection<string> _Rooms;
        public ObservableCollection<string> Rooms
        {
            get
            {
                return _Rooms;
            }

            set
            {
                _Rooms = value;
                OnPropertyChanged();
            }
        }
        string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }

            set
            {
                _Name = NameTextBox.Text;
                OnPropertyChanged();
            }
        }

        string _Status;
        public string Status
        {
            get
            {
                return _Status;
            }

            set
            {
                _Status = value;
                OnPropertyChanged();
            }
        }
        string _Desk;
        public string Desk
        {
            get
            {
                return _Desk;
            }

            set
            {
                _Desk = value;
                OnPropertyChanged();
            }
        }
        HubConnection connection;
        EStatus currentTurn;
        async void ConnectHub()
        {
            var hubUrl = "https://localhost:7086/chatHub";

            // Tạo kết nối tới Hub SignalR
            connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();

            // Định nghĩa hàm mà bạn muốn gọi khi nhận được tin nhắn từ Hub
            connection.On<int, int>("ClickNe", (x, status) =>
            {
                currentTurn = (EStatus)status;
                ClickAtPoint(Maps[x]);
            });
            connection.On<string>("UserJoined", (x) =>
            {
                Status ="Người chơi vào phòng: " + x;
            });
            connection.On<string>("RoomFull", (x) =>
            {
                Status = x;
                MessageBox.Show(x);
            });
            connection.On<string>("NotifyStatus", (x) =>
            {
                Desk = "Người chơi đánh : " + x;
            });
            connection.On<int>("ChangeTurn", (x) =>
            {
                currentTurn = (EStatus)x;
                UpdateStatus();
            });
            connection.On<List<string>>("Rooms", (x) =>
            {
                Rooms = new ObservableCollection<string>(x);
            });
            // Bắt đầu kết nối
            await connection.StartAsync();
            await connection.SendAsync("LoadRooms");
        }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            FirstLoad();
        }

        void FirstLoad()
        {

            ReloadMap();
            currentTurn = EStatus.X;
            UpdateStatus();
            ConnectHub();
        }
        void ReloadMap()
        {
            Maps = new ObservableCollection<OptionDetail>();
            for (int i = 0; i < 100; i++)
            {
                Maps.Add(new OptionDetail() { Status = EStatus.None });
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as Button).DataContext as OptionDetail;
            var index = Maps.IndexOf(data);
            Task.Run(async () => {
                await connection.SendAsync("Click", index, currentTurn);
            });
        }

        void ClickAtPoint(OptionDetail data)
        {
            if (data.Status == EStatus.None)
            {
                data.Status = currentTurn;
                CheckWin(data);
            }
        }

        void UpdateStatus()
        {
            Status = currentTurn == EStatus.X ? "Lượt của X" : "Lượt của O";
        }

        void CheckWinDoc(OptionDetail data)
        {
            var clickedIndex = Maps.IndexOf(data);
            var topIndex = clickedIndex;
            int countLine = 1;
            // đi lên
            while (true)
            {
                topIndex = topIndex - 10;
                if (topIndex > 0)
                {
                    if (Maps[topIndex].Status == data.Status)
                    {
                        countLine++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            // đi xuống
            topIndex = clickedIndex;
            while (true)
            {
                topIndex = topIndex + 10;
                if (topIndex < Maps.Count)
                {
                    if (Maps[topIndex].Status == data.Status)
                    {
                        countLine++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            if (countLine >= 5)
            {
                DisplayWinMessage(data.Status);
            }
        }
        void CheckWin(OptionDetail data)
        {
            // check đường thẳng dọc
            CheckWinDoc(data);  // Kiểm tra hàng dọc
            CheckWinNgang(data); // Kiểm tra hàng ngang
            CheckWinCheoChinh(data); // Kiểm tra chéo chính
            CheckWinCheoPhu(data); // Kiểm tra chéo phụ
        }
        void CheckWinNgang(OptionDetail data)
        {
            int clickedIndex = Maps.IndexOf(data);
            int rowStart = clickedIndex / 10 * 10;  // Bắt đầu dòng
            int countLine = 1;

            // Kiểm tra sang trái
            for (int i = clickedIndex - 1; i >= rowStart; i--)
            {
                if (Maps[i].Status == data.Status)
                    countLine++;
                else
                    break;
            }

            // Kiểm tra sang phải
            for (int i = clickedIndex + 1; i < rowStart + 10; i++)
            {
                if (Maps[i].Status == data.Status)
                    countLine++;
                else
                    break;
            }

            if (countLine >= 5)
            {
                DisplayWinMessage(data.Status);
            }
        }
        void CheckWinCheoChinh(OptionDetail data)
        {
            int clickedIndex = Maps.IndexOf(data);
            int countLine = 1;

            // Kiểm tra chéo lên
            for (int i = clickedIndex - 11; i >= 0 && (i % 10 < clickedIndex % 10); i -= 11)
            {
                if (Maps[i].Status == data.Status)
                    countLine++;
                else
                    break;
            }

            // Kiểm tra chéo xuống
            for (int i = clickedIndex + 11; i < Maps.Count && (i % 10 > clickedIndex % 10); i += 11)
            {
                if (Maps[i].Status == data.Status)
                    countLine++;
                else
                    break;
            }

            if (countLine >= 5)
            {
                DisplayWinMessage(data.Status);
            }
        }
        void CheckWinCheoPhu(OptionDetail data)
        {
            int clickedIndex = Maps.IndexOf(data);
            int countLine = 1;

            // Kiểm tra chéo lên
            for (int i = clickedIndex - 9; i >= 0 && (i % 10 > clickedIndex % 10); i -= 9)
            {
                if (Maps[i].Status == data.Status)
                    countLine++;
                else
                    break;
            }

            // Kiểm tra chéo xuống
            for (int i = clickedIndex + 9; i < Maps.Count && (i % 10 < clickedIndex % 10); i += 9)
            {
                if (Maps[i].Status == data.Status)
                    countLine++;
                else
                    break;
            }

            if (countLine >= 5)
            {
                DisplayWinMessage(data.Status);
            }
        }
        void DisplayWinMessage(EStatus winner)
        {
            var winnerText = winner == EStatus.X ? "X" : "O";
            Status = $"{winnerText} thắng!";
            MessageBox.Show($"{winnerText} đã chiến thắng!", "Kết quả");
            ReloadMap();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var data = (sender as Button).Content;
            var name = NameTextBox.Text;

            Task.Run(async() => {
                await connection.SendAsync("JoinChat", data, name);
            });
        }
    }

    public class OptionDetail : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        EStatus _Status;
        public EStatus Status
        {
            get
            {
                return _Status;
            }

            set
            {
                _Status = value;
                OnPropertyChanged();

                Content = _Status == EStatus.None ? "" : _Status == EStatus.X ? "X" : "0";
            }
        }

        string _Content;
        public string Content
        {
            get
            {
                return _Content;
            }

            set
            {
                _Content = value;
                OnPropertyChanged();
                Img = Content == "X" ? AppDomain.CurrentDomain.BaseDirectory + "Imgs/x.png" 
                    : Content == "0" ?AppDomain.CurrentDomain.BaseDirectory + "Imgs/o.png":"";
            }
        }

        string _Img;
        public string Img
        {
            get
            {
                return _Img;
            }

            set
            {
                _Img = value;
                OnPropertyChanged();
            }
        }
    }
    public enum EStatus
    {
        None,
        X,
        O
    }
}
