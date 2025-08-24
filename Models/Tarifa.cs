using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EstacionamentoMvc.Models
{
    public class Tarifa
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tarifa Inicial")]
        [Range(0, 9999.99)]
        public double TarifaInicial { get; set; }

        [Required]
        [Display(Name = "Tarifa Hora/Fração")]
        [Range(0, 9999.99)]
        public double TarifaHora { get; set; }
    }
}
