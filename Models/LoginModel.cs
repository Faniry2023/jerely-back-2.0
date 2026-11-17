namespace Jerely_api.Models
{
    public class LoginModel
    {
        public Guid Id { get; set; }
        public String? Email {  get; set; }
        public String? Mdp {  get; set; }
        //u:utilisateur - c:client - a:admin - 
        public char? Role {  get; set; }
        public String? Fournisseur_auth {  get; set; }
    }
}
