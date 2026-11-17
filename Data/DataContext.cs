using Jerely_api.Models;
using Microsoft.EntityFrameworkCore;

namespace Jerely_api.Data
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<LoginModel> Login { get; set; }
        public DbSet<PlateformeModel> Plateforme { get; set; }
        public DbSet<UtilisateurModel> Utilisateur { get; set; }
        public DbSet<CodeVerificationModel> CodeVerification { get; set; }
    }
}
