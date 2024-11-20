using Microsoft.AspNetCore.SignalR;
using System.Linq;
namespace Server
{
    public class ChatHub : Hub
    {
        private static int maxParticipants = 2; // Giới hạn 2 người
        private static List<ClientDetail> participants = new List<ClientDetail>();
        private static EStatus currentStatus = EStatus.X;
        private static List<string> rooms = new List<string>() { "Phòng 1", "Phòng 2", "Phòng 3"};
        public async Task JoinChat(string roomName, string name)
        {
            if (!rooms.Contains(roomName))
                return;

            var checkAny = participants.Count(p=>p.Id == Context.ConnectionId);
            if(checkAny > 0)
            {
                return;
            }

            var items = participants.Count(p => p.RoomName == roomName);
            if (items < maxParticipants)
            {
                    participants.Add(
                        new ClientDetail() 
                        { 
                            Id = Context.ConnectionId, RoomName = roomName, Desk = items == 1 ? EStatus.X:EStatus.O ,
                            Name = name
                        });
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
                    await Clients.Group(roomName).SendAsync("UserJoined", name);
                    await Clients.Client(Context.ConnectionId).SendAsync("NotifyStatus", items == 1 ? "X" : "O");
                               
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("RoomFull", "Phòng chat đã đầy.");
            }
        }

        public async Task LoadRooms()
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Rooms", rooms);
        }

        public async Task Click(int x, int status)
        {
            var item = participants.Where(p => p.Id == Context.ConnectionId).FirstOrDefault();
            var roomCount = participants.Count(p => p.RoomName == item.RoomName);

            if (roomCount < maxParticipants)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("RoomFull", "Cần đủ 2 người chơi");
                return;
            }
            
            var index = participants.IndexOf(item);
            if(currentStatus == item.Desk)
            {
                await Clients.Group(item.RoomName).SendAsync("ClickNe", x, status);
                currentStatus = currentStatus == EStatus.X ? EStatus.O : EStatus.X;
                await Clients.Group(item.RoomName).SendAsync("ChangeTurn", currentStatus);
            }
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            var item = participants.Where(p => p.Id == Context.ConnectionId).FirstOrDefault();
            participants.Remove(item);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, item.RoomName);
            await Clients.OthersInGroup(item.RoomName).SendAsync("UserLeft", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }

    public enum EStatus
    {
        None,
        X,
        O
    }

    public class ClientDetail
    {
        public string Id { get; set; }
        public string RoomName { get; set; }
        public EStatus Desk { get; set; }
        public string Name { get; set; }
    }
}
