
namespace Models
{
    public class Message
    {
        public string Email { get; set; } = default!;
        public Departments Department { get; set; } = default!;
        public int LeaveDurationInHours { get; set; } = default!;
        public DateTime LeaveStartDate { get; set; } = default!;
    }
}
