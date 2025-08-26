using EstacionamentoMvc.Data;
using EstacionamentoMvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
namespace EstacionamentoMvc.Controllers
{
	// public class EstacionamentoController(AppDbContext context) : Controller
	// 	{
	// 	private readonly AppDbContext _context = context;
	// 	}
	//namespace EstacionamentoMvc.Controllers
	//{
	
		public class EstacionamentoController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IWebHostEnvironment _env;

		public EstacionamentoController(AppDbContext context, IWebHostEnvironment env)
		{
			_context = context;
			_env = env;
		}

		// GET: Estacionamento
		public async Task<IActionResult> Index(bool showAll = false)
		{
			var lista = _context.Movimentos.AsQueryable();

			if (!showAll)
			{
				// Apenas veículos em aberto
				lista = lista.Where(m => m.DataSaida == null);
			}

			ViewBag.ShowAll = showAll;
			return View(await lista.ToListAsync());
		}


		// GET: Entrada
		[HttpGet]
		public IActionResult Entrada()
		{
			// Carregar lista de modelos do JSON em wwwroot/modelos.json
			var jsonPath = Path.Combine(_env.WebRootPath, "data","modelos.json");
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

			// Carregar tarifas

			var tarifa = _context.Tarifas.FirstOrDefault();
			ViewBag.TarifaInicial = tarifa?.TarifaInicial ?? 5.00m; // valor default
			ViewBag.TarifaHora = tarifa?.TarifaHora ?? 3.00m;       // valor default

			return View(new MovimentoEstacionamento());

			//}
			// ENTRADA (Create)
			// 	// GET - mostra popup login
			// 	[HttpGet]
			// public IActionResult AdminLogin(string? returnUrl = null)
			// {
			// 	ViewData["ReturnUrl"] = returnUrl;
			// 	return View();
			// }

			// [HttpPost]
			// public IActionResult AdminLogin(string usuario, string senha, string? returnUrl = null)
			// {
			// 	if (usuario == "admin" && senha == "1234")
			// 	{
			// 		HttpContext.Session.SetString("AdminLogado", "true");

			// 		if (!string.IsNullOrEmpty(returnUrl))
			// 			return Redirect(returnUrl);

			// 		return RedirectToAction("Index");
			// 	}

			// 	ViewBag.Erro = "Usuário ou senha inválidos!";
			// 	ViewBag.ReturnUrl = returnUrl;
			// 	return View();
			// }

			// 	[HttpGet]
			// public IActionResult Tarifa()
			// 	{
			// 		return View();
			// 	}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Entrada(MovimentoEstacionamento mov)
		{
			if (string.IsNullOrWhiteSpace(mov.Placa))
				ModelState.AddModelError(nameof(mov.Placa), "Informe a placa.");

			if (string.IsNullOrWhiteSpace(mov.Modelo))
				ModelState.AddModelError(nameof(mov.Modelo), "Informe o modelo.");

			// Busca a tarifa vigente (ajuste a regra conforme seu modelo de dados)
			var tarifa = await _context.Tarifas
				.OrderByDescending(t => t.Id) // ou por VigenciaInicio, etc.
				.FirstOrDefaultAsync();

			if (tarifa == null)
				ModelState.AddModelError("", "Não há tarifa cadastrada. Cadastre em Configurações/Tarifas.");

			if (!ModelState.IsValid) return View(mov);

			// Preenche server-side
			mov.DataEntrada = DateTime.Now;
			mov.PrecoInicial = (decimal)tarifa.TarifaInicial;
			mov.PrecoPorHora = (decimal)tarifa.TarifaHora;
			mov.ValorInicial = (decimal)tarifa.TarifaInicial;
			mov.ValorPago = null;
			mov.MinutosPermanencia = null;
			mov.DataSaida = null;

			_context.Movimentos.Add(mov);
			await _context.SaveChangesAsync();

			TempData["Msg"] = "Entrada registrada com sucesso.";
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
			mov.ValorPago = (decimal?)valorBase;

			// 🔹 Se admin, pode aplicar ajustes
			if (!string.IsNullOrEmpty(adminUser) && !string.IsNullOrEmpty(adminPass))
			{
				if (adminUser == "admin" && adminPass == "1234")
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
