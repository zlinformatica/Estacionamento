using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstacionamentoMvc.Models
{
    public class MovimentoEstacionamento
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe a placa.")]
        [StringLength(9, ErrorMessage = "A placa deve ter no máximo 9 caracteres.")]
        public string? Placa { get; set; }   // removi o ? para forçar preenchimento

        [Required(ErrorMessage = "Informe o modelo.")]
        public string? Modelo { get; set; }  // removi o ? para forçar preenchimento

        public DateTime DataEntrada { get; set; }
        public DateTime? DataSaida { get; set; }
        public int? MinutosPermanencia { get; set; }
        public decimal PrecoInicial { get; set; }
        public decimal PrecoPorHora { get; set; }
        public decimal? ValorInicial { get; set; }
        public decimal? ValorPago { get; set; }
    }
}

