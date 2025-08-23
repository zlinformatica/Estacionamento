using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EstacionamentoMvc.Models
{
    public class MovimentoEstacionamento
    {
        public int Id { get; set; }

        [Required, Display(Name = "Ve�culo/Placa")]
        public string Veiculo { get; set; } = string.Empty;

        [Display(Name = "Entrada")]
        public DateTime DataEntrada { get; set; } = DateTime.Now;

        [Display(Name = "Sa�da")]
        public DateTime? DataSaida { get; set; }
        public double? Permanencia { get; set; }

        [Display(Name = "Preço Inicial")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecoInicial { get; set; }

        [Display(Name = "Pre�o por Hora")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecoPorHora { get; set; }

        [Display(Name = "Permanencia")]
        public int? MinutosPermanencia { get; set; }

        [Display(Name = "Valor Inicial")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ValorInicial { get; set; }
        public double ValorPago { get; set; }

        // C�lculo: Hora/Fra��o (arredonda pra cima)
        public decimal CalcularValor(decimal precoInicial, decimal precoHora, int minutos)
        {
            var ValorPago = (decimal)Math.Ceiling(minutos / 60m);
            return precoInicial + (ValorPago * precoHora);
        }
    }
}
