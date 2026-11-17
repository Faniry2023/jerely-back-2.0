namespace Jerely_api.Models
{
    public class CodeVerificationModel
    {
        public Guid Id { get; set; }
        public String? Email {  get; set; }
        public String? CodeHash {  get; set; } 
        public DateTime Expire {  get; set; }
        public int Tentative {  get; set; }
        public bool Use {  get; set; }
    }
}
