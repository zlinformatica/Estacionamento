using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstacionamentoMvc.Data;
using EstacionamentoMvc.Models;

namespace EstacionamentoMvc.Controllers
{
	public class EstacionamentoController : Controller
	{
		private readonly AppDbContext _context;

		public EstacionamentoController(AppDbContext context)
		{
			_context = context;
		}

		// LISTA (abertos e/ou todos)
		public async Task<IActionResult> Index(bool mostrarTodos = false)
		{
			var query = _context.Movimentos.AsQueryable();

			if (!mostrarTodos)
				query = query.Where(m => m.DataSaida == null);

			var lista = await query
				.OrderByDescending(m => m.DataEntrada)
				.ToListAsync();

			ViewBag.MostrarTodos = mostrarTodos;
			return View(lista);
		}

		// ENTRADA (Create)
		public IActionResult Entrada()
		{
			return View(new MovimentoEstacionamento());
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
		public async Task<IActionResult> Baixa(int id)
		{
			var mov = await _context.Movimentos.FindAsync(id);
			if (mov == null) return NotFound();

			// sugestão: minutos sugeridos (diferença da entrada até agora)
			var sugestao = (int)Math.Ceiling((DateTime.Now - mov.DataEntrada).TotalMinutes);
			ViewBag.MinutosSugestao = sugestao;

			return View(mov);
		}

		// BAIXA (Checkout) - POST calcula valor e conclui
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Baixa(int id, int minutosPermanencia)
		{
			var mov = await _context.Movimentos.FindAsync(id);
			if (mov == null) return NotFound();

			mov.MinutosPermanencia = Math.Max(minutosPermanencia, 1);
			mov.DataSaida = DateTime.Now;
			mov.ValorPago = mov.CalcularValor(mov.PrecoInicial, mov.PrecoPorHora, mov.MinutosPermanencia.Value);

			_context.Update(mov);
			await _context.SaveChangesAsync();

			TempData["Msg"] = $"Veículo {mov.Veiculo} baixado. Valor a pagar: {mov.ValorPago:C}";
			return RedirectToAction(nameof(Detalhes), new { id = mov.Id });
		}

		// DETALHES
		public async Task<IActionResult> Detalhes(int id)
		{
			var mov = await _context.Movimentos.FirstOrDefaultAsync(m => m.Id == id);
			if (mov == null) return NotFound();
			return View(mov);
		}

		// EDITAR (Preço/Veículo enquanto aberto)
		public async Task<IActionResult> Editar(int id)
		{
			var mov = await _context.Movimentos.FindAsync(id);
			if (mov == null) return NotFound();
			if (mov.DataSaida != null) return BadRequest("Movimento já encerrado.");
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
