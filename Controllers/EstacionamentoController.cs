// ===============================
// EstacionamentoController.cs
// ===============================
using EstacionamentoMvc.Data;
using EstacionamentoMvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace EstacionamentoMvc.Controllers
{
	public class EstacionamentoController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IWebHostEnvironment _env;

		public EstacionamentoController(AppDbContext context, IWebHostEnvironment env)
		{
			_context = context;
			_env = env;
		}

		// GET: Estacionamento/Index (Lista)
		public async Task<IActionResult> Index(bool showAll = false)
		{
			var lista = _context.Movimentos.AsQueryable();

			if (!showAll)
			{
				lista = lista.Where(m => m.DataSaida == null); // Apenas ativos
			}

			ViewBag.ShowAll = showAll;
			return View(await lista.ToListAsync());
		}

		// GET: Entrada
		[HttpGet]
		public IActionResult Entrada()
		{
			CarregarModelos();
			CarregarTarifas();

			return View(new MovimentoEstacionamento());
		}

		// POST: Entrada
		[HttpPost]
		[ValidateAntiForgeryToken]
		
		public async Task<IActionResult> Entrada(MovimentoEstacionamento mov)
		{
			// Carregar lista de modelos do JSON em wwwroot/modelos.json

			var jsonPath = Path.Combine(_env.WebRootPath, "data", "modelos.json");
			if (System.IO.File.Exists(jsonPath))
			{
				var jsonData = System.IO.File.ReadAllText(jsonPath);
				var dict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonData);

				var itens = new List<SelectListItem>();
				foreach (var kv in dict)
				{
					foreach (var DropDownListFor in kv.Value)
					{
						itens.Add(new SelectListItem
						{
							Text = $"{kv.Key} - {DropDownListFor}",
							Value = DropDownListFor
						});
					}
				}

				ViewBag.Modelos = itens;
			}
			else
			{
				ViewBag.Modelos = new List<SelectListItem>();
			}

			var tarifa = await _context.Tarifas
				.OrderByDescending(t => t.Id)
				.FirstOrDefaultAsync();

        if (tarifa == null)
			{
				// fallback de segurança
				ModelState.AddModelError(string.Empty, "Não há tarifa cadastrada. Cadastre em Configurações > Tarifas.");
				CarregarModelos(); // se você tiver esse método para preencher dropdowns
				return View(mov);
			}
			// Preenche dados server-side
			mov.DataEntrada = DateTime.Now;
			mov.PrecoInicial = tarifa.TarifaInicial;
			mov.PrecoPorHora = tarifa.TarifaHora;
			mov.ValorInicial = tarifa.TarifaInicial;
			mov.ValorPago = null;
			mov.MinutosPermanencia = null;
			mov.DataSaida = null;

			Console.WriteLine(">>> CHEGOU NO POST <<</Estacionamento/Entrada"); // DEBUG

			Console.WriteLine($"Placa recebida: {mov.Placa}, Modelo: {mov.Modelo}");

			_context.Movimentos.Add(mov);
			await _context.SaveChangesAsync();

			TempData["Msg"] = "Entrada registrada com sucesso.";
			return RedirectToAction(nameof(Index));
		}

        // Helpers
		private void CarregarModelos()
		{
			var jsonPath = Path.Combine(_env.WebRootPath, "data", "modelos.json");
			if (System.IO.File.Exists(jsonPath))
			{
				var jsonData = System.IO.File.ReadAllText(jsonPath);
				var dict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonData);

				var itens = new List<SelectListItem>();
				foreach (var kv in dict)
				{
					foreach (var modelo in kv.Value)
					{
						itens.Add(new SelectListItem
						{
							Text = $"{kv.Key} - {modelo}",
							Value = modelo
						});
					}
				}
				ViewBag.Modelos = itens;
			}
			else
			{
				ViewBag.Modelos = new List<SelectListItem>();
			}
		}

		private void CarregarTarifas(Tarifa? tarifa = null)
		{
			if (tarifa == null)
				tarifa = _context.Tarifas.FirstOrDefault();

			ViewBag.TarifaInicial = tarifa?.TarifaInicial ?? 5.00m;
			ViewBag.TarifaHora = tarifa?.TarifaHora ?? 3.00m;

        }
    
		// BAIXA (Checkout) - GET solicita "tempo (minutos)"
		[HttpGet]
		// GET: Estacionamento/Baixa/5
		public async Task<IActionResult> Baixa(int id)
		{
			var mov = await _context.Movimentos.FindAsync(id);
			if (mov == null) return NotFound();

			// Calcula perman ncia para exibir na tela
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

			// 🚨 Se já tem DataSaida, não permite baixar novamente
    		if (mov.DataSaida != null)
    		{
        		TempData["Erro"] = "⚠️ Este veículo já foi baixado anteriormente.";
        		return RedirectToAction(nameof(Index));
    		}

			// ?? Calcula permananencoa
			var minutosTotais = (int)Math.Ceiling((DateTime.Now - mov.DataEntrada).TotalMinutes);
			if (minutosTotais < 0) minutosTotais = 0;

			// Aplica toler ncia de 15 min
			var minutosCobrados = Math.Max(0, minutosTotais - 15);

			// ?? Calcula valor base
			double valorBase = (double)mov.PrecoInicial + (minutosCobrados / 60.0) * (double)mov.PrecoPorHora;

			// ?? Inicializa campos
			mov.DataSaida = DateTime.Now;
			mov.MinutosPermanencia = minutosTotais;
			mov.ValorInicial = (decimal)valorBase;
			mov.ValorPago = (decimal?)valorBase;

			// ?? Se admin, pode aplicar ajustes
			if (!string.IsNullOrEmpty(adminUser) && !string.IsNullOrEmpty(adminPass))
			{
				if (adminUser == "admin" && adminPass == "123")
				{
					// Desconto percentual
					if (descontoPercentual.HasValue && descontoPercentual.Value > 0)
					{
						var desconto = (double)mov.ValorPago * (descontoPercentual.Value / 100.0);
						mov.ValorPago -= (decimal?)desconto;
					}

					// Desconto em minutos
					if (descontoMinutos.HasValue && descontoMinutos.Value > 0)
					{
						var valorMin = (descontoMinutos.Value / 60.0) * (double)mov.PrecoPorHora;
						mov.ValorPago -= (decimal?)valorMin;
					}

					if (mov.ValorPago < 0) mov.ValorPago = 0;
				}
				else
				{
					TempData["Erro"] = "?? Usu rio/senha inv lidos. Ajustes n o aplicados.";
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

		// EDITAR (Pre?o/Ve?culo enquanto aberto)
		[HttpGet]
		public async Task<IActionResult> Editar(int id)
		{
    		var mov = await _context.Movimentos.FindAsync(id);
    		if (mov == null) return NotFound();

    		// Só permite edição se DataSaida == null
    		if (mov.DataSaida != null)
    		{
        		TempData["Erro"] = "⚠️ Registro já finalizado. Não é possível editar.";
        		return RedirectToAction(nameof(Index));
    		}

    		return View(mov);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Editar(int id, string placa, string modelo)
		{
    		var mov = await _context.Movimentos.FindAsync(id);
    		if (mov == null) return NotFound();

    		// Só Admin pode salvar
    		if (HttpContext.Session.GetString("AdminLogado") != "true")
    		{
        		TempData["Erro"] = "❌ Apenas administradores podem editar este registro.";
        		return RedirectToAction(nameof(Index));
    		}

    		// Só permite editar se DataSaida == null
    		if (mov.DataSaida != null)
    		{
        		TempData["Erro"] = "⚠️ Registro já finalizado. Não é possível editar.";
        		return RedirectToAction(nameof(Index));
    		}

    		// Atualiza apenas Placa e Modelo
    		mov.Placa = placa;
    		mov.Modelo = modelo;

    		_context.Update(mov);
    		await _context.SaveChangesAsync();

    		TempData["Msg"] = "✅ Registro atualizado com sucesso.";
    		return RedirectToAction(nameof(Index));
		}


		// EXCLUIR
		// GET: Estacionamento/Excluir/5
		[HttpGet]
		public async Task<IActionResult> Excluir(int id)
		{
    		var mov = await _context.Movimentos.FindAsync(id);
    		if (mov == null) return NotFound();

    		return View(mov); // sempre exibe a tela
		}

		// POST: Estacionamento/Excluir/5
		[HttpPost, ActionName("Excluir")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExcluirConfirmado(int id)
		{
    	// 🚨 Verifica se Admin está logado
    		if (HttpContext.Session.GetString("AdminLogado") != "true")
    		{
        		TempData["Erro"] = "❌ Você não tem permissão para excluir registros.";
        		return RedirectToAction(nameof(Index));
    		}

    		var mov = await _context.Movimentos.FindAsync(id);
    		if (mov == null) return NotFound();

    		_context.Movimentos.Remove(mov);
    		await _context.SaveChangesAsync();

    		TempData["Msg"] = "✅ Registro excluído com sucesso.";
    		return RedirectToAction(nameof(Index));
		}
	}
}
