namespace SignalRTest.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Text { get; set; }
    }
}
