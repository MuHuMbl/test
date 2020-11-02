using System.Threading.Tasks;
using TestMessaging.DAL.Entities;

namespace TestMessaging.DAL.Repositories
{
    public interface IMessageRepository
    {
        Task<MessageEntity[]> GetHistoryAsync();
        Task SaveMessageAsync(MessageEntity message);
    }
}