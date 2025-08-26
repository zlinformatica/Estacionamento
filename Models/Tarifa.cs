using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EstacionamentoMvc.Models
{
    public class Tarifa
    {
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal TarifaInicial { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal TarifaHora { get; set; }
    }
}