using DotnetAPI.Models;

namespace DotnetAPI.Data
{

    public interface IUserRepository
    {

        // below are calls to methods
        public bool SaveChanges();

        public void AddEntity<T>(T entityToAdd);

        public void RemoveEntity<T>(T entityToRemove);

        public IEnumerable<User> GetUsers();

        public User GetSingleUser(int userId);


    }

}