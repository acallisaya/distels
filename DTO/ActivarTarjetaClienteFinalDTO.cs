namespace distels.DTO
{
    public class ActivarTarjetaClienteFinalDTO
    {
        public string CodigoTarjeta { get; set; } = null!;
        public string NombreCliente { get; set; } = null!;
        public string Celular { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string MetodoEnvio { get; set; } = "EMAIL";
        public string? Dispositivo { get; set; }
        public string? Navegador { get; set; }
    }
}
