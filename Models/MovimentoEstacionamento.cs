using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstacionamentoMvc.Models
{
    public class MovimentoEstacionamento
    {
        public int Id { get; set; }
        public string? Placa { get; set; }   // antes era "Veiculo"
        public string? Modelo { get; set; }  // novo campo
        public DateTime DataEntrada { get; set; }
        public DateTime? DataSaida { get; set; }
        public int? MinutosPermanencia { get; set; }
        public decimal PrecoInicial { get; set; }
        public decimal PrecoPorHora { get; set; }
        public decimal? ValorInicial { get; set; }
        public decimal? ValorPago { get; set; }
    }
}
