namespace distels.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarCredencialesAsync(
            string destinatario,
            string nombreCliente,
            string usuario,
            string contrasena,
            string asunto,
            string? perfil = null,
            string? pin = null,
            string? servicio = null,
            DateOnly? fechaVencimiento = null);

        Task<bool> EnviarTestSimpleAsync(); // Para pruebas
    }
}