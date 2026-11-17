namespace Jerely_api.Helper.ModelHelper
{
    public class LoginHelper
    {
        public String? Id { get; set; }
        public String? Email { get; set; }
        public String? Mdp { get; set; }
        //u:utilisateur - c:client - a:admin
        public String? Role { get; set; }
        public bool Se_souvenir { get; set; }
        public String? Fournisseur_auth { get; set; }
    }
}
