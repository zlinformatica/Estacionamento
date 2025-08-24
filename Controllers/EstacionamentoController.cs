using Microsoft.EntityFrameworkCore;
using EstacionamentoMvc.Models;
using Microsoft.AspNetCore.Mvc;
using EstacionamentoMvc.Data;
using System.Security.Cryptography.X509Certificates;

namespace Estacionamento.Controllers
{
	public class EstacionamentoController(AppDbContext context) : Controller
	{
		private readonly AppDbContext _context = context;

    // LISTA (abertos e/ou todos)
    public async Task<IActionResult> Index(bool showAll = false)
	{
    var lista = _context.Movimentos.AsQueryable();
    if (!showAll)
    	{
        	lista = lista.Where(e => e.DataSaida == null); // só ativos
    	}
    	return View(await lista.ToListAsync());
	}
	// ENTRADA (Create)
     // GET - mostra popup login
[HttpGet]
public IActionResult AdminLogin(string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;
    return View();
}

[HttpPost]
public IActionResult AdminLogin(string usuario, string senha, string? returnUrl = null)
{
    if (usuario == "admin" && senha == "123")
    {
        HttpContext.Session.SetString("AdminLogado", "true");

        if (!string.IsNullOrEmpty(returnUrl))
            return Json(new { success = true, redirectUrl = returnUrl });

        return Json(new { success = true, redirectUrl = Url.Action("Index", "Estacionamento") });
    }

    return Json(new { success = false, message = "Usuário ou senha inválidos!" });
}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Entrada(MovimentoEstacionamento model)
		{
			if (!ModelState.IsValid)
				return View(model);

			model.DataEntrada = DateTime.Now; // define entrada agora
			_context.Add(model);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index));
		}

	// BAIXA (Checkout) - GET solicita "tempo (minutos)"
[HttpGet]
	// GET: Estacionamento/Baixa/5
		public async Task<IActionResult> Baixa(int id)
		{
			var mov = await _context.Movimentos.FindAsync(id);
			if (mov == null) return NotFound();

			// Calcula permanência para exibir na tela
			var minutosTotais = (int)Math.Ceiling((DateTime.Now - mov.DataEntrada).TotalMinutes);
			if (minutosTotais < 0) minutosTotais = 0;

			ViewBag.Permanencia = minutosTotais;

			return View(mov); // <-- Isso abre a view Baixa.cshtml
		}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Baixa(
    int id,
    string? adminUser,
    string? adminPass,
    double? descontoPercentual,
    int? descontoMinutos)
{
    var mov = await _context.Movimentos.FindAsync(id);
    if (mov == null) return NotFound();

    // 🔹 Calcula permanência
    var minutosTotais = (int)Math.Ceiling((DateTime.Now - mov.DataEntrada).TotalMinutes);
    if (minutosTotais < 0) minutosTotais = 0;

    // Aplica tolerância de 15 min
    var minutosCobrados = Math.Max(0, minutosTotais - 15);

    // 🔹 Calcula valor base
    double valorBase = (double)mov.PrecoInicial + (minutosCobrados / 60.0) * (double)mov.PrecoPorHora;

    // 🔹 Inicializa campos
    mov.DataSaida = DateTime.Now;
    mov.MinutosPermanencia = minutosTotais;
    mov.ValorInicial = (decimal)valorBase;
    mov.ValorPago = valorBase;

    // 🔹 Se admin, pode aplicar ajustes
    if (!string.IsNullOrEmpty(adminUser) && !string.IsNullOrEmpty(adminPass))
    {
        if (adminUser == "admin" && adminPass == "1234")
        {
            // Desconto percentual
            if (descontoPercentual.HasValue && descontoPercentual.Value > 0)
            {
                var desconto = (double)mov.ValorPago * (descontoPercentual.Value / 100.0);
                mov.ValorPago -= desconto;
            }

            // Desconto em minutos
            if (descontoMinutos.HasValue && descontoMinutos.Value > 0)
            {
                var valorMin = (descontoMinutos.Value / 60.0) * (double)mov.PrecoPorHora;
                mov.ValorPago -= valorMin;
            }

            if (mov.ValorPago < 0) mov.ValorPago = 0;
        }
        else
        {
            TempData["Erro"] = "⚠️ Usuário/senha inválidos. Ajustes não aplicados.";
            return RedirectToAction(nameof(Index));
        }
    }

    _context.Update(mov);
    await _context.SaveChangesAsync();

    TempData["Msg"] = $"✅ Baixa concluída. Permanência: {mov.MinutosPermanencia} min | Valor pago: {mov.ValorPago:C}";
    return RedirectToAction(nameof(Index));
}
		// DETALHES
		public async Task<IActionResult> Detalhes(int id)
		{
			var mov = await _context.Movimentos.FirstOrDefaultAsync(m => m.Id == id);
			if (mov == null) return NotFound();
			return View(mov);
		}

		// EDITAR (Pre�o/Ve�culo enquanto aberto)
		public async Task<IActionResult> Editar(int id)
		{
			var mov = await _context.Movimentos.FindAsync(id);
			if (mov == null) return NotFound();
			if (mov.DataSaida != null) return BadRequest("Movimento j� encerrado.");
			return View(mov);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Editar(MovimentoEstacionamento model)
		{
			if (!ModelState.IsValid) return View(model);
			_context.Update(model);
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// EXCLUIR
		public async Task<IActionResult> Excluir(int id)
		{
			var mov = await _context.Movimentos.FindAsync(id);
			if (mov == null) return NotFound();
			return View(mov);
		}

		[HttpPost, ActionName("Excluir")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExcluirConfirmado(int id)
		{
			var mov = await _context.Movimentos.FindAsync(id);
			if (mov != null)
			{
				_context.Movimentos.Remove(mov);
				await _context.SaveChangesAsync();
			}
			return RedirectToAction(nameof(Index));
		}
	}
}
