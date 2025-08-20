using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstacionamentoMvc.Models
{
    public class MovimentoEstacionamento
    {
        public int Id { get; set; }

        [Required, Display(Name = "Ve�culo/Placa")]
        public string Veiculo { get; set; } = string.Empty;

        [Display(Name = "Pre�o Inicial")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecoInicial { get; set; }

        [Display(Name = "Pre�o por Hora")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecoPorHora { get; set; }

        [Display(Name = "Entrada")]
        public DateTime DataEntrada { get; set; } = DateTime.Now;

        [Display(Name = "Sa�da")]
        public DateTime? DataSaida { get; set; }

        [Display(Name = "Tempo (min) informado na Baixa")]
        public int? MinutosPermanencia { get; set; }

        [Display(Name = "Valor Pago")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ValorPago { get; set; }

        // C�lculo: Hora/Fra��o (arredonda pra cima)
        public decimal CalcularValor(decimal precoInicial, decimal precoHora, int minutos)
        {
            var horasCobradas = (decimal)Math.Ceiling(minutos / 60m);
            return precoInicial + (horasCobradas * precoHora);
        }
    }
}
