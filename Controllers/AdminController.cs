using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace EstacionamentoMvc.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _config;

        public AdminController(IConfiguration config)
        {
            _config = config;
        }

        // GET: /Admin/Login?returnUrl=/Tarifa/Edit/1
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string usuario, string senha, string returnUrl = null)
        {
            // Lê credenciais do appsettings.json (ou usa fallback)
            var adminUser = _config["Admin:User"] ?? "admin";
            var adminPass = _config["Admin:Password"] ?? "1234";

            if (usuario == adminUser && senha == adminPass)
            {
                HttpContext.Session.SetString("AdminLogado", "true");

                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Estacionamento");
            }

            ViewBag.Erro = "Usuário ou senha inválidos!";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminLogado");
            return RedirectToAction("Index", "Estacionamento");
        }
    }
}
