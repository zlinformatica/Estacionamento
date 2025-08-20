using EstacionamentoMvc.Models;

namespace EstacionamentoMvc.Data
{
	public partial class AppDbContext(AppDbContext.DbContextOptions options) : DbContext(options)
	{
        public required DbSet<MovimentoEstacionamento> Movimentos { get; set; }
    }

    public class DbSet<T>
    {
    }

    public class DbContext
    {
    }
}
