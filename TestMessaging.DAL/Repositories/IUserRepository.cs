using System.Threading.Tasks;
using TestMessaging.DAL.Entities;

namespace TestMessaging.DAL.Repositories
{
    public interface IUserRepository
    {
        Task<UserEntity> GetUserByNameAsync(string userName);

        Task CreateUser(UserEntity user);
    }
}