using Microsoft.EntityFrameworkCore;
using EstacionamentoMvc.Models; // ajuste o namespace se precisar

namespace EstacionamentoMvc.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {

        // DbSet representando a tabela
        public DbSet<MovimentoEstacionamento> Movimentos { get; set; }
    }
}
