using System.ComponentModel.DataAnnotations;

namespace APBD_TASK6.DTO;

public class CreateAppointmentRequestDTO
{
    [Required]
    public int IdPatient { get; set; }
    [Required]
    public int IdDoctor { get; set; }
    [Required]
    public DateTime AppointmentDate { get; set; }
    
    [StringLength(250, MinimumLength = 1)]
    public string Reason { get; set; }
}