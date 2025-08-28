using Microsoft.EntityFrameworkCore;
using EstacionamentoMvc.Models;

namespace EstacionamentoMvc.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MovimentoEstacionamento> Movimentos { get; set; }
        public DbSet<Tarifa> Tarifas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed inicial para garantir ao menos uma tarifa no banco
            modelBuilder.Entity<Tarifa>().HasData(
                new Tarifa
                {
                    Id = 1,
                    TarifaInicial = 5.00m,
                    TarifaHora = 3.00m
                }
            );
        }
    }
}

