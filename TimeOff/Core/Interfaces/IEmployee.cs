using Models;

namespace Core.Interfaces
{
    public interface IEmployee
    {
        Task OnCreateTopic();
        Task<String> OnMessage(Message item);
    }
}
