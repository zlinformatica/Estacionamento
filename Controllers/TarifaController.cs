using Microsoft.AspNetCore.Mvc;
using EstacionamentoMvc.Data;
using EstacionamentoMvc.Models;
using System.Linq;

namespace EstacionamentoMvc.Controllers
{
    public class TarifaController : Controller
    {
        private readonly AppDbContext _context;

        public TarifaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Tarifa/Edit/1
        public IActionResult Edit(int id = 1, string? returnUrl = null)
        {
            // Se não está logado, manda para o AdminLogin
            if (HttpContext.Session.GetString("AdminLogin") != "true")
            {
                return RedirectToAction("AdminLogin", "Estacionamento", new { returnUrl = Url.Action("Edit", new { id }) });
            }

            var tarifa = _context.Tarifas.FirstOrDefault(t => t.Id == id);
            if (tarifa == null)
                return NotFound();

            return View(tarifa);
        }

        // POST: Tarifa/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Tarifa tarifa)
        {
            if (id != tarifa.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                _context.Tarifas.Update(tarifa);
                _context.SaveChanges();
                return RedirectToAction("Index", "Estacionamento");
            }

            return View(tarifa);
        }
    }
}



