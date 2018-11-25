using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Notifier.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SubscribeToJobStatusUpdates(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
        }
    }
}
