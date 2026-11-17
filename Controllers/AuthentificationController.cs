using Google.Apis.Auth;
using Jerely_api.Data;
using Jerely_api.Helper;
using Jerely_api.Helper.ModelHelper;
using Jerely_api.Models;
using Jerely_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Jerely_api.Controllers
{
    public class AuthentificationController : Controller
    {
        private readonly DataContext dataContext;
        private JwtHelper jwtHelper;
        private EmailService emailService;
        private readonly IConfiguration _config;

        public AuthentificationController(DataContext dataContext, JwtHelper jwtHelper, EmailService emailService, IConfiguration config)
        {
            this.dataContext = dataContext;
            this.jwtHelper = jwtHelper;
            this.emailService = emailService;
            _config = config;
        }

        [HttpPost("api/login")]
        public async Task<IActionResult> Login([FromBody] LoginHelper model)
        {
            if (model == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Requete invalide",
                    detail: "La requête est invalide ou manquant"
                );
            }
            if (dataContext?.Login is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur serveur",
                        detail: "Le context de données est introuvable"
                );
            }

            var login = await dataContext.Login.FirstOrDefaultAsync(l => l.Email!.Equals(l.Email));
            if (login is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Email introuvable",
                        detail: "Email incorrect"
                    );
            }

            bool mdpOk = EncryptionPasswordHelper.VerifyPassword(model.Mdp!, login.Mdp!);
            if (!mdpOk)
            {
                return Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Echec de l'authentification",
                        detail: "Mot de passe incorrecte"
                    );
            }

            var token = jwtHelper.GenerateToken(login.Id, login.Email);
            int day = (model.Se_souvenir) ? 7 : 1;
            Response.Cookies.Append("jwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(day)
            });
            login.Mdp = string.Empty;

            char role = 'n';
            switch (role)
            {
                case 'u':
                    return Ok("Utilisateur");
                case 'c':
                    return Ok("Client");
                case 'a':
                    return Ok("Admin");
                default:
                    return Ok("aucun role trouvé");
            }
        }

        
        [HttpPost("api/inscri")]
        public async Task<IActionResult> Inscription([FromBody] LoginHelper model)
        {
            
            if (model == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Requete invalide",
                    detail: "La requête est invalide ou manquant"
                );
            }

            if (dataContext?.Login is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur serveur",
                        detail: "Le context de données est introuvable"
                );
            }

            var compteExiste = await dataContext.Login.AnyAsync(l => l.Email!.Equals(model.Email));
            if (compteExiste)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Email existe",
                        detail: "Vous ne pouvez pas creer un compte avec cet email"
                    );
            }
            LoginModel login = new()
            {
                Email = model.Email,
                Mdp = EncryptionPasswordHelper.HashPassword(model.Mdp!),
                Role = 'c'
            };

            try
            {
                var code_verification = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                var smtpEmailService = new SmtpEmailService();
                await smtpEmailService.SendEmailAsync(model.Email, "Creation compte jerely", code_verification);
                return Ok(true);
            }
            catch (Exception ex)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur envoi email",
                        detail: $"{ex.Message}"
                );
            }
        }

        [HttpPost("api/auth/google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleTokenHelper request)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
                        new GoogleJsonWebSignature.ValidationSettings
                        {
                            Audience = new[] { _config["Google:ClientId"] }
                        });

                GoogleAuthInfoHelper googleAuthInfoHelper = new();
                googleAuthInfoHelper.Email = payload.Email;
                googleAuthInfoHelper.Name = payload.Name;

                if (dataContext?.Login is null)
                {
                    return Problem(
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: "Erreur serveur",
                            detail: "Le context de données est introuvable"
                    );
                }

                var login_in_bd = await dataContext.Login.FirstOrDefaultAsync(l => l.Email!.Equals(googleAuthInfoHelper.Email));
                if (login_in_bd is null)
                {
                    LoginModel login = new();
                    login.Email = payload.Email;
                    login.Mdp = null;
                    login.Role = 'c';
                    login.Fournisseur_auth = "google";


                    await dataContext.Login.AddAsync(login);
                    await dataContext.SaveChangesAsync();

                    login_in_bd = login;

                    try
                    {
                        var smtpEmailService = new SmtpEmailService();
                        await smtpEmailService.CreationNewAccount(login_in_bd.Email, "Creation compte jerely",googleAuthInfoHelper.Name);
                        return Ok(true);
                    }
                    catch (Exception ex)
                    {
                        return Problem(
                                statusCode: StatusCodes.Status500InternalServerError,
                                title: "Erreur envoi email",
                                detail: $"{ex.Message}"
                        );
                    }
                };

                    
                var token = jwtHelper.GenerateToken(login_in_bd.Id, login_in_bd.Email);
                //int day = (model.Se_souvenir) ? 7 : 1;
                Response.Cookies.Append("jwtToken", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                return Ok(login_in_bd);
            }
            catch (InvalidJwtException)
            {
                return Problem(statusCode: 401, title: "Token Google invalide");
            }
        }

        [HttpPost("api/auth/google/email")]
        public async Task<IActionResult> GoogleLoginEmail([FromBody] LoginHelper model)
        {
            if (model == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Requete invalide",
                    detail: "La requête est invalide ou manquant"
                );
            }
            if (dataContext?.Login is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur serveur",
                        detail: "Le context de données est introuvable"
                );
            }
            var login_in_bd = await dataContext.Login.FirstOrDefaultAsync(e => e.Email!.Equals(model.Email));
            if(login_in_bd is null)
            {
                LoginModel login = new()
                {
                    Email = model.Email,
                    Mdp = null,
                    Role = 'c',
                    Fournisseur_auth = "email"
                };

                await dataContext.Login.AddAsync(login);

                await dataContext.SaveChangesAsync();

                login_in_bd = login;
            }
            try
            {
                
                var code_verification = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

                var old_code = dataContext.CodeVerification.Where(c => c.Email.Equals(login_in_bd.Email));
                dataContext.RemoveRange(old_code);

                CodeVerificationModel save_code = new()
                {
                    Email = login_in_bd.Email,
                    CodeHash = EncryptionPasswordHelper.HashPassword(code_verification.ToString()),
                    Expire = DateTime.UtcNow.AddMinutes(5),
                };

                await dataContext.CodeVerification.AddAsync(save_code);
                await dataContext.SaveChangesAsync();

                var smtpEmailService = new SmtpEmailService();
                await smtpEmailService.SendVerificationCodeAsync(login_in_bd.Email, "Vérification de votre compte", code_verification);

                return Ok(true);
            }
            catch (Exception ex)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur envoi email",
                        detail: $"{ex.Message}"
                );
            }

        }

        [HttpPost("api/auth/code")]
        public async Task<IActionResult> VerificationCode([FromBody]CodeVerificationHelper model)
        {
            if (model == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Requete invalide",
                    detail: "La requête est invalide ou manquant"
                );
            }
            if (dataContext?.Login is null || dataContext.CodeVerification is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur serveur",
                        detail: "Le context de données est introuvable"
                );
            }

            var verif = await dataContext.CodeVerification.FirstOrDefaultAsync(c => 
            c.Email!.Equals(model.Email));
            if(verif is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Aucune demande de code",
                        detail: "Aucun code de vérification n'est en attente pour cet email. Veuillez demander un nouveau code"
                );
            }
            if (verif.Use)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Code déjà utilisé",
                        detail: "Ce code a déjà été utilisé..."
                );
            }

            if (verif.Expire < DateTime.UtcNow)
            {
                var code_verification = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                var old_code = dataContext.CodeVerification.Where(c => c.Email.Equals(model.Email));
                dataContext.RemoveRange(old_code);
                CodeVerificationModel save_code = new()
                {
                    Email = model.Email,
                    CodeHash = EncryptionPasswordHelper.HashPassword(code_verification.ToString()),
                    Expire = DateTime.UtcNow.AddMinutes(10),
                };

                await dataContext.CodeVerification.AddAsync(save_code);
                await dataContext.SaveChangesAsync();

                var smtpEmailService = new SmtpEmailService();
                await smtpEmailService.SendVerificationCodeAsync(model.Email!, "Vérification de votre compte", code_verification);
                return Problem(
                        statusCode: StatusCodes.Status410Gone,
                        title: "Code expiré",
                        detail: "Votre code a expiré. " +
                        "Un nouveau code vous a été envoyé par email. Veuillez consulter votre boîte de réception."
                );
            }
            bool code_ok = EncryptionPasswordHelper.VerifyPassword(model.Code!, verif.CodeHash!);
            if (!code_ok)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Code incorrect",
                        detail: "Le code saisi est incorrect."
                );
            }
            verif.Use = true;
            await dataContext.SaveChangesAsync();

            var login = await dataContext.Login.FirstOrDefaultAsync(l => l.Email!.Equals(verif.Email));
            if (login is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Email introuvable",
                        detail: "Email non trouvée"
                    );
            }
            var token = jwtHelper.GenerateToken(login.Id, login.Email);
            //int day = (model.Se_souvenir) ? 7 : 1;
            Response.Cookies.Append("jwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(login);
        }

        [HttpGet("api/me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid id))
            {
                return Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Utilisateur introuvable",
                        detail: "Vous n'avez pas l'acces à cet compte"
                    );
            }
            if (dataContext == null || dataContext?.Login == null)
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );

            var login = await dataContext.Login.FindAsync(id);
            if (login == null)
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Utilisateur introuvable",
                        detail: "Aucun profil utilisateur associé à ce compte"
                    );

            return Ok(login);
        }


        [HttpPost("api/logout")]
        public IActionResult Logout()
        {
            //suppression
            //Response.Cookies.Delete("jwtToken");


            Response.Cookies.Delete("jwtToken", new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
            return Ok(new { message = "Déconnexion réussie" });
        }
        [HttpGet("api/testcode")]
        public IActionResult CodeVer()
        {
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            return Ok(code);
        }
    }
}
