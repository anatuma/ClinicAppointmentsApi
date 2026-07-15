using APBD_TASK6.DTO;
using APBD_TASK6.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_TASK6.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IValidator _validator;

        public AppointmentsController(IConfiguration configuration, IValidator validator)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Missing 'DefaultConnection' in appsettings.json.");
            _validator = validator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments(
            [FromQuery] string? status,
            [FromQuery] string? patientLastName)
        {
            const string sql = """
                               SELECT
                                   a.IdAppointment,
                                   a.AppointmentDate,
                                   a.Status,
                                   a.Reason,
                                   p.FirstName + N' ' + p.LastName AS PatientFullName,
                                   p.Email AS PatientEmail
                               FROM dbo.Appointments a
                               JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                               WHERE (@Status IS NULL OR a.Status = @Status)
                                 AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
                               ORDER BY a.AppointmentDate;
                               """;
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);
            
            command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
            command.Parameters.AddWithValue("@PatientLastName", (object?)patientLastName ?? DBNull.Value);
            
            await connection.OpenAsync();
            var results = new List<AppointmentListDto>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new AppointmentListDto
                {
                    IdAppointment = reader.GetInt32(0),
                    AppointmentDate = reader.GetDateTime(1),
                    Status = reader.GetString(2),
                    Reason = reader.GetString(3),
                    PatientFullName = reader.GetString(4),
                    PatientEmail = reader.GetString(5)
                });
            }
            return Ok(results);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment(
            [FromBody] CreateAppointmentRequestDTO request)
        {
            var val = await _validator.ValidateCreateAsync(request);
            if (!val.IsSuccess) return StatusCode(val.StatusCode, new ErrorResponseDTO(val.ErrorMessage));
            
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int newId;
            
            await using (var transaction = (SqlTransaction)await connection.BeginTransactionAsync())
            {
                const string insertSql = """
                                         INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Reason, Status)
                                         OUTPUT INSERTED.IdAppointment
                                         VALUES(@IdPatient, @IdDoctor, @AppointmentDate, @Reason, 'Scheduled');
                                         """;
                await using var command = new SqlCommand(insertSql, connection);
                command.Transaction = transaction;
                command.Parameters.AddWithValue("@IdPatient", request.IdPatient);
                command.Parameters.AddWithValue("@IdDoctor", request.IdDoctor);
                command.Parameters.AddWithValue("@AppointmentDate", request.AppointmentDate);
                command.Parameters.AddWithValue("@Reason", request.Reason);
                
                newId = (int)(await command.ExecuteScalarAsync());
                
                await transaction.CommitAsync();
                
            }
            return CreatedAtAction(nameof(GetAppointmentById), new {id = newId}, null);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAppointmentById(
            int id)
        {
            const string sql = """
                               SELECT 
                                   a.IdAppointment,
                                   a.AppointmentDate,
                                   a.Status,
                                   a.Reason,
                                   a.InternalNotes,
                                   a.CreatedAt,
                                   p.FirstName + N' ' + p.LastName AS PatientFullName,
                                   p.Email AS PatientEmail,
                                   p.PhoneNumber AS PatientPhoneNumber,
                                   d.FirstName + N' ' + d.LastName AS DoctorFullName,
                                   d.LicenseNumber AS DoctorLicenseNumber,
                                   s.Name AS SpecializationName
                               FROM dbo.Appointments a
                               JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                               JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
                               JOIN dbo.Specializations s ON d.IdSpecialization = s.IdSpecialization
                               WHERE a.IdAppointment = @IdAppointment
                               """;
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);
            
            command.Parameters.AddWithValue("@IdAppointment", id);

            await connection.OpenAsync();
            AppointmentDetailsDto result = null;
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                result = new AppointmentDetailsDto
                {
                    IdAppointment = reader.GetInt32(0),
                    AppointmentDate = reader.GetDateTime(1),
                    Status = reader.GetString(2),
                    Reason = reader.GetString(3),
                    InternalNotes = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5),
                    PatientFullName = reader.GetString(6),
                    PatientEmail = reader.GetString(7),
                    PatientPhoneNumber = reader.GetString(8),
                    DoctorFullName = reader.GetString(9),
                    DoctorLicenseNumber = reader.GetString(10),
                    SpecializationName = reader.GetString(11)
                };
                return Ok(result);
            }
            else return NotFound(new ErrorResponseDTO("Appointment not found"));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAppointment(int id,
            [FromBody] UpdateAppointmentRequestDto request)
        {
            var validation = await _validator.ValidateUpdateAsync(id, request);
            if (!validation.IsSuccess)
            {
                return StatusCode(validation.StatusCode, new ErrorResponseDTO(validation.ErrorMessage));
            }

            const string sql = """
                               UPDATE dbo.Appointments
                               SET IdPatient = @IdPatient,
                                   IdDoctor = @IdDoctor,
                                   AppointmentDate = @AppointmentDate,
                                   Status = @Status,
                                   Reason = @Reason,
                                   InternalNotes = @InternalNotes
                               WHERE IdAppointment = @IdAppointment
                               """;
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);
            
            command.Parameters.AddWithValue("@IdPatient", request.IdPatient);
            command.Parameters.AddWithValue("@IdDoctor", request.IdDoctor);
            command.Parameters.AddWithValue("@AppointmentDate", request.AppointmentDate);
            command.Parameters.AddWithValue("@Status", request.Status);
            command.Parameters.AddWithValue("@Reason", request.Reason);
            command.Parameters.AddWithValue("@IdAppointment", id);
            
            command.Parameters.AddWithValue("@InternalNotes", (object?) request.InternalNotes ?? DBNull.Value);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var validation = await _validator.ValidateDeleteAsync(id);
            if (!validation.IsSuccess)
                return StatusCode(validation.StatusCode, new ErrorResponseDTO(validation.ErrorMessage));
            const string sql = """
                               DELETE FROM dbo.Appointments
                               WHERE IdAppointment = @Id
                               """;
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            return NoContent();
        }
    }
}
